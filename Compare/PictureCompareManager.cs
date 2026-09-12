using System;
using System.Collections.Generic;

namespace PictureManager
{
    /// <summary>
    /// 所有对比器的注册中心。
    /// </summary>
    public static class PictureCompareManager
    {
        private static readonly Dictionary<string, PictureCompare> _comparers
            = new Dictionary<string, PictureCompare>(StringComparer.OrdinalIgnoreCase);

        private static bool _registered = false;

        /// <summary>
        /// 注册所有内置对比器（幂等）。
        /// </summary>
        public static void RegisterDefaults()
        {
            if (_registered) return;

            Register(new HashCompare());
            Register(new DctCompare());
            Register(new SiftCompare());
            Register(new HsvCompare());
            Register(new FaceCompare());
            Register(new PoseCompare());
            Register(new SceneCompare());

            _registered = true;
        }

        public static void Register(PictureCompare comparer)
        {
            if (comparer == null) return;
            _comparers[comparer.Name] = comparer;
        }

        public static PictureCompare Get(string name)
        {
            RegisterDefaults();
            return _comparers.TryGetValue(name, out var c) ? c : null;
        }

        public static IEnumerable<PictureCompare> GetAll()
        {
            RegisterDefaults();
            return _comparers.Values;
        }

        public static IEnumerable<string> GetAllNames()
        {
            RegisterDefaults();
            return _comparers.Keys;
        }
    }
}