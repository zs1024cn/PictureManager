namespace PictureManager
{
    /// <summary>
    /// 花玻璃马赛克参数。独立类，方便 EnC 修改。
    /// </summary>
    public static class GlassMosaicParams
    {
        public static int GridLines = 12;           // 网格密度
        public static int ShadowWidth = 1;          // 间隔线宽
        public static double ShadowFactor = 1.1;    // 间隔线 = 肉色 × 0.8
        public static double TriCoef = 1.0;         // 三角面积系数
        public static double MinBlackRatio = 0.001; // 黑红判定阈值
        public static int BlackThreshold = 20;      // 黑点：RGB < 40
    }
}