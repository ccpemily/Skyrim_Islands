using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class WorldComponent_SkyIslands : WorldComponent
    {
        private static readonly FieldInfo ConnectionsField =
            typeof(PlanetLayer).GetField("connections", BindingFlags.Instance | BindingFlags.NonPublic)!;

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

            if (fromLoad)
            {
                return;
            }

            NormalizeSkyLayerState();
            EnsureSkyLayerLinks();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (!ModsConfig.OdysseyActive || Current.ProgramState == ProgramState.Entry)
            {
                return;
            }

            PlanetLayer? skyLayer = SkyIslandLayerBootstrap.GetSkyLayer();
            if (skyLayer == null || !Find.WorldGrid.HasWorldData)
            {
                return;
            }

            NormalizeSkyLayerState();
            EnsureSkyLayerLinks();
        }

        public SkyIslandMapParent CreateStartingSkyIslandAt(PlanetTile tile)
        {
            PlanetLayer skyLayer = SkyIslandLayerBootstrap.GetSkyLayer()
                ?? throw new System.InvalidOperationException("Sky island layer was not registered during world initialization.");
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

        public PlanetLayer GetSkyLayer()
        {
            return SkyIslandLayerBootstrap.GetSkyLayer()
                ?? throw new System.InvalidOperationException("Sky island layer was not registered during world initialization.");
        }

        private void NormalizeSkyLayerState()
        {
            PlanetLayer? canonicalSkyLayer = GetCanonicalSkyLayer();
            if (canonicalSkyLayer == null)
            {
                return;
            }

            MigrateWorldObjectsToCanonicalLayer(canonicalSkyLayer);
            RemoveRedundantSkyLayers(canonicalSkyLayer);

            List<PlanetLayer> allLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
            for (int i = 0; i < allLayers.Count; i++)
            {
                NormalizeConnections(allLayers[i], canonicalSkyLayer);
            }

            SkyIslandLayerBootstrap.EnsureConnections(canonicalSkyLayer);
            SkyIslandLayerBootstrap.EnsureZoomChain(canonicalSkyLayer);
        }

        private PlanetLayer? GetCanonicalSkyLayer()
        {
            if (startingSkyIsland != null &&
                !startingSkyIsland.Destroyed &&
                startingSkyIsland.Tile.Valid &&
                startingSkyIsland.Tile.Layer.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer)
            {
                return startingSkyIsland.Tile.Layer;
            }

            SkyIslandMapParent? island = Find.WorldObjects.AllWorldObjects
                .OfType<SkyIslandMapParent>()
                .FirstOrDefault(static worldObject => worldObject.Tile.Valid &&
                                                     worldObject.Tile.Layer.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (island != null)
            {
                return island.Tile.Layer;
            }

            PlanetLayer? existingLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (existingLayer == null)
            {
                return null;
            }

            return existingLayer;
        }

        private static void RemoveRedundantSkyLayers(PlanetLayer canonicalSkyLayer)
        {
            List<PlanetLayer> duplicateLayers = Find.WorldGrid.PlanetLayers.Values
                .Where(layer => layer != canonicalSkyLayer && layer.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer)
                .ToList();

            for (int i = 0; i < duplicateLayers.Count; i++)
            {
                PlanetLayer duplicateLayer = duplicateLayers[i];
                bool hasWorldObjects = Find.WorldObjects.AllWorldObjects.Any(worldObject => worldObject.Tile.Layer == duplicateLayer);
                if (hasWorldObjects)
                {
                    continue;
                }

                Find.WorldGrid.RemovePlanetLayer(duplicateLayer);
            }
        }

        private void MigrateWorldObjectsToCanonicalLayer(PlanetLayer canonicalSkyLayer)
        {
            List<PlanetLayer> duplicateLayers = Find.WorldGrid.PlanetLayers.Values
                .Where(layer => layer != canonicalSkyLayer && layer.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer)
                .ToList();

            for (int i = 0; i < duplicateLayers.Count; i++)
            {
                PlanetLayer duplicateLayer = duplicateLayers[i];
                List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects
                    .Where(worldObject => worldObject.Tile.Valid && worldObject.Tile.Layer == duplicateLayer)
                    .ToList();

                for (int j = 0; j < worldObjects.Count; j++)
                {
                    WorldObject worldObject = worldObjects[j];
                    PlanetTile destinationTile = canonicalSkyLayer.GetClosestTile_NewTemp(worldObject.Tile, false);
                    if (!destinationTile.Valid)
                    {
                        destinationTile = canonicalSkyLayer.PlanetTileForID(worldObject.Tile.tileId);
                    }

                    worldObject.Tile = destinationTile;
                    if (ReferenceEquals(worldObject, startingSkyIsland))
                    {
                        startingSkyIsland = (SkyIslandMapParent)worldObject;
                    }
                }

                if (PlanetLayer.Selected == duplicateLayer)
                {
                    PlanetLayer.Selected = canonicalSkyLayer;
                }
            }
        }

        private static void NormalizeConnections(PlanetLayer layer, PlanetLayer canonicalSkyLayer)
        {
            Dictionary<PlanetLayer, PlanetLayerConnection>? existingConnections =
                ConnectionsField.GetValue(layer) as Dictionary<PlanetLayer, PlanetLayerConnection>;
            if (existingConnections == null || existingConnections.Count == 0)
            {
                return;
            }

            bool changed = false;
            Dictionary<PlanetLayer, PlanetLayerConnection> normalizedConnections = new Dictionary<PlanetLayer, PlanetLayerConnection>();

            foreach (KeyValuePair<PlanetLayer, PlanetLayerConnection> pair in existingConnections)
            {
                PlanetLayer? target = pair.Key ?? pair.Value?.target;
                if (target == null || target == layer)
                {
                    changed = true;
                    continue;
                }

                if (target.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer && target != canonicalSkyLayer)
                {
                    target = canonicalSkyLayer;
                    changed = true;
                }

                if (normalizedConnections.ContainsKey(target))
                {
                    changed = true;
                    continue;
                }

                PlanetLayerConnection connection = pair.Value ?? new PlanetLayerConnection();
                if (connection.origin != layer || connection.target != target)
                {
                    connection.origin = layer;
                    connection.target = target;
                    changed = true;
                }

                normalizedConnections[target] = connection;
            }

            if (changed)
            {
                ConnectionsField.SetValue(layer, normalizedConnections);
            }
        }

        private static bool IsTileUsable(PlanetTile tile)
        {
            return tile.Valid && !Find.WorldObjects.AnyWorldObjectAt(tile);
        }

        private static void EnsureSkyLayerLinks()
        {
            PlanetLayer? skyLayer = SkyIslandLayerBootstrap.GetSkyLayer();
            if (skyLayer == null)
            {
                return;
            }

            SkyIslandLayerBootstrap.EnsureConnections(skyLayer);
            SkyIslandLayerBootstrap.EnsureZoomChain(skyLayer);
        }
    }
}
