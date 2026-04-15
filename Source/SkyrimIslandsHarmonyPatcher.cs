using HarmonyLib;
using SkyrimIslands.Patches;
using Verse;

namespace SkyrimIslands
{
    [StaticConstructorOnStartup]
    public static class SkyrimIslandsHarmonyPatcher
    {
        public const string HarmonyId = "local.skyrim_islands";

        static SkyrimIslandsHarmonyPatcher()
        {
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
            SkyIslandLocalTimePatchRegistry.Apply(harmony);
            Log.Message("[Skyrim Islands] Harmony patches applied.");
        }
    }
}
