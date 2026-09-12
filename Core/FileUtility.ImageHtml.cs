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
            string fileUrl = "file:///" + Uri.EscapeUriString(imagePath.Replace("\\", "/"));

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
                            var parameters = new ImageEncodingParam[]
                            {
                                new ImageEncodingParam(ImwriteFlags.JpegQuality, 95),
                                new ImageEncodingParam(ImwriteFlags.JpegProgressive, 0),
                                new ImageEncodingParam(ImwriteFlags.JpegOptimize, 1)
                            };
                            byte[] jpegBytes = converted.ImEncode(".jpg", parameters);
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