using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;

namespace PictureManager
{
    public partial class FileUtility
    {
        private static Vec3b _currentBorderColor = new Vec3b();

        // ========== 缩放 ==========

        public static void ResizeImage(string sFilename)
        {
            string tempFile = null;
            try
            {
                using (Bitmap bmp = new Bitmap(sFilename))
                using (Bitmap bmNew = ZoomP(bmp, 0.5f, 0.5f))
                {
                    tempFile = sFilename + ".tmp";
                    Compress(bmNew, tempFile, 50);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string trashFile = sFilename + ".old";
                File.Move(sFilename, trashFile);
                File.Move(tempFile, sFilename);
                File.Delete(trashFile);
            }
            catch (Exception e)
            {
                Console.Write($"ResizeImage error: {e.Message}");
            }
        }

        public static Bitmap ZoomP(Bitmap a, float s, float v)
        {
            Bitmap bmp = new Bitmap((int)(a.Width * s), (int)(a.Height * v), a.PixelFormat);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.ScaleTransform(s, v);
                g.DrawImage(a, 0, 0, a.Width, a.Height);
            }
            return bmp;
        }

        // ========== 压缩 ==========

        public static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public static void Compress(Bitmap srcBitmap, Stream destStream, long level)
        {
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            myImageCodecInfo = GetEncoderInfo("image/jpeg");
            myEncoder = System.Drawing.Imaging.Encoder.Quality;
            myEncoderParameters = new EncoderParameters(1);

            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);
        }

        public static void Compress(Bitmap srcBitMap, string destFile, long level)
        {
            using (Stream s = new FileStream(destFile, FileMode.Create))
            {
                Compress(srcBitMap, s, level);
            }
        }

        // ========== 重建（去边框 + 去竖条 + 缩放）==========

