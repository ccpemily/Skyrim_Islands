using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public static class SkyIslandMovementRenderUtility
    {
        private const int MinArcSegments = 4;
        private const int MaxArcSegments = 20;
        private const float ArcSegmentAngle = 0.14f;
        private const float ArcBulgeFactor = 0.22f;
        private const float ArcBulgeMax = 1.75f;

        private enum PreviewMode
        {
            SurfaceWithProjection,
            SkyOnly
        }

        private const float SurfaceMarkerAltitude = 0.045f;
        private const float SkyMarkerAltitude = 0.06f;
        private const float CurrentMarkerSize = 0.9f;
        private const float WaypointMarkerSize = 0.78f;
        private const float PathLineAltitude = 0.08f;

        private static readonly Material CurrentSurfaceMarkerMat =
            MaterialPool.MatFrom("World/CurrentMapTile", ShaderDatabase.WorldOverlayTransparent, new Color(0.35f, 0.95f, 1f, 0.95f), 3560);

        private static readonly Material CurrentSkyMarkerMat =
            MaterialPool.MatFrom("World/CurrentMapTile", ShaderDatabase.WorldOverlayTransparent, new Color(0.8f, 1f, 1f, 0.95f), 3560);

        private static readonly Material WaypointSurfaceMarkerMat =
            MaterialPool.MatFrom("World/MouseTile", ShaderDatabase.WorldOverlayTransparent, new Color(1f, 0.85f, 0.35f, 0.95f), 3560);

        private static readonly Material WaypointSkyMarkerMat =
            MaterialPool.MatFrom("World/MouseTile", ShaderDatabase.WorldOverlayTransparent, new Color(1f, 0.65f, 0.2f, 0.95f), 3560);

        private static readonly Material ProjectionLineMat =
            MaterialPool.MatFrom(GenDraw.OneSidedLineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.7f, 0.95f, 1f, 0.85f), 3590);

        private static readonly Material PathLineMat =
            MaterialPool.MatFrom(GenDraw.OneSidedLineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(1f, 0.75f, 0.2f, 0.95f), 3590);

        public static void DrawPlanningOverlay(SkyIslandMapParent island)
        {
            DrawRoutePreview(island, PreviewMode.SurfaceWithProjection);
        }

        public static void DrawSurfaceRoutePreview(SkyIslandMapParent island)
        {
            DrawRoutePreview(island, PreviewMode.SurfaceWithProjection);
        }

        public static void DrawSkyRoutePreview(SkyIslandMapParent island)
        {
            DrawRoutePreview(island, PreviewMode.SkyOnly);
        }

        private static void DrawRoutePreview(SkyIslandMapParent island, PreviewMode previewMode)
        {
            if (!island.Tile.Valid)
            {
                return;
            }

            PlanetTile currentSurfaceTile = island.SurfaceProjectionTile;
            if (!currentSurfaceTile.Valid)
            {
                return;
            }

            Vector3 currentSurfacePos = island.CurrentSurfaceWorldPosition;
            Vector3 currentSkyPos = island.CurrentSkyWorldPosition;

            if (previewMode == PreviewMode.SurfaceWithProjection)
            {
                DrawProjectionPair(currentSurfacePos, currentSkyPos, true);
            }
            else
            {
                DrawMarker(currentSurfacePos, WaypointSurfaceMarkerMat, CurrentMarkerSize, SurfaceMarkerAltitude);
                DrawMarker(currentSkyPos, CurrentSkyMarkerMat, CurrentMarkerSize, SkyMarkerAltitude);
                DrawProjectionLine(currentSurfacePos, currentSkyPos, true);
            }

            Vector3 previousSkyPos = OffsetAlongNormal(currentSkyPos, PathLineAltitude);
            for (int i = 0; i < island.PlannedSurfaceWaypoints.Count; i++)
            {
                PlanetTile surfaceTile = island.PlannedSurfaceWaypoints[i];
                if (!surfaceTile.Valid)
                {
                    continue;
                }

                PlanetTile skyTile = island.PlannedSkyWaypoints[i];
                if (!skyTile.Valid)
                {
                    continue;
                }

                float waypointAltitude = island.WaypointAltitudes.Count > i ? island.WaypointAltitudes[i] : SkyIslandAltitude.DefaultAltitude;

                if (previewMode == PreviewMode.SurfaceWithProjection)
                {
                    DrawProjectionPair(surfaceTile, skyTile, waypointAltitude, false);
                }
                else
                {
                    DrawMarker(surfaceTile, WaypointSurfaceMarkerMat, WaypointMarkerSize, SurfaceMarkerAltitude);
                    DrawMarker(GetSkyWaypointBasePos(skyTile, waypointAltitude), WaypointSkyMarkerMat, WaypointMarkerSize, SkyMarkerAltitude);
                    DrawProjectionLine(surfaceTile, skyTile, waypointAltitude, false);
                }

                Vector3 waypointSkyPos = OffsetAlongNormal(GetSkyWaypointBasePos(skyTile, waypointAltitude), PathLineAltitude);
                DrawArcBetween(previousSkyPos, waypointSkyPos, PathLineMat, 1.15f);
                previousSkyPos = waypointSkyPos;
            }
        }

        private static void DrawProjectionPair(Vector3 surfacePos, Vector3 skyPos, bool isCurrent)
        {
            DrawMarker(surfacePos, isCurrent ? CurrentSurfaceMarkerMat : WaypointSurfaceMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SurfaceMarkerAltitude);
            DrawMarker(skyPos, isCurrent ? CurrentSkyMarkerMat : WaypointSkyMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SkyMarkerAltitude);
            DrawProjectionLine(surfacePos, skyPos, isCurrent);
        }

        private static void DrawProjectionPair(PlanetTile surfaceTile, PlanetTile skyTile, bool isCurrent)
        {
            DrawMarker(surfaceTile, isCurrent ? CurrentSurfaceMarkerMat : WaypointSurfaceMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SurfaceMarkerAltitude);
            DrawMarker(skyTile, isCurrent ? CurrentSkyMarkerMat : WaypointSkyMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SkyMarkerAltitude);
            DrawProjectionLine(surfaceTile, skyTile, isCurrent);
        }

        private static void DrawProjectionPair(PlanetTile surfaceTile, PlanetTile skyTile, float waypointAltitude, bool isCurrent)
        {
            DrawMarker(surfaceTile, isCurrent ? CurrentSurfaceMarkerMat : WaypointSurfaceMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SurfaceMarkerAltitude);
            DrawMarker(GetSkyWaypointBasePos(skyTile, waypointAltitude), isCurrent ? CurrentSkyMarkerMat : WaypointSkyMarkerMat, isCurrent ? CurrentMarkerSize : WaypointMarkerSize, SkyMarkerAltitude);
            DrawProjectionLine(surfaceTile, skyTile, waypointAltitude, isCurrent);
        }

        private static void DrawProjectionLine(Vector3 surfacePos, Vector3 skyPos, bool isCurrent)
        {
            Vector3 surfaceLinePos = OffsetAlongNormal(surfacePos, SurfaceMarkerAltitude);
            Vector3 skyLinePos = OffsetAlongNormal(skyPos, SkyMarkerAltitude);
            GenDraw.DrawWorldLineBetween(surfaceLinePos, skyLinePos, ProjectionLineMat, isCurrent ? 0.85f : 0.65f);
        }

        private static void DrawProjectionLine(PlanetTile surfaceTile, PlanetTile skyTile, bool isCurrent)
        {
            Vector3 surfaceLinePos = GetWorldPos(surfaceTile, SurfaceMarkerAltitude);
            Vector3 skyLinePos = GetWorldPos(skyTile, SkyMarkerAltitude);
            GenDraw.DrawWorldLineBetween(surfaceLinePos, skyLinePos, ProjectionLineMat, isCurrent ? 0.85f : 0.65f);
        }

        private static void DrawProjectionLine(PlanetTile surfaceTile, PlanetTile skyTile, float waypointAltitude, bool isCurrent)
        {
            Vector3 surfaceLinePos = GetWorldPos(surfaceTile, SurfaceMarkerAltitude);
            Vector3 skyLinePos = OffsetAlongNormal(GetSkyWaypointBasePos(skyTile, waypointAltitude), SkyMarkerAltitude);
            GenDraw.DrawWorldLineBetween(surfaceLinePos, skyLinePos, ProjectionLineMat, isCurrent ? 0.85f : 0.65f);
        }

        private static Vector3 GetSkyWaypointBasePos(PlanetTile skyTile, float waypointAltitude)
        {
            Vector3 direction = Find.WorldGrid.GetTileCenter(skyTile).normalized;
            float radius = SkyIslandAltitude.SurfaceRadius + waypointAltitude;
            return skyTile.Layer.Origin + direction * radius;
        }

        private static void DrawMarker(Vector3 worldPosition, Material material, float sizeFactor, float altitude)
        {
            float size = Find.WorldGrid.AverageTileSize * sizeFactor;
            WorldRendererUtility.DrawQuadTangentialToPlanet(worldPosition, size, altitude, material);
        }

        private static void DrawMarker(PlanetTile tile, Material material, float sizeFactor, float altitude)
        {
            float size = Find.WorldGrid.AverageTileSize * sizeFactor;
            Vector3 center = tile.Layer.Origin + Find.WorldGrid.GetTileCenter(tile);
            WorldRendererUtility.DrawQuadTangentialToPlanet(center, size, altitude, material);
        }

        private static Vector3 GetWorldPos(PlanetTile tile, float altitude)
        {
            Vector3 center = tile.Layer.Origin + Find.WorldGrid.GetTileCenter(tile);
            return OffsetAlongNormal(center, altitude);
        }

        private static Vector3 OffsetAlongNormal(Vector3 worldPosition, float altitude)
        {
            return worldPosition + worldPosition.normalized * altitude;
        }

        private static void DrawArcBetween(Vector3 start, Vector3 end, Material material, float widthFactor)
        {
            Vector3 dirA = start.normalized;
            Vector3 dirB = end.normalized;
            float angle = GenMath.SphericalDistance(dirA, dirB);
            if (angle <= 1E-05f)
            {
                return;
            }

            int segments = Mathf.Clamp(Mathf.CeilToInt(angle / ArcSegmentAngle), MinArcSegments, MaxArcSegments);
            float radiusA = start.magnitude;
            float radiusB = end.magnitude;
            float bulge = Mathf.Min(angle * 100f * ArcBulgeFactor, ArcBulgeMax);

            Vector3 previous = start;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 dir = Vector3.Slerp(dirA, dirB, t).normalized;
                float radius = Mathf.Lerp(radiusA, radiusB, t) + Mathf.Sin(t * Mathf.PI) * bulge;
                Vector3 current = dir * radius;
                GenDraw.DrawWorldLineBetween(previous, current, material, widthFactor);
                previous = current;
            }
        }
    }
}
