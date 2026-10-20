//Forms\Form1.Display.cs
using CefSharp;
using LibVLCSharp.Shared;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class Form1
    {
        // 显示相关方法（从 Form1.cs 移入）
        private async void ShowPicture()
        {
            var currentPos = Cursor.Position;
            if (currentPos != _lastCursorPos)
            {
                _lastCursorPos = currentPos;
                FileUtility.ShowCursor();
                _lastMouseMoveTime = DateTime.Now;
            }
            else if ((DateTime.Now - _lastMouseMoveTime).TotalSeconds > 1)
            {
                FileUtility.HideCursor();
            }

            if (this.dg1.CurrentRow == null)
            {
                if (this.dg1.Rows.Count > 0)
                    return;  // 还有行，只是排序中短暂为 null，等待下次正常调用
                else
                {
                    ClearPicture();
                    return;
                }
            }
            int r = this.dg1.CurrentRow.Index;
            if (this.dg1.Rows[r].Cells["Name"].Value == null) return;

            string sFileName = this.dg1.Rows[r].Cells["Name"].Value.ToString();
            toolStripStatusLabelNo.Text = (r + 1).ToString() + "/" + this.dg1.Rows.Count.ToString();
            if (this.sCurrentFileName == sFileName) return;

            this.sCurrentFileName = sFileName;
            if (sFileName.Length < 80)
                this.toolStripStatusLabelFileName.Text = sFileName;
            else
                this.toolStripStatusLabelFileName.Text = sFileName.Substring(0, 40) + "..." + sFileName.Substring(sFileName.Length - 37);
            this.labelDBRecords.Text = $"DB:{_myDb._DeletedRegistryCount}/{_myDb._RegistryCount}+{_deletedMetaList.Count}";

            if (!File.Exists(sFileName))
            {
                ClearPicture();
                return;
            }

            dCurrentVideoPlayTime = DateTime.Now;

            // ========== 取消上一个视频的等待任务 ==========
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            _videoLoadCts = new CancellationTokenSource();
            var token = _videoLoadCts.Token;
            // =============================================

            // ========== 懒加载当前行的点赞数据 ==========
            string existingMeta = dg1.Rows[r].Cells["Meta"].Value?.ToString();

            if (string.IsNullOrEmpty(existingMeta))
            {
                // ★ M292：读指纹改异步，避免 UI 线程同步读磁盘卡死
                existingMeta = await Task.Run(() => GetFileFingerprintCached(sFileName));

                // ★ 异步返回后，检查当前行是否还是原来那行（用户可能已切换）
                if (dg1.CurrentRow == null || dg1.CurrentRow.Index != r) return;

                dg1.Rows[r].Cells["Meta"].Value = existingMeta;

                if (_fileMeta.TryGetValue(existingMeta, out FileMeta meta))
                {
                    this.dg1.Rows[r].Cells["heart"].Value = meta.Heart;
                    this.dg1.Rows[r].Cells["break"].Value = meta.Break;
                }
            }
            // 如果 Meta 列有值，但 heart/break 为空（可能还没加载点赞数据）
            else if (dg1.Rows[r].Cells["heart"].Value == null || dg1.Rows[r].Cells["heart"].Value == DBNull.Value)
            {
                if (_fileMeta.TryGetValue(existingMeta, out FileMeta meta))
                {
                    this.dg1.Rows[r].Cells["heart"].Value = meta.Heart;
                    this.dg1.Rows[r].Cells["break"].Value = meta.Break;
                }
            }
            // =============================================

            try
            {
                toolStripStatusLabelTrack.Text = "";
                toolStripStatusLabelVolume.Text = "";

                string sFileExt = Path.GetExtension(sFileName);

                // 判断是否是视频格式
                bool isVideo = FileUtility.IsVideoFile(sFileExt);
                // 判断是否是图片格式
                bool isImage = FileUtility.IsImageFile(sFileExt);
                // 判断是否是小说格式
                bool isText = FileUtility.IsTextFile(sFileExt);

                if (isVideo)
                {
                    // ★ 先显示控件（句柄可能还没建，但 Visible 会触发布局）
                    videoView1.Visible = true;
                    trackBar1.Visible = true;
                    chromBrowser.Visible = false;

                    // ★ 记录待播文件
                    _pendingVideoFile = sFileName;

                    // ★ 句柄已就绪 → 直接播；否则挂事件等
                    if (videoView1.IsHandleCreated)
                    {
                        PlayPendingVideo();
                    }
                    else
                    {
                        videoView1.HandleCreated -= VideoView_HandleCreated;
                        videoView1.HandleCreated += VideoView_HandleCreated;
                    }
                    FileInfo fi2 = new FileInfo(sFileName);
                    toolStripStatusLabelSize.Text = FileUtility.FormatFileSize(fi2.Length);
                    return;   // ★ 其余逻辑在 PlayPendingVideo 里
                }
                else if (isImage)
                {
                    timer1.Stop();
                    SaveCurrentVideoProgress();
                    _currentVideoFingerprint = null;

                    videoView1.Visible = false;
                    trackBar1.Visible = false;
                    chromBrowser.Visible = true;

                    // 关键修复：如果播放器已经处于 Ended 状态，勿操作
                    if (_mediaPlayer != null && _mediaPlayer.State != VLCState.Ended)
                    {
                        try { _mediaPlayer?.Stop(); } catch { }
                        videoView1.MediaPlayer = null;
                    }
                    // 如果状态是 Ended，绝对不要去碰 Volume，甚至不要 Pause，直接忽略即可

                    string html = "";
                    int maxWidth = this.Width - this.chromBrowser.Left - 20;
                    int maxHeight = this.Height - this.chromBrowser.Top - 50;

                    if (_PictureMode == "H")
                    {
                        // 先显示一个黑色遮罩，等图片转完 base64 再替换
                        chromBrowser.LoadHtml("<html><body style='background:#000'></body></html>", "about:blank");
                        await Task.Delay(50);  // 等遮罩渲染

                        // 先尝试用 OpenCV 转 Base64（解决各种兼容性问题）
                        html = FileUtility.GenerateImageHtmlWithBase64(sFileName, maxWidth, maxHeight);
                    }
                    else if (_PictureMode == "M")
                    {
                        byte[] data = File.ReadAllBytes(sFileName);
                        // 检测 HEIC
                        if (data.Length > 12 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
                        {
                            html = "<html><body style='background:#000;color:#fff;font-size:24px;text-align:center;padding-top:100px'>HEIC 格式不支持直接显示<br/>请用画图打开后另存为 JPG</body></html>";
                        }
                        else
                        {
                            string base64 = Convert.ToBase64String(data);
                            string mime = FileUtility.GetMimeType(Path.GetExtension(sFileName).ToLower());
                            html = FileUtility.GenerateImageHtmlWithBase64Direct(base64, mime, _PictureSizeRatio);
                        }
                    }
                    else if (_PictureMode == "S")
                    {
                        // 直接用 file:// 方式加载
                        html = FileUtility.GenerateImageHtml_Simple(sFileName, maxWidth, maxHeight);
                    }
                    chromBrowser.LoadHtml(html, "file:///");
                }
                else if (isText)
                {
                    timer1.Stop();
                    try { _mediaPlayer?.Stop(); } catch { }
                    videoView1.MediaPlayer = null;

                    videoView1.Visible = false;
                    trackBar1.Visible = false;
                    chromBrowser.Visible = true;

                    if (sFileExt == ".txt")
                    {
                        string html = FileUtility.GenerateTextHtml(sFileName);
                        chromBrowser.LoadHtml(html, "file:///");
                    }
                    else // .html 或 .htm
                    {
                        chromBrowser.Load(sFileName);
                    }
                    // 恢复阅读进度（延迟等页面加载完成）
                    _ = Task.Delay(300, token).ContinueWith(_ =>
                    {
                        if (token.IsCancellationRequested) return;
                        BeginInvoke(new Action(() => RestoreTextScrollProgress()));
                    }, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
                }
                else
                {
                    timer1.Stop();
                    try { _mediaPlayer?.Stop(); } catch { }
                    videoView1.MediaPlayer = null;

                    // 完全解绑，让 VLC 停止渲染和声音
                    videoView1.MediaPlayer = null;
                    chromBrowser.Visible = false;
                    videoView1.Visible = false;
                    trackBar1.Visible = false;
                }

                FileInfo fi = new FileInfo(sFileName);
                toolStripStatusLabelSize.Text = FileUtility.FormatFileSize(fi.Length);
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常退出
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=== ShowPicture 异常 ===");
                Debug.WriteLine("文件: " + sFileName);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(new StackTrace(1, true).ToString());  // 跳过当前帧
                MessageBox.Show(ex.Message);

            }
            _lastShowPictureTime = DateTime.Now;
        }
        private void VideoView_HandleCreated(object sender, EventArgs e)
        {
            videoView1.HandleCreated -= VideoView_HandleCreated;
            if (!string.IsNullOrEmpty(_pendingVideoFile))
                PlayPendingVideo();
        }

        private void PlayPendingVideo()
        {
            string file = _pendingVideoFile;
            _pendingVideoFile = null;
            if (string.IsNullOrEmpty(file)) return;

            if (_libVLC == null)
            {
                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC);
            }
            videoView1.MediaPlayer = _mediaPlayer;

            timer1.Stop();
            trackBar1.Value = 0;

            // 恢复进度
            _currentVideoFingerprint = GetFileFingerprintCached(file);
            double savedProgress = 0;
            if (_fileMeta.TryGetValue(_currentVideoFingerprint, out FileMeta meta) && meta.Progress.HasValue)
            {
                savedProgress = meta.Progress.Value;
            }

            var media = new Media(_libVLC, file, FromType.FromPath);
            _mediaPlayer.Play(media);

            // ★ 启动 timer1，驱动 timer1_Tick 更新时长/音量
            timer1.Start();

            // 延迟设置音量
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_mediaPlayer.IsPlaying) await Task.Delay(50);
                    BeginInvoke(new Action(() =>
                    {
                        try { if (_mediaPlayer != null) _mediaPlayer.Volume = iVideoVolume; } catch { }
                    }));
                }
                catch { }
            });

            // 恢复进度
            if (savedProgress > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (_mediaPlayer.Length <= 0 || !_mediaPlayer.IsPlaying) await Task.Delay(100);
                        long totalLength = _mediaPlayer.Length;
                        if (savedProgress < totalLength)
                        {
                            float position = (float)(savedProgress / totalLength);
                            BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    if (_mediaPlayer != null)
                                    {
                                        _mediaPlayer.Position = position;
                                        trackBar1.Value = (int)(position * trackBar1.Maximum);
                                    }
                                }
                                catch { }
                            }));
                        }
                    }
                    catch { }
                });
            }
        }
        private void ClearPicture()
        {
            chromBrowser.LoadHtml("<html><body style='background:#000'></body></html>", "about:blank");
            videoView1.Visible = false;
            trackBar1.Visible = false;
            toolStripStatusLabelNo.Text = "";
            toolStripStatusLabelSize.Text = "";
        }
        private void SaveCurrentVideoProgress()
        {
            if (!string.IsNullOrEmpty(_currentVideoFingerprint) && _mediaPlayer != null)
            {
                // 等待 Length 可用
                if (_mediaPlayer.Length <= 0)
                {
                    // 如果 Length 还没准备好，直接返回，等下次 timer 保存
                    return;
                }

                double progressSeconds = _mediaPlayer.Position * _mediaPlayer.Length;

                // 播放少于 3 秒不保存
                if (progressSeconds < 3000)
                {
                    return;
                }

                if (!_fileMeta.TryGetValue(_currentVideoFingerprint, out FileMeta meta))
                {
                    meta = new FileMeta();
                    _fileMeta[_currentVideoFingerprint] = meta;
                }
                meta.Progress = progressSeconds;

                Debug.Write($"保存进度: {_currentVideoFingerprint} -> {progressSeconds}s");
            }
        }
        /// <summary>
        /// 判断当前文件是否为文本文件
        /// </summary>
        private bool IsCurrentFileText()
        {
            if (dg1.CurrentRow == null) return false;
            string sFileName = dg1.Rows[dg1.CurrentRow.Index].Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(sFileName)) return false;

            string ext = Path.GetExtension(sFileName);
            return FileUtility.IsTextFile(ext);// ext == ".txt" || ext == ".html" || ext == ".htm";
        }
        /// <summary>
        /// 保存文本阅读进度
        /// </summary>
        private void SaveTextScrollProgress(double scrollRatio)
        {
            if (dg1.CurrentRow == null) return;
            string filePath = dg1.CurrentRow.Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(filePath)) return;

            string fingerprint = GetFileFingerprintCached(filePath);

            if (!_fileMeta.TryGetValue(fingerprint, out FileMeta meta))
            {
                meta = new FileMeta();
                _fileMeta[fingerprint] = meta;
            }

            meta.TextScroll = Math.Min(1.0, Math.Max(0, scrollRatio));
        }
        /// <summary>
        /// 恢复文本阅读进度
        /// </summary>
        private void RestoreTextScrollProgress()
        {
            if (dg1.CurrentRow == null) return;
            string filePath = dg1.CurrentRow.Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(filePath)) return;

            string fingerprint = GetFileFingerprintCached(filePath);

            if (_fileMeta.TryGetValue(fingerprint, out FileMeta meta) && meta.TextScroll.HasValue)
            {
                double ratio = meta.TextScroll.Value;
                if (ratio > 0)
                {
                    chromBrowser.ExecuteScriptAsync($@"
                (function() {{
                    var scrollHeight = document.documentElement.scrollHeight || document.body.scrollHeight;
                    var clientHeight = document.documentElement.clientHeight || document.body.clientHeight;
                    var target = {ratio.ToString("F4")} * (scrollHeight - clientHeight);
                    window.scrollTo(0, target);
                }})();
            ");
                }
            }
        }
        private void SaveTextPages()
        {
            // 保存滚动进度
            chromBrowser.EvaluateScriptAsync(@"
        (function() {
            var scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
            var scrollHeight = document.documentElement.scrollHeight || document.body.scrollHeight;
            var clientHeight = document.documentElement.clientHeight || document.body.clientHeight;
            if (scrollHeight > clientHeight) {
                return scrollTop / (scrollHeight - clientHeight);
            }
            return 0;
        })();
    ").ContinueWith(task =>
            {
                if (!task.IsFaulted && task.Result?.Result != null)
                {
                    double scrollRatio = Convert.ToDouble(task.Result.Result);
                    SaveTextScrollProgress(scrollRatio);
                }
            });

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && !_isSeeking)
            {
                // ★ M323：Length 未就绪时跳过（不显示 00:00:00）
                if (_mediaPlayer.Length <= 0 && _mediaPlayer.State != VLCState.Ended)
                    return;

                // 检查是否播放结束
                if (_mediaPlayer.State == VLCState.Ended)
                {
                    if (tmrShow.Enabled)
                        return;

                    // 在后台线程执行 Stop，避免 UI 死锁
                    Task.Run(() =>
                    {
                        try { _mediaPlayer.Stop(); } catch { }
                        try { _mediaPlayer.Play(); } catch { }
                    });
                    if (videoView1.Visible)
                    {
                        trackBar1.Value = 0;
                        SaveCurrentVideoProgress();
                    }
                    return;
                }
                else if (_mediaPlayer.IsPlaying && videoView1.Visible)
                {
                    double progress = _mediaPlayer.Position;
                    if (progress > 1)
                        progress = 1;
                    trackBar1.Value = (int)(progress * trackBar1.Maximum);

                    int isec = (int)(_mediaPlayer.Length / 1000);

                    if (trackBar1.Visible)
                    {
                        TimeSpan timeprogress = TimeSpan.FromSeconds(progress * isec);
                        TimeSpan timeTotal = TimeSpan.FromSeconds(isec);
                        long lSize = (long)dg1.CurrentRow.Cells["Size"].Value;
                        toolStripStatusLabelTrack.Text = timeprogress.ToString(@"hh\:mm\:ss") + "/" + timeTotal.ToString(@"hh\:mm\:ss");
                        toolStripStatusLabelSize.Text = FileUtility.FormatFileSize(lSize);
                        toolStripStatusLabelVolume.Text = "Vol:" + iVideoVolume.ToString();
                    }
                    else
                    {
                        toolStripStatusLabelTrack.Text = "";
                        toolStripStatusLabelVolume.Text = "";
                    }

                    // 每 10 秒保存一次进度
                    if ((int)(progress * isec) % 10 == 0)
                    {
                        SaveCurrentVideoProgress();
                    }
                }
            }
        }
        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            _isSeeking = true;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                double newPosition = (double)trackBar1.Value / trackBar1.Maximum;
                _mediaPlayer.Position = (float)newPosition;  // 跳转到指定位置
            }
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            _isSeeking = false;
        }
    }
}