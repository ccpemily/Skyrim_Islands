using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    public static class SkyIslandLocalTimePatchRegistry
    {
        public static void Apply(Harmony harmony)
        {
            ApplyGenLocalDatePatches(harmony);
            ApplyGenCelestialPatch(harmony);
        }

        private static void ApplyGenLocalDatePatches(Harmony harmony)
        {
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayOfYear), m => SkyIslandLocalTimeUtility.DayOfYear(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.HourOfDay), m => SkyIslandLocalTimeUtility.HourOfDay(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayOfTwelfth), m => SkyIslandLocalTimeUtility.DayOfTwelfth(m));
            PatchTwelfth(harmony, typeof(GenLocalDate), nameof(GenLocalDate.Twelfth), m => SkyIslandLocalTimeUtility.Twelfth(m));
            PatchSeason(harmony, typeof(GenLocalDate), nameof(GenLocalDate.Season), m => SkyIslandLocalTimeUtility.Season(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.Year), m => SkyIslandLocalTimeUtility.Year(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayOfSeason), m => SkyIslandLocalTimeUtility.DayOfSeason(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayOfQuadrum), m => SkyIslandLocalTimeUtility.DayOfQuadrum(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayTick), m => SkyIslandLocalTimeUtility.DayTick(m));
            PatchFloat(harmony, typeof(GenLocalDate), nameof(GenLocalDate.DayPercent), m => SkyIslandLocalTimeUtility.DayPercent(m));
            PatchFloat(harmony, typeof(GenLocalDate), nameof(GenLocalDate.YearPercent), m => SkyIslandLocalTimeUtility.YearPercent(m));
            PatchInt(harmony, typeof(GenLocalDate), nameof(GenLocalDate.HourInteger), m => SkyIslandLocalTimeUtility.HourInteger(m));
            PatchFloat(harmony, typeof(GenLocalDate), nameof(GenLocalDate.HourFloat), m => SkyIslandLocalTimeUtility.HourFloat(m));
        }

        private static void ApplyGenCelestialPatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(typeof(GenCelestial), nameof(GenCelestial.CelestialSunGlow), new[] { typeof(Map), typeof(int) });
            MethodInfo prefix = typeof(SkyIslandLocalTimeRedirectPrefix).GetMethod(nameof(SkyIslandLocalTimeRedirectPrefix.PrefixMapIntToFloat), BindingFlags.Static | BindingFlags.Public);
            SkyIslandLocalTimeRedirectPrefix.RegisterMapIntToFloat(target, (m, t) => SkyIslandLocalTimeUtility.CelestialSunGlow(m, t));
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static void PatchInt(Harmony harmony, Type type, string methodName, Func<Map, int> redirect)
        {
            MethodInfo target = AccessTools.Method(type, methodName, new[] { typeof(Map) });
            MethodInfo prefix = typeof(SkyIslandLocalTimeRedirectPrefix).GetMethod(nameof(SkyIslandLocalTimeRedirectPrefix.PrefixMapToInt), BindingFlags.Static | BindingFlags.Public);
            SkyIslandLocalTimeRedirectPrefix.RegisterInt(target, redirect);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static void PatchFloat(Harmony harmony, Type type, string methodName, Func<Map, float> redirect)
        {
            MethodInfo target = AccessTools.Method(type, methodName, new[] { typeof(Map) });
            MethodInfo prefix = typeof(SkyIslandLocalTimeRedirectPrefix).GetMethod(nameof(SkyIslandLocalTimeRedirectPrefix.PrefixMapToFloat), BindingFlags.Static | BindingFlags.Public);
            SkyIslandLocalTimeRedirectPrefix.RegisterFloat(target, redirect);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static void PatchTwelfth(Harmony harmony, Type type, string methodName, Func<Map, Twelfth> redirect)
        {
            MethodInfo target = AccessTools.Method(type, methodName, new[] { typeof(Map) });
            MethodInfo prefix = typeof(SkyIslandLocalTimeRedirectPrefix).GetMethod(nameof(SkyIslandLocalTimeRedirectPrefix.PrefixMapToTwelfth), BindingFlags.Static | BindingFlags.Public);
            SkyIslandLocalTimeRedirectPrefix.RegisterTwelfth(target, redirect);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static void PatchSeason(Harmony harmony, Type type, string methodName, Func<Map, Season> redirect)
        {
            MethodInfo target = AccessTools.Method(type, methodName, new[] { typeof(Map) });
            MethodInfo prefix = typeof(SkyIslandLocalTimeRedirectPrefix).GetMethod(nameof(SkyIslandLocalTimeRedirectPrefix.PrefixMapToSeason), BindingFlags.Static | BindingFlags.Public);
            SkyIslandLocalTimeRedirectPrefix.RegisterSeason(target, redirect);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }
    }
}
