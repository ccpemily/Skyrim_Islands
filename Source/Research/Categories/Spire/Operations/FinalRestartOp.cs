using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Operations;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.Spire.Operations
{
    public class FinalRestartOp : ISpireManualOperation
    {
        public SpireManualOperationKind Kind => SpireManualOperationKind.FinalRestart;
        public string Label => "完全重启";
        public string Description => "最终阶段科技已经完成，现在可以执行完整重启，让尖塔恢复全部功能。";
        public JobDef? JobDef => SkyrimIslandsDefOf.SkyrimIslands_RestartSpire;
        public int DurationTicks => 600;

        public void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn, SpireResearchCategory category)
        {
            category.CompleteFinalRestart(spire, pawn);
        }
    }
}
