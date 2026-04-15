using HarmonyLib;
using Verse;

namespace SkyrimIslands
{
    [StaticConstructorOnStartup]
    public static class SkyrimIslandsHarmonyPatcher
    {
        public const string HarmonyId = "local.skyrim_islands";

        static SkyrimIslandsHarmonyPatcher()
        {
            new Harmony(HarmonyId).PatchAll();
            Log.Message("[Skyrim Islands] Harmony patches applied.");
        }
    }
}
