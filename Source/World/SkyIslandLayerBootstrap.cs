using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public static class SkyIslandLayerBootstrap
    {
        public const string ScenarioTag = "SkyrimIslandsSky";

        private const float SkyLayerRadius = 106f;
        private const float SkyLayerViewAngle = 180f;
        private const float SkyLayerExtraCameraAltitude = 20f;
        private const int SkyLayerSubdivisions = 10;
        private const float SkyLayerBackgroundWorldCameraOffset = 32f;
        private const float SkyLayerBackgroundWorldCameraParallaxDistancePer100Cells = 0.5f;

        public static PlanetLayer? GetSkyLayer()
        {
            return Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
        }

        public static PlanetLayer EnsureLayerRegistered(WorldGrid worldGrid)
        {
            PlanetLayer? skyLayer = worldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            bool createdNow = false;
            if (skyLayer == null)
            {
                skyLayer = worldGrid.RegisterPlanetLayer(
                    SkyrimIslandsDefOf.SkyrimIslands_SkyLayer,
                    origin: Vector3.zero,
                    radius: SkyLayerRadius,
                    viewAngle: SkyLayerViewAngle,
                    extraCameraAltitude: SkyLayerExtraCameraAltitude,
                    subdivisions: SkyLayerSubdivisions,
                    backgroundWorldCameraOffset: SkyLayerBackgroundWorldCameraOffset,
                    backgroundWorldCameraParallaxDistancePer100Cells: SkyLayerBackgroundWorldCameraParallaxDistancePer100Cells,
                    overrideViewCenter: null);
                createdNow = true;
            }

            skyLayer.ScenarioTag = ScenarioTag;

            // When backfilling the layer into an old save, world generation has already run,
            // so we need to explicitly generate data for the newly registered layer.
            if (createdNow && Current.CreatingWorld == null)
            {
                int seed = GenText.StableStringHash(Find.World.info.seedString);
                WorldGenerator.GeneratePlanetLayer(skyLayer, Find.World.info.seedString, seed);
                worldGrid.StandardizeTileData();
            }

            EnsureConnections(skyLayer);
            EnsureZoomChain(skyLayer);
            return skyLayer;
        }

        public static void EnsureConnections(PlanetLayer skyLayer)
        {
            PlanetLayer? orbitLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Orbit);
            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);

            EnsureConnection(skyLayer, surfaceLayer);
            EnsureConnection(surfaceLayer, skyLayer);
            EnsureConnection(skyLayer, orbitLayer);
            EnsureConnection(orbitLayer, skyLayer);
        }

        public static void EnsureZoomChain(PlanetLayer skyLayer)
        {
            PlanetLayer? orbitLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Orbit);
            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);

            if (surfaceLayer != null)
            {
                surfaceLayer.zoomOutToLayer = skyLayer;
                skyLayer.zoomInToLayer = surfaceLayer;
            }

            if (orbitLayer != null)
            {
                skyLayer.zoomOutToLayer = orbitLayer;
                orbitLayer.zoomInToLayer = skyLayer;
            }
        }

        private static void EnsureConnection(PlanetLayer? from, PlanetLayer? to)
        {
            if (from == null || to == null || from == to || from.HasConnectionFromTo(to))
            {
                return;
            }

            from.AddConnection(to, 0f);
        }
    }
}
