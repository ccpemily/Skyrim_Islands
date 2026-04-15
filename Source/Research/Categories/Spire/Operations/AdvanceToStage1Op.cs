using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Operations;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.Spire.Operations
{
    public class AdvanceToStage1Op : ISpireManualOperation
    {
        public SpireManualOperationKind Kind => SpireManualOperationKind.AdvanceToStage1;
        public string Label => "启动外环";
        public string Description => "当前阶段科技已经完成，现在可以启动尖塔外环，让系统正式进入第一阶段。";
        public JobDef? JobDef => SkyrimIslandsDefOf.SkyrimIslands_RepairSpire;
        public int DurationTicks => 420;

        public void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn, SpireResearchCategory category)
        {
            category.CompleteStageAdvance(1, spire, pawn, FloatingEnergySpireState.Investigating);
        }
    }
}
