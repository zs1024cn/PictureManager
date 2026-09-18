using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Diagnostics;
using System.Linq;

namespace PictureManager
{
    public partial class Duplicate
    {
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

        private static string GetBucketByDct(Mat hist)
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

        private static string GetBucketBySift(Tuple<KeyPoint[], Mat> sift)
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