using HarmonyLib;
using RimWorld;
using SkyrimIslands.MainTabs;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(MainTabsRoot), nameof(MainTabsRoot.ToggleTab))]
    public static class MainTabsRoot_ToggleTab_SkyIslandControlPatch
    {
        public static void Postfix()
        {
            if (Find.WindowStack.WindowOfType<MainTabWindow>() != null)
            {
                Window_SkyIslandControl? controlWindow = Find.WindowStack.WindowOfType<Window_SkyIslandControl>();
                if (controlWindow != null)
                {
                    Find.WindowStack.TryRemove(controlWindow, false);
                }
            }
        }
    }
}
