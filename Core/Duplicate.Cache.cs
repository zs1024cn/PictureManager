//Core\Duplicate.Cache.cs
using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager
{
    public partial class Duplicate
    {
        // ========== 缓存删除（移入回收站）==========

        public static void DeleteDctCache(string key)
        {
            if (DctCache.TryGetValue(key, out Mat mat))
            {
                DctCacheDeleted[key] = mat;
                DctCache.Remove(key);
            }
        }

        public static void DeleteSiftCache(string key)
        {
            if (SiftCache.TryGetValue(key, out var sift))
            {
                SiftCacheDeleted[key] = sift;
                SiftCache.Remove(key);
            }
        }

        public static void DeleteHashCache(string key)
        {
            if (HashCache.TryGetValue(key, out byte[] hash))
            {
                HashCacheDeleted[key] = hash;
                HashCache.Remove(key);
            }
        }

        public static void DeleteHsvCache(string file)
        {
            if (HsvCache.TryGetValue(file, out float[] hsv))
            {
                HsvCacheDeleted[file] = hsv;
                HsvCache.TryRemove(file, out _);
            }
        }

        // ========== 清空 ==========

        public static void CleanHashDctSiftCache()
        {
            HashCache.Clear();
            DctCache.Clear();
            SiftCache.Clear();
            HashCacheDeleted.Clear();
            DctCacheDeleted.Clear();
            SiftCacheDeleted.Clear();
        }

        // ========== 批量 HSV ==========

        public static void ReadAllHsv(List<string> files, IProgress<string> progress = null)
        {
            int total = files.Count;
            int processed = 0;

            foreach (var file in files)
            {
                if (GetHsv(file) != null)
                    processed++;

                if (progress != null && processed % 100 == 0)
                {
                    progress.Report($"HSV {processed}/{total}");
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }
    }
}