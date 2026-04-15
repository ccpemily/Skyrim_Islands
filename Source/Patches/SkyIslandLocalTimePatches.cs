using HarmonyLib;
using RimWorld;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(DateReadout), nameof(DateReadout.DateOnGUI))]
    public static class DateReadout_DateOnGUI_SkyIslandPatch
    {
        public static bool Prefix(Rect dateRect)
        {
            Map? currentMap = Find.CurrentMap;
            if (currentMap?.Parent is not SkyIslandMapParent)
            {
                return true;
            }

            SkyIslandLocalTimeUtility.DrawDateReadoutForSkyIsland(dateRect, currentMap);
            return false;
        }
    }
}
