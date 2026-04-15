using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    public static class SkyIslandLocalTimeRedirectPrefix
    {
        private static readonly Dictionary<MethodInfo, Func<Map, int>> IntRedirects = new Dictionary<MethodInfo, Func<Map, int>>();
        private static readonly Dictionary<MethodInfo, Func<Map, float>> FloatRedirects = new Dictionary<MethodInfo, Func<Map, float>>();
        private static readonly Dictionary<MethodInfo, Func<Map, Twelfth>> TwelfthRedirects = new Dictionary<MethodInfo, Func<Map, Twelfth>>();
        private static readonly Dictionary<MethodInfo, Func<Map, Season>> SeasonRedirects = new Dictionary<MethodInfo, Func<Map, Season>>();
        private static readonly Dictionary<MethodInfo, Func<Map, int, float>> MapIntToFloatRedirects = new Dictionary<MethodInfo, Func<Map, int, float>>();

        public static void RegisterInt(MethodInfo target, Func<Map, int> redirect)
        {
            IntRedirects[target] = redirect;
        }

        public static void RegisterFloat(MethodInfo target, Func<Map, float> redirect)
        {
            FloatRedirects[target] = redirect;
        }

        public static void RegisterTwelfth(MethodInfo target, Func<Map, Twelfth> redirect)
        {
            TwelfthRedirects[target] = redirect;
        }

        public static void RegisterSeason(MethodInfo target, Func<Map, Season> redirect)
        {
            SeasonRedirects[target] = redirect;
        }

        public static void RegisterMapIntToFloat(MethodInfo target, Func<Map, int, float> redirect)
        {
            MapIntToFloatRedirects[target] = redirect;
        }

        public static bool PrefixMapToInt(Map map, MethodInfo __originalMethod, ref int __result)
        {
            if (map.Parent is SkyIslandMapParent && IntRedirects.TryGetValue(__originalMethod, out var del))
            {
                __result = del(map);
                return false;
            }
            return true;
        }

        public static bool PrefixMapToFloat(Map map, MethodInfo __originalMethod, ref float __result)
        {
            if (map.Parent is SkyIslandMapParent && FloatRedirects.TryGetValue(__originalMethod, out var del))
            {
                __result = del(map);
                return false;
            }
            return true;
        }

        public static bool PrefixMapToTwelfth(Map map, MethodInfo __originalMethod, ref Twelfth __result)
        {
            if (map.Parent is SkyIslandMapParent && TwelfthRedirects.TryGetValue(__originalMethod, out var del))
            {
                __result = del(map);
                return false;
            }
            return true;
        }

        public static bool PrefixMapToSeason(Map map, MethodInfo __originalMethod, ref Season __result)
        {
            if (map.Parent is SkyIslandMapParent && SeasonRedirects.TryGetValue(__originalMethod, out var del))
            {
                __result = del(map);
                return false;
            }
            return true;
        }

        public static bool PrefixMapIntToFloat(Map map, int ticksAbs, MethodInfo __originalMethod, ref float __result)
        {
            if (map.Parent is SkyIslandMapParent && MapIntToFloatRedirects.TryGetValue(__originalMethod, out var del))
            {
                __result = del(map, ticksAbs);
                return false;
            }
            return true;
        }
    }
}
