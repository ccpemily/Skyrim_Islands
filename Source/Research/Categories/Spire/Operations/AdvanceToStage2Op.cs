using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Operations;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.Spire.Operations
{
    public class AdvanceToStage2Op : ISpireManualOperation
    {
        public SpireManualOperationKind Kind => SpireManualOperationKind.AdvanceToStage2;
        public string Label => "重构稳压";
        public string Description => "当前阶段科技已经完成，现在可以重构尖塔的稳压框架，推进到下一阶段。";
        public JobDef? JobDef => SkyrimIslandsDefOf.SkyrimIslands_RepairSpire;
        public int DurationTicks => 480;

        public void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn, SpireResearchCategory category)
        {
            category.CompleteStageAdvance(2, spire, pawn, FloatingEnergySpireState.PartiallyRestored);
        }
    }
}
