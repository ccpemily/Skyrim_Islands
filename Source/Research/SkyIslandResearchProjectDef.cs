using Verse;

namespace SkyrimIslands.Research
{
    public class SkyIslandResearchProjectDef : ResearchProjectDef
    {
        public SkyIslandDataTypeDef skyIslandDataType = null!;
        public int stage;
        public bool allowManualResearch;
    }
}
