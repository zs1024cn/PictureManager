using System;
using System.Diagnostics;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace PictureManager
{
    public partial class FileUtility
    {
        // ========== 人脸马赛克 ==========

        public static bool MaskFace(string inputFile, string outputFile)
        {
            try
            {
                using (var src = Cv2.ImDecode(File.ReadAllBytes(inputFile), ImreadModes.Color))
                {
                    if (src.Empty()) return false;

                    string protoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy.prototxt");
                    string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res10_300x300_ssd_iter_140000.caffemodel");

                    if (!File.Exists(protoPath) || !File.Exists(modelPath))
                    {
                        Debug.WriteLine("DNN 模型文件不存在");
                        return false;
                    }

                    using (var net = CvDnn.ReadNetFromCaffe(protoPath, modelPath))
                    {
                        using (var blob = CvDnn.BlobFromImage(src, 1.0, new OpenCvSharp.Size(300, 300),
                            new Scalar(104.0, 177.0, 123.0), false, false))
                        {
                            net.SetInput(blob);
                            using (var detections = net.Forward())
                            {
                                int numDetections = detections.Size(2);
                                int faceCount = 0;

                                for (int i = 0; i < numDetections; i++)
                                {
                                    float confidence = detections.At<float>(0, 0, i, 2);

                                    if (confidence > 0.5f)
                                    {
                                        int x1 = (int)(detections.At<float>(0, 0, i, 3) * src.Width);
                                        int y1 = (int)(detections.At<float>(0, 0, i, 4) * src.Height);
                                        int x2 = (int)(detections.At<float>(0, 0, i, 5) * src.Width);
                                        int y2 = (int)(detections.At<float>(0, 0, i, 6) * src.Height);

                                        x1 = Math.Max(0, x1);
                                        y1 = Math.Max(0, y1);
                                        x2 = Math.Min(src.Width - 1, x2);
                                        y2 = Math.Min(src.Height - 1, y2);

                                        Rect face = new Rect(x1, y1, x2 - x1, y2 - y1);
                                        if (face.Width > 10 && face.Height > 10)
                                        {
                                            ApplyMosaic(src, face);
                                            faceCount++;
                                        }
                                    }
                                }

                                if (faceCount == 0)
                                {
                                    Debug.WriteLine("DNN 未检测到人脸");
                                    return false;
                                }

                                byte[] jpegBytes = src.ImEncode(".jpg");
                                File.WriteAllBytes(outputFile, jpegBytes);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MaskFace 失败: {ex.Message}");
                return false;
            }
        }

        private static void ApplyMosaic(Mat src, Rect face)
        {
            int cropBottom = (int)(face.Height * 0.15);
            int cropTop = (int)(face.Height * 0.1);
            int cropSide = 0;

            int x = face.X + cropSide;
            int y = face.Y + cropTop;
            int w = face.Width - cropSide * 2;
            int h = face.Height - cropTop - cropBottom;

            x = Math.Max(0, x);
            y = Math.Max(0, y);
            w = Math.Min(src.Width - x, w);
            h = Math.Min(src.Height - y, h);

            if (w <= 0 || h <= 0) return;

            Rect mosaicRect = new Rect(x, y, w, h);

            using (var mask = new Mat(new OpenCvSharp.Size(w, h), MatType.CV_8UC1, Scalar.Black))
            {
                Cv2.Ellipse(mask, new OpenCvSharp.Point(w / 2, h / 2),
                    new OpenCvSharp.Size(w / 2, h / 2), 0, 0, 360, Scalar.White, -1);

                using (var faceRoi = new Mat(src, mosaicRect))
                using (var processed = new Mat())
                {
                    faceRoi.CopyTo(processed);

                    Scalar skinColor = GetSkinColor(processed);

                    ReplaceDarkPixels(processed, skinColor);
                    ReplaceRedPixels(processed, skinColor);

                    int blockSize = Math.Max(8, w / 8);
                    using (var small = new Mat())
                    {
                        Cv2.Resize(processed, small, new OpenCvSharp.Size(
                            Math.Max(1, w / blockSize),
                            Math.Max(1, h / blockSize)),
                            interpolation: InterpolationFlags.Linear);

                        Cv2.Resize(small, processed, new OpenCvSharp.Size(w, h),
                            interpolation: InterpolationFlags.Nearest);
                    }

                    using (var blurred = new Mat())
                    {
                        Cv2.GaussianBlur(processed, blurred, new OpenCvSharp.Size(0, 0), 12);
                        blurred.CopyTo(processed);
                    }

                    using (var hsv = new Mat())
                    {
                        Cv2.CvtColor(processed, hsv, ColorConversionCodes.BGR2HSV);
                        Mat[] channels = Cv2.Split(hsv);
                        channels[1].ConvertTo(channels[1], -1, 0.5, 0);
                        channels[2].ConvertTo(channels[2], -1, 1.0, 20);
                        Cv2.Merge(channels, hsv);
                        Cv2.CvtColor(hsv, processed, ColorConversionCodes.HSV2BGR);
                        foreach (var ch in channels) ch.Dispose();
                    }

                    processed.CopyTo(faceRoi, mask);
                }
            }
        }

        private static Scalar GetSkinColor(Mat roi)
        {
            using (var hsv = new Mat())
            {
                Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);
                Mat[] ch = Cv2.Split(hsv);

                using (var mask = new Mat())
                {
                    Cv2.InRange(hsv, new Scalar(0, 30, 60), new Scalar(25, 200, 230), mask);
                    var mean = Cv2.Mean(roi, mask);

                    if (double.IsNaN(mean.Val0) || mean.Val0 == 0)
                    {
                        foreach (var c in ch) c.Dispose();
                        return new Scalar(140, 160, 200);
                    }

                    foreach (var c in ch) c.Dispose();
                    return mean;
                }
            }
        }

        private static void ReplaceDarkPixels(Mat roi, Scalar skinColor)
        {
            using (var gray = new Mat())
            using (var darkMask = new Mat())
            {
                Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);

                Cv2.Threshold(gray, darkMask, 100, 255, ThresholdTypes.BinaryInv);

                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
                {
                    Cv2.Dilate(darkMask, darkMask, kernel);
                }

                using (var skinMat = new Mat(roi.Size(), roi.Type(), skinColor))
                {
                    skinMat.CopyTo(roi, darkMask);
                }
            }
        }

        private static void ReplaceRedPixels(Mat roi, Scalar skinColor)
        {
            using (var hsv = new Mat())
            using (var redMask1 = new Mat())
            using (var redMask2 = new Mat())
            using (var redMask = new Mat())
            {
                Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);

                Cv2.InRange(hsv, new Scalar(0, 80, 50), new Scalar(10, 255, 255), redMask1);
                Cv2.InRange(hsv, new Scalar(170, 80, 50), new Scalar(180, 255, 255), redMask2);
                Cv2.BitwiseOr(redMask1, redMask2, redMask);

                using (var skinMat = new Mat(roi.Size(), roi.Type(), skinColor))
                {
                    skinMat.CopyTo(roi, redMask);
                }
            }
        }
    }
}