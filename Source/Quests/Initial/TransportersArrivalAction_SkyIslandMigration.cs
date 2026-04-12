using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SkyrimIslands.World;

namespace SkyrimIslands.Quests.Initial
{
    public class TransportersArrivalAction_SkyIslandMigration : TransportersArrivalAction
    {
        public int cutsceneId;

        private SkyIslandMapParent island = null!;
        private Map sourceMap = null!;
        private bool abandonOriginalColony;

        public override bool GeneratesMap => true;

        public TransportersArrivalAction_SkyIslandMigration()
        {
        }

        public TransportersArrivalAction_SkyIslandMigration(int cutsceneId, SkyIslandMapParent island, Map sourceMap, bool abandonOriginalColony)
        {
            this.cutsceneId = cutsceneId;
            this.island = island;
            this.sourceMap = sourceMap;
            this.abandonOriginalColony = abandonOriginalColony;
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return island == null || !island.HasMap;
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            if (island == null || island.Destroyed)
            {
                return false;
            }

            return island.Tile == destinationTile;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            if (island == null || island.Destroyed)
            {
                Log.Error("[Skyrim Islands] Migration shuttle arrived, but the target sky island no longer exists.");
                return;
            }

            SkyIslandMigrationUtility.CompleteMigrationArrival(sourceMap, island, abandonOriginalColony, transporters);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cutsceneId, "cutsceneId", 0);
            Scribe_References.Look(ref island, "island");
            Scribe_References.Look(ref sourceMap, "sourceMap");
            Scribe_Values.Look(ref abandonOriginalColony, "abandonOriginalColony", false);
        }
    }
}
