using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public static class SkyIslandMigrationUtility
    {
        public const float PrototypeMassCapacity = 650f;
        private const int TargetTileMinDistance = 4;
        private const int TargetTileMaxDistance = 12;
        private const int TargetTileFallbackMaxDistance = 30;
        private const int ShuttleRotationAsInt = Rot4.EastInt;

        public static void BeginMigration(Map sourceMap, Thing shuttle, List<SkyIslandLoadSelection> selections, bool abandonOriginalColony, Quest quest, IntVec3 departureCell)
        {
            WorldComponent_SkyIslands skyIslands = Find.World.GetComponent<WorldComponent_SkyIslands>();
            PlanetLayer skyLayer = skyIslands.GetOrCreateSkyLayer();

            if (shuttle != null && shuttle.Spawned)
            {
                SendPassengerShuttleAway(shuttle);
            }

            Find.WindowStack.Add(new Screen_SkyIslandDepartureCinematics(sourceMap, departureCell, delegate
            {
                PlanetTile skyTile = FindMigrationTargetTile(sourceMap, skyLayer);
                Find.WindowStack.Add(new Screen_SkyIslandMigrationCinematics(delegate
                {
                    LongEventHandler.QueueLongEvent(
                        () => ExecuteMigration(sourceMap, selections, skyTile, abandonOriginalColony, quest),
                        "GeneratingMap",
                        doAsynchronously: false,
                        exceptionHandler: delegate(Exception exception)
                        {
                            ScreenFader.StartFade(Color.clear, 1f);
                            Log.Error("[Skyrim Islands] Sky island migration failed: " + exception);
                            quest.End(QuestEndOutcome.Fail);
                        });
                }));
            }));
        }

        private static void ExecuteMigration(Map sourceMap, List<SkyIslandLoadSelection> selections, PlanetTile tile, bool abandonOriginalColony, Quest quest)
        {
            WorldComponent_SkyIslands skyIslands = Find.World.GetComponent<WorldComponent_SkyIslands>();
            SkyIslandMapParent island = skyIslands.CreateStartingSkyIslandAt(tile);
            Map targetMap = GetOrGenerateMapUtility.GetOrGenerateMap(
                island.Tile,
                Find.World.info.initialMapSize,
                SkyrimIslandsDefOf.SkyrimIslands_SkyIslandWorldObject);

            island.Notify_MyMapSettled(targetMap);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                List<Thing> cargo = PrepareCargo(selections);

                Current.Game.CurrentMap = targetMap;
                Find.World.renderer.wantedMode = WorldRenderMode.None;
                CameraJumper.TryJump(targetMap.Center, targetMap);
                CameraJumper.TryHideWorld();
                ScreenFader.StartFade(Color.clear, 1f);

                if (abandonOriginalColony)
                {
                    RemoveOriginalColony(sourceMap, targetMap);
                }

                SpawnArrivalShuttle(targetMap, cargo);
                quest.End(QuestEndOutcome.Success, sendLetter: false, playSound: false);
                Find.LetterStack.ReceiveLetter(
                    "空岛启程完成",
                    "未知穿梭机将选中的殖民者与物资送抵云层上方。空岛殖民地已经建立。",
                    LetterDefOf.PositiveEvent,
                    new LookTargets(targetMap.Center, targetMap));
            });
        }

        private static List<Thing> PrepareCargo(List<SkyIslandLoadSelection> selections)
        {
            List<Thing> cargo = new List<Thing>();
            for (int i = 0; i < selections.Count; i++)
            {
                SkyIslandLoadSelection selection = selections[i];
                Thing thing = selection.Thing;
                if (thing.Destroyed)
                {
                    continue;
                }

                if (selection.Count <= 0)
                {
                    continue;
                }

                if (selection.Count < thing.stackCount)
                {
                    thing = thing.SplitOff(selection.Count);
                }

                if (thing.Spawned)
                {
                    thing.DeSpawn();
                }

                if (thing.holdingOwner != null)
                {
                    thing.holdingOwner.Remove(thing);
                }

                cargo.Add(thing);
            }

            return cargo;
        }

        private static void SpawnArrivalShuttle(Map targetMap, List<Thing> cargo)
        {
            Thing shuttle = ThingMaker.MakeThing(ThingDefOf.PassengerShuttle);
            shuttle.Rotation = new Rot4(ShuttleRotationAsInt);
            TransportShipDef shipDef = DefDatabase<TransportShipDef>.GetNamed("Ship_PassengerShuttle");
            TransportShip transportShip = TransportShipMaker.MakeTransportShip(shipDef, cargo, shuttle);
            IntVec3 landingCell = DropCellFinder.GetBestShuttleLandingSpot(targetMap, Faction.OfPlayer);
            transportShip.ArriveAt(landingCell, targetMap.Parent);

            ShipJob_Unload unloadJob = (ShipJob_Unload)ShipJobMaker.MakeShipJob(ShipJobDefOf.Unload);
            unloadJob.dropMode = TransportShipDropMode.All;
            unloadJob.unforbidAll = true;
            transportShip.AddJob(unloadJob);
        }

        private static void SendPassengerShuttleAway(Thing shuttle)
        {
            CompShuttle? compShuttle = shuttle.TryGetComp<CompShuttle>();
            if (compShuttle == null)
            {
                return;
            }

            shuttle.Rotation = new Rot4(ShuttleRotationAsInt);

            if (compShuttle.shipParent == null)
            {
                TransportShipDef shipDef = DefDatabase<TransportShipDef>.GetNamed("Ship_PassengerShuttle");
                compShuttle.shipParent = TransportShipMaker.MakeTransportShip(shipDef, null, shuttle);
            }

            compShuttle.shipParent.ForceJob(ShipJobDefOf.FlyAway);
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
