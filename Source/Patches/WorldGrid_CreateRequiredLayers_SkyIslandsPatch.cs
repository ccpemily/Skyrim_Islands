using HarmonyLib;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(WorldGrid), "CreateRequiredLayers")]
    public static class WorldGrid_CreateRequiredLayers_SkyIslandsPatch
    {
        public static void Postfix(WorldGrid __instance)
        {
            if (!ModsConfig.OdysseyActive)
            {
                return;
            }

            SkyIslandLayerBootstrap.EnsureLayerRegistered(__instance);
        }
    }
}
