//Compare\SiftCompare.cs
using OpenCvSharp;
using System;
using System.Diagnostics;

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

        // SIFT 相似度是绝对匹配数，用 >= MinMatchPoints 判定
        // threshold 转换为 matchRatio（Lowe's ratio）
        public override bool IsSimilar(object a, object b, double threshold)
        {
            if (a == null || b == null) return false;
            var s1 = (Tuple<KeyPoint[], Mat>)a;
            var s2 = (Tuple<KeyPoint[], Mat>)b;
            if (s1.Item2 == null || s1.Item2.Empty()) return false;
            if (s2.Item2 == null || s2.Item2.Empty()) return false;

            //Debug.WriteLine($"[SIFT] target kp={s1.Item1?.Length}");
            //Debug.WriteLine($"[SIFT] other kp={s2.Item1?.Length}");
            try
            {
             
                //float matchRatio = 1.2f - (float)threshold * 0.6f;  // 0.5→0.9, 0.9→0.66
                float matchRatio = 1.0f - (float)threshold * 0.4f;
                // 0.5 → 0.80
                // 0.7 → 0.72   ← 比 0.78 严，预计 33 → 10 左右
                // 0.9 → 0.64
                // float matchRatio = 1.5f - (float)threshold;
                int matches = Duplicate.MatchSiftFeatures(s1.Item2, s2.Item2, matchRatio);
                return matches >= MinMatchPoints;
            }
            catch { return false; }
        }
    }
}