//Forms\Form1.FileOps.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace PictureManager
{
    public partial class Form1
    {
        // 文件操作相关方法（从 Form1.cs 移入）
        private void btnDelete()
        {
            StopCurrentVideo();

            if (this.dg1.Rows.Count < 1) return;

            int r = this.dg1.CurrentRow.Index;
            string sFileName = this.dg1.Rows[r].Cells["Name"].Value.ToString();

            string ext = Path.GetExtension(sFileName);

            // 如果是当前正在播放的视频，先停止
            if (FileUtility.IsVideoFile(ext) && sFileName == sCurrentFileName)
            {
                _videoLoadCts?.Cancel();
                timer1.Stop();
                SaveCurrentVideoProgress();
                _currentVideoFingerprint = null;
                _mediaPlayer?.Stop();
                videoView1.MediaPlayer = null;
                videoView1.Visible = false;
                trackBar1.Visible = false;
            }

            if (DeleteSingleFile(sFileName, null))
            {
                sCurrentFileName = "";
                RemoveDatagridRows(r);
                ShowPicture();
            }
        }

        private bool DeleteSingleFile(string sFileName, string cachedFingerprint = null)
        {
            try
            {
                string fingerprint = cachedFingerprint ?? GetFileFingerprintCached(sFileName);
                _fileMeta.Remove(fingerprint);
                FileSystem.DeleteFile(sFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                // ⭐ 标记数据库：文件已删除（status=1，垃圾）
                // _myDb.SetRemoveTag(sFileName, 1);

                return true;
            }
            catch
            {
                return false;
            }
        }
        private void DeleteDirectoryFiles(bool MarkGarbage)
        {
            // 删除当前文件所在目录的所有文件
            if (dg1.CurrentRow == null) return;

            string currentFile = dg1.CurrentRow.Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(currentFile)) return;

            dg1.CurrentCellChanged -= dg1_CurrentCellChanged;

            string dir = Path.GetDirectoryName(currentFile);

            // 收集该目录下所有在 dg1 中的文件
            var filesToDelete = new List<string>();

            string sLog = "";
            for (int i = dg1.Rows.Count - 1; i >= 0; i--)
            {
                string filePath = dg1.Rows[i].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrEmpty(filePath) && Path.GetDirectoryName(filePath) == dir)
                {
                    sLog += i.ToString() + ",";
                    filesToDelete.Add(filePath);
                    if (MarkGarbage)
                    {
                        string meta = dg1.Rows[i].Cells["Meta"].Value?.ToString();
                        if (!string.IsNullOrEmpty(meta))
                            _deletedMetaList.Add(meta);

                    }
                    RemoveDatagridRows(i);
                }
            }
            if (filesToDelete.Contains(sCurrentFileName))
                StopCurrentVideo();

            BackgroundDeleteFiles(filesToDelete, null, MarkGarbage);

            AlbumForm.Clear(dir);

            sCurrentFileName = "";
            ShowPicture();
            dg1.CurrentCellChanged += dg1_CurrentCellChanged;
        }


        private void RemoveDatagridRows(int r)
        {
            string file = dg1.Rows[r].Cells["Name"].Value?.ToString();
            if (!string.IsNullOrEmpty(file))
            {
                FeatureCacheManager.Instance.MoveToRecycle(file);
            }
            dg1.Rows.RemoveAt(r);
        }
        private void RemoveDatagridRowsbyName(string file)
        {
            for (int i = dg1.Rows.Count - 1; i >= 0; i--)
            {
                string filename = dg1.Rows[i].Cells["Name"].Value?.ToString();
                if (filename == file)
                {
                    RemoveDatagridRows(i);
                    break;
                }
            }
        }
        private void RemoveDatagridRowsByNames(List<string> files)
        {
            var filesToRemove = new HashSet<string>(files);
            int removed = 0;

            for (int i = dg1.Rows.Count - 1; i >= 0; i--)
            {
                string name = dg1.Rows[i].Cells["Name"].Value?.ToString();
                if (name != null && filesToRemove.Contains(name))
                {
                    // 同步移缓存
                    FeatureCacheManager.Instance.MoveToRecycle(name);
                    dg1.Rows.RemoveAt(i);
                    removed++;
                }
            }
        }
        /// <summary>
        /// 后台批量删除文件，自适应 I/O 压力，同时记录已删除的 Meta
        /// </summary>
        private void BackgroundDeleteFiles(List<string> fileList, List<DataGridViewRow> rows = null, bool markGarbage = true)
        {
            if (fileList == null || fileList.Count == 0) return;
            foreach (var file in fileList)
            {
                FileUtility.EnqueueDelete(file, _TmpDirectory, _myDb);
            }

            return;
        }

        /// <summary>
        /// 遍历所有目录，删除 _deletedMetaList 中存在的文件（不需要 dg1）
        /// </summary>
        private void DeleteAllGarbage()
        {
            var result = MessageBox.Show(
"确定要 扫描磁盘 ，删除重复指纹的文件，删除被标记为垃圾的文件 吗？",
"MediaManager2026",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question);
            if (result == DialogResult.No)
                return;

            this.toolStripStatusLabelStatus.Text = "正在扫描所有目录...";

            var checkedDirs = directoryListControl1.CheckedDirectories;
            int iMaxCount = int.Parse(tbPageRows.Text) * 10;
            var filesToDelete = new ConcurrentBag<string>();

            Task.Run(() =>
            {
                foreach (string dir in checkedDirs)
                {
                    // ⭐ 第一步：收集所有文件路径
                    var allFiles = Directory.EnumerateFiles(dir, "*", System.IO.SearchOption.AllDirectories)
                                             .Take(iMaxCount)
                                             .ToArray();

                    if (allFiles.Length == 0) continue;

                    // ⭐ 第二步：批量查数据库指纹（一次 SQL，无锁）
                    var dbMetas = _myDb.GetMeta(allFiles);

                    // ⭐ 第三步：收集所有已知的 hash，批量查哪些是垃圾
                    var garbageHashes = _myDb.GetAllGarbageHashes();

                    // ⭐ 第四步：并行扫描
                    int scanned = 0;
                    Parallel.ForEach(allFiles,
                        new ParallelOptions { MaxDegreeOfParallelism = 8 },
                        file =>
                        {
                            int current = Interlocked.Increment(ref scanned);
                            if (current % 1000 == 0)
                            {
                                BeginInvoke(new Action(() =>
                                {
                                    this.toolStripStatusLabelStatus.Text = $"扫描中: {current}/{allFiles.Length}";
                                }));
                            }

                            try
                            {
                                string meta = null;

                                // 1. 内存缓存
                                if (!_fingerprintCache.TryGetValue(file, out meta))
                                {
                                    // 2. 数据库（批量查的结果）
                                    if (dbMetas.TryGetValue(file, out byte[] hash) && hash != null)
                                    {
                                        meta = Convert.ToBase64String(hash);
                                        _fingerprintCache.TryAdd(file, meta);
                                    }
                                    // 3. 读磁盘
                                    else
                                    {
                                        meta = GetFileFingerprint(file);
                                        if (!string.IsNullOrEmpty(meta))
                                        {
                                            _fingerprintCache.TryAdd(file, meta);
                                            _myDb.AddMetaDeferred(file, Convert.FromBase64String(meta));
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(meta)) return;

                                // ⭐ 判断是否垃圾：内存 + 数据库
                                if (_deletedMetaList.Contains(meta))
                                {
                                    filesToDelete.Add(file);
                                }
                                else if (this.cb删除文件扩大化.Checked && garbageHashes.Contains(meta))
                                {
                                    filesToDelete.Add(file);
                                }
                            }
                            catch { }
                        });
                }

                if (filesToDelete.IsEmpty)
                {
                    BeginInvoke(new Action(() =>
                    {
                        this.toolStripStatusLabelStatus.Text = "未找到废品文件";
                        btnCalc.Enabled = true;
                    }));
                    return;
                }
                
                if (filesToDelete.Contains(sCurrentFileName))
                    StopCurrentVideo();

                // ⭐ 第五步：删除文件
                int total = filesToDelete.Count;
                var fileList = filesToDelete.ToList();
                foreach (var file in fileList)
                {
                    FileUtility.EnqueueDelete(file, _TmpDirectory, _myDb);
                }
                _myDb.FlushBatch();
                _myDb.SetRemoveTagBatchDeferred(fileList, 1);
                BeginInvoke(new Action(() =>
                {
                    this.toolStripStatusLabelStatus.Text = $"清理完成: {total} 个废品文件已删除";
                    MessageBox.Show($"清理完成: {total} 个废品文件已删除");
                    btnLoadView_Click(this, EventArgs.Empty);
                }));
            });
        }
        /// <summary>
        /// 从 dg1 中收集 Meta 在 _deletedMetaList 中的文件路径
        /// </summary>
        private List<string> GetFilesFromDeletedMetaList()
        {
            var result = new List<string>();

            for (int i = dg1.Rows.Count - 1; i >= 0; i--)
            {
                string meta = dg1.Rows[i].Cells["Meta"].Value?.ToString();
                string filePath = dg1.Rows[i].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrEmpty(meta) && !string.IsNullOrEmpty(filePath) && _deletedMetaList.Contains(meta))
                {
                    result.Add(filePath);
                    RemoveDatagridRows(i);
                }
            }

            return result;
        }


        private void btnNameSize_Click(object sender, EventArgs e)
        {
            var filesToDelete = new List<string>();  // 后台删除列表

            dg1.CurrentCellChanged -= dg1_CurrentCellChanged;

            if (this.cb删除文件扩大化.Checked && dg1.Columns.Contains("Meta") && _MetaEnabled)
                filesToDelete.AddRange(GetFilesFromDeletedMetaList());

            var seenKeys = new HashSet<string>();
            for (int i = dg1.Rows.Count - 1; i >= 0; i--)
            {
                string file = dg1.Rows[i].Cells["Name"].Value?.ToString();
                if (string.IsNullOrEmpty(file)) continue;

                string key;
                if (_MetaEnabled)
                {
                    string meta = dg1.Rows[i].Cells["Meta"].Value?.ToString();
                    if (string.IsNullOrEmpty(meta)) continue;
                    key = meta;
                }
                else
                {
                    var sizeObj = dg1.Rows[i].Cells["Size"].Value;
                    if (sizeObj == null) continue;
                    long size = (long)sizeObj;
                    string name = Path.GetFileName(file);
                    key = $"{name}|{size}";
                }
                if (!seenKeys.Add(key))
                {
                    filesToDelete.Add(file);
                    RemoveDatagridRows(i);
                    if (filesToDelete.Count % 100 == 0)
                    {
                        log($"待删除队列{filesToDelete.Count}");
                        Application.DoEvents();
                    }
                }
            }
            if (filesToDelete.Contains(sCurrentFileName))
                StopCurrentVideo();

            //this.dg1.Sort(this.dg1.Columns["Date"], ListSortDirection.Descending);
            // ⭐ 暂时取消事件绑定
            if (this.dg1.Rows.Count > 0)
                this.dg1.CurrentCell = this.dg1.Rows[0].Cells["ID"];
            this.toolStripStatusLabelRecycleCount.Text = _deletedMetaList.Count.ToString();

            sCurrentFileName = "";
            ShowPicture();
            dg1.CurrentCellChanged += dg1_CurrentCellChanged;
            dg1.Focus();

            BackgroundDeleteFiles(filesToDelete, null, false);
        }


        /// <summary>
        /// 使用 SIFT 特征查找重复图片（包括裁剪、旋转、缩放）
        /// </summary>
        private void btnResize_Click(object sender, EventArgs e)
        {
            string sFileName;
            long iSumResizeSize = 0;
            int iFileResizeCount = 0;

            int iMaxSize = int.Parse(this.tbMaxSize.Text);
            int iMinSize = int.Parse(this.tbMinSize.Text);
            for (int i = 0; i < this.dg1.Rows.Count; i++)
            {
                sFileName = this.dg1.Rows[i].Cells["Name"].Value.ToString();
                string ext = Path.GetExtension(sFileName).ToLower();

                if (ext == ".webp") continue;  // 跳过 WebP

                int iFileSize = int.Parse(this.dg1.Rows[i].Cells["Size"].Value.ToString());
                sFileName = this.dg1.Rows[i].Cells["Name"].Value.ToString();
                if (iFileSize > iMaxSize)
                {

                    FileInfo fiOld = new FileInfo(sFileName);
                    iSumResizeSize += fiOld.Length;

                    FileUtility.ResizeImage(sFileName);

                    fiOld.Refresh();

                    iSumResizeSize -= fiOld.Length;
                    iFileResizeCount++;
                }
            }
            MessageBox.Show("共缩小" + iFileResizeCount.ToString() + "个文件，" + iSumResizeSize.ToString("N") + " byte");
        }

        private void btnDeleteMinFile_Click(object sender, EventArgs e)
        {
            dg1.CurrentCellChanged -= dg1_CurrentCellChanged;

            long iSumDeleteSize = 0;
            int iFileDeleteCount = 0;
            double MinSize = double.Parse(this.tbMinSize.Text);
            var fileList = new List<string>();

            for (int i = this.dg1.Rows.Count - 1; i >= 0; i--)
            {
                if (dg1.Rows[i].Cells["Size"].Value == null) continue;

                int iFileSize = int.Parse(this.dg1.Rows[i].Cells["Size"].Value.ToString());
                string sFileName = this.dg1.Rows[i].Cells["Name"].Value.ToString();

                if (iFileSize < MinSize)
                {
                    iSumDeleteSize += iFileSize;
                    iFileDeleteCount++;

                    try
                    {
                        if (sFileName == sCurrentFileName) StopCurrentVideo();

                        string fingerprint = GetFileFingerprintCached(sFileName);
                        _fileMeta.Remove(fingerprint);
                        //FileSystem.DeleteFile(sFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                        // ⭐ 程序删 → 写数据库
                        //_myDb.SetRemoveTag(sFileName, 1);

                        FileUtility.EnqueueDelete(sFileName, _TmpDirectory, _myDb);
                        fileList.Add(sFileName);

                        RemoveDatagridRows(i);
                    }
                    catch { }
                }
            }
            _myDb.SetRemoveTagBatchDeferred(fileList, 1);

            sCurrentFileName = "";
            ShowPicture();
            dg1.CurrentCellChanged += dg1_CurrentCellChanged;

            MessageBox.Show("共删除" + iFileDeleteCount.ToString() + "个文件，" + iSumDeleteSize.ToString("N") + " byte");
        }
        /// <summary>
        /// 将cache里，已经不存在dg1的部分，转入deletecache里。
        /// </summary>
        private void RefreshHashDctSiftCache()
        {
            var currentFiles = new HashSet<string>();
            for (int r = 0; r < dg1.Rows.Count; r++)
            {
                string f = dg1.Rows[r].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrEmpty(f)) currentFiles.Add(f);
            }

            FeatureCacheManager.Instance.KeepOnly(currentFiles);
            GC.Collect();
        }


    }
}