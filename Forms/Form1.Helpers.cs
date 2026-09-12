using LibVLCSharp.Shared;
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class Form1
    {
        // 辅助方法（从 Form1.cs 移入）
        private void AddFileToDataGrid()
        {
            this.toolStripStatusLabelFileName.Text = "";
            this.sCurrentFileName = "";
            // 记录添加前的行数

            if (this.dg1.Columns.Count == 0)
            {
                // 确保启用视觉样式
                this.dg1.EnableHeadersVisualStyles = false;

                // 创建字体
                var headerFont = new Font("宋体", 16F, FontStyle.Bold);
                var cellFont = new Font("宋体", 16F, FontStyle.Bold);

                // 新增 heart 列
                DataGridViewTextBoxColumn heartCol = new DataGridViewTextBoxColumn();
                heartCol.Name = "heart";
                heartCol.HeaderText = "❤";
                heartCol.ValueType = typeof(int);
                heartCol.Width = 50;
                heartCol.DefaultCellStyle.Font = cellFont;
                heartCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                heartCol.HeaderCell.Style.Font = headerFont;
                heartCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                this.dg1.Columns.Add(heartCol);

                // 新增 break 列
                DataGridViewTextBoxColumn breakCol = new DataGridViewTextBoxColumn();
                breakCol.Name = "break";
                breakCol.HeaderText = "✗";
                breakCol.ValueType = typeof(int);
                breakCol.Width = 50;
                breakCol.DefaultCellStyle.Font = cellFont;
                breakCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                breakCol.HeaderCell.Style.Font = headerFont;
                breakCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                this.dg1.Columns.Add(breakCol);


                this.dg1.Columns.Add("ID", "ID");
                this.dg1.Columns["ID"].ValueType = typeof(int);
                this.dg1.Columns.Add("Name", "Name");
                this.dg1.Columns.Add("Size", "Size");
                this.dg1.Columns["Size"].ValueType = typeof(long);
                this.dg1.Columns.Add("Date", "Date");
                this.dg1.Columns["Date"].ValueType = typeof(DateTime);
                // 新增 Meta 列（指纹列，放最右边）
                this.dg1.Columns.Add("Meta", "Meta");
                //this.dg1.Columns["Meta"].Visible = false;  // 隐藏，不需要显示

                this.dg1.Columns["ID"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 如果是追加模式，列已存在，直接添加行
            // 设置 ID 列宽（如果存在）
            if (this.dg1.Columns.Contains("ID"))
                this.dg1.Columns["ID"].Width = 40;

            int existingRowCount = this.dg1.Rows.Count;
            // 从 existingRowCount 开始添加，而不是从 0 开始
            for (int i = existingRowCount; i < alFile.Count; i++)
            {
                FileInfo fi = (FileInfo)alFile[i];
                object[] o = new object[7];  // 现在是 7 列
                o[0] = 0;  // heart 初始为 0
                o[1] = 0;  // break 初始为 0
                o[2] = this.dg1.Rows.Count;
                o[3] = fi.FullName;
                o[4] = fi.Length;
                o[5] = fi.LastWriteTime;
                o[6] = null;  // Meta
                int rowIndex = this.dg1.Rows.Add(o);

                // 根据文件类型设置背景色
                string ext = fi.Extension;
                if (FileUtility.IsVideoFile(ext))
                    this.dg1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(0x22, 0x22, 0x44);
                else if (FileUtility.IsImageFile(ext))
                    this.dg1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(0x22, 0x44, 0x22);
                else if (FileUtility.IsTextFile(ext))
                    this.dg1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(0x44, 0x22, 0x22);
                else
                    this.dg1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(0x99, 0x99, 0x99);
                this.dg1.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(0xff, 0xff, 0xff);

                if (i % 1000 == 0)
                    System.Windows.Forms.Application.DoEvents();
            }
        }
        private void AddFileToView(string sDirName)
        {
            int iPage = int.Parse(tbPage.Text);
            int iPageRows = int.Parse(tbPageRows.Text);
            int iPageStart = (iPage - 1) * iPageRows;
            int iPageEnd = iPage * iPageRows;
            if (ialFilesAllCount >= iPageEnd) return;

            // ✅ 当场截断当前目录名
            string parentDir = Path.GetDirectoryName(sDirName);
            string dirName = Path.GetFileName(sDirName);
            string newDirName = FileUtility.FixName(dirName, maxLen: 70);
            if (newDirName != dirName)
            {
                string newPath = Path.Combine(parentDir, newDirName);
                if (!Directory.Exists(newPath))
                {
                    try { Directory.Move(sDirName, newPath); sDirName = newPath; }
                    catch { }
                }
            }
            if (!Directory.Exists(sDirName)) return;

            DirectoryInfo diRoot = new DirectoryInfo(sDirName);
            DirectoryInfo[] di = diRoot.GetDirectories();
            string[] patterns = this.cbFileType.Text.Split(';');

            foreach (string pattern in patterns)
            {
                var allFiles = diRoot.EnumerateFileSystemInfos(pattern.Trim(), System.IO.SearchOption.TopDirectoryOnly)
                     .OfType<FileInfo>()
                     .ToArray();

                foreach (var file in allFiles)
                {
                    if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    if (file.Name.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase)) continue;
                    if (file.FullName.IndexOf(tbFilenameFilter.Text) < 0) continue;

                    // ✅ 当场截断文件名
                    string fileName = Path.GetFileNameWithoutExtension(file.FullName);
                    string ext = file.Extension;
                    string newFileName = FileUtility.FixName(fileName, maxLen: 50);
                    FileInfo fi = file;

                    if (newFileName != fileName)
                    {
                        string newPath = Path.Combine(sDirName, newFileName + ext);
                        if (!File.Exists(newPath))
                        {
                            try
                            {
                                File.Move(file.FullName, newPath);
                                fi = new FileInfo(newPath);  // ✅ 指向新名
                            }
                            catch { }
                        }
                    }

                    // 计数 + 分页
                    ialFilesAllCount++;
                    if (ialFilesAllCount > iPageStart && ialFilesAllCount <= iPageEnd)
                        alFile.Add(fi);
                    if (ialFilesAllCount >= iPageEnd)
                        return;
                }
            }

            if (ialFilesAllCount < iPageEnd)
            {
                Array.Sort(di, new DirectoryComparer());
                for (int i = 0; i < di.Length; i++)
                {
                    AddFileToView(di[i].FullName);
                    if (ialFilesAllCount >= iPageEnd) return;
                }
            }

            // 删除空目录
            var fiRoot = diRoot.GetFiles("*.*");
            List<string> checkedDirs = directoryListControl1.CheckedDirectories.ToList();
            if (fiRoot.Length == 0 && di.Length == 0)
            {
                // 不删除根目录
                if (!checkedDirs.Any(d => string.Equals(d, sDirName, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        Directory.Delete(sDirName);
                        iEmptyDirectoryCount++;
                        if (iEmptyDirectoryCount < 20)
                            sEmptyDirectory += sDirName + "\r\n";
                    }
                    catch (Exception ex)
                    {
                        this.toolStripStatusLabelStatus.Text = $"删除空目录失败: {sDirName}, 原因: {ex.Message}";
                    }
                }
            }
        }
        private void DatagridSeekFile(string file)
        {
            for (int r = 0; r < dg1.Rows.Count; r++)
            {
                string f = dg1.Rows[r].Cells["Name"].Value.ToString();
                if (f == file)
                {
                    dg1.CurrentCell = dg1.Rows[r].Cells[0];  // 设置当前行
                    dg1.FirstDisplayedScrollingRowIndex = r;  // 滚动到视野内
                    return;
                }
            }
        }



        private void ShowGroupsPictures(List<List<string>> groups)
        {
            var allFiles = new List<string>();
            foreach (var group in groups)
            {
                foreach (string file in group)
                    allFiles.Add(file);
            }

            if (allFiles.Count == 0)
            {
                MessageBox.Show("没有找到重复组");
                return;
            }

            allFiles = allFiles.Distinct().ToList();
            ShowAlbum(allFiles);
        }


        private void ShowAlbum(List<string> dirFiles, List<string> selectedFiles = null)
        {
            if (dirFiles.Count == 0) return;

            FileUtility.ShowCursor();
            try
            {
                using (var album = new AlbumForm(dirFiles, this))
                {
                    album.FilesDeleted += (deletedFiles) =>
                    {
                        RemoveDatagridRowsByNames(deletedFiles);
                    };
                    album.DirectoryList = directoryListControl1.CheckedDirectories;
                    album.TmpDirectory = _TmpDirectory;
                    if (selectedFiles != null)
                        album.SetSelectedFiles(selectedFiles);

                    album.ShowDialog(this);
                    if (album.ReturnValue == "F3")
                        DeleteDirectoryFiles(true);
                    else if (album.ReturnValue == "Select")
                        DatagridSeekFile(album.SelectedFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AlbumForm 异常: {ex.Message}");
            }

            ShowPicture();
        }
        private void DctPicture(string rowID, OpenCvSharp.Mat hist)
        {
            // 加载图像
            //var image = Cv2.ImRead(imagePath, ImreadModes.Color);
            //if (image.Empty())
            //{
            //    Console.WriteLine("Image not loaded.");
            //    return;
            // }

            // 转换为灰度图像
            //var grayImage = new Mat();
            //Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // 创建直方图计算的参数
            //Mat hist = new Mat();
            int[] histSize = { 256 }; // 直方图的bin数量，这里使用256个bin，对应于灰度值的范围（0-255）
            //Rangef[] histRange = { new Rangef(0, 256) }; // 直方图的范围
            //bool uniform = true, accumulate = false; // 直方图是否均匀分布，是否累积计算
            //const int channels = 0; // 对于灰度图像，通道设置为0，表示所有通道

            // 计算直方图
            //Cv2.CalcHist(new Mat[] { grayImage }, new int[] { channels }, new Mat(), hist, 1, histSize, histRange, uniform, accumulate);

            // 创建一个新的窗口用于显示直方图
            //Cv2.NamedWindow("Histogram");

            // 绘制直方图
            int canvasWidth = 512; // 画布宽度
            int canvasHeight = 400; // 画布高度
            OpenCvSharp.Mat histImage = new OpenCvSharp.Mat(canvasHeight, canvasWidth, MatType.CV_8SC3, new Scalar(0, 0, 0)); // 创建一个黑色的画布
            //Cv2.CalcHist(new Mat[] { grayImage }, new int[] { channels }, new Mat(), hist, 1, histSize, histRange, uniform, accumulate); // 可能需要再次计算，取决于需求是否累积计算
            Cv2.Normalize(hist, hist, 0, histImage.Rows, NormTypes.MinMax); // 归一化直方图，使其适合画布高度

            for (int i = 0; i < histSize[0]; i++) // 在画布上绘制直方图条形图
            {
                Cv2.Line(histImage, new OpenCvSharp.Point(i, canvasHeight), new OpenCvSharp.Point(i, canvasHeight - (int)hist.At<float>(i)), Scalar.Red, 2);
            }

            // 显示直方图窗口
            Cv2.ImShow(rowID, histImage);
            Cv2.WaitKey(0); // 等待按键操作，以便查看结果
            Cv2.DestroyAllWindows(); // 关闭所有窗口

        }

        private void MaskFace(string file)
        {
            string outfile = file + ".maskface.jpg";
            if (FileUtility.MaskFace(file, outfile))
            {
                using (var compare = new CompareForm(file, outfile))
                {
                    compare.ShowDialog();
                    // 根据返回值处理选中状态
                    switch (compare.AlbumReturnValue)
                    {
                        case "left":
                            File.Delete(outfile);
                            break;
                        case "right":
                            File.Delete(file);
                            File.Move(outfile, file);
                            break;
                        case "both":
                            // 保持两个选中状态，什么都不做
                            break;
                    }
                }
            }
        }
        private void edit(string file)
        {
            try
            {
                Process.Start(new ProcessStartInfo("画图") { FileName = file });
                return;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }


        private void btnShow_Click(object sender, EventArgs e)
        {
            if (this.btnShow.Text == "Show")
            {
                this.tmrShow.Start();
                this.btnShow.Text = "Stop";
            }
            else
            {
                this.tmrShow.Stop();
                this.btnShow.Text = "Show";
            }
        }

        private void tmrShow_Tick(object sender, EventArgs e)
        {
            int iCur = this.dg1.CurrentCell.RowIndex;
            if (iCur < this.dg1.RowCount - 1)
            {
                if (this.chromBrowser.Visible)
                {
                    this.dg1.Rows[iCur + 1].Selected = true;
                    this.dg1.CurrentCell = this.dg1.Rows[iCur + 1].Cells["ID"];
                }
                else
                {
                    if (_mediaPlayer != null && !_isSeeking)
                    {
                        // 检查是否播放结束
                        if (_mediaPlayer.State == VLCState.Ended)
                        {
                            // 在后台线程执行 Stop，避免 UI 死锁
                            Task.Run(() =>
                            {
                                try { _mediaPlayer.Stop(); } catch { }
                            });
                            SaveCurrentVideoProgress();
                            this.dg1.Rows[iCur + 1].Selected = true;
                            this.dg1.CurrentCell = this.dg1.Rows[iCur + 1].Cells["ID"];
                        }
                    }
                }
            }
        }

        private void log(string s)
        {
            //btnForumDownloader.Text = s;  // 恢复文字
            Console.WriteLine(s);
        }
        private void btnLoadView_Click(object sender, EventArgs e)
        {
            _TmpDirectory = this.tbTmpDirectory.Text.Trim();

            AlbumForm.Clear();

            // ===== 取消视频等待任务 =====
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            _videoLoadCts = null;
            // ===========================

            // ===== 异步清理视频播放状态（不卡 UI）=====
            SaveCurrentVideoProgress();
            _currentVideoFingerprint = null;
            timer1.Stop();

            var mp = _mediaPlayer;
            if (mp != null)
            {
                Task.Run(() =>
                {
                    try { mp.Stop(); } catch { }
                });
            }

            videoView1.MediaPlayer = null;  // 直接解绑
            videoView1.Visible = false;
            trackBar1.Visible = false;
            trackBar1.Value = 0;
            // ================================

            this.dg1.Visible = false;
            this.directoryListControl1.Visible = false;

            alFile.Clear();
            this.dg1.Columns.Clear();
            this.dg1.Rows.Clear();

            iEmptyDirectoryCount = 0;
            sEmptyDirectory = "";
            ialFilesAllCount = 0;
            var checkedDirs = directoryListControl1.CheckedDirectories;
            foreach (string dir in checkedDirs)
            {
                AddFileToView(dir);
            }

            AddFileToDataGrid();
            this.dg1.Sort(this.dg1.Columns["Date"], ListSortDirection.Descending);
            this.dg1.Visible = true;
            if (this.dg1.Rows.Count > 0)
            {
                this.dg1.Rows[0].Selected = true;
                this.dg1.CurrentCell = this.dg1.Rows[0].Cells[0];
                this.dg1.Focus();
            }

            ShowPicture();
            FileUtility.ShowCursor();

            //如果包含\\这种共享目录，就不要自动加载meta
            bool hasNetworkPath = directoryListControl1.CheckedDirectories.Any(d => d.StartsWith(@"\\"));
            if (!hasNetworkPath)
            {
                BeginInvoke(new Action(() =>
                {
                    btnMeta_Click(this, EventArgs.Empty);
                }));
            }
            if (iEmptyDirectoryCount > 0)
                MessageBox.Show("Delete " + iEmptyDirectoryCount.ToString() + " empty directory. \r\n" + sEmptyDirectory);

            this.labelDBRecords.Text = $"DB:{_myDb._DeletedRegistryCount}/{_myDb._RegistryCount}+{_deletedMetaList.Count}";

            this.tabControl1.SelectedTab = this.tabFiles;
            RefreshHashDctSiftCache();
        }
        private void btnClearGarbage_Click(object sender, EventArgs e)
        {
            btnClearGarbage.Enabled = false;
            DeleteAllGarbage();
            btnClearGarbage.Enabled = true;
        }




        private void btnClearDBRecord_Click(object sender, EventArgs e)
        {
            _myDb.ClearDBRecord();
            _myDb = new MyDb(Path.Combine(Application.StartupPath, "file_registry.db"));

            //数据库全清空了
            _fingerprintCache.Clear();        // 指纹缓存全部失效
            _deletedMetaList.Clear();         // 垃圾列表清空
            toolStripStatusLabelRecycleCount.Text = "垃圾:0/0";
        }


        private void btnClearDBStatus_Click(object sender, EventArgs e)
        {
            _myDb.ClearDBStatus();
            _deletedMetaList.Clear();         // 垃圾列表清空（数据库里没有垃圾了，内存也别留）
            toolStripStatusLabelRecycleCount.Text = $"垃圾:0/0";
        }
        private void btnDirectory_Click(object sender, EventArgs e)
        {
            directoryListControl1.Visible = !directoryListControl1.Visible;
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == toolStripStatusLabelFileName)
            {
                Clipboard.SetText(this.sCurrentFileName);
            }
        }

        private void btnESC_Click(object sender, EventArgs e)
        {
            Form1_KeyDown(this, new KeyEventArgs(Keys.Escape));
        }


        /// <summary>
        /// 保存当前正在播放的视频进度
        /// </summary>
        private void dg1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //ShowPicture();
        }

    }
}