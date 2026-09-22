//Compare\PictureCompare.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

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
            var resultLock = new object();
            var cache = FeatureCacheManager.Instance;

            var target = cache.GetOrExtract(targetFile, Name);
            if (target == null) { Debug.WriteLine($"[FindSimilar:{Name}] target null"); return result; }

            object targetFeature = GetFeature(target);
            if (targetFeature == null) { Debug.WriteLine($"[FindSimilar:{Name}] targetFeature null"); return result; }

            int total = 0, checkedCount = 0, skippedNull = 0;
            var allItems = cache.GetAll().ToList();   // ★ 快照，避免并行时枚举器冲突

            Parallel.ForEach(allItems,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                kvp =>
                {
                    Interlocked.Increment(ref total);
                    if (kvp.Key == targetFile) return;

                    object otherFeature = GetFeature(kvp.Value);
                    if (otherFeature == null) { Interlocked.Increment(ref skippedNull); return; }
                    if (otherFeature is float[] fa && fa.Length == 0) { Interlocked.Increment(ref skippedNull); return; }

                    Interlocked.Increment(ref checkedCount);
                    try
                    {
                        if (IsSimilar(targetFeature, otherFeature, threshold))
                        {
                            lock (resultLock) result.Add(kvp.Key);
                        }
                    }
                    catch { }
                });

            Debug.WriteLine($"[FindSimilar:{Name}] total={total}, checked={checkedCount}, skippedNull={skippedNull}, hits={result.Count}");
            return result;
        }
        /// <summary>
        /// 查找所有重复组（只处理已提取对应特征的图）。
        /// M250：桶间串行 + 桶内并行 + 连通分量分组 + 进度限频。
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

            var groups = new List<List<string>>();

            int bucketIndex = 0;
            int totalBuckets = buckets.Count;
            foreach (var bucket in buckets.Values)
            {
                bucketIndex++;
                if (bucketIndex % 20 == 0 || bucketIndex == totalBuckets)
                    progress?.Invoke($"桶 {bucketIndex}/{totalBuckets}, 桶内 {bucket.Count} 张");

                if (bucket.Count > DuplicateSettings.MaxBucketLimit) continue;

                if (Name == "SIFT")
                {
                    // ★ M244 贪心版（11 秒）
                    var localVisited = new HashSet<string>();
                    var localGroups = new List<List<string>>();
                    var localLock = new object();

                    Parallel.For(0, bucket.Count,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        i =>
                        {
                            lock (localLock) { if (localVisited.Contains(bucket[i])) return; }

                            var feature1 = GetFeature(cache.GetAll()[bucket[i]]);
                            if (feature1 == null) return;

                            var group = new List<string> { bucket[i] };
                            lock (localLock) { localVisited.Add(bucket[i]); }

                            for (int j = i + 1; j < bucket.Count; j++)
                            {
                                lock (localLock) { if (localVisited.Contains(bucket[j])) continue; }

                                var feature2 = GetFeature(cache.GetAll()[bucket[j]]);
                                if (feature2 == null) continue;

                                try
                                {
                                    if (IsSimilar(feature1, feature2, threshold))
                                    {
                                        group.Add(bucket[j]);
                                        lock (localLock) { localVisited.Add(bucket[j]); }
                                    }
                                }
                                catch { }
                            }

                            if (group.Count > 1)
                                lock (localLock) { localGroups.Add(group); }
                        });

                    groups.AddRange(localGroups);
                }
                else
                {
                    // ★ M250 连通分量版（Hash/DCT 用）
                    int n = bucket.Count;
                    var features = new object[n];
                    for (int i = 0; i < n; i++)
                        features[i] = GetFeature(cache.GetAll()[bucket[i]]);

                    var adj = new bool[n, n];
                    Parallel.For(0, n,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        i =>
                        {
                            if (features[i] == null) return;
                            for (int j = i + 1; j < n; j++)
                            {
                                if (features[j] == null) continue;
                                try
                                {
                                    if (IsSimilar(features[i], features[j], threshold))
                                    {
                                        adj[i, j] = true;
                                        adj[j, i] = true;
                                    }
                                }
                                catch { }
                            }
                        });

                    var visitedLocal = new bool[n];
                    for (int i = 0; i < n; i++)
                    {
                        if (visitedLocal[i]) continue;
                        var group = new List<string>();
                        var queue = new Queue<int>();
                        queue.Enqueue(i);
                        visitedLocal[i] = true;
                        while (queue.Count > 0)
                        {
                            int cur = queue.Dequeue();
                            group.Add(bucket[cur]);
                            for (int k = 0; k < n; k++)
                            {
                                if (!visitedLocal[k] && adj[cur, k])
                                {
                                    visitedLocal[k] = true;
                                    queue.Enqueue(k);
                                }
                            }
                        }
                        if (group.Count > 1) groups.Add(group);
                    }
                }
            }

            return groups;
        }
    }
}