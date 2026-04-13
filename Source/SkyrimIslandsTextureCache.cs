using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands
{
    [StaticConstructorOnStartup]
    internal static class SkyrimIslandsTextureCache
    {
        public static readonly Texture2D ResearchBarFillTex =
            (Texture2D)AccessTools.Field(typeof(MainTabWindow_Research), "ResearchBarFillTex")!.GetValue(null)!;

        public static readonly Texture2D ResearchBarBGTex =
            (Texture2D)AccessTools.Field(typeof(MainTabWindow_Research), "ResearchBarBGTex")!.GetValue(null)!;
    }
}
