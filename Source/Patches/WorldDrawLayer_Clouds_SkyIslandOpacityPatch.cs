using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(WorldDrawLayer_Clouds), "GetTargetOpacity")]
    public static class WorldDrawLayer_Clouds_SkyIslandOpacityPatch
    {
        private const float CloudFadeStartAltitude = 4.8f;
        private const float CloudFadeEndAltitude = 5.2f;

        public static void Postfix(WorldDrawLayer_Clouds __instance, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }

            if (!WorldRendererUtility.WorldBackgroundNow)
            {
                return;
            }

            Map currentMap = Find.CurrentMap;
            if (currentMap?.Parent is not SkyIslandMapParent island)
            {
                return;
            }

            PlanetLayer skyLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (__instance.planetLayer != skyLayer)
            {
                return;
            }

            float altitude = island.Altitude;
            if (altitude >= CloudFadeEndAltitude)
            {
                return;
            }

            if (altitude <= CloudFadeStartAltitude)
            {
                __result = 0f;
                return;
            }

            float t = (altitude - CloudFadeStartAltitude) / (CloudFadeEndAltitude - CloudFadeStartAltitude);
            __result *= t;
        }
    }
}
