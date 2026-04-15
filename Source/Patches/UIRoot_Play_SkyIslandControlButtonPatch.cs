using HarmonyLib;
using RimWorld;
using SkyrimIslands.MainTabs;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate))]
    public static class UIRoot_Play_SkyIslandControlButtonPatch
    {
        public static void Postfix()
        {
            SkyIslandControlButtonDrawer.Draw();
        }
    }
}
