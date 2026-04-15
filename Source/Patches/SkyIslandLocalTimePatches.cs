using HarmonyLib;
using RimWorld;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayOfYear), new[] { typeof(Map) })]
    public static class GenLocalDate_DayOfYear_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayOfYear(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.HourOfDay), new[] { typeof(Map) })]
    public static class GenLocalDate_HourOfDay_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.HourOfDay(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayOfTwelfth), new[] { typeof(Map) })]
    public static class GenLocalDate_DayOfTwelfth_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayOfTwelfth(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.Twelfth), new[] { typeof(Map) })]
    public static class GenLocalDate_Twelfth_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref Twelfth __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.Twelfth(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.Season), new[] { typeof(Map) })]
    public static class GenLocalDate_Season_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref Season __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.Season(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.Year), new[] { typeof(Map) })]
    public static class GenLocalDate_Year_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.Year(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayOfSeason), new[] { typeof(Map) })]
    public static class GenLocalDate_DayOfSeason_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayOfSeason(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayOfQuadrum), new[] { typeof(Map) })]
    public static class GenLocalDate_DayOfQuadrum_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayOfQuadrum(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayTick), new[] { typeof(Map) })]
    public static class GenLocalDate_DayTick_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayTick(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.DayPercent), new[] { typeof(Map) })]
    public static class GenLocalDate_DayPercent_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref float __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.DayPercent(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.YearPercent), new[] { typeof(Map) })]
    public static class GenLocalDate_YearPercent_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref float __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.YearPercent(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.HourInteger), new[] { typeof(Map) })]
    public static class GenLocalDate_HourInteger_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref int __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.HourInteger(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenLocalDate), nameof(GenLocalDate.HourFloat), new[] { typeof(Map) })]
    public static class GenLocalDate_HourFloat_SkyIslandPatch
    {
        public static bool Prefix(Map map, ref float __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.HourFloat(map);
            return false;
        }
    }

    [HarmonyPatch(typeof(GenCelestial), nameof(GenCelestial.CelestialSunGlow), new[] { typeof(Map), typeof(int) })]
    public static class GenCelestial_CelestialSunGlow_SkyIslandPatch
    {
        public static bool Prefix(Map map, int ticksAbs, ref float __result)
        {
            if (map.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            __result = SkyIslandLocalTimeUtility.CelestialSunGlow(map, ticksAbs);
            return false;
        }
    }

    [HarmonyPatch(typeof(DateReadout), nameof(DateReadout.DateOnGUI))]
    public static class DateReadout_DateOnGUI_SkyIslandPatch
    {
        public static bool Prefix(Rect dateRect)
        {
            Map? currentMap = Find.CurrentMap;
            if (currentMap?.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            SkyIslandLocalTimeUtility.DrawDateReadoutForSkyIsland(dateRect, currentMap);
            return false;
        }
    }
}
