using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using SkyrimIslands.World;

namespace SkyrimIslands.Quests.Initial
{
    public static class SkyIslandMigrationUtility
    {
        public const float PrototypeMassCapacity = 650f;
        private const float DepartureDelaySeconds = 4.2f;
        private const int TargetTileMinDistance = 4;
        private const int TargetTileMaxDistance = 12;
        private const int TargetTileFallbackMaxDistance = 30;
        private const int LaunchShuttleRotationAsInt = Rot4.NorthInt;
        private const int PreferredLandingMinRadius = 10;
        private const int PreferredLandingMaxRadius = 20;
        private const int SecondaryLandingSearchRadius = 45;

        public static void BeginMigrationFromLoadedShuttle(Map sourceMap, Thing shuttle, bool abandonOriginalColony, IntVec3 departureCell)
        {
            WorldComponent_SkyIslands skyIslands = Find.World.GetComponent<WorldComponent_SkyIslands>();
            PlanetLayer skyLayer = skyIslands.GetOrCreateSkyLayer();
            PlanetTile skyTile = FindMigrationTargetTile(sourceMap, skyLayer);
            SkyIslandMapParent island = skyIslands.CreateStartingSkyIslandAt(skyTile);
            int cutsceneId = Find.UniqueIDsManager.GetNextSignalTagID();

            Messages.Message("穿梭机正在升空……", new LookTargets(departureCell, sourceMap), MessageTypeDefOf.NeutralEvent, false);

            if (shuttle != null && shuttle.Spawned)
            {
                TransportersArrivalAction_SkyIslandMigration arrivalAction = new TransportersArrivalAction_SkyIslandMigration(
                    cutsceneId,
                    island,
                    sourceMap,
                    abandonOriginalColony);
                SendMissionShuttleAway(shuttle, skyTile, arrivalAction);
            }

            GameComponent_SkyIslandFlow.ScheduleRealtimeAction(DepartureDelaySeconds, delegate
            {
                Find.WindowStack.Add(new Screen_SkyIslandMigrationCinematics(delegate
                {
                    StartWorldFlightCutscene(cutsceneId, island);
                }));
            });
        }

        public static Map GenerateMigrationTargetMap(SkyIslandMapParent island)
        {
            Map targetMap = GetOrGenerateMapUtility.GetOrGenerateMap(
                island.Tile,
                Find.World.info.initialMapSize,
                SkyrimIslandsDefOf.SkyrimIslands_SkyIslandWorldObject);

            island.Notify_MyMapSettled(targetMap);
            return targetMap;
        }

        public static void CompleteMigrationArrival(Map sourceMap, SkyIslandMapParent island, bool abandonOriginalColony, List<ActiveTransporterInfo> transporters)
        {
            Map targetMap = GenerateMigrationTargetMap(island);

            if (abandonOriginalColony)
            {
                RemoveOriginalColony(sourceMap, targetMap);
            }

            SpawnArrivalShuttle(targetMap, transporters);

            Current.Game.CurrentMap = targetMap;
            Find.World.renderer.wantedMode = WorldRenderMode.None;
            CameraJumper.TryJump(targetMap.Center, targetMap, CameraJumper.MovementMode.Cut);
            CameraJumper.TryHideWorld();
            ScreenFader.StartFade(Color.clear, 1f);

            Find.LetterStack.ReceiveLetter(
                "空岛启程完成",
                "未知穿梭机已经飞抵空岛上空，并在降落过程中失控坠毁。幸存的殖民者与物资已经抵达新的家园。",
                LetterDefOf.PositiveEvent,
                new LookTargets(targetMap.Center, targetMap));
        }

        private static void SpawnArrivalShuttle(Map targetMap, List<ActiveTransporterInfo> transporters)
        {
            if (transporters.Count == 0)
            {
                throw new InvalidOperationException("No transported contents were available for the sky island arrival shuttle.");
            }

            Thing? originalShuttle = transporters[0].RemoveShuttle();
            if (originalShuttle != null && !originalShuttle.Destroyed)
            {
                originalShuttle.Destroy(DestroyMode.Vanish);
            }

            TransportShipDef shipDef = DefDatabase<TransportShipDef>.GetNamed("Ship_ShuttleCrashing");
            TransportShip transportShip = TransportShipMaker.MakeTransportShip(shipDef, null, null);
            transportShip.shipThing.SetFaction(Faction.OfPlayer);
            transportShip.TransporterComp.innerContainer.TryAddRangeOrTransfer(transporters[0].innerContainer, true, false);
            IntVec3 landingCell = FindCenteredShuttleLandingSpot(targetMap, Faction.OfPlayer);
            transportShip.ArriveAt(landingCell, targetMap.Parent);

            ShipJob_Unload unloadJob = (ShipJob_Unload)ShipJobMaker.MakeShipJob(ShipJobDefOf.Unload);
            unloadJob.dropMode = TransportShipDropMode.All;
            unloadJob.unforbidAll = true;
            transportShip.AddJob(unloadJob);
        }

