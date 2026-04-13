using RimWorld;
using SkyrimIslands.Research;
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

        public static QuestScriptDef SkyrimIslands_SpireRecoveryQuest = null!;

        public static ThingDef SkyrimIslands_MissionShuttle = null!;

        public static ThingDef SkyrimIslands_FloatingEnergySpire = null!;

        public static ThingDef SkyrimIslands_WeatherMonitor = null!;

        public static JobDef SkyrimIslands_InvestigateSpire = null!;

        public static JobDef SkyrimIslands_RepairSpire = null!;

        public static JobDef SkyrimIslands_RestartSpire = null!;

        public static JobDef SkyrimIslands_StudySpireProject = null!;

        public static JobDef SkyrimIslands_InvestigateWeatherMonitor = null!;

        public static ResearchTabDef SkyrimIslands_SkyResearchTab = null!;

        public static ResearchProjectDef SkyrimIslands_SpireTech_0 = null!;

        public static ResearchProjectDef SkyrimIslands_SpireTech_1 = null!;

        public static ResearchProjectDef SkyrimIslands_SpireTech_2 = null!;

        public static ResearchProjectDef SkyrimIslands_SpireTech_3 = null!;

        public static ResearchProjectDef SkyrimIslands_CloudSeaFishing = null!;

        public static SkyIslandDataTypeDef SkyrimIslandsData_SpireResearch = null!;

        public static SkyIslandDataTypeDef SkyrimIslandsData_CloudSeaResearch = null!;

        public static SkyIslandDataTypeDef SkyrimIslandsData_NodeObservation = null!;
    }
}
