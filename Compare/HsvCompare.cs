using System;

namespace PictureManager
{
    public class HsvCompare : PictureCompare
    {
        public override string Name => "HSV";

        protected override object GetFeature(ImageFeature f) => f.Hsv;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            float[] f1 = (float[])a;
            float[] f2 = (float[])b;
            if (f1.Length == 0 || f2.Length == 0) return 0;

            float sqDist = 0;
            int len = Math.Min(f1.Length, f2.Length);
            for (int i = 0; i < len; i++)
            {
                float diff = f1[i] - f2[i];
                sqDist += diff * diff;
            }
            return 1.0f - (float)Math.Sqrt(sqDist / len);
        }
    }
}