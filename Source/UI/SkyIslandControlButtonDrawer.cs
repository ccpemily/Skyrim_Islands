using RimWorld;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.MainTabs
{
    public static class SkyIslandControlButtonDrawer
    {
        public static void Draw()
        {
            SkyIslandControlWindowUtility.EnsureWindowState();
        }
    }
}
