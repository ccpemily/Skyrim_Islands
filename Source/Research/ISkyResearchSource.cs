using Verse;

namespace SkyrimIslands.Research
{
    public interface ISkyResearchSource
    {
        bool CanPerformResearch(Pawn pawn, SkyIslandResearchProjectDef project, out string reason);
    }
}
