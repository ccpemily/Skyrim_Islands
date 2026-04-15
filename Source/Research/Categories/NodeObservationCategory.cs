using Verse;

namespace SkyrimIslands.Research.Categories.NodeObservation
{
    public class NodeObservationCategory : ISkyResearchCategory
    {
        public SkyIslandDataTypeDef DataType => SkyrimIslandsDefOf.SkyrimIslandsData_NodeObservation;

        public void ExposeData()
        {
        }

        public bool IsProjectVisible(SkyIslandResearchProjectDef project)
        {
            return true;
        }

        public bool TryGetCurrentProject(out SkyIslandResearchProjectDef project)
        {
            project = null!;
            return false;
        }

        public bool HasAvailableWork(Pawn pawn)
        {
            return false;
        }

        public void AddProgress(Thing source, Pawn pawn, SkyIslandResearchProjectDef project, float amount)
        {
        }

        public void NotifyProjectSet(SkyIslandResearchProjectDef project)
        {
        }

        public void NotifyProjectStopped(SkyIslandResearchProjectDef project)
        {
        }

        public void NotifyProjectFinished(SkyIslandResearchProjectDef project)
        {
        }

        public void CategoryTick()
        {
        }
    }
}
