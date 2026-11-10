//Forms\Form1.Keyboard.cs
using CefSharp;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PictureManager
{
    public partial class Form1
    {
        private double _videoStepSeconds = 5.0;          // 当前步进（秒）
        private DateTime _lastStepKeyTime = DateTime.MinValue;   // 上次左右键时间
        // 键盘处理相关方法（从 Form1.cs 移入）
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Keys k = e.KeyCode;
            if (k == Keys.F1)
            {
                string s = "F2：删除目录里的全部文件，不标记为垃圾\r\n";
                s += "F3：删除目录里的全部文件，且标记为垃圾\r\n";
                s += "F4：显示当前目录的缩略图\r\n";
                s += "Ctrl+F4：显示所有文件的缩略图\r\n";
                s += "F5：重新加载文件列表\r\n";
                s += "F6：简易删除列表里重复文件，不标记为垃圾\r\n";
                s += "F8：删除磁盘里包含关键字的目录，不标记为垃圾\r\n";
                s += "F9：以兼容方式显示文件\r\n";
                s += "F11：幻灯片\r\n";
                s += "F12：全屏\r\n";
                s += "ctrl+s：缩减图片，ctrl+ r:旋转；ctrl+-:缩放图片\r\n";
                s += "v=Hash/Dct/Sift相似图; f=Hsv相似图; t=AI增强; z=点赞; x=踩; d=删除; e=画图;ctrl+e=脸部马赛克; \r\n";
                MessageBox.Show(s);
            }
            else if (e.Control && k == Keys.F4)
            {
                FileUtility.ShowCursor();

                var allImageFiles = new List<string>();
                var selectedFiles = new List<string>();

                for (int i = 0; i < dg1.Rows.Count; i++)
                {
                    string fp = dg1.Rows[i].Cells["Name"].Value?.ToString();
                    if (!string.IsNullOrEmpty(fp) && FileUtility.IsImageFile(Path.GetExtension(fp)))
                    {
                        allImageFiles.Add(fp);
                        if (i < dg1.CurrentRow.Index)
                            selectedFiles.Add(fp);
                    }
                }

                ShowAlbum(allImageFiles, selectedFiles);
                e.Handled = true;
            }
            else if (k == Keys.F5)
            {
                sCurrentFileName = "";
                this.btnLoadView_Click(sender, e);
            }
            else if (k == Keys.F6)
            {
                this.btnNameSize_Click(sender, e);
            }
            else if (k == Keys.F8)
            {
                string tag = Microsoft.VisualBasic.Interaction.InputBox("请输入标签", "删除目录", "").Trim();
                if (tag != "")
                {
                    FileUtility.DeleteDirectoryByTag(directoryListControl1.CheckedDirectories, tag, _TmpDirectory);
                }
            }
            else if (k == Keys.F11)
            {
                this.btnShow_Click(sender, e);
            }
            else if (k == Keys.F12 || k == Keys.Escape)
            {
                if (this.FormBorderStyle == FormBorderStyle.None)
                {
                    // 退出全屏
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Maximized;
                    this.tabControl1.Visible = true;
                    this.TopMost = false;
                    FileUtility.ShowCursor();  // ⭐ 显示鼠标
                    this.btnESC.Visible = false;
                    Form1_Resize(this, EventArgs.Empty);  // 恢复原来的布局
                }
                else if (k == Keys.F12)
                {
                    // 全屏
                    //_savedTabWidth = tabControl1.Width;
                    this.tabControl1.Visible = false;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                    FileUtility.HideCursor();  // ⭐ 不显示鼠标
                    this.btnESC.Visible = true;
                    Form1_Resize(this, EventArgs.Empty);  // 恢复原来的布局
                }
                e.Handled = true;
            }

            // 直接调 dg1 的键盘处理逻辑
            else if (this.FormBorderStyle == FormBorderStyle.None)
                dg1_KeyDown(sender, e);
        }
        private void dg1_KeyDown(object sender, KeyEventArgs e)
        {
            Keys k = e.KeyCode;
            if (dg1.CurrentRow == null) return;
            string filePath = dg1.CurrentRow.Cells["Name"].Value?.ToString();
            if (filePath == "") return;

            if (k == Keys.Delete || k == Keys.D)
            {
                this.btnDelete();
                e.Handled = true;
            }
            else if (k == Keys.F2) //F2时只删目录里文件，但是不作为垃圾，
            {
                DeleteDirectoryFiles(false);
            }
            else if (k == Keys.F3) //F3时做为垃圾删除目录里的文件，以后见了也自动删。
            {
                DeleteDirectoryFiles(true);
            }
            else if ((k == Keys.F4 && !e.Alt && !e.Control))
            {
                if (string.IsNullOrEmpty(filePath)) return;

                string dir = Path.GetDirectoryName(filePath);
                // 从 dg1 收集同目录的图片文件，保持 dg1 顺序
                var dirFiles = new List<string>();
                var selectedFiles = new List<string>();
                for (int r = 0; r < dg1.Rows.Count; r++)
                {
                    string f = dg1.Rows[r].Cells["Name"].Value?.ToString();
                    if (!string.IsNullOrEmpty(f) && Path.GetDirectoryName(f) == dir
                        && FileUtility.IsImageFile(Path.GetExtension(f)))
                    {
                        dirFiles.Add(f);
                        if (r < dg1.CurrentRow.Index)
                            selectedFiles.Add(f);
                    }
                }
                if (dirFiles.Count == 0)
                {
                    // ★ 无图片，打开资源管理器
                    FileUtility.ExploreDirectory(dir);
                }
                else
                {
                    ShowAlbum(dirFiles, selectedFiles);
                }
                e.Handled = true;
            }
            else if (k == Keys.F9)
            {
                if (_PictureMode == "H")
                    _PictureMode = "S";
                else if (_PictureMode == "M")
                    _PictureMode = "H";
                else if (_PictureMode == "S")
                    _PictureMode = "M";
                this.toolStripStatusLabelPictureMode.Text = "PM:" + _PictureMode + $" {_PictureSizeRatio}%";

                sCurrentFileName = "";
                ShowPicture();

            }
            else if (e.Control && k == Keys.S)
            {
                e.Handled = true;

                string ext = Path.GetExtension(filePath).ToLower();
                if (!FileUtility.IsImageFile(ext) || ext == ".gif" || ext == ".heic") return;

                string tempFile = filePath + ".rebuild.jpg";
                try
                {
                    if (!FileUtility.RebuildPictureTo(filePath, tempFile))
                    {
                        MessageBox.Show("重建失败");
                        return;
                    }

                    FileUtility.ShowCursor();
                    using (var compare = new CompareForm(filePath, tempFile))
                    {
                        compare.ShowDialog();
                        switch (compare.AlbumReturnValue)
                        {
                            case "left":
                                // 留原图，删重建临时文件
                                try { File.Delete(tempFile); } catch { }
                                break;
                            case "right":
                                // 留重建图，覆盖原图
                                try
                                {
                                    File.Delete(filePath);
                                    File.Move(tempFile, filePath);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"覆盖原图失败: {ex.Message}");
                                    try { File.Delete(tempFile); } catch { }
                                }
                                break;
                            case "both":
                                // 两个都留，重建临时文件改名为正式文件
                                string finalName = Path.Combine(
                                    Path.GetDirectoryName(filePath),
                                    Path.GetFileNameWithoutExtension(filePath) + ".rebuild" + ext);
                                try
                                {
                                    if (File.Exists(finalName)) File.Delete(finalName);
                                    File.Move(tempFile, finalName);
                                }
                                catch { }
                                break;
                        }
                    }
                }
                finally
                {
                    // 兜底：临时文件还在就删掉
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                }

                sCurrentFileName = "";
                ShowPicture();
            }
            else if (e.Control && k == Keys.R)
            {
                FileUtility.RotateClockwise(filePath);
                e.Handled = true;
                sCurrentFileName = "";
                ShowPicture();
            }
            else if (k == Keys.T)
            {
                string ext = Path.GetExtension(sCurrentFileName).ToLower();
                if (!FileUtility.IsImageFile(ext) || ext == ".gif") return; // 跳过 gif

                FileUtility.ShowCursor();
                using (var compare = new CompareForm(sCurrentFileName))
                {
                    compare.ShowDialog();
                }
                sCurrentFileName = "";
                ShowPicture();
                e.Handled = true;
            }
            else if (k == Keys.Space)
            {
                // 如果正在播放视频，空格控制暂停/继续
                if (videoView1.Visible && _mediaPlayer != null)
                {
                    trackBar1_KeyDown(sender, e);
                    e.Handled = true;
                    return;
                }
                // 显示 DCT 直方图
                if (!string.IsNullOrEmpty(filePath))
                {
                    var feature = FeatureCacheManager.Instance.GetOrExtract(filePath);
                    if (feature?.Dct != null)
                    {
                        DctPicture(dg1.CurrentCell.RowIndex.ToString(), feature.Dct);
                    }
                }
            }
            else if (k == Keys.W || k == Keys.Up)
            {
                if (this.dg1.CurrentCell.RowIndex > 0)
                    this.dg1.CurrentCell = this.dg1.Rows[this.dg1.CurrentCell.RowIndex - 1].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.S || k == Keys.Down)
            {
                if (this.dg1.CurrentCell.RowIndex < this.dg1.RowCount - 1)
                    this.dg1.CurrentCell = this.dg1.Rows[this.dg1.CurrentCell.RowIndex + 1].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.PageUp)
            {
                int iNewRow = this.dg1.CurrentCell.RowIndex - 30;
                if (iNewRow < 0)
                    iNewRow = 0;
                this.dg1.CurrentCell = this.dg1.Rows[iNewRow].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.PageDown)
            {
                int iNewRow = this.dg1.CurrentCell.RowIndex + 30;
                if (iNewRow > this.dg1.RowCount - 1)
                    iNewRow = this.dg1.RowCount - 1;
                this.dg1.CurrentCell = this.dg1.Rows[iNewRow].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.Home && e.Control)
            {
                if (this.dg1.RowCount > 0)
                    this.dg1.CurrentCell = this.dg1.Rows[0].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.End && e.Control)
            {
                if (this.dg1.RowCount > 0)
                    this.dg1.CurrentCell = this.dg1.Rows[dg1.RowCount - 1].Cells[this.dg1.CurrentCell.ColumnIndex];
                e.Handled = true;
            }
            else if (k == Keys.E && !e.Control)
            {
                edit(sCurrentFileName);
                e.Handled = true;
            }
            else if (k == Keys.E && e.Control)
            {
                MaskFace(sCurrentFileName);
                e.Handled = true;
            }
            else if (k == Keys.V && !e.Control)
            {
                SyncSettingsFromUI();

                double threshold = DuplicateSettings.DefaultThreshold;
                Debug.WriteLine($"[V] threshold={threshold}, tbHashRatio={tbHashRatio.Text}, DefaultThreshold={DuplicateSettings.DefaultThreshold}");

                var similarFiles = new HashSet<string>();

                // 三种算法联查（Hash + DCT + SIFT）
                foreach (var name in new[] { "Hash", "DCT", "SIFT" })
                {
                    var comparer = PictureCompareManager.Get(name);
                    if (comparer == null) continue;

                    try
                    {
                        //foreach (var f in comparer.FindSimilar(filePath, threshold, bucketType))
                        //    similarFiles.Add(f);
                        var list = comparer.FindSimilar(filePath, threshold);
                        Debug.WriteLine($"[V] {name}: {list.Count}");   // ★ 加这行
                        foreach (var f in list) similarFiles.Add(f);
                    }
                    catch { }
                }

                similarFiles.Add(filePath);

                if (similarFiles.Count > 1)
                    ShowAlbum(similarFiles.ToList());
                e.Handled = true;
            }
            else if (k == Keys.F)
            {
                var comparer = PictureCompareManager.Get("HSV");
                if (comparer == null) return;

                SyncSettingsFromUI();
                double threshold = DuplicateSettings.DefaultThreshold;

                List<string> similarFiles;
                try
                {
                    similarFiles = comparer.FindSimilar(filePath, threshold);
                }
                catch
                {
                    return;
                }

                similarFiles.Insert(0, filePath);

                if (similarFiles.Count > 1)
                    ShowAlbum(similarFiles);
                e.Handled = true;
            }
            else if (k == Keys.Right)
            {
                if (trackBar1.Visible && _mediaPlayer != null && _mediaPlayer.Length > 0)
                {
                    trackBar1_KeyDown(sender, e);
                    e.Handled = true;
                }
                else if (chromBrowser.Visible && IsCurrentFileText())
                {
                    // 文本文件：向下翻页（Page Down）
                    chromBrowser.ExecuteScriptAsync("window.scrollBy(0, window.innerHeight * 0.8);");
                    SaveTextPages();
                    e.Handled = true;
                }
            }
            else if (k == Keys.Left)
            {
                if (trackBar1.Visible && _mediaPlayer != null && _mediaPlayer.Length > 0)
                {
                    trackBar1_KeyDown(sender, e);
                    e.Handled = true;
                }
                else if (chromBrowser.Visible && IsCurrentFileText())
                {
                    // 文本文件：向上翻页（Page Up）
                    chromBrowser.ExecuteScriptAsync("window.scrollBy(0, -window.innerHeight * 0.8);");

                    SaveTextPages();
                    e.Handled = true;
                }
            }
            else if (k == Keys.Z)
            {
                // 点赞
                UpdateMetaAndCell(meta => meta.Heart++, "heart");
                e.Handled = true;
            }
            else if (!e.Control && k == Keys.X)
            {
                // 点踩
                UpdateMetaAndCell(meta => meta.Break++, "break");
                e.Handled = true;
            }
            else if (e.Control && k == Keys.X)
            {
                int currentRow = dg1.CurrentRow.Index;

                FileUtility.ShowCursor();
                using (var dialog = new FolderBrowserDialog
                {
                    Description = "选择目标目录",
                    ShowNewFolderButton = true,
                    SelectedPath = _MainDirectory
                })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        dg1.CurrentCellChanged -= dg1_CurrentCellChanged;

                        string targetDir = dialog.SelectedPath;
                        _MainDirectory = targetDir;

                        Directory.CreateDirectory(targetDir);

                        for (int i = currentRow; i >= 0; i--)
                        {
                            filePath = dg1.Rows[i].Cells["Name"].Value?.ToString();
                            if (string.IsNullOrEmpty(filePath)) continue;

                            // 去掉盘符后的完整目录结构
                            string pathWithoutDrive = filePath.Substring(Path.GetPathRoot(filePath).Length);
                            string destPath = Path.Combine(targetDir, pathWithoutDrive.TrimStart('\\'));

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                            if (File.Exists(destPath))
                            {
                                string name = Path.GetFileNameWithoutExtension(filePath);
                                string ext = Path.GetExtension(filePath);
                                string dir = Path.GetDirectoryName(destPath);
                                destPath = Path.Combine(dir, $"{name}_{DateTime.Now:HHmmss}{ext}");
                            }

                            try
                            {
                                File.Move(filePath, destPath);
                                RemoveDatagridRows(i);

                            }
                            catch { }
                            dg1.CurrentCellChanged += dg1_CurrentCellChanged;
                        }

                        if (dg1.Rows.Count == 0)
                            this.btnLoadView_Click(sender, e);
                    }
                }
                e.Handled = true;
            }
            // 音量控制
            else if (e.Control && (k == Keys.OemMinus || k == Keys.Subtract))  // Ctrl + 减号
            {
                if (trackBar1.Visible)
                    trackBar1_KeyDown(sender, e);
                else
                {
                    if (_PictureSizeRatio > 0)
                    {
                        _PictureSizeRatio -= 50;
                        this.toolStripStatusLabelPictureMode.Text = "PM:" + _PictureMode + $" {_PictureSizeRatio}%";
                        sCurrentFileName = "";
                        ShowPicture();
                    }
                }
                e.Handled = true;
            }
            else if (e.Control && (k == Keys.Oemplus || k == Keys.Add))   // Ctrl + 加号
            {
                if (trackBar1.Visible)
                    trackBar1_KeyDown(sender, e);
                else
                {
                    _PictureSizeRatio += 50;
                    this.toolStripStatusLabelPictureMode.Text = "PM:" + _PictureMode + $" {_PictureSizeRatio}%";
                    sCurrentFileName = "";
                    ShowPicture();
                }
                e.Handled = true;
            }
        }

        private void trackBar1_KeyDown(object sender, KeyEventArgs e)
        {
            if (trackBar1.Visible && _mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left)
                {
                    double totalSeconds = _cachedVideoLength / 1000.0;
                    if (totalSeconds <= 0) return;

                    // ★ 两次左右键间隔 < 1 秒 → 跳跃模式（步长 = max(5秒, 总长10%)）
                    DateTime now = DateTime.Now;
                    if ((now - _lastStepKeyTime).TotalSeconds < 1.0)
                    {
                        _videoStepSeconds = Math.Max(5.0, totalSeconds * 0.1);
                    }
                    else
                    {
                        _videoStepSeconds = 5.0;
                    }
                    _lastStepKeyTime = now;

                    double stepPercent = (_videoStepSeconds / totalSeconds) * 10000.0;

                    if (e.KeyCode == Keys.Right)
                    {
                        int newValue = trackBar1.Value + (int)Math.Round(stepPercent);
                        if (newValue > trackBar1.Maximum - 1) newValue = trackBar1.Maximum - 1;
                        _isSeeking = true;
                        trackBar1.Value = newValue;
                        trackBar1_Scroll(sender, e);
                        Task.Delay(1000).ContinueWith(_ => BeginInvoke(new Action(() => { _isSeeking = false; })));
                        e.Handled = true;
                    }
                    else
                    {
                        int newValue = trackBar1.Value - (int)Math.Round(stepPercent);
                        if (newValue < trackBar1.Minimum) newValue = trackBar1.Minimum;
                        _isSeeking = true;
                        trackBar1.Value = newValue;
                        trackBar1_Scroll(sender, e);
                        Task.Delay(1000).ContinueWith(_ => BeginInvoke(new Action(() => { _isSeeking = false; })));
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Space)
                {
                    if (_mediaPlayer.IsPlaying)
                        _mediaPlayer.Pause();
                    else
                        _mediaPlayer.Play();
                }
                else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))  // Ctrl + 减号
                {
                    int newVolume = Math.Max(0, _mediaPlayer.Volume - 10);
                    _mediaPlayer.Volume = newVolume;
                    iVideoVolume = newVolume;
                    e.Handled = true;
                }
                else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))   // Ctrl + 加号
                {
                    int newVolume = Math.Min(100, _mediaPlayer.Volume + 10);
                    _mediaPlayer.Volume = newVolume;
                    iVideoVolume = newVolume;
                    e.Handled = true;
                }
                else
                {
                    dg1.Focus();
                    dg1_KeyDown(sender, e);
                }
            }
        }
    }
}