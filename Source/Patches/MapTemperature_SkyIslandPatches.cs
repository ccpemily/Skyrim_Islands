using HarmonyLib;
using RimWorld;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(MapTemperature), "OutdoorTemp", MethodType.Getter)]
    public static class MapTemperature_OutdoorTemp_SkyIslandPatch
    {
        private static readonly AccessTools.FieldRef<MapTemperature, Map> MapRef =
            AccessTools.FieldRefAccess<MapTemperature, Map>("map");

        public static bool Prefix(MapTemperature __instance, ref float __result)
        {
            Map? map = MapRef(__instance);
            if (map != null && SkyIslandTemperatureUtility.TryGetSmoothedOutdoorTemp(map, out float temp))
            {
                __result = temp;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(MapTemperature), "SeasonalTemp", MethodType.Getter)]
    public static class MapTemperature_SeasonalTemp_SkyIslandPatch
    {
        private static readonly AccessTools.FieldRef<MapTemperature, Map> MapRef =
            AccessTools.FieldRefAccess<MapTemperature, Map>("map");

        public static bool Prefix(MapTemperature __instance, ref float __result)
        {
            Map? map = MapRef(__instance);
            if (map != null && SkyIslandTemperatureUtility.TryGetSmoothedSeasonalTemp(map, out float temp))
            {
                __result = temp;
                return false;
            }

            return true;
        }
    }
}
