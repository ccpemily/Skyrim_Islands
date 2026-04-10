using RimWorld;
using Verse;

namespace SkyrimIslands.MapGen
{
    public class GenStep_SkyIslandCore : GenStep
    {
        public override int SeedPart => 483210772;

        public override void Generate(Map map, GenStepParams parms)
        {
            CellRect coreRect = CellRect.CenteredOn(map.Center, 8, 8);
            foreach (IntVec3 cell in coreRect)
            {
                if (cell.InBounds(map))
                {
                    map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_IslandCore);
                }
            }

            IntVec3 startCell = coreRect.CenterCell;
            MapGenerator.PlayerStartSpot = startCell;
            MapGenerator.rootsToUnfog.Add(startCell);
            MapGenerator.UsedRects.Add(coreRect);
        }
    }
}
