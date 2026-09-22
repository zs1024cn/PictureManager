//Compare\PictureCompare.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PictureManager
{
    /// <summary>
    /// 图片对比抽象基类。
    /// 分桶方式（bucketType）和比较算法独立。
    /// </summary>
    public abstract class PictureCompare
    {
        public abstract string Name { get; }

        protected abstract object GetFeature(ImageFeature f);

        public abstract double Compare(object a, object b);

        public virtual bool IsSimilar(object a, object b, double threshold)
        {
            return Compare(a, b) >= threshold;
        }

        /// <summary>
        /// 查找与 targetFile 相似的文件（限定同桶）。
        /// 只比较已提取对应特征的图。
        /// </summary>
        public List<string> FindSimilar(string targetFile, double threshold, string bucketType = "Allin")
        {
            var result = new List<string>();
            var cache = FeatureCacheManager.Instance;

            var target = cache.GetOrExtract(targetFile, Name);
            if (target == null)
            {
                Debug.WriteLine($"[FindSimilar:{Name}] target null, file={targetFile}");
                return result;
            }

            object targetFeature = GetFeature(target);
            if (targetFeature == null)
            {
                Debug.WriteLine($"[FindSimilar:{Name}] targetFeature null");
                return result;
            }

            int total = 0, checkedCount = 0, skippedNull = 0;
            foreach (var kvp in cache.GetAll())
            {
                total++;
                if (kvp.Key == targetFile) continue;

                object otherFeature = GetFeature(kvp.Value);
                if (otherFeature == null) { skippedNull++; continue; }
                if (otherFeature is float[] fa && fa.Length == 0) { skippedNull++; continue; }

                checkedCount++;
                try
                {
                    // ★★★ M218：只针对 Hash，只打前 5 张 ★★★
                    if (Name == "Hash" && checkedCount <= 5)
                    {
                        byte[] t = (byte[])targetFeature;
                        byte[] o = (byte[])otherFeature;
                        Debug.WriteLine($"[Hash] t.len={t.Length}, o.len={o.Length}, " +
                            $"t[0..5]={string.Join(",", t.Take(5))}, " +
                            $"o[0..5]={string.Join(",", o.Take(5))}, " +
                            $"cmp={Compare(targetFeature, otherFeature):F4}");
                    }

                    if (IsSimilar(targetFeature, otherFeature, threshold))
                        result.Add(kvp.Key);
                }
                catch { }
            }

            Debug.WriteLine($"[FindSimilar:{Name}] total={total}, checked={checkedCount}, skippedNull={skippedNull}, hits={result.Count}, targetType={targetFeature?.GetType().Name}");
            return result;
        }

        /// <summary>
        /// 查找所有重复组（只处理已提取对应特征的图）。
        /// </summary>
        public List<List<string>> FindAllDuplicates(double threshold, string bucketType = "Allin", Action<string> progress = null)
        {
            var cache = FeatureCacheManager.Instance;

            // 只取有对应特征的图
            var files = new List<string>();
            foreach (var kvp in cache.GetAll())
            {
                var feat = GetFeature(kvp.Value);
                if (feat == null) continue;
                // DNN 特征：float[0] 表示无有效结果，跳过
                if (feat is float[] fa && fa.Length == 0) continue;
                files.Add(kvp.Key);
            }

            // 分桶
            var buckets = new Dictionary<string, List<string>>();
            foreach (var file in files)
            {
                string key = Duplicate.GetBucket(bucketType, file);
                if (string.IsNullOrEmpty(key)) key = "default";
                if (!buckets.ContainsKey(key))
                    buckets[key] = new List<string>();
                buckets[key].Add(file);
            }

            Debug.WriteLine($"FindAllDuplicates [{Name}] bucket={bucketType}: 有效文件 {files.Count}, 桶数 {buckets.Count}, 最大桶 {(buckets.Count > 0 ? buckets.Values.Max(b => b.Count) : 0)}");

            var visited = new HashSet<string>();
            var groups = new List<List<string>>();

            int bucketIndex = 0;
            int totalBuckets = buckets.Count;
            foreach (var bucket in buckets.Values)
            {
                bucketIndex++;
                progress?.Invoke($"桶 {bucketIndex}/{totalBuckets}, 桶内 {bucket.Count} 张");

                // 超大桶跳过（避免 O(n²) 爆炸）
                if (bucket.Count > DuplicateSettings.MaxBucketLimit)
                {
                    Debug.WriteLine($"跳过超大桶: {bucket.Count} 张");
                    continue;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    if (visited.Contains(bucket[i])) continue;

                    var feature1 = GetFeature(cache.GetAll()[bucket[i]]);
                    if (feature1 == null) continue;

                    var group = new List<string> { bucket[i] };
                    visited.Add(bucket[i]);

                    for (int j = i + 1; j < bucket.Count; j++)
                    {
                        if (visited.Contains(bucket[j])) continue;

                        var feature2 = GetFeature(cache.GetAll()[bucket[j]]);
                        if (feature2 == null) continue;

                        try
                        {
                            if (IsSimilar(feature1, feature2, threshold))
                            {
                                group.Add(bucket[j]);
                                visited.Add(bucket[j]);
                            }
                        }
                        catch { }
                    }

                    if (group.Count > 1)
                        groups.Add(group);
                }
            }

            return groups;
        }
    }
}