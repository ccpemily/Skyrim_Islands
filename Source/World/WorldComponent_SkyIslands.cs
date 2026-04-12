using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class WorldComponent_SkyIslands : WorldComponent
    {
        private const float SkyLayerRadius = 106f;
        private const float SkyLayerViewAngle = 180f;
        private const float SkyLayerExtraCameraAltitude = 20f;
        private const int SkyLayerSubdivisions = 10;
        private const float SkyLayerBackgroundWorldCameraOffset = 32f;
        private const float SkyLayerBackgroundWorldCameraParallaxDistancePer100Cells = 0.5f;

        private SkyIslandMapParent? startingSkyIsland;

        public WorldComponent_SkyIslands(RimWorld.Planet.World world)
            : base(world)
        {
        }

        public SkyIslandMapParent? StartingSkyIsland => startingSkyIsland;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref startingSkyIsland, "startingSkyIsland");
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);

            if (!ModsConfig.OdysseyActive)
            {
                return;
            }

            EnsureSkyLayer();
        }

        public SkyIslandMapParent CreateStartingSkyIslandAt(PlanetTile tile)
        {
            PlanetLayer skyLayer = EnsureSkyLayer();
            if (tile.Layer != skyLayer)
            {
                tile = skyLayer.GetClosestTile_NewTemp(tile, true);
            }

            if (!IsTileUsable(tile))
            {
                throw new System.InvalidOperationException("The selected sky island tile is not usable.");
            }

            Find.WorldGrid[tile].PrimaryBiome = SkyrimIslandsDefOf.SkyrimIslands_SkyBiome;

            SkyIslandMapParent island = (SkyIslandMapParent)WorldObjectMaker.MakeWorldObject(SkyrimIslandsDefOf.SkyrimIslands_SkyIslandWorldObject);
            island.Tile = tile;
            island.SetFaction(Faction.OfPlayer);
            island.Name = "浮空岛";
            Find.WorldObjects.Add(island);

            startingSkyIsland = island;
            return island;
        }

        public PlanetLayer GetOrCreateSkyLayer()
        {
            return EnsureSkyLayer();
        }

        private static PlanetLayer EnsureSkyLayer()
        {
            PlanetLayer? existingLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (existingLayer != null)
            {
                EnsureConnections(existingLayer);
                EnsureZoomChain(existingLayer);
                return existingLayer;
            }

            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);

            PlanetLayer layer = Find.WorldGrid.RegisterPlanetLayer(
                SkyrimIslandsDefOf.SkyrimIslands_SkyLayer,
                origin: Vector3.zero,
                radius: SkyLayerRadius,
                viewAngle: SkyLayerViewAngle,
                extraCameraAltitude: SkyLayerExtraCameraAltitude,
                subdivisions: SkyLayerSubdivisions,
                backgroundWorldCameraOffset: SkyLayerBackgroundWorldCameraOffset,
                backgroundWorldCameraParallaxDistancePer100Cells: SkyLayerBackgroundWorldCameraParallaxDistancePer100Cells,
                overrideViewCenter: null);

            int seed = GenText.StableStringHash(Find.World.info.seedString);
            WorldGenerator.GeneratePlanetLayer(layer, Find.World.info.seedString, seed);
            Find.WorldGrid.StandardizeTileData();

            EnsureConnections(layer);
            EnsureZoomChain(layer);

            return layer;
        }

        private static void EnsureConnections(PlanetLayer skyLayer)
        {
            PlanetLayer? orbitLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Orbit);
            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);

            EnsureConnection(skyLayer, surfaceLayer);
            EnsureConnection(surfaceLayer, skyLayer);
            EnsureConnection(skyLayer, orbitLayer);
            EnsureConnection(orbitLayer, skyLayer);
        }

        private static void EnsureZoomChain(PlanetLayer skyLayer)
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

        private static bool IsTileUsable(PlanetTile tile)
        {
            return tile.Valid && !Find.WorldObjects.AnyWorldObjectAt(tile);
        }
    }
}
