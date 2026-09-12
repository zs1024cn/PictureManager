using System;

namespace PictureManager
{
    public class FaceCompare : PictureCompare
    {
        public override string Name => "Face";

        protected override object GetFeature(ImageFeature f) => f.Face;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            float[] fa = (float[])a;
            float[] fb = (float[])b;
            // float[0] 表示"计算过但没人脸"，不参与比较
            if (fa.Length == 0 || fb.Length == 0) return 0;
            return CosineSimilarity(fa, fb);
        }

        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null) return 0;
            int len = Math.Min(a.Length, b.Length);
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < len; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }
    }
}