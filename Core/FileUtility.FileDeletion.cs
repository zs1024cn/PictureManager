//Core\FileUtility.FileDeletion.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class FileUtility
    {
        public sealed class DeleteItem
        {
            public string File;
            public string TmpDirectory;
            public MyDb Db;
        }

        public static ConcurrentQueue<DeleteItem> _filesToDelete = new ConcurrentQueue<DeleteItem>();
        private static bool _isDeleteConsuming = false;
        public static volatile bool _hasDeleteBatch = false;

        // ========== 删除队列 ==========

        public static void EnqueueDelete(string file, string tmpDirectory, MyDb db = null)
        {
            _filesToDelete.Enqueue(new DeleteItem { File = file, TmpDirectory = tmpDirectory, Db = db });
            StartConsumer();
        }

        private static void StartConsumer()
        {
            if (_isDeleteConsuming) return;
            _isDeleteConsuming = true;
            Task.Run(() => ConsumeLoop());
        }

        private static async Task ConsumeLoop()
        {
            while (true)
            {
                var batch = new List<DeleteItem>();
                while (_filesToDelete.TryDequeue(out DeleteItem item))
                {
                    if (item != null && File.Exists(item.File))
                        batch.Add(item);
                }

                if (batch.Count > 0)
                {
                    _hasDeleteBatch = true;
                    try
                    {
                        var seen = new HashSet<string>();
                        batch = batch.Where(i => seen.Add(i.File)).ToList();
                        for (int i = 0; i < batch.Count; i++)
                        {
                            try
                            {
                                DeleteItem item = batch[i];
                                string file = item.File;
                                if (item.TmpDirectory != "")
                                {
                                    string relativePath = file.Substring(Path.GetPathRoot(file).Length);
                                    string destPath = Path.Combine(item.TmpDirectory, relativePath.TrimStart('\\'));
                                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                                    if (File.Exists(destPath))
                                        File.Delete(file);
                                    else
                                        File.Move(file, destPath);
                                }
                                else
                                    File.Delete(file);
                            }
                            catch { }
                        }

                        var existingFiles = batch.Where(i => !File.Exists(i.File)).ToList();
                        foreach (var group in existingFiles.GroupBy(i => i.Db))
                        {
                            if (group.Key != null)
                                group.Key.SetRemoveTagBatchDeferred(group.Select(i => i.File).ToList(), 1);
                        }
                    }
                    finally
                    {
                        _hasDeleteBatch = false;
                    }
                }
                else
                {
                    await Task.Delay(2000);
                    if (_filesToDelete.IsEmpty)
                    {
                        _isDeleteConsuming = false;
                        return;
                    }
                }
            }
        }

        // ========== 按标签删除目录 ==========

        public static void DeleteDirectoryByTag(List<string> checkedDirs, string sTag, string tmpDirectory)
        {
            var filesToDelete = new List<string>();

            foreach (string dir in checkedDirs)
            {
                try
                {
                    var subDirs = Directory.GetDirectories(dir, "*", System.IO.SearchOption.AllDirectories);
                    foreach (var subDir in subDirs)
                    {
                        string dirName = Path.GetFileName(subDir);
                        if (dirName.Contains(sTag))
                        {
                            var files = Directory.GetFiles(subDir, "*", System.IO.SearchOption.AllDirectories);
                            filesToDelete.AddRange(files);
                        }
                    }
                }
                catch { }
            }

            if (filesToDelete.Count == 0)
            {
                MessageBox.Show($"未找到包含 \"{sTag}\" 的目录");
                return;
            }

            var result = MessageBox.Show(
                $"找到 {filesToDelete.Count} 个文件（目录包含 \"{sTag}\"），确定删除？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                foreach (var file in filesToDelete)
                {
                    EnqueueDelete(file, tmpDirectory);
                }
            }
        }

        // ========== WebView2 缓存清理 ==========

        public static void CleanWebView2Cache()
        {
            try
            {
                string basePath = Application.StartupPath;

                string webview2Dir = Path.Combine(basePath, "PictureManager.exe.WebView2");
                CleanCacheSubDirs(webview2Dir);

                foreach (var dir in Directory.GetDirectories(basePath, "webview2Data_*"))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        private static void CleanCacheSubDirs(string basePath)
        {
            if (!Directory.Exists(basePath)) return;

            string[] cacheDirs =
            {
                "EBWebView\\Default\\Cache",
                "EBWebView\\Default\\Code Cache",
                "EBWebView\\Default\\GPUCache",
                "EBWebView\\Default\\Service Worker",
                "EBWebView\\Default\\Session Storage",
                "EBWebView\\Default\\blob_storage",
                "EBWebView\\GrShaderCache",
                "EBWebView\\ShaderCache",
                "EBWebView\\Crashpad"
            };

            foreach (var sub in cacheDirs)
            {
                string full = Path.Combine(basePath, sub);
                if (Directory.Exists(full))
                {
                    try { Directory.Delete(full, true); } catch { }
                }
            }
        }
    }
}