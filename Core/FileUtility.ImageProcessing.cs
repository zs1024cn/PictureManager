using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using System.Windows.Forms;
namespace PictureManager
{
    public partial class FileUtility
    {
        // ========== 通透/清晰化 ==========

        public static void MakeClear(string file)
        {
            using (var src = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (src.Empty()) return;

                using (var result = MakeClearInternal(src))
                {
                    byte[] jpegBytes = result.ImEncode(".jpg");
                    File.WriteAllBytes(file, jpegBytes);
                }
            }
        }

        private static Mat MakeClearInternal(Mat src)
        {
            Mat gammaCorrected = GammaCorrection(src, 0.85);
            Mat saturated = EnhanceSaturation(gammaCorrected, 1.05);
            gammaCorrected.Dispose();
            Mat sharpened = Sharpen(saturated, 0.2);
            saturated.Dispose();
            return sharpened;
        }

        private static Mat GammaCorrection(Mat src, double gamma)
        {
            Mat result = new Mat();

            using (var lookUpTable = new Mat(1, 256, MatType.CV_8U))
            {
                for (int i = 0; i < 256; i++)
                {
                    lookUpTable.At<byte>(0, i) = (byte)Math.Min(255,
                        (int)(Math.Pow(i / 255.0, gamma) * 255.0));
                }

                Cv2.LUT(src, lookUpTable, result);
            }

            return result;
        }

        private static Mat EnhanceSaturation(Mat src, double saturationScale)
        {
            using (var hsv = new Mat())
            {
                Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

                Mat[] channels = Cv2.Split(hsv);

                Mat saturated = new Mat();
                channels[1].ConvertTo(saturated, -1, saturationScale, 0);
                channels[1].Dispose();
                channels[1] = saturated;

                Cv2.Merge(channels, hsv);

                Mat result = new Mat();
                Cv2.CvtColor(hsv, result, ColorConversionCodes.HSV2BGR);

                foreach (var ch in channels) ch.Dispose();

                return result;
            }
        }

        private static Mat Sharpen(Mat src, double amount)
        {
            using (var blur = new Mat())
            {
                Cv2.GaussianBlur(src, blur, new OpenCvSharp.Size(0, 0), 3);

                Mat result = new Mat();
                Cv2.AddWeighted(src, 1 + amount, blur, -amount, 0, result);

                return result;
            }
        }

        // ========== 风格化 ==========

