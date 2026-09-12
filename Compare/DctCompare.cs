using OpenCvSharp;
using System;

namespace PictureManager
{
    /// <summary>
    /// DCT 直方图对比。支持 Correl / Chisqr / Bhattacharyya / Hellinger / KLDiv。
    /// </summary>
    public class DctCompare : PictureCompare
    {
        public override string Name => "DCT";

        /// <summary>比较方法</summary>
        public string Mode { get; set; } = DuplicateSettings.DctMode;

        protected override object GetFeature(ImageFeature f) => f.Dct;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            Mat m1 = (Mat)a, m2 = (Mat)b;
            if (m1.Empty() || m2.Empty()) return 0;

            try
            {
                switch (Mode)
                {
                    case "Correl": return Cv2.CompareHist(m1, m2, HistCompMethods.Correl);
                    case "Chisqr": return Cv2.CompareHist(m1, m2, HistCompMethods.Chisqr);
                    case "Bhattacharyya": return Cv2.CompareHist(m1, m2, HistCompMethods.Bhattacharyya);
                    case "Hellinger": return Cv2.CompareHist(m1, m2, HistCompMethods.Hellinger);
                    case "KLDiv": return Cv2.CompareHist(m1, m2, HistCompMethods.KLDiv);
                    default: return Cv2.CompareHist(m1, m2, HistCompMethods.Correl);
                }
            }
            catch { return 0; }
        }

        // 不同 Mode 的阈值语义不同，重写 IsSimilar
        public override bool IsSimilar(object a, object b, double threshold)
        {
            if (a == null || b == null) return false;
            Mat m1 = (Mat)a, m2 = (Mat)b;
            if (m1.Empty() || m2.Empty()) return false;

            try
            {
                switch (Mode)
                {
                    case "Correl":
                        return Cv2.CompareHist(m1, m2, HistCompMethods.Correl) >= threshold;

                    case "Chisqr":
                        return Cv2.CompareHist(m1, m2, HistCompMethods.Chisqr) < (1 - threshold) * 500;

                    case "Bhattacharyya":
                        {
                            double s = Cv2.CompareHist(m1, m2, HistCompMethods.Bhattacharyya);
                            return s < (1 - threshold) || s < 0.1;
                        }

                    case "Hellinger":
                        return Cv2.CompareHist(m1, m2, HistCompMethods.Hellinger) < (1 - threshold);

                    case "KLDiv":
                        return Cv2.CompareHist(m1, m2, HistCompMethods.KLDiv) < 1 - threshold;

                    default:
                        return Cv2.CompareHist(m1, m2, HistCompMethods.Correl) >= threshold;
                }
            }
            catch { return false; }
        }
    }
}