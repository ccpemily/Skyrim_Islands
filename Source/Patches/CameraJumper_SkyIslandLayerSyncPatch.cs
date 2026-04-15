using HarmonyLib;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(CameraJumper), nameof(CameraJumper.TryHideWorld))]
    public static class CameraJumper_TryHideWorld_SkyIslandLayerSyncPatch
    {
        public static void Postfix(bool __result)
        {
            if (!__result)
            {
                return;
            }

            Map? currentMap = Find.CurrentMap;
            if (currentMap?.Parent is not SkyIslandMapParent)
            {
                return;
            }

            PlanetLayer? skyLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (skyLayer != null && PlanetLayer.Selected != skyLayer)
            {
                PlanetLayer.Selected = skyLayer;
            }
        }
    }
}
