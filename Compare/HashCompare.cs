using System;

namespace PictureManager
{
    /// <summary>
    /// Hash 感知哈希对比。相似度 = 相同字节数 / 总字节数。
    /// </summary>
    public class HashCompare : PictureCompare
    {
        public override string Name => "Hash";

        protected override object GetFeature(ImageFeature f) => f.Hash;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            byte[] h1 = (byte[])a;
            byte[] h2 = (byte[])b;
            if (h1.Length == 0 || h2.Length == 0) return 0;

            int len = Math.Min(h1.Length, h2.Length);
            int sameCount = 0;
            for (int i = 0; i < len; i++)
                if (h1[i] == h2[i]) sameCount++;

            return (double)sameCount / len;
        }
    }
}