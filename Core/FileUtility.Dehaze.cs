//Core\FileUtility.Dehaze.cs
using System;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.XImgProc;

namespace PictureManager
{
    public partial class FileUtility
    {
        // ===== 去雾参数（多张样片实测调好的默认值） =====
        public static double DehazeOmega = 0.78;
        public static int DehazeKernel = 15;
        public static double DehazeTMin = 0.50;
        public static double DehazeClaheClip = 1.45;
        public static double DehazeSatBoost = 0.08;
        public static int DehazeWarm = 0;
        public static double DehazeExposure = 1.03;
        public static double DehazeBlend = 0.50;
        public static double DehazeSkinLift = 0.85;
        public static double DehazeStrength = 1.00;   // 总强度 0..1，最终与原图的混合比例

        /// <summary>参数恢复默认值</summary>
        public static void ResetDehazeDefaults()
        {
            DehazeOmega = 0.78;
            DehazeKernel = 15;
            DehazeTMin = 0.50;
            DehazeClaheClip = 1.45;
            DehazeSatBoost = 0.08;
            DehazeWarm = 0;
            DehazeExposure = 1.03;
            DehazeBlend = 0.50;
            DehazeSkinLift = 0.85;
            DehazeStrength = 1.00;
        }
        //想达到的效果 改哪个
        //更通透（雾更少）	DehazeOmega 0.78 → 0.85，或 DehazeBlend 0.50 → 0.6
        //更自然（少动原图）	DehazeBlend → 0.4
        //暗部死黑 DehazeTMin 0.50 → 0.55
        //更鲜艳 DehazeSatBoost 0.08 → 0.12
        //更暖 / 更冷 DehazeWarm 0 → 2 / -2
        //脸还是偏色 DehazeSkinLift 0.85 → 0.95（保护更强）

        /// <summary>
        /// 通透去雾：白平衡 → 暗通道去雾 → 曝光归一 → 局部对比 → 选择性提饱和。
        /// 肤色区域减少去雾，且不做提饱和/暖调，避免脸发蓝发灰。
        /// </summary>
        public static void ApplyDehazePro(string file, double strength = 1.0)
        {
            if (string.IsNullOrEmpty(file)) return;

            using (var src = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (src == null || src.Empty()) return;

                using (var result = DehazePro(src, strength))
                {
                    File.WriteAllBytes(file, result.ImEncode(".jpg"));
                }
            }
        }

        public static Mat DehazePro(Mat src, double strength = 1.0)
        {
            if (strength <= 0) strength = 0.01;
            if (strength > 2) strength = 2;

            double blend = DehazeBlend;

            Mat wb = WB_Dehaze(src);
            try
            {
                using (var dark = DarkChannelPro(wb, DehazeKernel))
                {
                    Scalar A = EstimateAirLightPro(wb, dark);

                    using (var t0 = TransmissionPro(wb, A, DehazeOmega, DehazeKernel))
                    using (var t1 = RefinePro(wb, t0))
                    using (var skin = SkinMaskPro(src))
                    {
                        SkinProtectPro(t1, skin, DehazeSkinLift);

                        using (var J = RecoverPro(wb, A, t1, DehazeTMin))
                        using (var mix = BlendPro(wb, J, blend))
                        using (var expo = MatchExposurePro(mix, src, DehazeExposure))
                        using (var c = ClahePro(expo, DehazeClaheClip))
                        using (var s = SaturatePro(c, DehazeSatBoost))
                        using (var w = WarmPro(s, DehazeWarm))
                        {
                            using (var full = SkinBlendPro(w, c, skin))
                            {
                                double strengthW = Math.Max(0.0, Math.Min(1.0, strength));
                                var final = new Mat();
                                Cv2.AddWeighted(src, 1.0 - strengthW, full, strengthW, 0, final);
                                return final;
                            }
                        }
                    }
                }
            }
            finally
            {
                wb.Dispose();
            }
        }

        private static Mat WB_Dehaze(Mat src)
        {
            Mat[] ch = Cv2.Split(src);
            try
            {
                double m0 = Cv2.Mean(ch[0]).Val0;
                double m1 = Cv2.Mean(ch[1]).Val0;
                double m2 = Cv2.Mean(ch[2]).Val0;
                double target = (m0 + m1 + m2) / 3.0;

                for (int i = 0; i < 3; i++)
                {
                    double m = i == 0 ? m0 : (i == 1 ? m1 : m2);
                    if (m < 1) m = 1;
                    double k = target / m;
                    if (k < 0.7) k = 0.7;
                    if (k > 1.4) k = 1.4;
                    ch[i].ConvertTo(ch[i], ch[i].Type(), k, 0);
                }

                var res = new Mat();
                Cv2.Merge(ch, res);
                return res;
            }
            finally
            {
                foreach (var c in ch) c.Dispose();
            }
        }

