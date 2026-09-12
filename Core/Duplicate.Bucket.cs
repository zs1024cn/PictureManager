using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;

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

            Mat desc = sift.Item2;
            if (desc.Empty()) return "";

            float sum = 0;
            for (int r = 0; r < desc.Rows; r++)
                for (int c = 0; c < desc.Cols; c++)
                    sum += desc.At<float>(r, c);
            float mean = sum / (desc.Rows * desc.Cols);

            float variance = 0;
            for (int r = 0; r < desc.Rows; r++)
                for (int c = 0; c < desc.Cols; c++)
                {
                    float diff = desc.At<float>(r, c) - mean;
                    variance += diff * diff;
                }
            variance /= (desc.Rows * desc.Cols);

            int kpCount = sift.Item1.Length;
            int kpBucket = Math.Min(9, kpCount / 5);

            int meanBucket = (int)(mean * 1000) % 10;
            if (meanBucket < 0) meanBucket = 0;

            int varBucket = (int)(variance * 1000) % 10;
            if (varBucket < 0) varBucket = 0;

            return $"{kpBucket:D1}{meanBucket:D1}{varBucket:D1}";
        }
    }
}