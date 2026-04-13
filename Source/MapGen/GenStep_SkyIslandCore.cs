using RimWorld;
using Verse;
using Verse.AI;

namespace SkyrimIslands.MapGen
{
    public class GenStep_SkyIslandCore : GenStep
    {
        public override int SeedPart => 483210772;

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 center = map.Center;
            CellRect shapeBounds = SkyIslandShapeUtility.GetShapeBounds(center).ClipInsideMap(map);
            foreach (IntVec3 cell in shapeBounds)
            {
                if (SkyIslandShapeUtility.IsCoreCell(cell, center))
                {
                    map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_IslandCore);
                }
            }

            Thing spire = ThingMaker.MakeThing(SkyrimIslandsDefOf.SkyrimIslands_FloatingEnergySpire);
            spire.SetFaction(null);
            GenSpawn.Spawn(spire, center, map, WipeMode.Vanish);

            IntVec3 startCell;
            if (!CellFinder.TryFindRandomCellNear(
                center,
                map,
                4,
                c => c.Standable(map) &&
                     !c.Fogged(map) &&
                     map.reachability.CanReach(center, c, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly)),
                out startCell))
            {
                startCell = CellFinder.RandomClosewalkCellNear(center, map, 4);
            }

            MapGenerator.PlayerStartSpot = startCell;
            MapGenerator.rootsToUnfog.Add(startCell);
            MapGenerator.rootsToUnfog.Add(center);
            MapGenerator.UsedRects.Add(shapeBounds);
        }
    }
}