        private static void StartWorldFlightCutscene(int cutsceneId, SkyIslandMapParent island)
        {
            ScreenFader.StartFade(Color.clear, 1f);

            TravellingTransporters? travellingShuttle = FindTravellingMissionShuttle(cutsceneId);
            if (travellingShuttle == null)
            {
                Log.Error("[Skyrim Islands] Could not find the travelling shuttle world object for migration cutscene.");
                CameraJumper.TryShowWorld();
                Find.World.renderer.wantedMode = WorldRenderMode.Planet;
                PlanetLayer.Selected = island.Tile.Layer;
                Find.WorldCameraDriver.JumpTo(island.Tile);
                return;
            }

            CameraJumper.TryShowWorld();
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            PlanetLayer.Selected = island.Tile.Layer;
            Find.WorldCameraDriver.ResetAltitude();
            Find.WorldCameraDriver.JumpTo(travellingShuttle.DrawPos);
            Find.WindowStack.Add(new Screen_SkyIslandWorldFlightCutscene(travellingShuttle, island));
        }

        private static TravellingTransporters? FindTravellingMissionShuttle(int cutsceneId)
        {
            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                if (worldObjects[i] is TravellingTransporters travelling &&
                    travelling.arrivalAction is TransportersArrivalAction_SkyIslandMigration migration &&
                    migration.cutsceneId == cutsceneId)
                {
                    return travelling;
                }
            }

            return null;
        }

        private static void SendMissionShuttleAway(Thing shuttle, PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            CompShuttle? compShuttle = shuttle.TryGetComp<CompShuttle>();
            if (compShuttle == null)
            {
                return;
            }

            shuttle.Rotation = new Rot4(LaunchShuttleRotationAsInt);

            if (compShuttle.shipParent == null)
            {
                TransportShipDef shipDef = DefDatabase<TransportShipDef>.GetNamed("Ship_Shuttle");
                compShuttle.shipParent = TransportShipMaker.MakeTransportShip(shipDef, null, shuttle);
            }

            ShipJob_FlyAway flyAwayJob = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
            flyAwayJob.destinationTile = destinationTile;
            flyAwayJob.arrivalAction = arrivalAction;
            compShuttle.shipParent.ForceJob(flyAwayJob);
        }

        public static IntVec3 FindCenteredShuttleLandingSpot(Map map, Faction factionForFindingSpot)
        {
            IntVec3 center = map.Center;
            IntVec2 shuttleSize = ThingDefOf.Shuttle.Size + new IntVec2(2, 2);
            IntVec3? bestCell = null;
            int bestScore = int.MaxValue;

            int minRadiusSquared = PreferredLandingMinRadius * PreferredLandingMinRadius;
            int maxRadiusSquared = PreferredLandingMaxRadius * PreferredLandingMaxRadius;
            int targetRadius = (PreferredLandingMinRadius + PreferredLandingMaxRadius) / 2;

            CellRect searchRect = CellRect.CenteredOn(center, PreferredLandingMaxRadius * 2 + 1, PreferredLandingMaxRadius * 2 + 1).ClipInsideMap(map);
            for (int x = searchRect.minX; x <= searchRect.maxX; x++)
            {
                for (int z = searchRect.minZ; z <= searchRect.maxZ; z++)
                {
                    IntVec3 candidate = new IntVec3(x, 0, z);
                    int distanceSquared = (candidate - center).LengthHorizontalSquared;
                    if (distanceSquared < minRadiusSquared || distanceSquared > maxRadiusSquared)
                    {
                        continue;
                    }

                    if (candidate.Fogged(map))
                    {
                        continue;
                    }

                    if (!DropCellFinder.SkyfallerCanLandAt(candidate, map, shuttleSize, factionForFindingSpot))
                    {
                        continue;
                    }

                    if (!IsReachableFromAnyColonist(candidate, map))
                    {
                        continue;
                    }

                    int distance = Mathf.RoundToInt(Mathf.Sqrt(distanceSquared));
                    int score = Mathf.Abs(distance - targetRadius) * 1000 + distanceSquared;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCell = candidate;
                    }
                }
            }

