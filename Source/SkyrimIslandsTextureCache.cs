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

        public static readonly Texture2D WorldRoutePlannerTex =
            ContentFinder<Texture2D>.Get("UI/Misc/WorldRoutePlanner");

        public static readonly Texture2D WaypointMouseAttachmentTex =
            ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment");

        public static readonly Material SkyIslandControlButtonMat =
            MaterialPool.MatFrom(WorldRoutePlannerTex, ShaderDatabase.Transparent, Color.white);

        public static readonly Texture2D MinimapHexTileTex = CreateFlatTopHexTexture(32);

        private static Texture2D CreateFlatTopHexTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = size / 2f;
            float cy = size / 2f;
            float a = size * 0.48f;
            float halfHeight = a * Mathf.Sqrt(3f) / 2f;
            float invSqrt3 = 1f / Mathf.Sqrt(3f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - cx;
                    float py = y - cy;
                    bool inside = Mathf.Abs(py) <= halfHeight && Mathf.Abs(px) <= a - Mathf.Abs(py) * invSqrt3;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
