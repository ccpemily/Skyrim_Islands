using HarmonyLib;
using Verse;

namespace SkyrimIslands
{
    public sealed class SkyrimIslandsMod : Mod
    {
        public const string HarmonyId = "local.skyrim_islands";

        private static bool patched;

        public SkyrimIslandsMod(ModContentPack content)
            : base(content)
        {
            if (patched)
            {
                return;
            }

            patched = true;
            new Harmony(HarmonyId).PatchAll();
            Log.Message("[Skyrim Islands] Mod initialized.");
        }
    }
}
