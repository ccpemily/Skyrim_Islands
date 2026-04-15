using System.Collections.Generic;
using RimWorld;
using SkyrimIslands.Research;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Buildings.WeatherMonitor
{
    public class JobDriver_InvestigateWeatherMonitor : JobDriver
    {
        private const int InvestigateTicks = 300;

        private Building_WeatherMonitor Monitor => (Building_WeatherMonitor)TargetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Monitor, job, 1, -1, null, errorOnFailed) &&
                   pawn.ReserveSittableOrSpot(Monitor.InteractionCell, job, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil investigate = ToilMaker.MakeToil("InvestigateWeatherMonitor");
            investigate.FailOn((System.Func<bool>)(() =>
                !Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!
                    .TryGetCurrentSkyResearchProject(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch, out _)));
            investigate.FailOn((System.Func<bool>)delegate
            {
                if (!Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!
                        .TryGetCurrentSkyResearchProject(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch, out SkyIslandResearchProjectDef? project))
                {
                    return true;
                }

                return !Monitor.CanPerformInvestigation(pawn, project!, out _);
            });
            investigate.tickIntervalAction = delegate(int delta)
            {
                pawn.skills.Learn(SkillDefOf.Intellectual, 0.08f * delta, false, false);
                pawn.GainComfortFromCellIfPossible(delta);
            };
            investigate.defaultCompleteMode = ToilCompleteMode.Delay;
            investigate.defaultDuration = InvestigateTicks;
            investigate.WithProgressBarToilDelay(TargetIndex.A);
            investigate.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            investigate.activeSkill = () => SkillDefOf.Intellectual;
            yield return investigate;

            yield return Toils_General.Do(delegate
            {
                Monitor.CompleteInvestigation(pawn);
            });
        }
    }
}
