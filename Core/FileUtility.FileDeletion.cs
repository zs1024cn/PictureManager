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
        public static ConcurrentQueue<string> _filesToDelete = new ConcurrentQueue<string>();
        private static bool _isDeleteConsuming = false;

        // ========== 删除队列 ==========

        public static void EnqueueDelete(string file, string tmpDirectory, MyDb db = null)
        {
            _filesToDelete.Enqueue(file);
            StartConsumer(tmpDirectory, db);
        }

        private static void StartConsumer(string tmpDirectory, MyDb db)
        {
            if (_isDeleteConsuming) return;
            _isDeleteConsuming = true;
            Task.Run(() => ConsumeLoop(tmpDirectory, db));
        }

        private static async Task ConsumeLoop(string tmpDirectory, MyDb db)
        {
            while (true)
            {
                var batch = new List<string>();
                while (_filesToDelete.TryDequeue(out string file))
                {
                    if (File.Exists(file))
                        batch.Add(file);
                }

                if (batch.Count > 0)
                {
                    batch = batch.Distinct().ToList();
                    for (int i = 0; i < batch.Count; i++)
                    {
                        try
                        {
                            string file = batch[i];
                            if (tmpDirectory != "")
                            {
                                string relativePath = file.Substring(Path.GetPathRoot(file).Length);
                                string destPath = Path.Combine(tmpDirectory, relativePath.TrimStart('\\'));
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                                File.Move(file, destPath);
                            }
                            else
                                File.Delete(file);
                        }
                        catch { }
                    }

                    var existingFiles = batch.Where(f => !File.Exists(f)).ToList();
                    if (existingFiles.Count > 0 && db != null)
                        db.SetRemoveTagBatchDeferred(existingFiles, 1);
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