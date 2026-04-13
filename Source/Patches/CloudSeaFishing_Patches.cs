using HarmonyLib;
using RimWorld;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(Designator_ZoneAdd_Fishing), "get_Visible")]
    public static class Designator_ZoneAdd_Fishing_Visible_SkyrimIslandsPatch
    {
        public static void Postfix(ref bool __result)
        {
            if (__result || !ModsConfig.OdysseyActive)
            {
                return;
            }

            __result = SkyrimIslandsDefOf.SkyrimIslands_CloudSeaFishing.IsFinished;
        }
    }
}
