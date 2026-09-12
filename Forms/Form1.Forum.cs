using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ForumDownloader;

namespace PictureManager
{
    public partial class Form1
    {
        // 论坛下载相关方法（从 Form1.cs 移入）
        bool _downloaderRunning = false;
        private async void btnForumDownloader_Click(object sender, EventArgs e)
        {
            if (_downloaderRunning) return;
            _downloaderRunning = true;
            btnForumDownloader.Enabled = false;

            log("正在检测反爬...");

            // 用 WebView2 加载页面
            _downloadBrowser.CoreWebView2.Navigate("https://cl.ug9bzk.vip/thread0806.php?fid=8&search=&page=1");
            await Task.Delay(3000);

            string rawResult = await _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML");
            string html = System.Text.Json.JsonSerializer.Deserialize<string>(rawResult) ?? "";
            if (html.Contains("满18岁") || html.Contains("safeid") || html.Contains("滿 18 歲, 請按此"))
            {
                log("检测到18岁，自动点击...");
                string js = @"(function() { 
        var links = document.querySelectorAll('a'); 
        for (var i = 0; i < links.length; i++) { 
            if (links[i].innerText.indexOf('滿 18 歲') >= 0 || links[i].innerText.indexOf('满18岁') >= 0) { 
                links[i].click(); 
                return 'clicked'; 
            } 
        } 
        return 'no_btn'; 
    })();";
                await _downloadBrowser.ExecuteScriptAsync(js);
                await Task.Delay(3000);
                log("已通过验证，开始下载...");
            }
            else if (html.Contains("thread0806.php") || html.Contains("htm_data"))
            {
                log("已登录，开始下载...");
            }

            log("正在预热浏览器...");

            // 第一次访问，触发 Cookie 验证并等 JS 执行完
            _downloadBrowser.CoreWebView2.Navigate("https://cl.ug9bzk.vip/thread0806.php?fid=8&search=&page=1");
            await Task.Delay(5000);
            // 第一次访问，触发 Cookie 验证并等 JS 执行完
            //_downloadBrowser.CoreWebView2.Navigate("https://cl.ug9bzk.vip/thread0806.php?fid=8");
            //await Task.Delay(3000);

            log("开始下载...");

            _downloader = new ForumDownloader
            {
                StartPage = 13,//int.Parse(tbForumStartPage.Text),
                EndPage = 1507,//int.Parse(tbForumEndPage.Text),
                GroupID = "8",
                ThreadRegex = @"/htm_data/\d+/\d+/(\d+)\.html",//@"/r15/(\d+)\.html",
                ThreadUrl = "",// "https://www.63721685.xyz/r{版块}/{tid}.html",
                SaveRoot = @"F:\草榴社区",
                BaseUrl = "https://cl.ug9bzk.vip/thread0806.php?fid={版块}&search=377126&page={页}",//https://www.63721685.xyz/sort/r{版块}.html/{页}/",
                Db = _myDb,
                EnableGarbageCheck = true,
                GetHtmlAsync = async (url) => await GetDownloadHtmlFast(url),
                GetPostHtmlAsync = async (url) => await GetDownloadHtml(url),
            };
            /*            _downloader = new ForumDownloader 
                        {
                            StartPage = 100,//int.Parse(tbForumStartPage.Text),
                            EndPage = 6514,//int.Parse(tbForumEndPage.Text),
                            GroupID = "95",//50-978页，48亚洲-613，49-1436欧洲，125-279转贴自拍，155-131原创自拍，95综合讨论区
                            ThreadRegex = "thread-(\\d+)-1-1\\.html",
                            ThreadUrl = "https://rsli.mhg5i.com/thread-{tid}-1-1.html",
                            SaveRoot = @"F:\98Tang",
                            BaseUrl = "https://rsli.mhg5i.com/forum-{版块}-{页}.html",
                            Db = _myDb,
                            EnableGarbageCheck = true,
                            GetHtmlAsync = async (url) => await GetDownloadHtmlFast(url),
                            GetPostHtmlAsync = async (url) => await GetDownloadHtml(url),
                        };
            */
            var progress = new Progress<string>(msg =>
            {
                this.toolStripStatusLabelStatus.Text = msg;
            });

            this.timerDownloadPicture.Stop();
            this.timerDownloadPicture.Start();

            await _downloader.Run(progress);