        public static void ApplySharpen(string file)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;
                using (var blur = new Mat())
                {
                    Cv2.GaussianBlur(img, blur, new OpenCvSharp.Size(0, 0), 3);
                    using (var result = new Mat())
                    {
                        Cv2.AddWeighted(img, 1.5, blur, -0.5, 0, result);
                        byte[] jpegBytes = img.ImEncode(".jpg");
                        File.WriteAllBytes(file, jpegBytes);
                    }
                }
            }
        }

        public static void ApplySoftSkin(string file)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;
                using (var smooth = new Mat())
                {
                    Cv2.EdgePreservingFilter(img, smooth, EdgePreservingMethods.RecursFilter, 60, 0.4f);
                    byte[] jpegBytes = img.ImEncode(".jpg");
                    File.WriteAllBytes(file, jpegBytes);
                }
            }
        }

        public static void ApplyWarmTone(string file)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;
                Cv2.Add(img, new Scalar(-10, -5, 15), img);
                byte[] jpegBytes = img.ImEncode(".jpg");
                File.WriteAllBytes(file, jpegBytes);
            }
        }

        public static void ApplyCoolTone(string file)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;
                Cv2.Add(img, new Scalar(15, 5, -10), img);
                byte[] jpegBytes = img.ImEncode(".jpg");
                File.WriteAllBytes(file, jpegBytes);
            }
        }

        public static void ApplyPinkSoft(string file)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;

                using (var floatImg = new Mat())
                {
                    img.ConvertTo(floatImg, MatType.CV_32FC3);
                    Cv2.Add(floatImg, new Scalar(-5, -5, 15), floatImg);
                    Cv2.Multiply(floatImg, 1.05, floatImg);
                    Cv2.Add(floatImg, new Scalar(5, 5, 5), floatImg);
                    floatImg.ConvertTo(img, MatType.CV_8UC3);
                }

                byte[] jpegBytes = img.ImEncode(".jpg");
                File.WriteAllBytes(file, jpegBytes);
            }
        }

        public static void ApplyColorEnhance(string file, Color c)
        {
            double enhanceRatio = 0.1;
            int gbLimit = 30;
            int whiteLimit = 100;

            int dominant = Math.Max(c.R, Math.Max(c.G, c.B));

            using (var img = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (img.Empty()) return;

                var channels = img.Split();
                Mat b = channels[0];
                Mat g = channels[1];
                Mat r = channels[2].Clone();

                Mat target = dominant == c.R ? r : dominant == c.G ? g : b;
                Mat other1 = dominant == c.R ? g : dominant == c.G ? r : r;
                Mat other2 = dominant == c.R ? b : dominant == c.G ? b : g;

                using (var mask = new Mat())
                {
                    using (var maskR = new Mat())
                    using (var maskG = new Mat())
                    using (var maskB = new Mat())
                    {
                        Cv2.Threshold(r, maskR, whiteLimit, 255, ThresholdTypes.Binary);
                        Cv2.Threshold(g, maskG, whiteLimit, 255, ThresholdTypes.Binary);
                        Cv2.Threshold(b, maskB, whiteLimit, 255, ThresholdTypes.Binary);
                        Cv2.BitwiseAnd(maskR, maskG, mask);
                        Cv2.BitwiseAnd(mask, maskB, mask);
                    }

                    using (var other1Plus = new Mat())
                    using (var mask1 = new Mat())
                    {
                        Cv2.Add(other1, new Scalar(gbLimit), other1Plus);
                        Cv2.Compare(target, other1Plus, mask1, CmpTypes.GT);
                        Cv2.BitwiseAnd(mask, mask1, mask);
                    }

                    using (var other2Plus = new Mat())
                    using (var mask2 = new Mat())
                    {
                        Cv2.Add(other2, new Scalar(gbLimit), other2Plus);
                        Cv2.Compare(target, other2Plus, mask2, CmpTypes.GT);
                        Cv2.BitwiseAnd(mask, mask2, mask);
                    }

                    using (var otherSum = new Mat())
                    using (var otherHalf = new Mat())
                    using (var diff = new Mat())
                    using (var enhancement = new Mat())
                    {
                        Cv2.Add(other1, other2, otherSum);
                        Cv2.Divide(otherSum, 2.0, otherHalf);
                        Cv2.Subtract(target, otherHalf, diff);
                        Cv2.Multiply(diff, enhanceRatio, enhancement);

                        Cv2.Add(target, enhancement, target, mask);
                        Cv2.Subtract(other1, enhancement * 2, other1, mask);
                        Cv2.Subtract(other2, enhancement * 2, other2, mask);
                    }
                }

                using (var merged = new Mat())
                {
                    Cv2.Merge(new[] { b, g, r }, merged);
                    byte[] jpegBytes = merged.ImEncode(".jpg");
                    File.WriteAllBytes(file, jpegBytes);
                }

                r.Dispose();
                foreach (var ch in channels) ch.Dispose();
            }
        }

        // ========== 增强（AI / OpenCV）==========

        public static void EnhanceByOpenCv(string file, string target, int smoothLevel = 60, float edgeLevel = 0.4f, double enhanceLevel = 1.3)
        {
            using (var src = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (src.Empty()) return;

                using (var smooth = new Mat())
                {
                    Cv2.EdgePreservingFilter(src, smooth, EdgePreservingMethods.RecursFilter, smoothLevel, edgeLevel);

                    using (var blur = new Mat())
                    using (var detail = new Mat())
                    {
                        Cv2.GaussianBlur(smooth, blur, new OpenCvSharp.Size(0, 0), 3);
                        Cv2.Subtract(src, blur, detail);

                        using (var gray = src.CvtColor(ColorConversionCodes.BGR2GRAY))
                        using (var edges = new Mat())
                        {
                            Cv2.Canny(gray, edges, 40, 120);
                            Cv2.Dilate(edges, edges, new Mat());

                            using (var result = smooth.Clone())
                            using (var sharpened = new Mat())
                            {
                                Cv2.AddWeighted(smooth, 1.0, detail, enhanceLevel, 0, sharpened);
                                sharpened.CopyTo(result, edges);

                                byte[] jpegBytes = result.ImEncode(".jpg");
                                File.WriteAllBytes(target, jpegBytes);
                            }
                        }
                    }
                }
            }
        }

        public static bool EnhanceByUpScaylbin(string inputFile, string outputFile, string modelName, string modelPath, string sParam)
        {
            string exePath = Path.Combine(Application.StartupPath, "Upscayl", "resources", "bin", "upscayl-bin.exe");

            if (!File.Exists(exePath)) { Debug.WriteLine($"exe不存在: {exePath}"); return false; }
            if (!File.Exists(inputFile)) { Debug.WriteLine($"输入文件不存在: {inputFile}"); return false; }
            string tempInput = null;
            string outTmp = Path.Combine(Path.GetTempPath(), $"Upscayl_out_{Guid.NewGuid():N}.tmp.jpg");
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-i \"{inputFile}\" -o \"{outTmp}\" -m \"{modelPath}\" -n {modelName} -f jpg {sParam} ",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                bool success = p.ExitCode == 0 && File.Exists(outTmp);
                if (success)
                {
                    CompressJpeg(outTmp, outputFile, 85);
                }
                File.Delete(outTmp);
                if (tempInput != null) File.Delete(tempInput);
                return success && File.Exists(outputFile);
            }
        }

        public static bool EnhanceByUpScayl(string inputFile)
        {
            string exePath = Path.Combine(Application.StartupPath, "Upscayl", "upscayl.exe");
            if (!File.Exists(exePath)) { Debug.WriteLine($"exe不存在: {exePath}"); return false; }
            if (!File.Exists(inputFile)) { Debug.WriteLine($"输入文件不存在: {inputFile}"); return false; }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-i \"{inputFile}\"  ",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
            }
            return true;
        }

        public static void CompressJpeg(string inputFile, string outputFile, int quality = 85)
        {
            using (var img = Cv2.ImDecode(File.ReadAllBytes(inputFile), ImreadModes.Color))
            {
                if (img.Empty()) return;
                var parameters = new ImageEncodingParam[]
                {
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, quality)
                };
                byte[] jpegBytes = img.ImEncode(".jpg", parameters);
                File.WriteAllBytes(outputFile, jpegBytes);
            }
        }

        public static void MergeImagesAverage(List<string> files, string targetFile)
        {
            if (files == null || files.Count == 0) return;

            var images = new List<Mat>();
            foreach (var file in files)
            {
                var mat = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color);
                if (!mat.Empty()) images.Add(mat);
            }
            if (images.Count == 0) return;

            int w = images[0].Width, h = images[0].Height;
            using (var result = new Mat(h, w, MatType.CV_32FC3, new Scalar(0, 0, 0)))
            {
                foreach (var img in images)
                {
                    using (var floatImg = new Mat())
                    {
                        img.ConvertTo(floatImg, MatType.CV_32FC3);
                        Cv2.Add(result, floatImg, result);
                    }
                    img.Dispose();
                }
                Cv2.Divide(result, (double)images.Count, result);

                using (var output = new Mat())
                {
                    result.ConvertTo(output, MatType.CV_8UC3);
                    byte[] jpegBytes = output.ImEncode(".jpg");
                    File.WriteAllBytes(targetFile, jpegBytes);
                }
            }
        }

        // ========== 去雾 / CLAHE（保留备用）==========

        private static Mat ApplyCLAHE(Mat src, double clipLimit = 2.5)
        {
            using (var lab = new Mat())
            {
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);

                Mat[] channels = Cv2.Split(lab);

                using (var clahe = Cv2.CreateCLAHE(clipLimit, new OpenCvSharp.Size(8, 8)))
                {
                    clahe.Apply(channels[0], channels[0]);
                }

                Cv2.Merge(channels, lab);

                Mat result = new Mat();
                Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

                foreach (var ch in channels) ch.Dispose();

                return result;
            }
        }

        private static Mat Dehaze(Mat src, double omega = 0.95, int kernelSize = 15)
        {
            Mat darkChannel = GetDarkChannel(src, kernelSize);
            Scalar atmosphericLight = GetAtmosphericLight(src, darkChannel);
            Mat transmission = GetTransmission(src, atmosphericLight, omega, kernelSize);
            darkChannel.Dispose();

            Mat refinedTransmission = new Mat();
            Cv2.GaussianBlur(transmission, refinedTransmission, new OpenCvSharp.Size(15, 15), 0);
            transmission.Dispose();

            Mat t = new Mat();
            Cv2.Max(refinedTransmission, 0.1, t);
            refinedTransmission.Dispose();

            Mat t3Channel = new Mat();
            Cv2.Merge(new Mat[] { t, t, t }, t3Channel);
            t.Dispose();

            Mat srcFloat = new Mat();
            src.ConvertTo(srcFloat, MatType.CV_32FC3, 1.0 / 255.0);

            Mat tFloat = new Mat();
            t3Channel.ConvertTo(tFloat, MatType.CV_32FC3);
            t3Channel.Dispose();

            Mat aFloat = new Mat();
            Mat atmosphericMat = new Mat(src.Size(), MatType.CV_32FC3, atmosphericLight);
            atmosphericMat.ConvertTo(aFloat, MatType.CV_32FC3, 1.0 / 255.0);
            atmosphericMat.Dispose();

            Mat numerator = new Mat();
            Cv2.Subtract(srcFloat, aFloat, numerator);

            Mat resultFloat = new Mat();
            Cv2.Divide(numerator, tFloat, resultFloat);

            Cv2.Add(resultFloat, aFloat, resultFloat);

            Mat result = new Mat();
            resultFloat.ConvertTo(result, MatType.CV_8UC3, 255.0);

            srcFloat.Dispose();
            tFloat.Dispose();
            aFloat.Dispose();
            numerator.Dispose();
            resultFloat.Dispose();

            return result;
        }

        private static Mat GetDarkChannel(Mat src, int kernelSize)
        {
            Mat darkChannel = new Mat();

            using (var bgr = new Mat())
            {
                src.CopyTo(bgr);

                Mat[] channels = Cv2.Split(bgr);

                Cv2.Min(channels[0], channels[1], darkChannel);
                Cv2.Min(darkChannel, channels[2], darkChannel);

                var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kernelSize, kernelSize));
                Cv2.Erode(darkChannel, darkChannel, kernel);

                foreach (var ch in channels) ch.Dispose();
            }

            return darkChannel;
        }

        private static Scalar GetAtmosphericLight(Mat src, Mat darkChannel)
        {
            int pixelCount = (int)(src.Rows * src.Cols * 0.001);
            if (pixelCount < 1) pixelCount = 1;

            Mat flatDark = darkChannel.Reshape(1, 1);
            Mat sortedIdx = new Mat();
            Cv2.SortIdx(flatDark, sortedIdx, SortFlags.Descending | SortFlags.EveryRow);

            Mat srcFlat = src.Reshape(1, 1);
            double maxSum = 0;
            Scalar atmosphericLight = new Scalar(0, 0, 0);

            int count = Math.Min(pixelCount, sortedIdx.Cols);
            for (int i = 0; i < count; i++)
            {
                int idx = sortedIdx.At<int>(0, i);
                Vec3b pixel = srcFlat.At<Vec3b>(0, idx);
                double sum = pixel[0] + pixel[1] + pixel[2];
                if (sum > maxSum)
                {
                    maxSum = sum;
                    atmosphericLight = new Scalar(pixel[0], pixel[1], pixel[2]);
                }
            }

            flatDark.Dispose();
            sortedIdx.Dispose();
            srcFlat.Dispose();

            return atmosphericLight;
        }

        private static Mat GetTransmission(Mat src, Scalar atmosphericLight, double omega, int kernelSize)
        {
            Mat normalized = new Mat();
            Mat normalizedFloat = new Mat();

            src.ConvertTo(normalizedFloat, MatType.CV_32FC3, 1.0 / 255.0);

            Mat aMat = new Mat(src.Size(), MatType.CV_32FC3, new Scalar(
                atmosphericLight[0] / 255.0,
                atmosphericLight[1] / 255.0,
                atmosphericLight[2] / 255.0));

            Cv2.Divide(normalizedFloat, aMat, normalized);

            Mat darkChannel = GetDarkChannel(normalized, kernelSize);

            Mat transmission = new Mat();
            Cv2.Subtract(new Scalar(1.0), darkChannel, transmission);
            transmission *= omega;

            normalized.Dispose();
            normalizedFloat.Dispose();
            aMat.Dispose();
            darkChannel.Dispose();

            return transmission;
        }

        private static Mat ApplyCLAHE(Mat src)
        {
            using (var lab = new Mat())
            {
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);

                Mat[] channels = Cv2.Split(lab);

                using (var clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8)))
                {
                    clahe.Apply(channels[0], channels[0]);
                }

                Cv2.Merge(channels, lab);

                Mat result = new Mat();
                Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

                foreach (var ch in channels) ch.Dispose();

                return result;
            }
        }
    }
}