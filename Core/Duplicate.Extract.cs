//Core\Duplicate.Extract.cs
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;
using System;
using System.Diagnostics;
using System.IO;

namespace PictureManager
{
    public partial class Duplicate
    {
        // ========== 读图 ==========

        public static Mat ReadPictureToMat(string file, int cropIndex = 0)
        {
            if (string.IsNullOrEmpty(file)) return null;
            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                var src = Cv2.ImDecode(fileData, ImreadModes.AnyColor);
                if (src == null || src.Empty()) return null;
                if (cropIndex != 0)
                {
                    var cropped = CropByIndex(src, cropIndex);
                    src.Dispose();
                    return cropped;
                }
                return src;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ExtractHashFeaturesToByte_OpenCV_FromMat(Mat src, int iSize, int iGray)
        {
            try
            {
                using (var gray = new Mat())
                {
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.Resize(gray, gray, new OpenCvSharp.Size(iSize, iSize));

                    // 找 min / max
                    byte minVal = 255, maxVal = 0;
                    for (int x = 0; x < iSize; x++)
                    {
                        for (int y = 0; y < iSize; y++)
                        {
                            byte v = gray.At<byte>(y, x);
                            if (v < minVal) minVal = v;
                            if (v > maxVal) maxVal = v;
                        }
                    }

                    // range / iGray = 每级大小
                    double range = maxVal - minVal;
                    double iInterval = range / iGray;
                    if (iInterval < 1) iInterval = 1;  // 防止除 0

                    byte[] bFeatures = new byte[iSize * iSize];
                    for (int x = 0; x < iSize; x++)
                    {
                        for (int y = 0; y < iSize; y++)
                        {
                            byte v = gray.At<byte>(y, x);
                            int bucket = (int)((v - minVal) / iInterval);
                            if (bucket > iGray) bucket = iGray;
                            if (bucket < 0) bucket = 0;
                            bFeatures[x * iSize + y] = (byte)bucket;
                        }
                    }
                    return bFeatures;
                }
            }
            catch
            {
                return null;
            }
        }

        private static Mat ExtractDctFeatures2_FromMat(Mat src)
        {
            try
            {
                using (var small = new Mat())
                {
                    int maxDim = 256;
                    double scale = Math.Min((double)maxDim / src.Width, (double)maxDim / src.Height);
                    if (scale < 1.0)
                        Cv2.Resize(src, small, new OpenCvSharp.Size(0, 0), scale, scale, InterpolationFlags.Area);
                    else
                        src.CopyTo(small);

                    using (var matA = new Mat())
                    {
                        small.ConvertTo(matA, MatType.CV_32FC1);
                        Mat[] matA_S;
                        Cv2.Split(matA, out matA_S);

                        Hist_A_Single(matA_S[0], out Mat histB);
                        Hist_A_Single(matA_S[1], out Mat histG);
                        Hist_A_Single(matA_S[2], out Mat histR);

                        Mat histMerged = new Mat();
                        Cv2.VConcat(new[] { histR, histG, histB }, histMerged);

                        foreach (var m in matA_S) m?.Dispose();
                        histR?.Dispose(); histG?.Dispose(); histB?.Dispose();

                        return histMerged;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static void Hist_A_Single(Mat channel, out Mat hist)
        {
            int[] channels = { 0 };
            Rangef[] histRange = { new Rangef(0, 256) };
            int[] histSize = { 256 };
            hist = new Mat();
            Cv2.CalcHist(new[] { channel }, channels, null, hist, 1, histSize, histRange, true, false);
            Cv2.Normalize(hist, hist, 0, 1, NormTypes.MinMax);
        }


        private static int _siftExtractLogCount = 0;

        private static Tuple<KeyPoint[], Mat> ExtractSiftFeaturesByMat(Mat mat, string imagePath, int maxFeatures = 500, int maxDim = 800)
        {
            try
            {
                if (mat == null || mat.Empty()) return null;

                var swTotal = Stopwatch.StartNew();
                var swGray = Stopwatch.StartNew();

                Mat gray;
                if (mat.Channels() == 1)
                    gray = mat.Clone();
                else
                {
                    gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                }
                long grayMs = swGray.ElapsedMilliseconds;

                using (gray)
                {
                    long resizeMs = 0, siftMs = 0;
                    KeyPoint[] kp = null;
                    Mat desc = new Mat();

                    if (gray.Width > maxDim || gray.Height > maxDim)
                    {
                        var swResize = Stopwatch.StartNew();
                        double scale = Math.Min((double)maxDim / gray.Width, (double)maxDim / gray.Height);
                        var resized = new Mat();
                        Cv2.Resize(gray, resized, new OpenCvSharp.Size(gray.Width * scale, gray.Height * scale));
                        resizeMs = swResize.ElapsedMilliseconds;

                        using (resized)
                        {
                            var swSift = Stopwatch.StartNew();
                            var sift = SIFT.Create(maxFeatures);
                            sift.DetectAndCompute(resized, null, out kp, desc);
                            siftMs = swSift.ElapsedMilliseconds;
                        }
                    }
                    else
                    {
                        var swSift = Stopwatch.StartNew();
                        var sift2 = SIFT.Create(maxFeatures);
                        sift2.DetectAndCompute(gray, null, out kp, desc);
                        siftMs = swSift.ElapsedMilliseconds;
                    }

                    swTotal.Stop();

                    if (kp == null || kp.Length == 0) return null;

                    if (_siftExtractLogCount < 20)
                    {
                        _siftExtractLogCount++;
                        Debug.WriteLine($"[SIFT-Extract] total={swTotal.ElapsedMilliseconds}ms, gray={grayMs}ms, resize={resizeMs}ms, sift={siftMs}ms, kp={kp.Length}, img={gray.Width}x{gray.Height}, file={Path.GetFileName(imagePath)}");
                    }

                    return Tuple.Create(kp, desc);
                }
            }
            catch { return null; }
        }

        // ========== HSV ==========

        public static float[] GetHsv(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (HsvCache.TryGetValue(file, out float[] hsv)) return hsv;
            if (HsvCacheDeleted.TryGetValue(file, out hsv)) return hsv;

            hsv = ComputeHsv(file);
            if (hsv != null)
            {
                lock (_hsvLock) { HsvCache[file] = hsv; }
            }
            return hsv;
        }

        private static float[] ComputeHsv(string file)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat.Empty()) return null;
                    return ComputeHsvByMat(mat);
                }
            }
            catch
            {
                return null;
            }
        }

        private static float[] ComputeHsvByMat(Mat src)
        {
            try
            {
                if (src == null || src.Empty())
                {
                    Debug.WriteLine("src 为空");
                    return null;
                }

                using (var small = new Mat())
                {
                    Cv2.Resize(src, small, new OpenCvSharp.Size(64, 64));
                    using (var hsvMat = new Mat())
                    {
                        Cv2.CvtColor(small, hsvMat, ColorConversionCodes.BGR2HSV);

                        var features = new float[8 * 8 * 3 + 4];
                        int idx = 0;

                        for (int gy = 0; gy < 8; gy++)
                        {
                            for (int gx = 0; gx < 8; gx++)
                            {
                                Rect blockRect = new Rect(gx * 8, gy * 8, 8, 8);
                                using (var block = new Mat(hsvMat, blockRect))
                                {
                                    var avg = Cv2.Mean(block);
                                    features[idx++] = (float)avg.Val0 / 180;
                                    features[idx++] = (float)avg.Val1 / 255;
                                    features[idx++] = (float)avg.Val2 / 255;
                                }
                            }
                        }

                        var wholeAvg = Cv2.Mean(hsvMat);
                        features[idx++] = (float)wholeAvg.Val0 / 180;
                        features[idx++] = (float)wholeAvg.Val1 / 255;
                        features[idx++] = (float)wholeAvg.Val2 / 255;

                        using (var gray = new Mat())
                        using (var sobel = new Mat())
                        {
                            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
                            Cv2.Sobel(gray, sobel, MatType.CV_32F, 1, 0, 3);
                            var edgeAvg = Cv2.Mean(sobel);
                            features[idx++] = (float)edgeAvg.Val0 / 255;
                        }

                        return features;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ComputeHsvByMat 异常: {ex.Message}");
                return null;
            }
        }

        // ========== 裁剪 ==========

        public static Mat CropByIndex(Mat src, int cropIndex)
        {
            if (cropIndex == 0)
                return src.Clone();

            int iCropWidth = Math.Min(300, src.Width);
            int iCropHeight = Math.Min(300, src.Height);

            Rect cropArea;
            switch (cropIndex)
            {
                case 0: cropArea = new Rect(0, 0, src.Width, src.Height); break;
                case 1: cropArea = new Rect(0, 0, iCropWidth, iCropHeight); break;
                case 2: cropArea = new Rect(src.Width - iCropWidth, 0, iCropWidth, iCropHeight); break;
                case 3: cropArea = new Rect(0, src.Height - iCropHeight, iCropWidth, iCropHeight); break;
                case 4: cropArea = new Rect(src.Width - iCropWidth, src.Height - iCropHeight, iCropWidth, iCropHeight); break;
                case 5:  // 中心 1/2
                    {
                        int cw = src.Width / 2;
                        int ch = src.Height / 2;
                        cropArea = new Rect(src.Width / 4, src.Height / 4, cw, ch);
                    }
                    break;
                default: cropArea = new Rect(src.Width - iCropWidth, src.Height - iCropHeight, iCropWidth, iCropHeight); break;
            }

            return new Mat(src, cropArea).Clone();
        }

        // ========== 公开的 ByMat 方法（供 ImageFeatureExtractor 一次读图提取） ==========

        /// <summary>
        /// 从已解码的 Mat 提取 Hash。同时填充 HashCache。
        /// </summary>
        public static byte[] ExtractHashByMat(Mat src, string fileName, int iSize, int iGray)
        {
            if (src == null || src.Empty()) return null;

            byte[] hash = ExtractHashFeaturesToByte_OpenCV_FromMat(src, iSize, iGray);
            if (hash != null && !string.IsNullOrEmpty(fileName))
            {
                lock (_cacheLock) { HashCache[fileName] = hash; }
            }
            return hash;
        }

        /// <summary>
        /// 从已解码的 Mat 提取 DCT。同时填充 DctCache。
        /// </summary>
        public static Mat ExtractDctByMat(Mat src, string fileName)
        {
            if (src == null || src.Empty()) return null;

            Mat dct = ExtractDctFeatures2_FromMat(src);
            if (dct != null && !dct.Empty() && !string.IsNullOrEmpty(fileName))
            {
                lock (_cacheLock) { DctCache[fileName] = dct; }
            }
            return dct;
        }

        /// <summary>
        /// 从已解码的 Mat 提取 SIFT。同时填充 SiftCache。
        /// </summary>
        public static Tuple<KeyPoint[], Mat> ExtractSiftByMat(Mat src, string fileName, int maxFeatures, int maxDim)
        {
            if (src == null || src.Empty()) return null;

            var features = ExtractSiftFeaturesByMat(src, fileName, maxFeatures, maxDim);
            if (features != null && !string.IsNullOrEmpty(fileName))
            {
                lock (_cacheLock) { SiftCache[fileName] = features; }
            }
            return features;
        }

        /// <summary>
        /// 从已解码的 Mat 提取 HSV。同时填充 HsvCache。
        /// </summary>
        public static float[] ExtractHsvByMat(Mat src, string fileName)
        {
            if (src == null || src.Empty()) return null;

            float[] hsv = ComputeHsvByMat(src);
            if (hsv != null && !string.IsNullOrEmpty(fileName))
            {
                HsvCache[fileName] = hsv;
            }
            return hsv;
        }
    }
}