using RimWorld;
using Verse;

namespace SkyrimIslands
{
    [DefOf]
    public static class SkyrimIslandsDefOf
    {
        static SkyrimIslandsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SkyrimIslandsDefOf));
        }

        public static WorldObjectDef SkyrimIslands_SkyIslandWorldObject = null!;

        public static MapGeneratorDef SkyrimIslands_SkyIslandMapGenerator = null!;

        public static TerrainDef SkyrimIslands_CloudSea = null!;

        public static TerrainDef SkyrimIslands_IslandCore = null!;

        public static TerrainDef SkyrimIslands_FloatingPlatform = null!;

        public static BiomeDef SkyrimIslands_SkyBiome = null!;

        public static PlanetLayerDef SkyrimIslands_SkyLayer = null!;

        public static QuestScriptDef SkyrimIslands_MigrationQuest = null!;

        public static ThingDef SkyrimIslands_MissionShuttle = null!;
    }
}
