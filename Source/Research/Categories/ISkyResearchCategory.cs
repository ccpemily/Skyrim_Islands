using Verse;

namespace SkyrimIslands.Research.Categories
{
    public interface ISkyResearchCategory : IExposable
    {
        SkyIslandDataTypeDef DataType { get; }

        bool IsProjectVisible(SkyIslandResearchProjectDef project);
        bool TryGetCurrentProject(out SkyIslandResearchProjectDef project);
        bool HasAvailableWork(Pawn pawn);
        void AddProgress(Thing source, Pawn pawn, SkyIslandResearchProjectDef project, float amount);
        void NotifyProjectSet(SkyIslandResearchProjectDef project);
        void NotifyProjectStopped(SkyIslandResearchProjectDef project);
        void NotifyProjectFinished(SkyIslandResearchProjectDef project);
        void CategoryTick();
    }
}
