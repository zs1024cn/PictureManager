//Core\FileUtility.SkinWhiten.cs
using System;
using System.IO;
using OpenCvSharp;

namespace PictureManager
{
    public partial class FileUtility
    {
        // ===== 嫩白参数（面板可调）=====
        public static double SkinLGamma = 0.70;             // L 的 gamma：越小越亮
        public static double SkinAAlpha = 0.85;             // a 系数：越小越去黄
        public static double SkinABeta = 2.0;               // 红移：负=去红，正=加红（默认 2）
        public static double SkinBAlpha = 0.62;             // b 系数：越小越白
        public static double SkinBShift = 0.0;              // 黄移：负=去黄，正=加黄
        public static double SkinStrength = 1.00;           // 整体强度

        public static void ResetSkinDefaults()
        {
            SkinLGamma = 0.70;
            SkinAAlpha = 0.85;
            SkinABeta = 2.0;
            SkinBAlpha = 0.62;
            SkinBShift = 0.0;
            SkinStrength = 1.00;
        }
        /// <summary>
           /// 肤色嫩白：只在肤色区域提亮、降黄、微加红，环境不动。
           /// </summary>
        public static void ApplySkinWhiten(string file, double strength = 1.0)
        {
            if (string.IsNullOrEmpty(file)) return;

            using (var src = Cv2.ImDecode(File.ReadAllBytes(file), ImreadModes.Color))
            {
                if (src == null || src.Empty()) return;

                using (var outp = SkinWhiten(src, strength))
                {
                    File.WriteAllBytes(file, outp.ImEncode(".jpg"));
                }
            }
        }

