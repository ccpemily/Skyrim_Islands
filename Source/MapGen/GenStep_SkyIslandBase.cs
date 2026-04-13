using RimWorld;
using Verse;

namespace SkyrimIslands.MapGen
{
    public class GenStep_SkyIslandBase : GenStep
    {
        public override int SeedPart => 483210771;

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 center = map.Center;
            int shapeSeed = Gen.HashCombineInt(map.Tile, SeedPart);

            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_CloudSea);
            }

            CellRect shapeBounds = SkyIslandShapeUtility.GetShapeBounds(center).ClipInsideMap(map);
            foreach (IntVec3 cell in shapeBounds)
            {
                if (SkyIslandShapeUtility.IsPlatformCell(cell, center, shapeSeed))
                {
                    map.terrainGrid.SetTerrain(cell, SkyrimIslandsDefOf.SkyrimIslands_FloatingPlatform);
                }
            }

            MapGenerator.SetVar("RectOfInterest", shapeBounds);
            MapGenerator.UsedRects.Add(shapeBounds);
        }
    }
}
