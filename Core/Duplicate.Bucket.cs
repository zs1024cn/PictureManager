//Core\Duplicate.Bucket.cs
using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Diagnostics;
using System.Linq;

namespace PictureManager
{
    public partial class Duplicate
    {
        public static string GetBucketByMode(string mode, ImageFeature feature, int param)
        {
            switch (mode)
            {
                case "Hash":
                    return feature.Hash != null ? GetBucketByHashWithLevels(feature.Hash, param) : null;
                case "DCT":
                    return feature.Dct != null ? GetBucketByDctWithLevels(feature.Dct, param) : null;
                case "SIFT":
                    return feature.Sift != null ? GetBucketBySiftWithPoints(feature.Sift, param) : null;
                default:
                    return null;
            }
        }
        public static string GetBucket(string bucketType, string file)
        {
            string key = "Allin";
            if (bucketType == "Hash")
            {
                if (HashCache.TryGetValue(file, out byte[] hash) && hash != null)
                    key = GetBucketByHash(hash);
            }
            else if (bucketType == "Dct")
            {
                if (DctCache.TryGetValue(file, out Mat hist) && hist != null)
                    key = GetBucketByDct(hist);
            }
            else if (bucketType == "Sift")
            {
                if (SiftCache.TryGetValue(file, out var sift) && sift != null)
                    key = GetBucketBySift(sift);
            }
            return key;
        }
        // ★ M343：带档位的 Hash 分桶
        public static string GetBucketByHashWithLevels(byte[] hash, int levels)
        {
            if (hash == null || hash.Length < 8) return "short";
            if (levels < 2) levels = 2;

            int blockCount = 8;
            int blockSize = hash.Length / blockCount;
            if (blockSize == 0) return "short";

            double step = DuplicateSettings.HashGray / (double)levels;
            if (step < 1) step = 1;

            var sb = new System.Text.StringBuilder();
            for (int b = 0; b < blockCount; b++)
            {
                double sum = 0;
                for (int i = 0; i < blockSize; i++)
                    sum += hash[b * blockSize + i];
                double blockMean = sum / blockSize;

                int q = (int)(blockMean / step);
                if (q >= levels) q = levels - 1;
                if (q < 0) q = 0;
                sb.Append(q);
            }
            return sb.ToString();
        }

        // ★ M343：带档位的 DCT 分桶
        public static string GetBucketByDctWithLevels(Mat hist, int levels)
        {
            if (hist == null || hist.Empty()) return "";
            if (levels < 2) levels = 2;
            if (levels > 4) levels = 4;   // DCT 每段最多 4 档

            int perChannel = 256;
            int segments = 4;
            int perSeg = perChannel / segments;
            int perLevel = perSeg / levels;

            string result = "";
            for (int ch = 0; ch < 3; ch++)
            {
                int offset = ch * perChannel;
                for (int s = 0; s < segments; s++)
                {
                    float maxVal = 0;
                    int maxIdx = 0;
                    for (int i = 0; i < perSeg; i++)
                    {
                        float val = hist.At<float>(offset + s * perSeg + i);
                        if (val > maxVal) { maxVal = val; maxIdx = i; }
                    }
                    int level = maxIdx / perLevel;
                    if (level >= levels) level = levels - 1;
                    result += level.ToString();
                }
            }
            return result;
        }

