using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Operations;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.Spire.Operations
{
    public class BootstrapInvestigationOp : ISpireManualOperation
    {
        public SpireManualOperationKind Kind => SpireManualOperationKind.BootstrapInvestigation;
        public string Label => "调查尖塔";
        public string Description => "接受任务后，先完成一次初步调查，以建立尖塔的第一条研究链路。";
        public JobDef? JobDef => SkyrimIslandsDefOf.SkyrimIslands_InvestigateSpire;
        public int DurationTicks => 300;

        public void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn, SpireResearchCategory category)
        {
            category.CompleteBootstrapInvestigation(spire, pawn);
        }
    }
}
