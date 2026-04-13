using HarmonyLib;
using RimWorld;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
    public static class CloudSeaDebrisPatch
    {
        public static void Postfix(Thing __result, IntVec3 loc, Map map)
        {
            if (__result == null || map == null || !loc.InBounds(map))
            {
                return;
            }

            if (loc.GetTerrain(map) != SkyrimIslandsDefOf.SkyrimIslands_CloudSea)
            {
                return;
            }

            if (__result is Filth || __result.def == ThingDefOf.ChunkSlagSteel || __result.def == ThingDefOf.ChunkMechanoidSlag)
            {
                __result.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