        // ★ M343：带点数的 SIFT 分桶
        public static string GetBucketBySiftWithPoints(Tuple<KeyPoint[], Mat> sift, int points)
        {
            if (sift == null || sift.Item1 == null) return "";
            if (points < 4) points = 4;
            if (points > 20) points = 20;

            var sorted = sift.Item1.OrderByDescending(k => k.Size).Take(points).ToArray();
            int n = sorted.Length;
            if (n < 2) return "";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n - 1; i++)
            {
                float dx = sorted[i + 1].Pt.X - sorted[i].Pt.X;
                float dy = sorted[i + 1].Pt.Y - sorted[i].Pt.Y;

                int quadrant;
                if (dx >= 0 && dy >= 0) quadrant = 0;
                else if (dx < 0 && dy >= 0) quadrant = 1;
                else if (dx < 0 && dy < 0) quadrant = 2;
                else quadrant = 3;

                sb.Append(quadrant);
            }
            return sb.ToString();
        }
        public static string GetBucketByHash(byte[] hash)
        {
            if (hash == null || hash.Length < 8) return "short";

            int blockCount = 8;
            int blockSize = hash.Length / blockCount;
            if (blockSize == 0) return "short";

            // 固定 step，基于 HashGray（与 ExtractHashByMat 一致）
            double step = DuplicateSettings.HashGray / 4.0;
            if (step < 1) step = 1;

            var sb = new System.Text.StringBuilder();
            for (int b = 0; b < blockCount; b++)
            {
                double sum = 0;
                for (int i = 0; i < blockSize; i++)
                    sum += hash[b * blockSize + i];
                double blockMean = sum / blockSize;

                int q;
                if (blockMean < step * 1) q = 0;
                else if (blockMean < step * 2) q = 1;
                else if (blockMean < step * 3) q = 2;
                else q = 3;
                sb.Append(q);
            }
            return sb.ToString();
        }
        public static string GetBucketByHash_bak(byte[] hash)
        {
            if (hash == null || hash.Length < 8) return "short";

            int blockCount = 8;
            int blockSize = hash.Length / blockCount;
            if (blockSize == 0) return "short";

            var sb = new System.Text.StringBuilder();

            double[] blockMeans = new double[blockCount];
            for (int b = 0; b < blockCount; b++)
            {
                // 每块的均值
                double sum = 0;
                for (int i = 0; i < blockSize; i++)
                    sum += hash[b * blockSize + i];
                double blockMean = sum / blockSize;
                blockMeans[b] = blockMean;

                // 量化到 3 档，保留一下，也能找出一些
                //int q = Math.Min(2, (int)(blockMean / 2));
                //sb.Append(q);
                
                // 量化到 3 档（0-1.5, 1.5-3, 3+）
                //int q;
                //if (blockMean < 1.5) q = 0;
                //else if (blockMean < 3.0) q = 1;
                //else q = 2;
                //sb.Append(q);

                // 量化到 4 档（0-1, 1-2, 2-3, 3+）
                int q;
                if (blockMean < 1.0) q = 0;
                else if (blockMean < 2.0) q = 1;
                else if (blockMean < 3.0) q = 2;
                else q = 3;
                sb.Append(q);
            }

            // 诊断：前几条 hash 的块均值
            if (_bucketLogCount < 100)
            {
                _bucketLogCount++;
                //Debug.WriteLine($"[GetBucketByHash] hash.Length={hash.Length}, blockMeans=[{string.Join(",", blockMeans.Select(x => x.ToString("F2")))}]");
            }

            return sb.ToString();  // 3^8 = 6561 种
        }

        public static string GetBucketByDct(Mat hist)
        {
            if (hist == null || hist.Empty()) return "";

            int perChannel = 256;
            int segments = 4;
            int perSeg = perChannel / segments;
            int levels = 4;
            int perLevel = perSeg / levels;

            string result = "";
            for (int ch = 0; ch < 3; ch++)
            {
                int offset = ch * perChannel;
                for (int s = 0; s < segments; s++)
                {
                    float maxVal = 0;
                    int maxIdx = 0;
                    for (int i = 0; i < perSeg; i++)
                    {
                        float val = hist.At<float>(offset + s * perSeg + i);
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxIdx = i;
                        }
                    }
                    int level = maxIdx / perLevel;
                    if (level >= levels) level = levels - 1;
                    result += (level / 2).ToString();
                }
            }
            return result;
        }

        public static string GetBucketBySift(Tuple<KeyPoint[], Mat> sift)
        {
            if (sift == null) return "";
            if (sift.Item1 == null || sift.Item1.Length < 2) return "";

            KeyPoint[] kps = sift.Item1;

            // 按 size 降序，取前 7
            var sorted = kps.OrderByDescending(k => k.Size).Take(7).ToArray();
            int n = sorted.Length;
            if (n < 2) return "";

            // 相邻点的"相对位置"（4 象限）
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n - 1; i++)
            {
                float dx = sorted[i + 1].Pt.X - sorted[i].Pt.X;
                float dy = sorted[i + 1].Pt.Y - sorted[i].Pt.Y;

                int quadrant;
                if (dx >= 0 && dy >= 0) quadrant = 0;   // 右下
                else if (dx < 0 && dy >= 0) quadrant = 1; // 左下
                else if (dx < 0 && dy < 0) quadrant = 2;  // 左上
                else quadrant = 3;                        // 右上

                sb.Append(quadrant);
            }

            return sb.ToString();  // 4^(n-1) 种
        }
    }
}