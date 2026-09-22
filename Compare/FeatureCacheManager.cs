//Compare\FeatureCacheManager.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager
{
    /// <summary>
    /// 特征统一缓存管理器，带回收站。
    /// 按模式提取：Hash 模式一次取 4 种，其他模式单独提取。
    /// </summary>
    public class FeatureCacheManager
    {
        private static FeatureCacheManager _instance;
        public static FeatureCacheManager Instance
        {
            get
            {
                if (_instance == null) _instance = new FeatureCacheManager();
                return _instance;
            }
        }

        private readonly ConcurrentDictionary<string, ImageFeature> _cache
            = new ConcurrentDictionary<string, ImageFeature>();

        private readonly ConcurrentDictionary<string, ImageFeature> _recycleBin
            = new ConcurrentDictionary<string, ImageFeature>();

        public int Count => _cache.Count;
        public int RecycleCount => _recycleBin.Count;

        /// <summary>
        /// 按模式提取。
        /// - "Hash"：提取 Hash + DCT + SIFT + HSV（四种基础）
        /// - "DCT"："SIFT"："HSV"："Face"："Pose"："Scene"：只提取对应的一种
        /// </summary>
        public ImageFeature GetOrExtract(string file, string mode)
        {
            if (string.IsNullOrEmpty(file)) return null;

            // 回收站命中直接返回
            if (_recycleBin.TryGetValue(file, out var recycled))
                return recycled;

            if (!_cache.TryGetValue(file, out var feature))
            {
                feature = new ImageFeature { FilePath = file };
                _cache[file] = feature;
            }

            // 按 mode 提取（若缺少）
            switch (mode)
            {
                case "Hash":
                    // Hash 模式：如果 Hash 已经有了就跳过（DCT/SIFT/HSV 也一起有）
                    if (feature.Hash == null)
                        ImageFeatureExtractor.ExtractBasic(feature, file);
                    break;

                case "DCT":
                    if (feature.Dct == null)
                        ImageFeatureExtractor.ExtractDct(feature, file);
                    break;

                case "SIFT":
                    if (feature.Sift == null)
                        ImageFeatureExtractor.ExtractSift(feature, file);
                    break;

                case "HSV":
                    if (feature.Hsv == null)
                        ImageFeatureExtractor.ExtractHsv(feature, file);
                    break;

                case "Face":
                    // null = 未计算，float[0] = 已计算（无结果）
                    if (feature.Face == null)
                        ImageFeatureExtractor.ExtractFace(feature, file);
                    break;

                case "Pose":
                    if (feature.Pose == null)
                        ImageFeatureExtractor.ExtractPose(feature, file);
                    break;

                case "Scene":
                    if (feature.Scene == null)
                        ImageFeatureExtractor.ExtractScene(feature, file);
                    break;

                default:
                    // 未知 mode，按 Hash 处理
                    if (feature.Hash == null)
                        ImageFeatureExtractor.ExtractBasic(feature, file);
                    break;
            }

            return feature;
        }

        /// <summary>
        /// 兼容旧调用：默认按 Hash 模式（一次取 4 种基础特征）。
        /// </summary>
        public ImageFeature GetOrExtract(string file)
        {
            return GetOrExtract(file, "Hash");
        }

        public void MoveToRecycle(string file)
        {
            if (_cache.TryRemove(file, out ImageFeature f))
                _recycleBin[file] = f;
        }

        public void Restore(string file)
        {
            if (_recycleBin.TryRemove(file, out ImageFeature f))
                _cache[file] = f;
        }

        public IReadOnlyDictionary<string, ImageFeature> GetAll()
        {
            return _cache;
        }

        public void ClearAll()
        {
            _cache.Clear();
            _recycleBin.Clear();
            Duplicate.CleanHashDctSiftCache();
        }

        public void KeepOnly(HashSet<string> keepFiles)
        {
            foreach (var key in _cache.Keys.ToList())
            {
                if (!keepFiles.Contains(key))
                    MoveToRecycle(key);
            }
        }
    }
}