//git reset --hard <commit-hash>
//Core\FileUtility.cs
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PictureManager
{
    /// <summary>
    /// 文件/图像工具类。
    /// 拆分后：
    /// - HTML 生成 → File.ImageHtml.cs
    /// - 图像处理 → File.ImageProcessing.cs
    /// - 人脸马赛克 → File.ImageMask.cs
    /// - 缩放/重建 → File.ImageResize.cs
    /// - 删除队列 → File.FileDeletion.cs
    /// </summary>
    public partial class FileUtility
    {
        private static readonly Random Random = new Random();
        private static bool _cursorVisible = true;

        // ========== 图片读取 ==========

        public static Bitmap ReadPictureToBitmap(string file)
        {
            return ReadPictureToImage(file) as Bitmap;
        }

        public static Image ReadPictureToImage(string file, int size = -1)
        {
            if (string.IsNullOrEmpty(file)) return null;

            try
            {
                using (var src = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
                {
                    if (src.Empty()) return null;

                    byte[] data;
                    if (size <= 0)
                    {
                        data = src.ImEncode(".jpg");
                    }
                    else
                    {
                        using (var resized = new Mat())
                        {
                            int w, h;
                            if (src.Width > src.Height) { w = size; h = (int)(src.Height * (size / (double)src.Width)); }
                            else { h = size; w = (int)(src.Width * (size / (double)src.Height)); }
                            Cv2.Resize(src, resized, new OpenCvSharp.Size(w, h));
                            data = resized.ImEncode(".jpg");
                        }
                    }

                    using (var ms = new MemoryStream(data))
                    {
                        try
                        {
                            var bmp = new Bitmap(ms);
                            return bmp;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // ========== 光标控制 ==========

        public static void ShowCursor()
        {
            if (!_cursorVisible)
            {
                Cursor.Show();
                _cursorVisible = true;
            }
        }

        public static void HideCursor()
        {
            if (_cursorVisible)
            {
                Cursor.Hide();
                _cursorVisible = false;
            }
        }

        // ========== 日志 ==========

        public static void LogMethod(string msg, [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {method}: {msg}");
        }

        // ========== 文件名处理 ==========

        public static string FixName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return name;
            name = RemoveEmoji(name);
            string clean = name;
            string[] markers = { " - 技術討論區", " - 新時代的我們", " - 達蓋爾的旗幟", " _ 草榴社區", " - t66y.com" };
            foreach (var m in markers)
            {
                int idx = clean.IndexOf(m);
                if (idx >= 0) clean = clean.Substring(0, idx);
            }
            clean = clean.TrimEnd(' ', '_', '-');

            if (clean.Length > maxLen)
                clean = clean.Substring(0, maxLen);

            if (clean == name) return name;
            return clean + "_" + (char)Random.Next(0x4E00, 0x9FFF + 1);
        }

        public static string RemoveEmoji(string name)
        {
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore > 0 && lastUnderscore == name.Length - 2)
            {
                char lastChar = name[name.Length - 1];
                if (lastChar >= 0x4E00 && lastChar <= 0x9FFF)
                    name = name.Substring(0, lastUnderscore);
            }

            var sb = new StringBuilder();
            foreach (char c in name)
            {
                int code = (int)c;
                if (code >= 0x1F300 && code <= 0x1F9FF) continue;
                if (code >= 0x2600 && code <= 0x27BF) continue;
                if (code >= 0xFE00 && code <= 0xFE0F) continue;
                if (code == 0x200D) continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        // ========== 文件类型判断 ==========

        public static bool IsImageFile(string ext)
        {
            ext = ext.ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".jpe" || ext == ".jfif" || ext == ".png" ||
                   ext == ".gif" || ext == ".webp" || ext == ".bmp" || ext == ".dib" || ext == ".rle" ||
                   ext == ".tif" || ext == ".tiff" || ext == ".ico" || ext == ".wdp" || ext == ".jxr" ;
        }

        public static bool IsVideoFile(string ext)
        {
            ext = ext.ToLower();
            return ext == ".mp4" || ext == ".mpg" || ext == ".avi" || ext == ".m4v" || ext == ".m4a" ||
            ext == ".mov" || ext == ".mkv" || ext == ".wmv" || ext == ".ts" || ext == ".m2ts" ||
            ext == ".webm" || ext == ".3gp" || ext == ".flv" || ext == ".vob" || ext == ".ogv" ||
            ext == ".mp3" || ext == ".wav" || ext == ".flac" || ext == ".ogg" || ext == ".m4a" ||
            ext == ".wma" || ext == ".aac" || ext == ".opus" ;
        }

        public static bool IsTextFile(string ext)
        {
            ext = ext.ToLower();
            return ext == ".txt" || ext == ".html" || ext == ".htm";
        }

        // ========== 格式化 ==========

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        // ========== 系统操作 ==========

        public static void ExploreDirectory(string directory)
        {
            Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
        }

        public static void UnblockFile(string filePath)
        {
            string zoneIdentifier = filePath + ":Zone.Identifier";
            try
            {
                if (File.Exists(zoneIdentifier))
                {
                    File.Delete(zoneIdentifier);
                }
            }
            catch
            {
            }
        }

        public static float GetDiskUsage(string driveLetter)
        {
            try
            {
                using (var diskCounter = new PerformanceCounter("LogicalDisk", "% Disk Time", $"{driveLetter}:"))
                {
                    diskCounter.NextValue();
                    Thread.Sleep(100);
                    return diskCounter.NextValue();
                }
            }
            catch
            {
                return 50;
            }
        }

        // ========== 相对路径 ==========

        public static string GetRelativePath(string fullPath)
        {
            string root = Path.GetPathRoot(fullPath);
            string relative = fullPath.Substring(root.Length);

            int firstSlash = relative.IndexOf('\\');
            if (firstSlash >= 0 && firstSlash < relative.Length - 1)
                return relative.Substring(firstSlash + 1);

            return "";
        }
    }
}