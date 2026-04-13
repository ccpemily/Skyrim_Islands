using HarmonyLib;
using RimWorld.Planet;
using SkyrimIslands.Quests.Initial;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(TravellingTransporters), "TickInterval")]
    public static class TravellingTransporters_SkyIslandMigrationPatch
    {
        public static bool Prefix(TravellingTransporters __instance, int delta)
        {
            if (__instance.arrivalAction is not TransportersArrivalAction_SkyIslandMigration migration)
            {
                return true;
            }

            // Freeze the world shuttle until the world-flight cutscene has actually opened.
            return migration.WorldFlightStarted;
        }
    }
}
