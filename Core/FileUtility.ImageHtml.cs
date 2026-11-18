//Core\FileUtility.ImageHtml.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using OpenCvSharp;

namespace PictureManager
{
    public partial class FileUtility
    {
        // ========== HTML 生成 ==========

        public static string GenerateImageHtml_Simple(string imagePath, int maxWidth, int maxHeight)
        {
            string fileUrl = new Uri(imagePath).AbsoluteUri;

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><style>
*{{margin:0;padding:0}}
html,body{{width:100%;height:100vh;display:flex;justify-content:center;align-items:center;background:#000;overflow:hidden}}
img{{width:100%;height:100vh;object-fit:contain}}
</style></head>
<body><img src='{fileUrl}'/></body>
</html>";
        }

        public static string GenerateImageHtmlWithBase64(string imagePath, int maxWidth, int maxHeight)
        {
            // 先尝试 OpenCV
            try
            {
                byte[] fileData = File.ReadAllBytes(imagePath);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (!mat.Empty())
                    {
                        Mat converted;
                        if (mat.Channels() == 4)
                        {
                            converted = new Mat();
                            Cv2.CvtColor(mat, converted, ColorConversionCodes.BGRA2BGR);
                        }
                        else if (mat.Channels() == 1)
                        {
                            converted = new Mat();
                            Cv2.CvtColor(mat, converted, ColorConversionCodes.GRAY2BGR);
                        }
                        else
                        {
                            converted = mat.Clone();
                        }

                        using (converted)
                        {
                            byte[] jpegBytes = EncodeViewportJpeg(converted, maxWidth, maxHeight);
                            string base64 = Convert.ToBase64String(jpegBytes);

                            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><style>
*{{margin:0;padding:0}}
html,body{{width:100%;height:100vh;display:flex;justify-content:center;align-items:center;background:#000;overflow:hidden}}
img{{width:100%;height:100vh;object-fit:contain}}
</style></head>
<body><img src='data:image/jpeg;base64,{base64}'/></body>
</html>";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenCV 转换失败: {ex.Message}");
            }

            // OpenCV 失败，尝试 GDI+
            try
            {
                using (var bmp = new Bitmap(imagePath))
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    string base64 = Convert.ToBase64String(ms.ToArray());
                    return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><style>
*{{margin:0;padding:0}}
html,body{{width:100%;height:100vh;display:flex;justify-content:center;align-items:center;background:#000;overflow:hidden}}
img{{width:100%;height:100vh;object-fit:contain}}
</style></head>
<body><img src='data:image/jpeg;base64,{base64}'/></body>
</html>";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GDI+ 转换失败: {ex.Message}");
            }

            return GenerateImageHtml_Simple(imagePath, maxWidth, maxHeight);
        }
        /// <summary>
        /// GIF / APNG / 动画 WebP 不能被重编码，否则会变成静帧。
        /// </summary>
        private static bool LooksAnimated(byte[] data)
        {
            if (data == null || data.Length < 16) return false;

            // GIF
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) return true;

            int n = Math.Min(data.Length, 8192);
            string head = Encoding.ASCII.GetString(data, 0, n);

            // APNG：PNG 签名 + acTL 块
            bool isPng = data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
            if (isPng && head.Contains("acTL")) return true;

            // 动画 WebP：RIFF....WEBP + ANIM 块
            bool isWebp = data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                          && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50;
            if (isWebp && head.Contains("ANIM")) return true;

            return false;
        }

        /// <summary>
        /// JPEG 的 4 通道是 CMYK，不是 alpha。
        /// </summary>
        private static bool IsJpeg(byte[] data)
        {
            return data != null && data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8;
        }

        /// <summary>
        /// 缩到视口后编码；keepAlpha 为 true 时输出 PNG（保留透明），否则输出 JPEG。
        /// </summary>
        private static byte[] EncodeViewportJpeg(Mat src, int maxWidth, int maxHeight, bool keepAlpha = false)
        {
            Mat work = src;
            Mat resized = null;
            Mat converted = null;
            try
            {
                if (maxWidth > 0 && maxHeight > 0 && src.Width > 0 && src.Height > 0)
                {
                    double k = Math.Min(1.0, Math.Min((double)maxWidth / src.Width, (double)maxHeight / src.Height));
                    if (k < 0.999)
                    {
                        resized = new Mat();
                        Cv2.Resize(src, resized,
                            new OpenCvSharp.Size(
                                Math.Max(1, (int)Math.Round(src.Width * k)),
                                Math.Max(1, (int)Math.Round(src.Height * k))),
                            0, 0, InterpolationFlags.Area);
                        work = resized;
                    }
                }

                if (keepAlpha)
                {
                    if (work.Channels() == 3)
                    {
                        converted = new Mat();
                        Cv2.CvtColor(work, converted, ColorConversionCodes.BGR2BGRA);
                        work = converted;
                    }
                    else if (work.Channels() == 1)
                    {
                        converted = new Mat();
                        Cv2.CvtColor(work, converted, ColorConversionCodes.GRAY2BGRA);
                        work = converted;
                    }
                    return work.ImEncode(".png");
                }

                if (work.Channels() == 4)
                {
                    converted = new Mat();
                    Cv2.CvtColor(work, converted, ColorConversionCodes.BGRA2BGR);
                    work = converted;
                }
                else if (work.Channels() == 1)
                {
                    converted = new Mat();
                    Cv2.CvtColor(work, converted, ColorConversionCodes.GRAY2BGR);
                    work = converted;
                }

                var parameters = new ImageEncodingParam[]
                {
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, 88),
                    new ImageEncodingParam(ImwriteFlags.JpegProgressive, 0),
                    new ImageEncodingParam(ImwriteFlags.JpegOptimize, 1)
                };
                return work.ImEncode(".jpg", parameters);
            }
            finally
            {
                converted?.Dispose();
                resized?.Dispose();
            }
        }

        /// <summary>
        /// M 模式：静态图缩到视口再编码；动画图原样 base64；解码失败退回原始字节。
        /// </summary>
        public static string GenerateImageHtmlWithScaledBase64(byte[] data, string mime, int maxWidth, int maxHeight, int ratio = 100)
        {
            if (data == null || data.Length == 0)
                return GenerateImageHtmlWithBase64Direct("", mime, ratio);

            // 动画类保持原样，让 Chrome 自己播放
            if (LooksAnimated(data))
                return GenerateImageHtmlWithBase64Direct(Convert.ToBase64String(data), mime, ratio);

            try
            {
                var flags = IsJpeg(data) ? ImreadModes.Color : ImreadModes.Unchanged;
                using (var mat = Cv2.ImDecode(data, flags))
                {
                    if (!mat.Empty())
                    {
                        bool keepAlpha = mat.Channels() == 4;
                        byte[] encoded = EncodeViewportJpeg(mat, maxWidth, maxHeight, keepAlpha);
                        string outMime = keepAlpha ? "image/png" : "image/jpeg";
                        return GenerateImageHtmlWithBase64Direct(Convert.ToBase64String(encoded), outMime, ratio);
                    }
                }
            }
            catch { }

            return GenerateImageHtmlWithBase64Direct(Convert.ToBase64String(data), mime, ratio);
        }
        public static string GenerateImageHtmlWithBase64Direct(string base64, string mime, int ratio = 100)
        {
            float scale = ratio / 100f;
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><style>
*{{margin:0;padding:0}}
html,body{{width:100%;height:100vh;display:flex;justify-content:center;align-items:center;background:#000;overflow:auto}}
img{{width:100%;height:100vh;object-fit:contain;transform:scale({scale});transform-origin:center center;}}
</style></head>
<body><img src='data:{mime};base64,{base64}'/></body>
</html>";
        }

        public static string GetMimeType(string ext)
        {
            switch (ext)
            {
                case ".webp": return "image/webp";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                default: return "image/jpeg";
            }
        }

        // ========== 文本 HTML ==========

        public static string GenerateTextHtml(string filePath)
        {
            try
            {
                string content = ReadTextFileWithAutoEncoding(filePath);
                content = System.Net.WebUtility.HtmlEncode(content);

                return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            background-color: #475;
            color: #fff;
            font-size: 40px;
            font-family: 'Segoe UI', '微软雅黑', sans-serif;
            padding: 20px;
            line-height: 1.8;
            white-space: pre-wrap;
            word-wrap: break-word;
            margin: 0;
        }}
    </style>
</head>
<body>{content}</body>
</html>";
            }
            catch
            {
                return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'></head>
<body style='background:#000;color:#fff;'>无法读取文件</body>
</html>";
            }
        }

        public static string ReadTextFileWithAutoEncoding(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
            }

            if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(fileBytes, 2, fileBytes.Length - 2);
            }

            if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(fileBytes, 2, fileBytes.Length - 2);
            }

            if (IsValidUtf8(fileBytes))
            {
                return Encoding.UTF8.GetString(fileBytes);
            }

            return Encoding.Default.GetString(fileBytes);
        }

        public static bool IsValidUtf8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                byte b = bytes[i];

                if (b <= 0x7F)
                {
                    i++;
                }
                else if (b >= 0xC2 && b <= 0xDF)
                {
                    if (i + 1 >= bytes.Length) return false;
                    if ((bytes[i + 1] & 0xC0) != 0x80) return false;
                    i += 2;
                }
                else if (b >= 0xE0 && b <= 0xEF)
                {
                    if (i + 2 >= bytes.Length) return false;
                    if ((bytes[i + 1] & 0xC0) != 0x80) return false;
                    if ((bytes[i + 2] & 0xC0) != 0x80) return false;
                    i += 3;
                }
                else if (b >= 0xF0 && b <= 0xF4)
                {
                    if (i + 3 >= bytes.Length) return false;
                    if ((bytes[i + 1] & 0xC0) != 0x80) return false;
                    if ((bytes[i + 2] & 0xC0) != 0x80) return false;
                    if ((bytes[i + 3] & 0xC0) != 0x80) return false;
                    i += 4;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}