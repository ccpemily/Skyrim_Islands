using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SkyrimIslands.World
{
    public class GameComponent_SkyIslandFlow : GameComponent
    {
        private bool initializedStartingSkyIsland;

        public GameComponent_SkyIslandFlow(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initializedStartingSkyIsland, "initializedStartingSkyIsland", false);
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();

            if (initializedStartingSkyIsland || !ModsConfig.OdysseyActive)
            {
                return;
            }

            Map? sourceMap = Find.Maps.FirstOrDefault((Map map) => map.IsPlayerHome);
            if (sourceMap == null)
            {
                Log.Warning("[Skyrim Islands] Could not find the starting home map.");
                return;
            }

            initializedStartingSkyIsland = true;

            WorldComponent_SkyIslands worldComponent = Find.World.GetComponent<WorldComponent_SkyIslands>();
            SkyIslandMapParent island = worldComponent.EnsureStartingSkyIsland(sourceMap.Tile);
            Map targetMap = GetOrGenerateMapUtility.GetOrGenerateMap(
                island.Tile,
                Find.World.info.initialMapSize,
                SkyrimIslandsDefOf.SkyrimIslands_SkyIslandWorldObject);

            island.Notify_MyMapSettled(targetMap);
            MoveStartingPawns(sourceMap, targetMap);
            MoveStartingItems(sourceMap, targetMap);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Current.Game.CurrentMap = targetMap;
                Find.LetterStack.ReceiveLetter(
                    "空岛已建立",
                    "初始空岛已经在空岛专属图层生成。当前版本会先把起始殖民者与附近的开局物资转移到空岛，原始地表地图暂时保留，后续再接入完整的迁移任务流程。",
                    LetterDefOf.PositiveEvent,
                    new LookTargets(targetMap.Center, targetMap));
            });

            Log.Message("[Skyrim Islands] Starting sky island created.");
        }

        private static void MoveStartingPawns(Map sourceMap, Map targetMap)
        {
            List<Pawn> pawns = sourceMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                .Where((Pawn pawn) => pawn.Spawned)
                .ToList();

            IntVec3 anchor = targetMap.Center;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(anchor, targetMap, 6);
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, cell, targetMap);
            }
        }

        private static void MoveStartingItems(Map sourceMap, Map targetMap)
        {
            IntVec3 sourceAnchor = sourceMap.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? sourceMap.Center;
            IntVec3 targetAnchor = targetMap.Center;

            List<Thing> items = sourceMap.listerThings.AllThings
                .Where((Thing thing) =>
                    thing.Spawned &&
                    thing.def.category == ThingCategory.Item &&
                    thing.Position.InHorDistOf(sourceAnchor, 12f))
                .ToList();

            for (int i = 0; i < items.Count; i++)
            {
                Thing thing = items[i];
                thing.DeSpawn();
                GenPlace.TryPlaceThing(thing, CellFinder.RandomClosewalkCellNear(targetAnchor, targetMap, 8), targetMap, ThingPlaceMode.Near);
            }
        }
    }
}
