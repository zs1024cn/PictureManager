using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
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

        private static string GetBucketByHash(byte[] hash)
        {
            if (hash == null) return "";

            string key;
            if (hash != null && hash.Length >= 8)
            {
                int step = hash.Length / 8;
                var sampled = new byte[8];
                for (int k = 0; k < 8; k++)
                    sampled[k] = hash[k * step];
                key = Convert.ToBase64String(sampled);
            }
            else
            {
                key = "short";
            }
            return key;
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

            // 按 size 降序，取前 6
            var sorted = kps.OrderByDescending(k => k.Size).Take(6).ToArray();
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