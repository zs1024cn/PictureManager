using CefSharp;
using CefSharp.WinForms;
using PictureManager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

public class ForumDownloader
{
    static readonly HttpClient client = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });
    public string BaseUrl { get; set; } = "https://byoo.3a004a3395044cb3.com/thread.php?fid={版块}&page={页}";
    public string ThreadUrl { get; set; } = "https://byoo.3a004a3395044cb3.com/read.php?tid={tid}";
    public string ThreadRegex { get; set; } = "/read.php?tid=(\\d+)";
    public string SaveRoot { get; set; } = @"F:\byoo";
    public string GroupID { get; set; } = @"345";
    public int StartPage { get; set; } = 1;
    public int EndPage { get; set; } = 1000;
    private ConcurrentQueue<(string url, string savePath, int index, int tid)> _imageQueue = new ConcurrentQueue<(string, string, int, int)>();

    public Func<string, Task<string>> GetHtmlAsync { get; set; }
    public Func<string, Task<string>> GetPostHtmlAsync { get; set; }

    public MyDb Db { get; set; }
    public bool EnableGarbageCheck { get; set; } = true;

    private List<(string url, string savePath, int index, int tid)> _failedDownloads = new List<(string, string, int, int)>();
    private string savePath = @"F:\byoo";
    private IProgress<string> _progress;
    private List<string> _failedImages = new List<string>();
    private object _failedLock = new object();
    private int _DownloadedCount = 0;

    private static readonly SemaphoreSlim _normalImageSemaphore = new SemaphoreSlim(6);
    private SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(4);
    private SemaphoreSlim _downloadSemaphoreFast = new SemaphoreSlim(40);

    private ConcurrentDictionary<string, int> _domainFailCount = new ConcurrentDictionary<string, int>();
    private HashSet<string> _blockedDomains = new HashSet<string>();
    private bool _blockedDomainActive = false; //是否启用域名黑名单
    private object _blockedLock = new object();
    private int MaxFailPerDomain = 30;
    
    private bool _slowMode = false;

    private void Log(string msg)
    {
        _progress?.Report(msg);
        Console.WriteLine(msg);
    }

    public async Task Run(IProgress<string> progress = null)
    {
        ServicePointManager.DefaultConnectionLimit = 50;
        ServicePointManager.Expect100Continue = false;

        _progress = progress;
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        //client.Timeout = TimeSpan.FromSeconds(5); //5
        savePath = Path.Combine(SaveRoot, GroupID);

        Log("=== 论坛图片下载器 ===");
        Log($"保存路径: {savePath}");
        Log("");

        for (int page = StartPage; page <= EndPage; page++)
        {
            await ProcessPage(page);
        }
        // Run 末尾，Db?.FlushBatch() 之前
        while (!_imageQueue.IsEmpty)
        {
            await Task.Delay(2000);  // 等 Timer 消费完
        }
        await FlushImageQueue();  // 收尾

        Db?.FlushBatch();

        if (_failedDownloads.Count > 0)
        {
            Log($"\n=== 补救 {_failedDownloads.Count} 张失败图片 ===");

            var retryTasks = new List<Task>();
            foreach (var (url, savePath, index, tid) in _failedDownloads)
            {
                retryTasks.Add(DownloadImageAsync(url, savePath, index, tid,true));
            }
            await Task.WhenAll(retryTasks);

            // 还是失败的，打印出来
            var stillFailed = _failedDownloads.Where(f => !File.Exists(f.savePath)).ToList();
            if (stillFailed.Count > 0)
            {
                Log($"\n=== {stillFailed.Count} 张最终失败 ===");
                foreach (var (url, _, index, tid) in stillFailed)
                    Log($"  帖子{tid} 第{index}张: {url}");
            }
        }
        Log("全部完成！");
    }
    
    public void EnqueueImage(string url, string savePath, int index, int tid)
    {
        _imageQueue.Enqueue((url, savePath, index, tid));
    }
    // 加方法：批量消费队列
    public async Task FlushImageQueue()
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(20);  // 限制并发
        int maxPerBatch = _slowMode?20:500;
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        while (_imageQueue.TryDequeue(out var item))
        {
            await semaphore.WaitAsync();
            var task = DownloadImageAsync(item.url, item.savePath, item.index, item.tid, false, cts.Token)
                .ContinueWith(_ => semaphore.Release());
            tasks.Add(task);

            // 清理已完成的任务，防止 List 无限增长
            tasks.RemoveAll(t => t.IsCompleted);
        }
        await Task.WhenAll(tasks);

        var timeout = Task.Delay(120000);
        var completed = await Task.WhenAny(Task.WhenAll(tasks), timeout);
        if (completed == timeout)
        {
            cts.Cancel();
            Log("下载队列超时，跳过剩余任务");
        }
    }
    private async Task ProcessPage(int page)
    {
        DateTime d0 = DateTime.Now;
        Log($"--- 第 {page}/{EndPage} 页开始 ---");
        string url = BaseUrl.Replace("{版块}", GroupID).Replace("{页}", page.ToString());
        string html = "";

        int MaxRetry = 30;
        if (DateTime.Now.Hour > 5)
            MaxRetry = 10;

        for (int retry = 0; retry < MaxRetry; retry++)
        {
            try
            {
                html = await GetHtmlAsync(url);
                if (!string.IsNullOrEmpty(html) && html.IndexOf("thread0806.php") >= 0)
                    break;
                Log($"  第 {page} 页: 加载异常，重试{retry}...");
                if (retry > 0)
                    await Task.Delay(10000);
            }
            catch
            {
                Log($"  第 {page} 页: 加载异常，重试{retry}...");
            }
        }

        if (string.IsNullOrEmpty(html) || html.IndexOf("thread0806.php") < 0)
        {
            Log($"  第 {page} 页: 无法加载，跳过");
            return;
        }

        var matches = Regex.Matches(html, @"href=""(/htm_data/\d+/\d+/(\d+)\.html)""");
        var postList = new List<Tuple<int, string>>();
        var seenTids = new HashSet<int>();
        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[2].Value, out int tid) && seenTids.Add(tid))
                postList.Add(Tuple.Create(tid, m.Groups[1].Value));
        }

        Log($"  第 {page} 页: 找到 {postList.Count} 个帖子");

        if (postList.Count == 0)
        {
            Log($"  第 {page} 页没有帖子，可能已到末尾");
            return;
        }

        var semaphore = new SemaphoreSlim(1);

        var tasks = postList.Select(async post =>
        {
            int tid = post.Item1;
            string path = post.Item2;
            string saveDir = Path.Combine(savePath, $"第{page}页");
            
/*            if (Directory.Exists(saveDir)) 
            {
                var dirs = Directory.GetDirectories(saveDir, $"{tid}*");
                if (dirs.Length > 0) return;
            }
*/
            await semaphore.WaitAsync();
            try
            {
                await DownloadPostImages(tid, page, path);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        Log($"第 {page} 页完成,用时{(int)DateTime.Now.Subtract(d0).TotalSeconds}s");
    }

    private async Task DownloadPostImages(int tid, int page, string postPath)
    {
        string postUrl = "https://cl.ug9bzk.vip" + postPath;
        DateTime d0 = DateTime.Now;
        string html = "";

        int MaxRetry = 10;
        if (DateTime.Now.Hour > 5)
            MaxRetry = 2;
        for (int retry = 0; retry < MaxRetry; retry++)
        {
            try
            {
                html = await GetPostHtmlAsync(postUrl);
                if (!string.IsNullOrEmpty(html))
                    break;
                Log($"页{page} 帖子 {tid}: 第{retry + 1}次加载返回空，重试...");
                if (retry > 0)
                    await Task.Delay(retry * 5000);  // 0, 5s, 10s, 15s... 递增等待
                else
                    await Task.Delay(300);

            }
            catch
            {
                Log($"页{page} 帖子 {tid}: 第{retry + 1}次加载异常，重试...");
            }
            await Task.Delay(500);// 每个帖子之间间隔半秒，让cef喘气
        }

        var imageUrls = new List<string>();

        // ess-data 单引号
        foreach (Match m in Regex.Matches(html, @"ess-data='([^']+)'"))
            imageUrls.Add(m.Groups[1].Value);

        // ess-data 双引号
        foreach (Match m in Regex.Matches(html, @"ess-data=""([^""]+)"""))
            imageUrls.Add(m.Groups[1].Value);
        // 现有的 ess-data 匹配后面加一条转义版本
        foreach (Match m in Regex.Matches(html, @"ess-data=\\'([^\\']+)\\'"))
            imageUrls.Add(m.Groups[1].Value);

        // data-link 单引号
        foreach (Match m in Regex.Matches(html, @"data-link='([^']+)'"))
            imageUrls.Add(m.Groups[1].Value);

        // data-link 双引号
        foreach (Match m in Regex.Matches(html, @"data-link=""([^""]+)"""))
            imageUrls.Add(m.Groups[1].Value);

        // 去重
        imageUrls = imageUrls.Distinct().ToList();

        if (tid == 1725851)
        {
            Console.Write(tid.ToString());
        }
        if (imageUrls.Count == 0)
        {
            // 检测反爬
            if (html.Length < 3000 && (html.Contains("safeid") || html.Contains("满18岁")))
            {
                Log($"页{page} 帖子 {tid}: 触发反爬验证，自动点击...");
                await GetHtmlAsync("https://rsli.mhg5i.com/");  // 触发自动点击逻辑
                return;
            }
            Log($"页{page} 帖子 {tid}: 无图片");
            return;
        }

        string title = "";
        var titleMatch = Regex.Match(html, @"<h4[^>]*>\[.*?\] (.*?)</h4>");
        if (!titleMatch.Success)
            titleMatch = Regex.Match(html, @"<title>\[.*?\] (.*?) - 新時代的我們</title>");
        if (titleMatch.Success)
        {
            title = titleMatch.Groups[1].Value;
            foreach (char c in Path.GetInvalidFileNameChars())
                title = title.Replace(c, '_');
            if (title.Length > 80)
                title = title.Substring(0, 80);
            title = title.Trim();
        }
        string dirName = string.IsNullOrEmpty(title) ? tid.ToString() : $"{tid}_{title}";
        string saveDir = Path.Combine(savePath, $"第{page}页", dirName);

        //Log($"page {page}  帖子 {tid}: {imageUrls.Count} 张图片，开始下载");

        var downloadTasks = new List<Task>();
        int index = 0;

        foreach (string imgUrl in imageUrls)
        {
            string url = imgUrl;

            if (url.StartsWith("//"))
                url = "https:" + url;
            else if (!url.StartsWith("http"))
                continue;
            
            // ⭐ 加这行，非法 URL 直接跳过
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                continue;

            // 检查域名是否被封
            if (_blockedDomainActive)
            {
                try
                {
                    var uri = new Uri(url);
                    lock (_blockedLock)
                    {
                        if (_blockedDomains.Contains(uri.Host))
                            continue;
                    }
                }
                catch { }
            }
            //无效或非常慢的图床
            /*            if (url.Contains("tupian.li/images")
                        || url.Contains("wkt41.app/tupian")
                        || url.Contains("i.postimg.cc")
                        || url.Contains("luoimg.com")
                        || url.Contains("skeimg.com")
                        || url.Contains("ej22g.com")
                        || url.Contains("imx.to/")
                        || url.Contains("fappenpics.com")
                        || url.Contains("img3.qq.tc")
                        || url.Contains("images2.imgbox.com")
                        || url.Contains("s1.xoimg.com")
                        || url.Contains("simp6.cuckcapital.cr")
                        || imgUrl.Contains("image.imx.to"))
                            continue;
            */
            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext) || ext.Length > 5) ext = ".jpg";
            if (ext.Contains("?")) ext = ext.Substring(0, ext.IndexOf("?"));

            index++;
            string saveImgPath = Path.Combine(saveDir, $"{index:D3}{ext}");

            if (!File.Exists(saveImgPath) || new FileInfo(saveImgPath).Length == 0)
            {
                // 如果已经失败过，不再尝试
                bool alreadyFailed;
                lock (_failedLock)
                {
                    alreadyFailed = _failedDownloads.Any(f => f.url == url);
                }
                if (alreadyFailed) continue;                //downloadTasks.Add(DownloadImageAsync(url, saveImgPath, index, tid));
                //await DownloadImageAsync(url, saveImgPath, index, tid);
                _imageQueue.Enqueue((url, saveImgPath, index, tid));
            }
        }

        //await Task.WhenAll(downloadTasks);
        //await Task.Delay(1000);  // 每个帖子完成后等 10 秒
        var blockedCount = _blockedDomains.Count;
        var failedDomains = _domainFailCount.Count(kv => kv.Value > 0);

        Log($"page {page} 帖子 {tid} 添加队列{imageUrls.Count} ;已下载 {_DownloadedCount}，排队{_imageQueue.Count} ，失败队列{_failedDownloads.Count}  黑名单{blockedCount}个,失败域名{failedDomains}个  用时{(int)DateTime.Now.Subtract(d0).TotalSeconds}s");
        //Log($"page {page} 帖子 {tid} 添加队列{imageUrls.Count} ;已下载 {_DownloadedCount}，排队{_imageQueue.Count} ，失败队列{_failedDownloads.Count}  用时{(int)DateTime.Now.Subtract(d0).TotalSeconds}s");
    }

    private async Task DownloadImageAsync(string imgUrl, string savePath, int index, int tid, bool Slow, CancellationToken token = default)
    {
        if (_blockedDomainActive)
        {
            try
            {
                var uri = new Uri(imgUrl);
                lock (_blockedLock)
                {
                    if (_blockedDomains.Contains(uri.Host))
                        return;
                }
            }
            catch { }
        }

        int timeOut = Slow ? 60 : 10;
        int maxRetry = _blockedDomainActive ? 3 : 10;

        for (int retry = 0; retry < maxRetry; retry++)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                byte[] data = null;
                using (var request = new HttpRequestMessage(HttpMethod.Get, imgUrl))
                {
                    if (imgUrl.Contains("23img.com"))
                    {
                        request.Headers.Referrer = new Uri("https://cl.ug9bzk.vip/");
                        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "image");
                        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "no-cors");
                        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
                    }
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    request.Headers.Add("Accept", "image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");

                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        linkedCts.CancelAfter(TimeSpan.FromSeconds(timeOut));
                        using (var response = await client.SendAsync(request, linkedCts.Token))
                        {
                            response.EnsureSuccessStatusCode();
                            data = await response.Content.ReadAsByteArrayAsync();
                        }
                    }
                }

                if (data == null || data.Length < 16000) return;

                byte[] fingerprint = ComputeFingerprint(data);

                if (EnableGarbageCheck && Db != null)
                {
                    var existingHash = Db.GetMeta(savePath);
                    if (existingHash != null && existingHash.SequenceEqual(fingerprint))
                    {
                        Log($"  帖子 {tid} 第{index}张: 垃圾，跳过");
                        return;
                    }
                }

                string dir = Path.GetDirectoryName(savePath);
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }
                catch
                {
                    string fileName = Path.GetFileName(savePath);
                    dir = Path.Combine(Path.GetDirectoryName(dir), tid.ToString());
                    Directory.CreateDirectory(dir);
                    savePath = Path.Combine(dir, fileName);
                }

                try
                {
                    File.WriteAllBytes(savePath, data);
                    _DownloadedCount++;

                    try
                    {
                        var uri = new Uri(imgUrl);
                        _domainFailCount.TryRemove(uri.Host, out _);
                    }
                    catch { }
                }
                catch (Exception ee)
                {
                    Log($"  帖子 {tid} 第{index}张存盘 {savePath} 失败: {ee.Message}");
                    if (retry < maxRetry - 1) { await Task.Delay((retry + 1) * 2000); continue; }
                    lock (_failedLock) { _failedDownloads.Add((imgUrl, savePath, index, tid)); }
                }

                if (Db != null) Db.AddMetaDeferred(savePath, fingerprint);
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return; // 外部取消，直接退出
            }
            catch (Exception ex)
            {
                // 403 404不重试，直接记失败
                bool is403or404 = (ex is HttpRequestException httpEx) &&
                  (httpEx.Message.Contains("403") || httpEx.Message.Contains("404"));
                if (is403or404)
                {
                    Console.WriteLine($"  帖子 {tid} 第{index}张 {imgUrl} 失败: 403(Forbidden)或404(未找到文件)或其他，不重试");
                    lock (_failedLock) { _failedDownloads.Add((imgUrl, savePath, index, tid)); }
                    return;
                }

                if (retry < maxRetry - 1) { await Task.Delay((retry + 1) * 2000); continue; }

                if (_blockedDomainActive)
                {
                    try
                    {
                        var uri = new Uri(imgUrl);
                        string domain = uri.Host;
                        _domainFailCount.AddOrUpdate(domain, 1, (k, v) => v + 1);
                        if (_domainFailCount.TryGetValue(domain, out int count) && count >= MaxFailPerDomain)
                        {
                            lock (_blockedLock)
                            {
                                if (!_blockedDomains.Contains(domain))
                                {
                                    _blockedDomains.Add(domain);
                                    Log($"域名 {domain} 失败 {count} 次，已加入黑名单");
                                }
                            }
                        }
                    }
                    catch { }
                }

                string errorMsg;
                if (ex is TaskCanceledException)
                {
                    if (ex.InnerException is TimeoutException)
                        errorMsg = "超时";
                    else
                        errorMsg = "取消";
                }
                //else if (ex is HttpRequestException httpEx)
                //    errorMsg = $"HTTP错误: {httpEx.Message}";
                else if (ex is WebException webEx)
                    errorMsg = $"网络错误: {webEx.Status}";
                else
                    errorMsg = ex.Message;

                Log($"  帖子 {tid} 第{index}张 {imgUrl} 失败: {errorMsg}");
                lock (_failedLock) { _failedDownloads.Add((imgUrl, savePath, index, tid)); }
            }
        }
    }

    private static byte[] ComputeFingerprint(byte[] data)
    {
        int sampleSize = 4096;
        int startPos;

        if (data.Length <= sampleSize)
        {
            startPos = 0;
            sampleSize = data.Length;
        }
        else
        {
            startPos = data.Length / 2 - sampleSize / 2;
        }

        byte[] buffer = new byte[sampleSize];
        Buffer.BlockCopy(data, startPos, buffer, 0, sampleSize);

        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            return md5.ComputeHash(buffer);
        }
    }

    public class ForumConfig
    {
        public string StartPage { get; set; }
        public string EndPage { get; set; }
        public string GroupID { get; set; }
        public string ThreadRegex { get; set; }
        public string ThreadUrl { get; set; }
        public string SaveRoot { get; set; }
        public string BaseUrl { get; set; }
        public string Cookie { get; set; }
    }
}