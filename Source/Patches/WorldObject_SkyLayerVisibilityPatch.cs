using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(WorldObject), "get_VisibleInBackground")]
    public static class WorldObject_SkyLayerVisibilityPatch
    {
        public static void Postfix(WorldObject __instance, ref bool __result)
        {
            if (__result || !ModsConfig.OdysseyActive)
            {
                return;
            }

            PlanetLayer? selectedLayer = Find.WorldSelector?.SelectedLayer;
            if (selectedLayer?.Def != PlanetLayerDefOf.Surface)
            {
                return;
            }

            if (!__instance.Tile.Valid || __instance.Tile.LayerDef != SkyrimIslandsDefOf.SkyrimIslands_SkyLayer)
            {
                return;
            }

            __result = true;
        }
    }
}
