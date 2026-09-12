namespace PictureManager
{
    /// <summary>
    /// 查重参数集中管理。Form1 修改这里，Duplicate 和 Extractor 从这里读。
    /// </summary>
    public static class DuplicateSettings
    {
        // Hash 参数
        public static int HashSize = 32;
        public static int HashGray = 4;
        public static float HashRed = 1.0f;
        public static float HashGreen = 1.0f;
        public static float HashBlue = 1.0f;
        public static int HashCrop = 0;

        // SIFT 参数
        public static int SiftMaxFeatures = 500;
        public static int SiftMaxDim = 800;

        // SIFT 匹配阈值
        public static int SiftMinMatchPoints = 7;
        public static float SiftMatchRatio = 0.75f;

        // DCT 参数
        public static string DctMode = "Correl";

        // 默认相似度阈值（Hash/HSV/DCT 用）
        public static double DefaultThreshold = 0.9;

        // 分桶参数
        public static string BucketType = "Hash";
    }
}