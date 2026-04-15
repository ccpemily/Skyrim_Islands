using RimWorld;
using SkyrimIslands.Research;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Buildings.WeatherMonitor
{
    public class WorkGiver_InvestigateWeatherMonitor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(SkyrimIslandsDefOf.SkyrimIslands_WeatherMonitor);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool Prioritized => true;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            return research == null || !research.ShouldUseSkyResearchWork(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_WeatherMonitor monitor)
            {
                return false;
            }

            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null || !research.TryGetCurrentSkyResearchProject(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch, out SkyIslandResearchProjectDef? project))
            {
                return false;
            }

            return monitor.CanPerformInvestigation(pawn, project!, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(SkyrimIslandsDefOf.SkyrimIslands_InvestigateWeatherMonitor, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            if (t.Thing is not Building_WeatherMonitor monitor)
            {
                return 0f;
            }

            return 6f + monitor.StoredDataPercent;
        }
    }
}