        public static Mat SkinWhiten(Mat src, double strength = 1.0)
        {
            if (strength <= 0) return src.Clone();
            double s = Math.Min(1.5, strength);

            Mat skin = SkinMaskWhiten(src);

            // 覆盖率上限：肤色最多占 35% 生效，避免大面积误判把整图提亮
            int count = Cv2.CountNonZero(skin);
            double coverage = (double)count / (src.Rows * src.Cols);
            double w = s;
            if (coverage > 0.40) w *= 0.40 / coverage;

            // 自适应亮度窗口：以肤色平均亮度为中心 ±45，排除明显更亮的墙面
            using (var labPre = new Mat())
            {
                Cv2.CvtColor(src, labPre, ColorConversionCodes.BGR2Lab);
                Mat[] lp = Cv2.Split(labPre);
                try
                {
                    if (Cv2.CountNonZero(skin) > 0)
                    {
                        double meanL = Cv2.Mean(lp[0], skin).Val0;
                        double lo = Math.Max(0, meanL - 45);
                        double hi = Math.Min(255, meanL + 45);

                        using (var mWin = new Mat())
                        {
                            Cv2.InRange(lp[0], new Scalar(lo), new Scalar(hi), mWin);
                            Cv2.BitwiseAnd(skin, mWin, skin);
                        }

                        count = Cv2.CountNonZero(skin);
                        coverage = (double)count / (src.Rows * src.Cols);
                        w = s;
                        if (coverage > 0.60) w *= 0.60 / coverage;
                    }
                }
                finally
                {
                    foreach (var c in lp) c.Dispose();
                }
            }
            using (var lab = new Mat())
            {
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);
                Mat[] ch = Cv2.Split(lab);
                try
                {
                    // L：gamma 0.62 提亮中间调（越暗的皮肤提得越多）
                    using (var lf = new Mat())
                    {
                        ch[0].ConvertTo(lf, MatType.CV_32F, 1.0 / 255.0);
                        using (var g = new Mat())
                        {
                            Cv2.Pow(lf, SkinLGamma, g);
                            g.ConvertTo(ch[0], ch[0].Type(), 255.0);
                        }
                    }

                    // a：略向红（更"嫩"）；b：明显降黄（去棕）
                    // 基准 128 由系数自动补偿，RedShift/YellowShift 才是"偏色量"
                    double aBeta = 128 * (1.0 - SkinAAlpha) + SkinABeta;
                    double bBeta = 128 * (1.0 - SkinBAlpha) + SkinBShift;
                    ch[1].ConvertTo(ch[1], ch[1].Type(), SkinAAlpha, aBeta);
                    ch[2].ConvertTo(ch[2], ch[2].Type(), SkinBAlpha, bBeta);

                    using (var merged = new Mat())
                    {
                        Cv2.Merge(ch, merged);
                        using (var white = new Mat())
                        {
                            Cv2.CvtColor(merged, white, ColorConversionCodes.Lab2BGR);

                            using (var m = new Mat())
                            using (var mf = new Mat())
                            using (var m3 = new Mat())
                            using (var inv3 = new Mat())
                            using (var a = new Mat())
                            using (var b = new Mat())
                            using (var srcF = new Mat())
                            using (var wF = new Mat())
                            {
                                skin.ConvertTo(m, MatType.CV_32FC1, w / 255.0);

                                // 亮度权重：越亮的区域越不动（白墙、高光自动豁免）
                                using (var gray = new Mat())
                                using (var gF = new Mat())
                                using (var inv = new Mat())
                                {
                                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                                    gray.ConvertTo(gF, MatType.CV_32F, 1.0 / 255.0);
                                    using (var ones1 = new Mat(gF.Size(), MatType.CV_32FC1, Scalar.All(1.0)))
                                    {
                                        gF.ConvertTo(inv, MatType.CV_32F, 0.75, 0);   // 保留 75% 亮度保护（亮处少动）
                                        Cv2.Subtract(ones1, inv, inv);
                                    }
                                    Cv2.Multiply(m, inv, m);
                                }

                                Cv2.GaussianBlur(m, mf, new OpenCvSharp.Size(31, 31), 0);
                                Cv2.Merge(new[] { mf, mf, mf }, m3);

                                using (var ones = new Mat(m3.Size(), MatType.CV_32FC3, Scalar.All(1.0)))
                                    Cv2.Subtract(ones, m3, inv3);

                                src.ConvertTo(srcF, MatType.CV_32FC3);
                                white.ConvertTo(wF, MatType.CV_32FC3);

                                Cv2.Multiply(srcF, inv3, a);
                                Cv2.Multiply(wF, m3, b);
                                Cv2.Add(a, b, a);

                                var dst = new Mat();
                                a.ConvertTo(dst, MatType.CV_8UC3);
                                return dst;
                            }
                        }
                    }
                }
                finally
                {
                    foreach (var c in ch) c.Dispose();
                }
            }
        }

        // 肤色掩膜：YCrCb 范围 + Cr> Cb + 饱和度下限
        private static Mat SkinMaskWhiten(Mat src)
        {
            var mask = new Mat();

            using (var ycc = new Mat())
            using (var hsv = new Mat())
            {
                Cv2.CvtColor(src, ycc, ColorConversionCodes.BGR2YCrCb);
                Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

                Cv2.InRange(ycc, new Scalar(60, 135, 85), new Scalar(250, 173, 125), mask);

                // 条件2：Cr 明显高于 Cb（肤色偏红；白墙/灰墙不满足）
                Mat[] cy = Cv2.Split(ycc);
                using (var diff = new Mat())
                using (var mRed = new Mat())
                {
                    Cv2.Subtract(cy[1], cy[2], diff);   // Cr - Cb，8U 负值截断为 0
                    Cv2.Threshold(diff, mRed, 9, 255, ThresholdTypes.Binary);
                    Cv2.BitwiseAnd(mask, mRed, mask);
                }
                foreach (var c in cy) c.Dispose();

                // 条件3：饱和度不能太低（再排一次白墙/浅灰）
                Mat[] ch = Cv2.Split(hsv);
                using (var mSat = new Mat())
                {
                    Cv2.Threshold(ch[1], mSat, 26, 255, ThresholdTypes.Binary);
                    Cv2.BitwiseAnd(mask, mSat, mask);
                }
                foreach (var c in ch) c.Dispose();

                using (var k = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(7, 7)))
                {
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Close, k);
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Open, k);
                }
            }

            return mask;
        }
    }
}