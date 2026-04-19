using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World.Movement
{
    public static class SkyIslandMovementGeometry
    {
        private static readonly List<PlanetTile> tmpNeighbors = new List<PlanetTile>();

        public static Vector3 GetSkyWorldPosition(Vector3 direction, PlanetTile tile, float altitude)
        {
            float radius = SkyIslandAltitude.SurfaceRadius + altitude;
            Vector3 dir = direction == Vector3.zero ? Find.WorldGrid.GetTileCenter(tile).normalized : direction.normalized;
            return tile.Layer.Origin + dir * radius;
        }

        public static Vector3 GetSurfaceWorldPosition(Vector3 direction, PlanetTile tile)
        {
            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
                return Vector3.zero;

            return GetWorldPositionOnLayer(direction, surfaceLayer);
        }

        public static Vector2 GetSkyLongLat(Vector3 direction, PlanetTile tile, float altitude)
        {
            return GetLongLatOnLayer(tile.Layer, GetSkyWorldPosition(direction, tile, altitude));
        }

        public static Vector2 GetSurfaceLongLat(Vector3 direction, PlanetTile tile)
        {
            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
                return Vector2.zero;

            return GetLongLatOnLayer(surfaceLayer, GetSurfaceWorldPosition(direction, tile));
        }

        public static bool IsCenteredOnTile(PlanetTile tile, Vector3 direction)
        {
            if (!tile.Valid || direction == Vector3.zero)
                return true;

            return Vector3.Dot(direction.normalized, Find.WorldGrid.GetTileCenter(tile).normalized) >= SkyIslandMovementConstants.AnchorSnapDotThreshold;
        }

        public static PlanetTile RecalculateSurfaceProjection(WorldObject parent, PlanetTile fallbackSurfaceProjectionTile, Vector3 direction)
        {
            if (direction == Vector3.zero)
            {
                return fallbackSurfaceProjectionTile;
            }

            parent.Tile = FindClosestNeighboringTile(parent.Tile, parent.Tile.Layer, direction);

            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer != null)
            {
                PlanetTile anchorSurfaceTile = fallbackSurfaceProjectionTile.Valid
                    ? fallbackSurfaceProjectionTile
                    : surfaceLayer.GetClosestTile_NewTemp(parent.Tile, false);
                return FindClosestNeighboringTile(anchorSurfaceTile, surfaceLayer, direction);
            }

            return fallbackSurfaceProjectionTile;
        }

        public static PlanetTile FindClosestNeighboringTile(PlanetTile currentTile, PlanetLayer layer, Vector3 direction)
        {
            if (!currentTile.Valid || currentTile.Layer != layer)
            {
                return currentTile;
            }

            PlanetTile bestTile = currentTile;
            float bestDot = Vector3.Dot(Find.WorldGrid.GetTileCenter(currentTile).normalized, direction);

            bool improved;
            int safety = 8;
            do
            {
                improved = false;
                tmpNeighbors.Clear();
                layer.GetTileNeighbors(bestTile, tmpNeighbors);
                for (int i = 0; i < tmpNeighbors.Count; i++)
                {
                    float dot = Vector3.Dot(Find.WorldGrid.GetTileCenter(tmpNeighbors[i]).normalized, direction);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestTile = tmpNeighbors[i];
                        improved = true;
                    }
                }
            }
            while (improved && --safety > 0);

            return bestTile;
        }

        public static void EnsureDirection(ref Vector3 currentDirection, PlanetTile fallbackTile, PlanetTile parentTile)
        {
            if (currentDirection != Vector3.zero)
            {
                return;
            }

            PlanetTile anchorTile = fallbackTile.Valid ? fallbackTile : parentTile;
            if (!anchorTile.Valid)
            {
                return;
            }

            currentDirection = Find.WorldGrid.GetTileCenter(anchorTile).normalized;
        }

        public static Vector3 GetWorldPositionOnLayer(Vector3 direction, PlanetLayer layer)
        {
            Vector3 dir = direction == Vector3.zero ? Vector3.zero : direction.normalized;
            return layer.Origin + dir * layer.Radius;
        }

        public static Vector2 GetLongLatOnLayer(PlanetLayer layer, Vector3 worldPosition)
        {
            Vector3 local = worldPosition - layer.Origin;
            if (local == Vector3.zero)
            {
                return Vector2.zero;
            }

            float magnitude = local.magnitude;
            float longitude = Mathf.Atan2(local.x, -local.z) * 57.29578f;
            float latitude = Mathf.Asin(local.y / magnitude) * 57.29578f;
            return new Vector2(longitude, latitude);
        }
    }
}
