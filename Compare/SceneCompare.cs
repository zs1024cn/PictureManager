using System;

namespace PictureManager
{
    public class SceneCompare : PictureCompare
    {
        public override string Name => "Scene";

        protected override object GetFeature(ImageFeature f) => f.Scene;

        public override double Compare(object a, object b)
        {
            if (a == null || b == null) return 0;
            float[] fa = (float[])a;
            float[] fb = (float[])b;
            if (fa.Length == 0 || fb.Length == 0) return 0;
            return FaceCompare.CosineSimilarity(fa, fb);
        }
    }
}