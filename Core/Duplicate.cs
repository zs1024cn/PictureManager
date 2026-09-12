using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PictureManager
{
    /// <summary>
    /// 查重底层算法。
    /// 拆分后：
    /// - 缓存管理 → Duplicate.Cache.cs
    /// - 特征提取 → Duplicate.Extract.cs
    /// - 比较/匹配 → Duplicate.Match.cs
    /// - 分桶 → Duplicate.Bucket.cs
    /// </summary>
    public partial class Duplicate
    {
        // ========== 缓存字段 ==========

        public static Dictionary<string, Mat> DctCache = new Dictionary<string, Mat>();
        private static Dictionary<string, Mat> DctCacheDeleted = new Dictionary<string, Mat>();

        public static Dictionary<string, Tuple<KeyPoint[], Mat>> SiftCache = new Dictionary<string, Tuple<KeyPoint[], Mat>>();
        private static Dictionary<string, Tuple<KeyPoint[], Mat>> SiftCacheDeleted = new Dictionary<string, Tuple<KeyPoint[], Mat>>();

        public static Dictionary<string, byte[]> HashCache = new Dictionary<string, byte[]>();
        private static Dictionary<string, byte[]> HashCacheDeleted = new Dictionary<string, byte[]>();

        public static ConcurrentDictionary<string, float[]> HsvCache = new ConcurrentDictionary<string, float[]>();
        private static ConcurrentDictionary<string, float[]> HsvCacheDeleted = new ConcurrentDictionary<string, float[]>();

        private static object _cacheLock = new object();
        private static object _hsvLock = new object();

        // ========== 参数 ==========

        public static string CompareDctMode = "Correl";
    }
}