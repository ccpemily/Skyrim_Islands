using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.MainTabs
{
    public static class SkyIslandMinimapUtility
    {
        private const float BaseVisibleTiles = 3f;
        private const float TilesPerAltitudeUnit = 0.15f;
        private const float MaxTileDrawCount = 600f;

        private static SkyIslandMapParent? cachedIsland;
        private static PlanetTile cachedCenterTile = PlanetTile.Invalid;
        private static float cachedRadius = -1f;
        private static readonly List<PlanetTile> cachedTiles = new List<PlanetTile>();

        private static float minimapYaw = 0f;
        public static float MinimapYaw
        {
            get => minimapYaw;
            set => minimapYaw = value;
        }

        public static void ResetYaw() => minimapYaw = 0f;

        private const float PitchScaleY = 0.5f;

        public static void DrawMinimap(Rect rect, SkyIslandMapParent island)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.06f, 0.08f, 0.1f, 0.95f));
            Widgets.DrawBox(rect);

            PlanetTile centerTile = island.SurfaceProjectionTile;
            if (!centerTile.Valid)
            {
                return;
            }

            float altitude = island.Altitude;
            float visibleTileRadius = BaseVisibleTiles + altitude * TilesPerAltitudeUnit;
            float pixelsPerTile = rect.width / 2f / visibleTileRadius;
            Vector2 centerPixel = new Vector2(rect.center.x, rect.y + rect.height * 0.75f);
            Vector2 centerLongLat = island.CurrentSurfaceLongLat;
            float cosLat = Mathf.Cos(centerLongLat.y * Mathf.Deg2Rad);

            List<PlanetTile> tiles = GetOrCollectTiles(island, centerTile, visibleTileRadius);
            float tilePixelSize = Mathf.Clamp(pixelsPerTile * 0.9f, 3f, 8f);

            foreach (PlanetTile tile in tiles)
            {
                Vector2 pixelPos = TileToMinimapPixel(tile, centerLongLat, cosLat, pixelsPerTile, centerPixel);
                if (!rect.Contains(pixelPos))
                {
                    continue;
                }

                Color tileColor = GetTileColor(tile);
                Widgets.DrawBoxSolid(new Rect(pixelPos.x - tilePixelSize * 0.5f, pixelPos.y - tilePixelSize * 0.5f, tilePixelSize, tilePixelSize), tileColor);
            }

            DrawWorldObjects(rect, island, centerLongLat, cosLat, pixelsPerTile, centerPixel, visibleTileRadius);
            DrawAltitudeIndicator(rect, centerPixel, altitude, out Vector2 skyIconPos);
            DrawMovementArrow(rect, island, skyIconPos, cosLat);
        }

        private static List<PlanetTile> GetOrCollectTiles(SkyIslandMapParent island, PlanetTile center, float radius)
        {
            if (cachedIsland == island && cachedCenterTile == center && Mathf.Approximately(cachedRadius, radius))
            {
                return cachedTiles;
            }

            cachedIsland = island;
            cachedCenterTile = center;
            cachedRadius = radius;
            cachedTiles.Clear();
            CollectVisibleTiles(center, radius, cachedTiles);
            return cachedTiles;
        }

        private static void CollectVisibleTiles(PlanetTile center, float visibleTileRadius, List<PlanetTile> outTiles)
        {
            if (!center.Valid)
            {
                return;
            }

            Queue<PlanetTile> queue = new Queue<PlanetTile>();
            HashSet<int> visited = new HashSet<int>();
            Dictionary<int, int> depthMap = new Dictionary<int, int>();
            List<PlanetTile> tmpNeighbors = new List<PlanetTile>();
            int maxDepth = Mathf.CeilToInt(visibleTileRadius);

            queue.Enqueue(center);
            visited.Add(center.tileId);
            depthMap[center.tileId] = 0;
            outTiles.Add(center);

            while (queue.Count > 0)
            {
                PlanetTile current = queue.Dequeue();
                int currentDepth = depthMap[current.tileId];
                if (currentDepth >= maxDepth || outTiles.Count >= MaxTileDrawCount)
                {
                    continue;
                }

                tmpNeighbors.Clear();
                Find.WorldGrid.GetTileNeighbors(current, tmpNeighbors);
                for (int i = 0; i < tmpNeighbors.Count; i++)
                {
                    PlanetTile neighbor = tmpNeighbors[i];
                    if (neighbor.Valid && neighbor.LayerDef == PlanetLayerDefOf.Surface && visited.Add(neighbor.tileId))
                    {
                        queue.Enqueue(neighbor);
                        depthMap[neighbor.tileId] = currentDepth + 1;
                        outTiles.Add(neighbor);
                    }
                }
            }
        }

        private static Vector2 TileToMinimapPixel(PlanetTile tile, Vector2 centerLongLat, float cosLat, float pixelsPerTile, Vector2 centerPixel)
        {
            Vector2 tileLongLat = Find.WorldGrid.LongLatOf(tile);
            float dx = (tileLongLat.x - centerLongLat.x) * cosLat;
            float dy = tileLongLat.y - centerLongLat.y;

            float cosYaw = Mathf.Cos(minimapYaw);
            float sinYaw = Mathf.Sin(minimapYaw);
            float rotatedX = dx * cosYaw - dy * sinYaw;
            float rotatedY = dx * sinYaw + dy * cosYaw;
            rotatedY *= PitchScaleY;

            return centerPixel + new Vector2(rotatedX * pixelsPerTile, -rotatedY * pixelsPerTile);
        }

        private static void DrawWorldObjects(Rect rect, SkyIslandMapParent island, Vector2 centerLongLat, float cosLat, float pixelsPerTile, Vector2 centerPixel, float visibleTileRadius)
        {
            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects)
            {
                if (wo == island || !wo.Tile.Valid || wo.Tile.LayerDef != PlanetLayerDefOf.Surface)
                {
                    continue;
                }

                Vector2 pixelPos = TileToMinimapPixel(wo.Tile, centerLongLat, cosLat, pixelsPerTile, centerPixel);
                if (!rect.Contains(pixelPos))
                {
                    continue;
                }

                float pxDistX = (pixelPos.x - centerPixel.x) / pixelsPerTile;
                float pxDistY = -(pixelPos.y - centerPixel.y) / pixelsPerTile;
                float unscaledY = pxDistY / PitchScaleY;
                float tileDistance = Mathf.Sqrt(pxDistX * pxDistX + unscaledY * unscaledY);

                float opacity = tileDistance <= visibleTileRadius
                    ? 1f
                    : Mathf.Clamp01(1f - (tileDistance - visibleTileRadius) / visibleTileRadius);

                if (opacity <= 0.01f)
                {
                    continue;
                }

                Color baseColor = wo.Faction?.Color ?? new Color(0.7f, 0.7f, 0.7f);
                baseColor.a *= opacity;
                GUI.color = baseColor;
                GUI.DrawTexture(new Rect(pixelPos.x - 3f, pixelPos.y - 3f, 6f, 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                Rect hitRect = new Rect(pixelPos.x - 7f, pixelPos.y - 7f, 14f, 14f);
                if (Mouse.IsOver(hitRect))
                {
                    Widgets.DrawBoxSolid(hitRect, new Color(1f, 1f, 1f, 0.35f));
                }

                TooltipHandler.TipRegion(hitRect, new TipSignal(() => GetWorldObjectTooltip(wo), wo.ID + 2000000));
            }
        }

        private static string GetWorldObjectTooltip(WorldObject wo)
        {
            string factionName = wo.Faction?.Name ?? "无";
            return $"{wo.LabelCap}\n类型: {wo.def.LabelCap}\n势力: {factionName}";
        }

        private static string GetSkyIslandTooltip(SkyIslandMapParent island)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(island.LabelCap);

            Vector2 longLat = island.CurrentSurfaceLongLat;
            sb.AppendLine($"位置: {longLat.y:F1}°N, {longLat.x:F1}°E");
            sb.AppendLine($"高度: {island.Altitude:F0}m");

            float speed = island.CurrentSpeedTilesPerDay;
            string stateStr = island.MovementState switch
            {
                SkyIslandMapParent.SkyIslandMovementState.Idle => "静止",
                SkyIslandMapParent.SkyIslandMovementState.Accelerating => "加速中",
                SkyIslandMapParent.SkyIslandMovementState.Cruising => "巡航中",
                SkyIslandMapParent.SkyIslandMovementState.Decelerating => "减速中",
                SkyIslandMapParent.SkyIslandMovementState.Braking => "制动中",
                SkyIslandMapParent.SkyIslandMovementState.Interrupting => "中断中",
                SkyIslandMapParent.SkyIslandMovementState.Docking => "停靠中",
                _ => "未知"
            };

            if (speed < 0.01f)
            {
                sb.AppendLine($"状态: {stateStr}");
            }
            else
            {
                sb.AppendLine($"速度: {speed:F1} 格/天 ({stateStr})");
            }

            return sb.ToString().TrimEndNewlines();
        }

        private static void DrawAltitudeIndicator(Rect rect, Vector2 centerPixel, float altitude, out Vector2 skyIconPos)
        {
            float t = Mathf.InverseLerp(0f, SkyIslandAltitude.MaxAltitude, altitude);
            float maxYOffset = rect.height * 0.42f;
            float minYOffset = rect.height * 0.08f;
            float yOffset = Mathf.Lerp(minYOffset, maxYOffset, t);
            skyIconPos = centerPixel + new Vector2(0f, -yOffset);

            Widgets.DrawLine(skyIconPos, centerPixel, new Color(0.5f, 0.8f, 1f, 0.85f), 1f);
            Widgets.DrawBoxSolid(new Rect(skyIconPos.x - 3f, skyIconPos.y - 3f, 6f, 6f), Color.white);
        }

        private static void DrawMovementArrow(Rect rect, SkyIslandMapParent island, Vector2 skyIconPos, float cosLat)
        {
            if (island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return;
            }

            Vector2 skyLongLat = island.CurrentSkyLongLat;
            Vector2 surfaceLongLat = island.CurrentSurfaceLongLat;
            float deltaLon = skyLongLat.x - surfaceLongLat.x;
            float deltaLat = skyLongLat.y - surfaceLongLat.y;

            if (Mathf.Abs(deltaLon) < 0.0001f && Mathf.Abs(deltaLat) < 0.0001f)
            {
                return;
            }

            float arrowAngle = Mathf.Atan2(deltaLon * cosLat, deltaLat) - minimapYaw;
            float arrowLen = Mathf.Min(rect.width, rect.height) * 0.18f;

            Vector2 tip = skyIconPos + new Vector2(Mathf.Sin(arrowAngle), -Mathf.Cos(arrowAngle)) * arrowLen;
            Vector2 wing1 = skyIconPos + new Vector2(Mathf.Sin(arrowAngle - 2.4f), -Mathf.Cos(arrowAngle - 2.4f)) * (arrowLen * 0.5f);
            Vector2 wing2 = skyIconPos + new Vector2(Mathf.Sin(arrowAngle + 2.4f), -Mathf.Cos(arrowAngle + 2.4f)) * (arrowLen * 0.5f);

            Color arrowColor = new Color(0.35f, 0.95f, 1f);
            Widgets.DrawLine(skyIconPos, tip, arrowColor, 2f);
            Widgets.DrawLine(tip, wing1, arrowColor, 2f);
            Widgets.DrawLine(tip, wing2, arrowColor, 2f);
        }

        private static Color GetTileColor(PlanetTile tile)
        {
            Hilliness hilliness = Find.WorldGrid[tile].hilliness;
            return hilliness switch
            {
                Hilliness.Mountainous => new Color(0.50f, 0.35f, 0.20f),
                Hilliness.LargeHills => new Color(0.72f, 0.55f, 0.32f),
                Hilliness.SmallHills => new Color(0.82f, 0.68f, 0.45f),
                _ => GetBiomeColor(Find.WorldGrid[tile].PrimaryBiome)
            };
        }

        private static Color GetBiomeColor(BiomeDef biome)
        {
            if (biome == null)
            {
                return new Color(0.5f, 0.5f, 0.5f);
            }

            string name = biome.defName;
            return name switch
            {
                "IceSheet" or "SeaIce" => new Color(0.92f, 0.96f, 1f),
                "Tundra" => new Color(0.72f, 0.82f, 0.76f),
                "BorealForest" => new Color(0.22f, 0.42f, 0.22f),
                "TemperateForest" => new Color(0.28f, 0.52f, 0.24f),
                "TropicalRainforest" => new Color(0.18f, 0.48f, 0.18f),
                "Desert" => new Color(0.82f, 0.74f, 0.42f),
                "ExtremeDesert" => new Color(0.88f, 0.68f, 0.36f),
                "AridShrubland" => new Color(0.68f, 0.58f, 0.32f),
                "Savanna" => new Color(0.58f, 0.68f, 0.28f),
                "TemperateSwamp" => new Color(0.24f, 0.44f, 0.22f),
                "ColdBog" => new Color(0.20f, 0.38f, 0.28f),
                "TropicalSwamp" => new Color(0.18f, 0.42f, 0.18f),
                "Grasslands" => new Color(0.52f, 0.68f, 0.22f),
                "Ocean" => new Color(0.18f, 0.32f, 0.52f),
                "Lake" => new Color(0.28f, 0.44f, 0.62f),
                _ => GenerateStableColor(name)
            };
        }

        private static Color GenerateStableColor(string input)
        {
            int hash = GenText.StableStringHash(input);
            return new Color(
                0.3f + (Mathf.Abs(hash) % 100) / 200f,
                0.3f + (Mathf.Abs(hash >> 8) % 100) / 200f,
                0.3f + (Mathf.Abs(hash >> 16) % 100) / 200f);
        }
    }
}