        private static Mat DarkChannelPro(Mat src, int ksize)
        {
            using (var min = new Mat())
            using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(ksize, ksize)))
            {
                Mat[] ch = Cv2.Split(src);
                try
                {
                    Cv2.Min(ch[0], ch[1], min);
                    Cv2.Min(min, ch[2], min);
                    var dark = new Mat();
                    Cv2.Erode(min, dark, kernel);
                    return dark;
                }
                finally
                {
                    foreach (var c in ch) c.Dispose();
                }
            }
        }

        // 大气光：取暗通道最亮 10% 像素的均值，并限制在 200..255
        private static Scalar EstimateAirLightPro(Mat src, Mat dark)
        {
            double lo, hi;
            OpenCvSharp.Point l1, l2;
            Cv2.MinMaxLoc(dark, out lo, out hi, out l1, out l2);

            double thr = Math.Max(hi * 0.9, hi - 8);
            using (var mask = new Mat())
            {
                Cv2.Threshold(dark, mask, thr, 255, ThresholdTypes.Binary);
                if (Cv2.CountNonZero(mask) == 0) return new Scalar(255, 255, 255);

                Scalar m = Cv2.Mean(src, mask);
                return new Scalar(
                    Math.Min(255, Math.Max(200, m.Val0)),
                    Math.Min(255, Math.Max(200, m.Val1)),
                    Math.Min(255, Math.Max(200, m.Val2)));
            }
        }

        private static Mat TransmissionPro(Mat src, Scalar A, double omega, int ksize)
        {
            using (var f = new Mat())
            using (var aMat = new Mat(src.Size(), MatType.CV_32FC3,
                       new Scalar(A.Val0 / 255.0, A.Val1 / 255.0, A.Val2 / 255.0)))
            using (var norm = new Mat())
            using (var darkN = new Mat())
            using (var ones = new Mat(src.Size(), MatType.CV_32FC1, Scalar.All(1.0)))
            {
                src.ConvertTo(f, MatType.CV_32FC3, 1.0 / 255.0);
                Cv2.Divide(f, aMat, norm);

                Mat[] ch = Cv2.Split(norm);
                try
                {
                    Cv2.Min(ch[0], ch[1], darkN);
                    Cv2.Min(darkN, ch[2], darkN);
                }
                finally
                {
                    foreach (var c in ch) c.Dispose();
                }

                var t = new Mat();
                Cv2.AddWeighted(darkN, -omega, ones, 1.0, 0, t);
                return t;
            }
        }

        // 导向滤波细化传输图；不支持时退回双边滤波
        private static Mat RefinePro(Mat guide, Mat t)
        {
            try
            {
                using (var gray = new Mat())
                {
                    Cv2.CvtColor(guide, gray, ColorConversionCodes.BGR2GRAY);
                    var dst = new Mat();
                    CvXImgProc.GuidedFilter(gray, t, dst, 40, 1e-3);
                    return dst;
                }
            }
            catch
            {
                var dst = new Mat();
                Cv2.BilateralFilter(t, dst, 9, 0.1, 15);
                return dst;
            }
        }

        // 肤色区域把传输拉向 1（少去雾）
        private static void SkinProtectPro(Mat t, Mat skin, double strength)
        {
            using (var m = new Mat())
            using (var mf = new Mat())
            using (var inv = new Mat())
            using (var ones = new Mat(t.Size(), MatType.CV_32FC1, Scalar.All(1.0)))
            using (var part1 = new Mat())
            using (var part2 = new Mat())
            {
                skin.ConvertTo(m, MatType.CV_32FC1, (1.0 / 255.0) * strength);
                Cv2.GaussianBlur(m, mf, new OpenCvSharp.Size(31, 31), 0);

                Cv2.Subtract(ones, mf, inv);
                Cv2.Multiply(t, inv, part1);
                Cv2.Multiply(ones, mf, part2);
                Cv2.Add(part1, part2, t);
            }
        }

        private static Mat RecoverPro(Mat src, Scalar A, Mat t, double tMin)
        {
            using (var tc = new Mat())
            {
                Cv2.Max(t, tMin, tc);
                using (var t3 = new Mat())
                {
                    Cv2.Merge(new[] { tc, tc, tc }, t3);

                    using (var f = new Mat())
                    using (var aMat = new Mat(src.Size(), MatType.CV_32FC3,
                               new Scalar(A.Val0 / 255.0, A.Val1 / 255.0, A.Val2 / 255.0)))
                    using (var num = new Mat())
                    using (var res = new Mat())
                    {
                        src.ConvertTo(f, MatType.CV_32FC3, 1.0 / 255.0);
                        Cv2.Subtract(f, aMat, num);
                        Cv2.Divide(num, t3, res);
                        Cv2.Add(res, aMat, res);

                        var out8 = new Mat();
                        res.ConvertTo(out8, MatType.CV_8UC3, 255.0);
                        return out8;
                    }
                }
            }
        }

        private static Mat BlendPro(Mat a, Mat b, double s)
        {
            var dst = new Mat();
            Cv2.AddWeighted(a, 1.0 - s, b, s, 0, dst);
            return dst;
        }

        // 把亮度拉回与原图一致（×factor），避免去雾后整体发暗
        private static Mat MatchExposurePro(Mat src, Mat reference, double factor)
        {
            using (var l1 = new Mat())
            using (var l2 = new Mat())
            {
                Cv2.CvtColor(src, l1, ColorConversionCodes.BGR2Lab);
                Cv2.CvtColor(reference, l2, ColorConversionCodes.BGR2Lab);

                double m1 = Cv2.Mean(l1).Val0;
                double m2 = Cv2.Mean(l2).Val0;
                if (m1 < 1) m1 = 1;

                double scale = (m2 * factor) / m1;
                if (scale < 0.6) scale = 0.6;
                if (scale > 2.5) scale = 2.5;

                var dst = new Mat();
                src.ConvertTo(dst, src.Type(), scale, 0);
                return dst;
            }
        }

        private static Mat ClahePro(Mat src, double clip)
        {
            using (var lab = new Mat())
            {
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);
                Mat[] ch = Cv2.Split(lab);
                try
                {
                    using (var clahe = Cv2.CreateCLAHE(clip, new OpenCvSharp.Size(8, 8)))
                        clahe.Apply(ch[0], ch[0]);

                    var merged = new Mat();
                    Cv2.Merge(ch, merged);
                    var bgr = new Mat();
                    Cv2.CvtColor(merged, bgr, ColorConversionCodes.Lab2BGR);
                    merged.Dispose();
                    return bgr;
                }
                finally
                {
                    foreach (var c in ch) c.Dispose();
                }
            }
        }

        // 饱和度：S' = S*(1+k) - k*S^2/255（灰色不动，高饱和不溢出）
        private static Mat SaturatePro(Mat src, double k)
        {
            if (k <= 0) return src.Clone();

            using (var hsv = new Mat())
            {
                Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);
                Mat[] ch = Cv2.Split(hsv);
                try
                {
                    using (var s32 = new Mat())
                    using (var sq = new Mat())
                    using (var lin = new Mat())
                    {
                        ch[1].ConvertTo(s32, MatType.CV_32F);
                        Cv2.Multiply(s32, s32, sq);
                        Cv2.ConvertScaleAbs(sq, sq, k / 255.0);
                        Cv2.ConvertScaleAbs(s32, lin, 1.0 + k);
                        Cv2.Subtract(lin, sq, lin);
                        lin.ConvertTo(ch[1], ch[1].Type());
                    }

                    Cv2.Merge(ch, hsv);
                    var bgr = new Mat();
                    Cv2.CvtColor(hsv, bgr, ColorConversionCodes.HSV2BGR);
                    return bgr;
                }
                finally
                {
                    foreach (var c in ch) c.Dispose();
                }
            }
        }

        private static Mat WarmPro(Mat src, int amount)
        {
            if (amount == 0) return src.Clone();
            var dst = new Mat();
            Cv2.Add(src, new Scalar(-amount / 2.0, 0, amount), dst);
            return dst;
        }

        // 肤色掩膜（YCrCb 范围 + 形态学开闭）
        private static Mat SkinMaskPro(Mat src)
        {
            using (var ycc = new Mat())
            {
                Cv2.CvtColor(src, ycc, ColorConversionCodes.BGR2YCrCb);
                var mask = new Mat();
                Cv2.InRange(ycc, new Scalar(50, 135, 85), new Scalar(255, 173, 125), mask);

                using (var k = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(7, 7)))
                {
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Close, k);
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Open, k);
                }
                return mask;
            }
        }

        // 肤色区域用"只做对比"的版本覆盖"提饱和+暖调"的版本
        private static Mat SkinBlendPro(Mat colored, Mat plain, Mat skin)
        {
            using (var m = new Mat())
            using (var mf = new Mat())
            using (var m3 = new Mat())
            using (var inv3 = new Mat())
            using (var a = new Mat())
            using (var b = new Mat())
            using (var coloredF = new Mat())
            using (var plainF = new Mat())
            {
                // 只保护一半：肤色仍保留 50% 的提饱和/暖调，避免大面积肤色把滑块"吃掉"
                skin.ConvertTo(m, MatType.CV_32FC1, 0.5 / 255.0);
                Cv2.GaussianBlur(m, mf, new OpenCvSharp.Size(31, 31), 0);
                Cv2.Merge(new[] { mf, mf, mf }, m3);

                using (var ones = new Mat(m3.Size(), MatType.CV_32FC3, Scalar.All(1.0)))
                    Cv2.Subtract(ones, m3, inv3);

                colored.ConvertTo(coloredF, MatType.CV_32FC3);
                plain.ConvertTo(plainF, MatType.CV_32FC3);

                Cv2.Multiply(coloredF, inv3, a);
                Cv2.Multiply(plainF, m3, b);
                Cv2.Add(a, b, a);

                var dst = new Mat();
                a.ConvertTo(dst, MatType.CV_8UC3);
                return dst;
            }
        }
    }
}