            _downloaderRunning = false;
            btnForumDownloader.Enabled = true;
        }
        private void SaveForumConfig()
        {
            var config = new ForumConfig
            {
                StartPage = tbForumStartPage.Text,
                EndPage = tbForumEndPage.Text,
                GroupID = tbForumGroup.Text,
                ThreadRegex = tbForumThreadRegex.Text,
                ThreadUrl = tbForumThreadUrl.Text,
                SaveRoot = tbForumSavePath.Text,
                BaseUrl = tbForumGroupUrl.Text,
                Cookie = tbForumCookie.Text,
            };

            string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_forumConfigPath, json);
        }

        private void LoadForumConfig()
        {
            return;
            if (!File.Exists(_forumConfigPath)) return;

            try
            {
                string json = File.ReadAllText(_forumConfigPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<ForumConfig>(json);
                if (config == null) return;

                tbForumStartPage.Text = config.StartPage;
                tbForumEndPage.Text = config.EndPage;
                tbForumGroup.Text = config.GroupID;
                tbForumThreadRegex.Text = config.ThreadRegex;
                tbForumThreadUrl.Text = config.ThreadUrl;
                tbForumSavePath.Text = config.SaveRoot;
                tbForumGroupUrl.Text = config.BaseUrl;
                tbForumCookie.Text = config.Cookie;
            }
            catch { }
        }
        public async Task<string> GetDownloadHtml(string url)
        {
            while (!_downloadBrowserReady) await Task.Delay(100);

            _downloadBrowser.CoreWebView2.Navigate(url);
            await Task.Delay(3000);
            _lastCefActivity = DateTime.Now;

            var rawResult = await _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML");
            // 反序列化 JSON 字符串（去掉外层引号和转义）
            if (string.IsNullOrEmpty(rawResult)) return "";
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<string>(rawResult) ?? "";
            }
            catch
            {
                // 降级：手动去掉首尾引号，反转义
                return rawResult.Trim('"').Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\u003C", "<").Replace("\\u003E", ">");
            }
        }

        public async Task<string> GetDownloadHtmlFast(string url)
        {
            while (!_downloadBrowserReady || _downloadBrowser.CoreWebView2 == null)
                await Task.Delay(100);

            _downloadBrowser.CoreWebView2.Navigate(url);
            var tcs = new TaskCompletionSource<string>();

            EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
            handler = (s, e) =>
            {
                _downloadBrowser.CoreWebView2.NavigationCompleted -= handler;
                _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML")
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted || string.IsNullOrEmpty(t.Result))
                        {
                            tcs.TrySetResult("");
                            return;
                        }
                        // ⭐ 反序列化 JSON 转义字符串
                        try
                        {
                            string decoded = System.Text.Json.JsonSerializer.Deserialize<string>(t.Result);
                            tcs.TrySetResult(decoded ?? "");
                        }
                        catch
                        {
                            tcs.TrySetResult(t.Result.Trim('"').Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\u003C", "<").Replace("\\u003E", ">"));
                        }
                    });
            };

            _downloadBrowser.CoreWebView2.NavigationCompleted += handler;
            _lastCefActivity = DateTime.Now;

            var timeout = Task.Delay(30000);
            var completed = await Task.WhenAny(tcs.Task, timeout);

            if (completed == timeout)
            {
                // 超时也尝试取一下
                try
                {
                    var raw = await _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML");
                    var fallback = System.Text.Json.JsonSerializer.Deserialize<string>(raw) ?? "";
                    if (!string.IsNullOrEmpty(fallback) && fallback.Contains("thread0806.php"))
                    {
                        _downloadBrowser.CoreWebView2.NavigationCompleted -= handler;
                        return fallback;
                    }
                }
                catch { }
                return "";
            }

            string html = await tcs.Task ?? "";

            // ⭐ 检测反爬 - 98堂的
            if (!string.IsNullOrEmpty(html) && (html.Contains("满18岁") || html.Contains("safeid")))
            {
                log("18岁");
                string js = @"
    (function() {
        var btns = document.querySelectorAll('.enter-btn');
        if (btns.length > 0) {
            btns[0].click();
            return 'clicked';
        }
        return 'no_btn';
    })();
";
                await _downloadBrowser.ExecuteScriptAsync(js);
                await Task.Delay(3000);

                html = await GetDownloadHtmlFastInternal(url);
            }
            // ⭐ 检测反爬 - 绅士会所的 Cookie 验证
            if (!string.IsNullOrEmpty(html) && html.Contains("jump()") && html.Contains("clearCookie"))
            {
                log("检测到绅士会所Cookie验证，等待跳转...");
                await Task.Delay(5000);  // 等 JS 执行完自动跳转

                // 重新获取页面
                _downloadBrowser.CoreWebView2.Navigate(url);
                await Task.Delay(3000);

                var rawResult = await _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML");
                html = System.Text.Json.JsonSerializer.Deserialize<string>(rawResult) ?? "";
            }
            //草榴的
            if (html.Contains("滿 18 歲") || html.Contains("满18岁"))
            {
                // 先访问首页建立 Cookie
                _downloadBrowser.CoreWebView2.Navigate("https://cl.ug9bzk.vip/thread0806.php?fid=8");
                await Task.Delay(3000);

                // 再重新访问目标 URL
                _downloadBrowser.CoreWebView2.Navigate(url);
                await Task.Delay(3000);

                var rawResult = await _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML");
                html = System.Text.Json.JsonSerializer.Deserialize<string>(rawResult) ?? "";
            }
            return html ?? "";
        }
        private async Task<string> GetDownloadHtmlFastInternal(string url)
        {
            _downloadBrowser.CoreWebView2.Navigate(url);
            var tcs = new TaskCompletionSource<string>();

            EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
            handler = (s, e) =>
            {
                _downloadBrowser.CoreWebView2.NavigationCompleted -= handler;
                _downloadBrowser.ExecuteScriptAsync("document.documentElement.outerHTML")
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted || string.IsNullOrEmpty(t.Result))
                        {
                            tcs.TrySetResult("");
                            return;
                        }
                        try
                        {
                            string decoded = System.Text.Json.JsonSerializer.Deserialize<string>(t.Result);
                            tcs.TrySetResult(decoded ?? "");
                        }
                        catch
                        {
                            tcs.TrySetResult(t.Result.Trim('"').Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\u003C", "<").Replace("\\u003E", ">"));
                        }
                    });
            };

            _downloadBrowser.CoreWebView2.NavigationCompleted += handler;

            var timeout = Task.Delay(30000);
            var completed = await Task.WhenAny(tcs.Task, timeout);

            if (completed == timeout) return "";
            return await tcs.Task ?? "";
        }

        private async void timerDownloadPicture_Tick(object sender, EventArgs e)
        {
            if (_isFlushing) return;
            _isFlushing = true;
            try
            {
                await Task.Run(() => _downloader?.FlushImageQueue());  // ← 丢到线程池
            }
            finally
            {
                _isFlushing = false;
            }
        }

    }
}