using RimWorld;
using Verse;

namespace SkyrimIslands.MapGen
{
    public class GenStep_SkyIslandBase : GenStep
    {
        public override int SeedPart => 483210771;

        public override void Generate(Map map, GenStepParams parms)
        {
            CellRect coreRect = CellRect.CenteredOn(map.Center, 8, 8);

            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_CloudSea);
            }

            CellRect platformRect = CellRect.CenteredOn(map.Center, 16, 16);
            foreach (IntVec3 cell in platformRect)
            {
                if (cell.InBounds(map) && !coreRect.Contains(cell))
                {
                    map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_FloatingPlatform);
                }
            }

            MapGenerator.SetVar("RectOfInterest", platformRect);
            MapGenerator.UsedRects.Add(platformRect);
        }
    }
}
