//Core\Duplicate.Match.cs
using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Linq;

namespace PictureManager
{
    public partial class Duplicate
    {
        private static int _siftLogCount = 0;
        // ========== DCT 比较 ==========

        public static bool CompareDct(Mat mat1, Mat mat2, double minRatio, out double correlation)
        {
            bool similar = false;
            correlation = 0;

            if (CompareDctMode == "Correl")
            {
                correlation = Cv2.CompareHist(mat1, mat2, HistCompMethods.Correl);
                if (correlation >= minRatio) similar = true;
            }
            if (CompareDctMode == "Chisqr")
            {
                correlation = Cv2.CompareHist(mat1, mat2, HistCompMethods.Chisqr);
                if (correlation < (1 - minRatio) * 500) similar = true;
                correlation = Cv2.CompareHist(mat1, mat2, HistCompMethods.ChisqrAlt);
                if (correlation < (1 - minRatio) * 500 || correlation < 10) similar = true;
            }

            if (CompareDctMode == "Bhattacharyya")
            {
                correlation = Cv2.CompareHist(mat1, mat2, HistCompMethods.Bhattacharyya);
                if (correlation < (1 - minRatio) || correlation < 0.1) similar = true;
            }
            if (CompareDctMode == "Hellinger")
            {
                correlation = Cv2.CompareHist(mat1, mat2, HistCompMethods.Hellinger);
                if (correlation < (1 - minRatio)) similar = true;
            }
            if (CompareDctMode == "KLDiv")
            {
                double c1 = Cv2.CompareHist(mat1, mat2, HistCompMethods.KLDiv);
                double c2 = Cv2.CompareHist(mat1, mat2, HistCompMethods.KLDiv);
                if (Math.Min(c1, c2) < 1 - minRatio) similar = true;
            }
            return similar;
        }

        // ========== HSV 比较 ==========

        public static float CompareHsv(string file1, string file2)
        {
            var f1 = GetHsv(file1);
            var f2 = GetHsv(file2);
            return CompareHsv(f1, f2);
        }

        public static float CompareHsv(float[] f1, float[] f2)
        {
            if (f1 == null || f2 == null) return 0;

            float sqDist = 0;
            for (int i = 0; i < f1.Length; i++)
            {
                float diff = f1[i] - f2[i];
                sqDist += diff * diff;
            }

            return 1.0f - (float)Math.Sqrt(sqDist / f1.Length);
        }

        // ========== SIFT 匹配 ==========
        public static int MatchSiftFeatures(Mat descriptors1, Mat descriptors2, float matchThreshold = 0.75f)
        {
            if (descriptors1.Empty() || descriptors2.Empty()) return 0;

            // ★ 点数少时，暴力匹配比 FLANN 建索引快得多
            if (descriptors1.Rows <= 100 && descriptors2.Rows <= 100)
            {
                return MatchByBF(descriptors1, descriptors2, matchThreshold);
            }

            try
            {
                using (var matcher = new FlannBasedMatcher())
                {
                    var matches = matcher.KnnMatch(descriptors1, descriptors2, 2);
                    int goodMatches = 0;
                    foreach (var match in matches)
                    {
                        if (match.Length == 2 && match[0].Distance < matchThreshold * match[1].Distance)
                            goodMatches++;
                    }
                    return goodMatches;
                }
            }
            catch
            {
                return MatchByBF(descriptors1, descriptors2, matchThreshold);
            }
        }
        private static int MatchByBF(Mat descriptors1, Mat descriptors2, float matchThreshold)
        {
            using (var matcher = new BFMatcher(NormTypes.L2, false))
            {
                var matches = matcher.KnnMatch(descriptors1, descriptors2, 2);
                int goodMatches = 0;
                foreach (var match in matches)
                {
                    if (match.Length == 2 && match[0].Distance < matchThreshold * match[1].Distance)
                        goodMatches++;
                }
                return goodMatches;
            }
        }
    }
}