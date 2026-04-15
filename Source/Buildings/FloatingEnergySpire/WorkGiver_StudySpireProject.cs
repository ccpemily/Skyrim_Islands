using RimWorld;
using SkyrimIslands.Research;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Buildings.FloatingEnergySpire
{
    public class WorkGiver_StudySpireProject : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(SkyrimIslandsDefOf.SkyrimIslands_FloatingEnergySpire);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool Prioritized => true;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            return research == null || !research.ShouldUseSpireResearchWork();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_FloatingEnergySpire spire)
            {
                return false;
            }

            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null || !research.TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project))
            {
                return false;
            }

            return spire.CanPerformResearch(pawn, project!, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(SkyrimIslandsDefOf.SkyrimIslands_StudySpireProject, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            return t.Thing.GetStatValue(StatDefOf.ResearchSpeedFactor);
        }
    }
}
