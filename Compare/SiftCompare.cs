using OpenCvSharp;
using System;

namespace PictureManager
{
    public class SiftCompare : PictureCompare
    {
        public override string Name => "SIFT";

        /// <summary>
        /// 最小匹配点数（绝对数量，不是比例）
        /// </summary>
        public int MinMatchPoints { get; set; } = DuplicateSettings.SiftMinMatchPoints;
        public float MatchRatio { get; set; } = DuplicateSettings.SiftMatchRatio;

        protected override object GetFeature(ImageFeature f) => f.Sift;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            var s1 = (Tuple<KeyPoint[], Mat>)a;
            var s2 = (Tuple<KeyPoint[], Mat>)b;
            if (s1.Item2 == null || s1.Item2.Empty()) return 0;
            if (s2.Item2 == null || s2.Item2.Empty()) return 0;

            try
            {
                return Duplicate.MatchSiftFeatures(s1.Item2, s2.Item2, MatchRatio);
            }
            catch { return 0; }
        }

        // SIFT 的相似度是绝对匹配数，用 >= MinMatchPoints 判定
        public override bool IsSimilar(object a, object b, double threshold)
        {
            double matches = Compare(a, b);
            return matches >= MinMatchPoints;
        }
    }
}