            if (bestCell.HasValue)
            {
                return bestCell.Value;
            }

            CellRect fallbackRect = CellRect.CenteredOn(center, SecondaryLandingSearchRadius * 2 + 1, SecondaryLandingSearchRadius * 2 + 1).ClipInsideMap(map);
            for (int x = fallbackRect.minX; x <= fallbackRect.maxX; x++)
            {
                for (int z = fallbackRect.minZ; z <= fallbackRect.maxZ; z++)
                {
                    IntVec3 candidate = new IntVec3(x, 0, z);
                    if (candidate.Fogged(map))
                    {
                        continue;
                    }

                    if (!DropCellFinder.SkyfallerCanLandAt(candidate, map, shuttleSize, factionForFindingSpot))
                    {
                        continue;
                    }

                    if (!IsReachableFromAnyColonist(candidate, map))
                    {
                        continue;
                    }

                    int distanceSquared = (candidate - center).LengthHorizontalSquared;
                    if (distanceSquared < bestScore)
                    {
                        bestScore = distanceSquared;
                        bestCell = candidate;
                    }
                }
            }

            if (bestCell.HasValue)
            {
                return bestCell.Value;
            }

            Log.Warning("[Skyrim Islands] Mission shuttle landing spot selection fell back to vanilla DropCellFinder logic.");
            return DropCellFinder.GetBestShuttleLandingSpot(map, factionForFindingSpot);
        }

        private static bool IsReachableFromAnyColonist(IntVec3 destination, Map map)
        {
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly);
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (!pawn.Spawned || pawn.Position.Fogged(map))
                {
                    continue;
                }

                if (map.reachability.CanReach(pawn.Position, destination, PathEndMode.Touch, traverseParms))
                {
                    return true;
                }
            }

            return false;
        }

        private static PlanetTile FindMigrationTargetTile(Map sourceMap, PlanetLayer skyLayer)
        {
            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
            {
                throw new InvalidOperationException("Could not find the surface planet layer for migration target selection.");
            }

            if (TryFindMigrationSurfaceAnchor(sourceMap.Tile, surfaceLayer, skyLayer, TargetTileMinDistance, TargetTileMaxDistance, out PlanetTile skyTile) ||
                TryFindMigrationSurfaceAnchor(sourceMap.Tile, surfaceLayer, skyLayer, 1, TargetTileFallbackMaxDistance, out skyTile))
            {
                return skyTile;
            }

            throw new InvalidOperationException("Failed to find a nearby empty sky island tile for migration.");
        }

        private static bool TryFindMigrationSurfaceAnchor(PlanetTile nearTile, PlanetLayer surfaceLayer, PlanetLayer skyLayer, int minDist, int maxDist, out PlanetTile skyTile)
        {
            bool found = TileFinder.TryFindNewSiteTile(
                out PlanetTile surfaceTile,
                nearTile,
                minDist: minDist,
                maxDist: maxDist,
                canBeSpace: false,
                layer: surfaceLayer,
                validator: delegate(PlanetTile tile)
                {
                    PlanetTile candidateSkyTile = skyLayer.GetClosestTile_NewTemp(tile, true);
                    return candidateSkyTile.Valid && !Find.WorldObjects.AnyWorldObjectAt(candidateSkyTile);
                });

            if (found)
            {
                skyTile = skyLayer.GetClosestTile_NewTemp(surfaceTile, true);
                return skyTile.Valid;
            }

            skyTile = PlanetTile.Invalid;
            return false;
        }

        private static void RemoveOriginalColony(Map sourceMap, Map targetMap)
        {
            if (sourceMap == null || sourceMap == targetMap || !Current.Game.Maps.Contains(sourceMap))
            {
                return;
            }

            MapParent parent = sourceMap.Parent;
            if (parent != null)
            {
                parent.SetFaction(null);
            }

            Current.Game.DeinitAndRemoveMap(sourceMap, false);
            if (parent != null && !parent.Destroyed)
            {
                parent.Destroy();
            }
        }
    }
}
