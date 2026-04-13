using System.Collections.Generic;
using RimWorld;
using SkyrimIslands.Research;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Quests.Spire
{
    public class JobDriver_StudySpireProject : JobDriver
    {
        private const int StudyTicks = 360;
        private const int StudyPulseTicks = 120;
        private const float ProgressPerPulse = 0.34f;

        private Building_FloatingEnergySpire Spire => (Building_FloatingEnergySpire)TargetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Spire, job, 1, -1, null, errorOnFailed) &&
                   pawn.ReserveSittableOrSpot(Spire.InteractionCell, job, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil study = ToilMaker.MakeToil("StudySpireProject");
            study.FailOn(() => !Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!.TryGetCurrentSpireResearchProject(out _));
            study.FailOn((System.Func<bool>)delegate
            {
                if (!Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!.TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project))
                {
                    return true;
                }

                return !Spire.CanPerformResearch(pawn, project!, out _);
            });
            study.tickIntervalAction = delegate(int delta)
            {
                if (!Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!.TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project))
                {
                    return;
                }

                int currentTick = Find.TickManager.TicksGame;
                if (currentTick % StudyPulseTicks == 0)
                {
                    Current.Game.GetComponent<GameComponent_SkyIslandResearch>()!.AddSpireResearchProgress(Spire, pawn, project!, ProgressPerPulse);
                }

                pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f * delta, false, false);
                pawn.GainComfortFromCellIfPossible(delta);
            };
            study.defaultCompleteMode = ToilCompleteMode.Delay;
            study.defaultDuration = StudyTicks;
            study.WithProgressBarToilDelay(TargetIndex.A);
            study.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            study.activeSkill = () => SkillDefOf.Intellectual;
            yield return study;

            yield return Toils_General.Do(delegate
            {
                Spire.StartResearchCooldown();
            });
        }
    }
}
