//Compare\PoseCompare.cs
using System;

namespace PictureManager
{
    public class PoseCompare : PictureCompare
    {
        public override string Name => "Pose";

        protected override object GetFeature(ImageFeature f) => f.Pose;

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