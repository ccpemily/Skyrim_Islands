using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Quests.Spire
{
    public class JobDriver_OperateFloatingEnergySpire : JobDriver
    {
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

            Toil wait = Toils_General.WaitWith(TargetIndex.A, Spire.GetJobDurationTicks(job.def), true, true);
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            yield return Toils_General.Do(delegate
            {
                Spire.FinishManualOperation(pawn);
            });
        }
    }
}