        public static void RebuildPicture(string file, int MBSize = 2)
        {
            try
            {
                var fi = new FileInfo(file);
                bool needResize = fi.Length > MBSize * 1024 * 1024;

                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat.Empty()) return;

                    var cropped = CropBorder(mat);
                    cropped = RemoveMiddleBar(cropped);

                    if (needResize)
                    {
                        int newWidth = cropped.Width / 2;
                        int newHeight = cropped.Height / 2;
                        using (var resized = new Mat())
                        {
                            Cv2.Resize(cropped, resized, new OpenCvSharp.Size(newWidth, newHeight));
                            SaveAndReplace(resized, file);
                        }
                    }
                    else
                    {
                        SaveAndReplace(cropped, file);
                    }

                    cropped.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RebuildPicture 失败: {file}, {ex.Message}");
            }
        }

        private static Mat RemoveMiddleBar(Mat mat)
        {
            int w = mat.Width, h = mat.Height;
            int midX = w / 2;

            var c1 = mat.At<Vec3b>(h / 4, midX);
            var c2 = mat.At<Vec3b>(h / 2, midX);
            var c3 = mat.At<Vec3b>(h * 3 / 4, midX);
            var c4 = mat.At<Vec3b>(h - 1, midX);

            if (!IsSimilar(c1, c2, 3) || !IsSimilar(c1, c3, 3) || !IsSimilar(c1, c4, 3))
                return mat;

            int left = midX;
            while (left > 0)
            {
                var cl1 = mat.At<Vec3b>(h / 4, left - 1);
                var cl2 = mat.At<Vec3b>(h / 2, left - 1);
                var cl3 = mat.At<Vec3b>(h * 3 / 4, left - 1);
                var cl4 = mat.At<Vec3b>(h - 1, left - 1);
                if (!IsSimilar(cl1, c1, 3) || !IsSimilar(cl2, c1, 3) || !IsSimilar(cl3, c1, 3) || !IsSimilar(cl4, c1, 3))
                    break;
                left--;
            }

            int right = midX;
            while (right < w - 1)
            {
                var cr1 = mat.At<Vec3b>(h / 4, right + 1);
                var cr2 = mat.At<Vec3b>(h / 2, right + 1);
                var cr3 = mat.At<Vec3b>(h * 3 / 4, right + 1);
                var cr4 = mat.At<Vec3b>(h - 1, right + 1);
                if (!IsSimilar(cr1, c1, 3) || !IsSimilar(cr2, c1, 3) || !IsSimilar(cr3, c1, 3) || !IsSimilar(cr4, c1, 3))
                    break;
                right++;
            }

            if (left == midX && right == midX) return mat;

            int barWidth = right - left + 1;
            int rightPartWidth = w - right - 1;
            if (rightPartWidth <= 0) return mat;

            Rect rightPart = new Rect(right + 1, 0, rightPartWidth, h);
            using (var rightMat = new Mat(mat, rightPart))
            {
                rightMat.CopyTo(new Mat(mat, new Rect(left, 0, rightPartWidth, h)));
            }

            int newWidth = left + rightPartWidth;
            if (newWidth < w)
            {
                Rect cropRect = new Rect(0, 0, newWidth, h);
                return new Mat(mat, cropRect).Clone();
            }

            return mat;
        }

        private static void SaveAndReplace(Mat mat, string file)
        {
            string tempFile = file + ".tmp";
            byte[] data = mat.ImEncode(".jpg");
            File.WriteAllBytes(tempFile, data);
            File.Delete(file);
            File.Move(tempFile, file);
        }

        private static Mat CropBorder(Mat mat)
        {
            _currentBorderColor = new Vec3b();

            int top = FindBorderTop(mat);
            int bottom = FindBorderBottom(mat);
            int left = FindBorderLeft(mat);
            int right = FindBorderRight(mat);

            if (top == 0 && bottom == mat.Height - 1 && left == 0 && right == mat.Width - 1)
                return mat.Clone();

            Rect roi = new Rect(left, top, right - left + 1, bottom - top + 1);
            return new Mat(mat, roi);
        }

        private static int FindBorderTop(Mat mat)
        {
            int w = mat.Width;
            for (int y = 0; y < mat.Height; y++)
            {
                var c1 = mat.At<Vec3b>(y, w / 2);
                var c2 = mat.At<Vec3b>(y, w / 4);
                var c3 = mat.At<Vec3b>(y, w * 3 / 4);
                if (!IsSimilar(c1, c2) || !IsSimilar(c1, c2))
                    return y;
            }
            return mat.Height - 1;
        }

        private static int FindBorderBottom(Mat mat)
        {
            int w = mat.Width;
            for (int y = mat.Height - 1; y >= 0; y--)
            {
                var c1 = mat.At<Vec3b>(y, w / 2);
                var c2 = mat.At<Vec3b>(y, w / 4);
                var c3 = mat.At<Vec3b>(y, w * 3 / 4);
                if (!IsSimilar(c1, c2) || !IsSimilar(c1, c2))
                    return y;
            }
            return 0;
        }

        private static int FindBorderLeft(Mat mat)
        {
            int h = mat.Height;
            for (int x = 0; x < mat.Width; x++)
            {
                var c1 = mat.At<Vec3b>(h / 2, x);
                var c2 = mat.At<Vec3b>(h / 4, x);
                var c3 = mat.At<Vec3b>(h * 3 / 4, x);
                if (!IsSimilar(c1, c2) || !IsSimilar(c1, c2))
                    return x;
            }
            return mat.Width - 1;
        }

        private static int FindBorderRight(Mat mat)
        {
            int h = mat.Height;
            for (int x = mat.Width - 1; x >= 0; x--)
            {
                var c1 = mat.At<Vec3b>(h / 2, x);
                var c2 = mat.At<Vec3b>(h / 4, x);
                var c3 = mat.At<Vec3b>(h * 3 / 4, x);
                if (!IsSimilar(c1, c2) || !IsSimilar(c1, c2))
                    return x;
            }
            return 0;
        }

        private static bool IsSimilar(Vec3b a, Vec3b b, int diff = 10)
        {
            return Math.Abs(a.Item0 - b.Item0) <= diff &&
                   Math.Abs(a.Item1 - b.Item1) <= diff &&
                   Math.Abs(a.Item2 - b.Item2) <= diff;
        }
    }
}