using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Categories.Spire;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Operations
{
    public interface ISpireManualOperation
    {
        SpireManualOperationKind Kind { get; }
        string Label { get; }
        string Description { get; }
        JobDef? JobDef { get; }
        int DurationTicks { get; }
        void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn, SpireResearchCategory category);
    }
}
