//Compare\ImageFeature.cs
using OpenCvSharp;
using System;

namespace PictureManager
{
    /// <summary>
    /// 一张图片的全部特征。
    /// 基础特征（Hash/DCT/SIFT/HSV）一次读图提取。
    /// DNN 特征（Face/Pose/Scene）按需提取。
    ///
    /// DNN 特征约定：
    /// - null      = 从未计算过
    /// - float[0]  = 计算过，但没找到有效结果（无人脸等）
    /// - float[N]  = 有效特征
    /// </summary>
    public class ImageFeature
    {
        public string FilePath { get; set; }
        public DateTime ExtractTime { get; set; }

        // 基础特征
        public byte[] Hash { get; set; }
        public Mat Dct { get; set; }
        public Tuple<KeyPoint[], Mat> Sift { get; set; }
        public float[] Hsv { get; set; }

        // DNN 特征
        public float[] Face { get; set; }
        public float[] Pose { get; set; }
        public float[] Scene { get; set; }
    }
}