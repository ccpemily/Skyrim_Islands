using HarmonyLib;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(FishShadowComponent), "FindFishFleckLocation")]
    public static class FishShadowComponentPatch
    {
        public static void Postfix(WaterBody body, ref IntVec3? __result)
        {
            if (!__result.HasValue)
            {
                return;
            }

            TerrainDef terrain = body.map.terrainGrid.BaseTerrainAt(body.rootCell);
            if (terrain == SkyrimIslandsDefOf.SkyrimIslands_CloudSea)
            {
                __result = null;
            }
        }
    }
}
