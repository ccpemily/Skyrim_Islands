using RimWorld;
using RimWorld.Planet;

namespace SkyrimIslands.World
{
    public class BiomeWorker_SkyIsland : BiomeWorker
    {
        public override bool CanPlaceOnLayer(BiomeDef biome, PlanetLayer layer)
        {
            return layer.Def == SkyrimIslandsDefOf.SkyrimIslands_SkyLayer;
        }

        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            return -100f;
        }
    }
}
