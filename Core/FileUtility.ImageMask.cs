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
                                        y2 = (int)(Math.Min(src.Height - 1, y2) * 1.1);

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
            ApplyGlassMosaic(src, face);  // 用花玻璃版本
            return;
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
        private static void ApplyGlassMosaic(Mat src, Rect face)
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

            int gridLines = GlassMosaicParams.GridLines;
            int shadowWidth = GlassMosaicParams.ShadowWidth;
            double shadowFactor = GlassMosaicParams.ShadowFactor;
            double triCoef = GlassMosaicParams.TriCoef;
            double minRatio = GlassMosaicParams.MinBlackRatio;
            int blackTh = GlassMosaicParams.BlackThreshold;

            using (var mask = new Mat(new OpenCvSharp.Size(w, h), MatType.CV_8UC1, Scalar.Black))
            {
                Cv2.Ellipse(mask, new OpenCvSharp.Point(w / 2, h / 2),
                    new OpenCvSharp.Size(w / 2, h / 2), 0, 0, 360, Scalar.White, -1);

                using (var faceRoi = new Mat(src, mosaicRect))
                {
                    // 1. 肉色 r
                    Scalar skinColor = GetSkinColor(faceRoi);
                    byte r = (byte)skinColor.Val2;
                    byte g = (byte)skinColor.Val1;
                    byte b = (byte)skinColor.Val0;

                    // 2. 底图（黑红替换为肉色）
                    using (var baseImg = new Mat())
                    {
                        faceRoi.CopyTo(baseImg);
                        ReplaceDarkPixels(baseImg, skinColor);
                        ReplaceRedPixels(baseImg, skinColor);

                        // 3. 分格统计
                        int cellW = w / gridLines;
                        int cellH = h / gridLines;
                        if (cellW < 2 || cellH < 2) return;

                        int[,] blackCount = new int[gridLines, gridLines];
                        int[,] redCount = new int[gridLines, gridLines];
                        double[,] blackSumR = new double[gridLines, gridLines];
                        double[,] blackSumG = new double[gridLines, gridLines];
                        double[,] blackSumB = new double[gridLines, gridLines];
                        double[,] redSumR = new double[gridLines, gridLines];
                        double[,] redSumG = new double[gridLines, gridLines];
                        double[,] redSumB = new double[gridLines, gridLines];

                        for (int gy = 0; gy < gridLines; gy++)
                        {
                            for (int gx = 0; gx < gridLines; gx++)
                            {
                                int x0 = gx * cellW;
                                int y0 = gy * cellH;
                                int x1 = Math.Min(w, x0 + cellW);
                                int y1 = Math.Min(h, y0 + cellH);

                                for (int py = y0; py < y1; py++)
                                {
                                    for (int px = x0; px < x1; px++)
                                    {
                                        Vec3b p = faceRoi.At<Vec3b>(py, px);
                                        // 黑点
                                        if (p.Item2 < blackTh && p.Item1 < blackTh && p.Item0 < blackTh)
                                        {
                                            blackCount[gy, gx]++;
                                            blackSumR[gy, gx] += p.Item2;
                                            blackSumG[gy, gx] += p.Item1;
                                            blackSumB[gy, gx] += p.Item0;
                                        }
                                        // 红点
                                        else if (p.Item2 > 100 &&
                                                 p.Item2 > p.Item1 + 30 &&
                                                 p.Item2 > p.Item0 + 30)
                                        {
                                            redCount[gy, gx]++;
                                            redSumR[gy, gx] += p.Item2;
                                            redSumG[gy, gx] += p.Item1;
                                            redSumB[gy, gx] += p.Item0;
                                        }
                                    }
                                }
                            }
                        }

                        // 4. 邻域加权平均（本格权重 3，邻居权重 1，共 11 份）
                        double[,] blackAvg = new double[gridLines, gridLines];
                        double[,] redAvg = new double[gridLines, gridLines];
                        double[,] blackAvgR = new double[gridLines, gridLines];
                        double[,] blackAvgG = new double[gridLines, gridLines];
                        double[,] blackAvgB = new double[gridLines, gridLines];
                        double[,] redAvgR = new double[gridLines, gridLines];
                        double[,] redAvgG = new double[gridLines, gridLines];
                        double[,] redAvgB = new double[gridLines, gridLines];
                        int cellArea = cellW * cellH;

                        for (int gy = 0; gy < gridLines; gy++)
                        {
                            for (int gx = 0; gx < gridLines; gx++)
                            {
                                double totalBlack = 0, totalRed = 0;
                                double totalBR = 0, totalBG = 0, totalBB = 0;
                                double totalRR = 0, totalRG = 0, totalRB = 0;
                                double weightSum = 0;

                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        int ny = gy + dy;
                                        int nx = gx + dx;
                                        if (ny < 0 || ny >= gridLines || nx < 0 || nx >= gridLines) continue;

                                        // 本格权重 10，邻居权重 1
                                        double weight = (dy == 0 && dx == 0) ? 10.0 : 1.0;

                                        totalBlack += weight * (double)blackCount[ny, nx] / cellArea;
                                        totalRed += weight * (double)redCount[ny, nx] / cellArea;

                                        if (blackCount[ny, nx] > 0)
                                        {
                                            totalBR += weight * blackSumR[ny, nx] / blackCount[ny, nx];
                                            totalBG += weight * blackSumG[ny, nx] / blackCount[ny, nx];
                                            totalBB += weight * blackSumB[ny, nx] / blackCount[ny, nx];
                                        }
                                        if (redCount[ny, nx] > 0)
                                        {
                                            totalRR += weight * redSumR[ny, nx] / redCount[ny, nx];
                                            totalRG += weight * redSumG[ny, nx] / redCount[ny, nx];
                                            totalRB += weight * redSumB[ny, nx] / redCount[ny, nx];
                                        }

                                        weightSum += weight;
                                    }
                                }

                                // weightSum = 11（本格 3 + 8 邻居）
                                blackAvg[gy, gx] = totalBlack / weightSum;
                                redAvg[gy, gx] = totalRed / weightSum;
                                blackAvgR[gy, gx] = totalBR / weightSum;
                                blackAvgG[gy, gx] = totalBG / weightSum;
                                blackAvgB[gy, gx] = totalBB / weightSum;
                                redAvgR[gy, gx] = totalRR / weightSum;
                                redAvgG[gy, gx] = totalRG / weightSum;
                                redAvgB[gy, gx] = totalRB / weightSum;
                            }
                        }

                        // 5. 画图
                        using (var result = baseImg.Clone())
                        {
                            byte shadowR = (byte)(r * shadowFactor);
                            byte shadowG = (byte)(g * shadowFactor);
                            byte shadowB = (byte)(b * shadowFactor);

                            for (int gy = 0; gy < gridLines; gy++)
                            {
                                for (int gx = 0; gx < gridLines; gx++)
                                {
                                    int x0 = gx * cellW;
                                    int y0 = gy * cellH;
                                    int x1 = Math.Min(w, x0 + cellW);
                                    int y1 = Math.Min(h, y0 + cellH);

                                    // 画间隔线（肉色 × shadowFactor）
                                    if (shadowWidth >= 1)
                                    {
                                        for (int px = x0; px < x1; px++)
                                        {
                                            result.Set(y0, px, new Vec3b(shadowB, shadowG, shadowR));
                                            result.Set(y1 - 1, px, new Vec3b(shadowB, shadowG, shadowR));
                                        }
                                        for (int py = y0; py < y1; py++)
                                        {
                                            result.Set(py, x0, new Vec3b(shadowB, shadowG, shadowR));
                                            result.Set(py, x1 - 1, new Vec3b(shadowB, shadowG, shadowR));
                                        }
                                    }

                                    // 判定黑/红
                                    double blackRatio = blackAvg[gy, gx];
                                    double redRatio = redAvg[gy, gx];

                                    Scalar triColor;
                                    double triAreaRatio;

                                    if (blackRatio >= redRatio && blackRatio > minRatio)
                                    {
                                        triAreaRatio = blackRatio;
                                        triColor = new Scalar(
                                            blackAvgB[gy, gx],
                                            blackAvgG[gy, gx],
                                            blackAvgR[gy, gx]);
                                    }
                                    else if (redRatio > minRatio)
                                    {
                                        triAreaRatio = redRatio;
                                        triColor = new Scalar(
                                            redAvgB[gy, gx],
                                            redAvgG[gy, gx],
                                            redAvgR[gy, gx]);
                                    }
                                    else continue;

                                    // 三角面积
                                    double triArea = triAreaRatio * cellArea * triCoef;
                                    double side = Math.Sqrt(2 * triArea);

                                    int innerX0 = x0 + shadowWidth;
                                    int innerY0 = y0 + shadowWidth;
                                    int innerX1 = x1 - shadowWidth;
                                    int innerY1 = y1 - shadowWidth;

                                    int maxSide = Math.Min(innerX1 - innerX0, innerY1 - innerY0);
                                    side = Math.Min(side, maxSide);
                                    if (side < 2) continue;

                                    // 右上角直角三角
                                    int txRight = innerX1;
                                    int tyTop = innerY0;
                                    int txLeft = txRight - (int)side;
                                    int tyBottom = tyTop + (int)side;

                                    var pts = new[]
                                    {
                                        new OpenCvSharp.Point(txRight, tyTop),
                                        new OpenCvSharp.Point(txLeft, tyTop),
                                        new OpenCvSharp.Point(txRight, tyBottom)
                                    };
                                    Cv2.FillPoly(result, new[] { pts }, triColor);
                                }
                            }

                            result.CopyTo(baseImg);
                        }

                        baseImg.CopyTo(faceRoi, mask);
                    }
                }
            }
        }
    }
}