using System.Collections.Generic;
using RimWorld;
using SkyrimIslands.Research;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Quests.Spire
{
    public enum FloatingEnergySpireState : byte
    {
        Abandoned,
        Investigating,
        PartiallyRestored,
        FullyRestarted
    }

    public class Building_FloatingEnergySpire : Building
    {
        private const int ResearchCooldownTicks = 3000;

        private static readonly Color AbandonedColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color InvestigatingColor = new Color(0.95f, 0.82f, 0.38f);
        private static readonly Color PartiallyRestoredColor = new Color(0.7f, 0.95f, 0.6f);
        private static readonly Color FullyRestartedColor = new Color(0.45f, 1f, 1f);

        private FloatingEnergySpireState state;
        private int researchCooldownUntilTick = -1;

        public FloatingEnergySpireState State => state;

        public int TicksUntilResearchReady => Mathf.Max(0, researchCooldownUntilTick - Find.TickManager.TicksGame);

        public bool ResearchReadyNow => TicksUntilResearchReady <= 0;

        public override Graphic Graphic => def.graphicData?.GraphicColoredFor(this) ?? base.Graphic;

        public override Color DrawColor
        {
            get
            {
                return state switch
                {
                    FloatingEnergySpireState.Investigating => InvestigatingColor,
                    FloatingEnergySpireState.PartiallyRestored => PartiallyRestoredColor,
                    FloatingEnergySpireState.FullyRestarted => FullyRestartedColor,
                    _ => AbandonedColor
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref state, "state", FloatingEnergySpireState.Abandoned);
            Scribe_Values.Look(ref researchCooldownUntilTick, "researchCooldownUntilTick", -1);
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            string statusText = "状态：" + GetStateLabel();
            string taskText;

            if (research == null)
            {
                taskText = "尖塔研究系统尚未初始化。";
            }
            else if (!research.IsCurrentSpireQuestAccepted() && state != FloatingEnergySpireState.FullyRestarted)
            {
                taskText = "下一步：先处理当前尖塔任务。";
            }
            else if (research.GetCurrentManualOperation() != SpireManualOperationKind.None)
            {
                taskText = "下一步：" + research.GetCurrentOperationLabel() + "。";
            }
            else if (research.TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project))
            {
                taskText = "当前空岛研究项目：" + project!.LabelCap;
            }
            else
            {
                taskText = "当前没有分配中的尖塔科研项目。";
            }

            string cooldownText = ResearchReadyNow ? "调查冷却：可进行研究" : "调查冷却：" + TicksUntilResearchReady.ToStringTicksToPeriod();
            return statusText + "\n" + taskText + "\n" + cooldownText +
                   (inspectString.NullOrEmpty() ? string.Empty : "\n" + inspectString);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            foreach (Gizmo gizmo in QuestUtility.GetQuestRelatedGizmos(this))
            {
                yield return gizmo;
            }

            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null || research.GetCurrentManualOperation() == SpireManualOperationKind.None)
            {
                yield break;
            }

            if (!TryGetCurrentManualJobDef(research, out JobDef? jobDef))
            {
                yield break;
            }

            Pawn? assignedPawn = GetPreferredPawnForManualOperation(research);
            string disableReason = "当前没有可执行该操作的殖民者。";
            bool canStart = assignedPawn != null && CanStartManualOperation(assignedPawn, research, out disableReason);

            yield return new Command_Action
            {
                defaultLabel = research.GetCurrentOperationLabel(),
                defaultDesc = research.GetCurrentOperationDescription(),
                icon = GetOperationIcon(jobDef!),
                Disabled = !canStart,
                disabledReason = disableReason,
                action = delegate
                {
                    if (assignedPawn != null)
                    {
                        OrderManualOperation(assignedPawn, research, showMessage: true);
                    }
                }
            };
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null || research.GetCurrentManualOperation() == SpireManualOperationKind.None)
            {
                yield break;
            }

            if (!TryGetCurrentManualJobDef(research, out JobDef? jobDef))
            {
                yield break;
            }

            if (!CanStartManualOperation(selPawn, research, out string disableReason))
            {
                yield return new FloatMenuOption(research.GetCurrentOperationLabel() + ": " + disableReason, null);
                yield break;
            }

            yield return new FloatMenuOption(research.GetCurrentOperationLabel(), delegate
            {
                OrderManualOperation(selPawn, research, showMessage: false);
            });
        }

        public void ApplyState(FloatingEnergySpireState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            if (Spawned)
            {
                DirtyMapMesh(Map);
            }
        }

        public bool CanPerformResearch(Pawn pawn, SkyIslandResearchProjectDef project, out string reason)
        {
            reason = string.Empty;

            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null || !research.IsCurrentSpireQuestAccepted())
            {
                reason = "当前没有可进行的尖塔任务。";
                return false;
            }

            if (project.IsFinished)
            {
                reason = "当前阶段科技已经完成。";
                return false;
            }

            if (!ResearchReadyNow)
            {
                reason = "尖塔仍在调查冷却中。";
                return false;
            }

            if (pawn.Downed || pawn.InMentalState)
            {
                reason = "该殖民者目前无法执行研究工作。";
                return false;
            }

            if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                reason = "无法抵达尖塔的交互位置。";
                return false;
            }

            if (!pawn.CanReserve(this))
            {
                reason = "尖塔当前正被其他殖民者占用。";
                return false;
            }

            return true;
        }

        public void StartResearchCooldown()
        {
            researchCooldownUntilTick = Find.TickManager.TicksGame + ResearchCooldownTicks;
        }

        public int GetJobDurationTicks(JobDef jobDef)
        {
            if (jobDef == SkyrimIslandsDefOf.SkyrimIslands_StudySpireProject)
            {
                return 0;
            }

            return Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.GetCurrentOperationDurationTicks() ?? 0;
        }

        public void FinishManualOperation(Pawn pawn)
        {
            Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.NotifySpireManualOperationCompleted(this, pawn);
        }

        private bool CanStartManualOperation(Pawn pawn, GameComponent_SkyIslandResearch research, out string reason)
        {
            reason = string.Empty;

            if (research.GetCurrentManualOperation() == SpireManualOperationKind.None)
            {
                reason = "当前没有可执行的尖塔操作。";
                return false;
            }

            if (pawn.Downed)
            {
                reason = "该殖民者当前倒地。";
                return false;
            }

            if (pawn.InMentalState)
            {
                reason = "该殖民者目前无法执行精密操作。";
                return false;
            }

            if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                reason = "无法抵达尖塔的交互位置。";
                return false;
            }

            if (!pawn.CanReserve(this))
            {
                reason = "尖塔当前正被其他殖民者占用。";
                return false;
            }

            return true;
        }

        private bool OrderManualOperation(Pawn pawn, GameComponent_SkyIslandResearch research, bool showMessage)
        {
            string reason = string.Empty;
            if (!TryGetCurrentManualJobDef(research, out JobDef? jobDef) || !CanStartManualOperation(pawn, research, out reason))
            {
                if (showMessage && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, this, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            Job job = JobMaker.MakeJob(jobDef!, this);
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            if (showMessage)
            {
                Messages.Message(pawn.LabelShortCap + " 正在前往" + research.GetCurrentOperationLabel() + "。", this, MessageTypeDefOf.TaskCompletion, false);
            }

            return true;
        }

        private Pawn? GetPreferredPawnForManualOperation(GameComponent_SkyIslandResearch research)
        {
            Thing? singleSelectedThing = Find.Selector.SingleSelectedThing;
            if (singleSelectedThing is Pawn selectedPawn &&
                selectedPawn.IsColonistPlayerControlled &&
                selectedPawn.Map == Map &&
                CanStartManualOperation(selectedPawn, research, out _))
            {
                return selectedPawn;
            }

            Pawn? result = null;
            int bestDistance = int.MaxValue;
            List<Pawn> colonists = Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (!CanStartManualOperation(pawn, research, out _))
                {
                    continue;
                }

                int distance = (pawn.Position - Position).LengthHorizontalSquared;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    result = pawn;
                }
            }

            return result;
        }

        private bool TryGetCurrentManualJobDef(GameComponent_SkyIslandResearch research, out JobDef? jobDef)
        {
            jobDef = research.GetCurrentSpireOperation();
            return jobDef != null;
        }

        private string GetStateLabel()
        {
            return state switch
            {
                FloatingEnergySpireState.Investigating => "第一阶段运行",
                FloatingEnergySpireState.PartiallyRestored => "第二阶段运行",
                FloatingEnergySpireState.FullyRestarted => "完全重启",
                _ => "废弃"
            };
        }

        private static Texture2D GetOperationIcon(JobDef jobDef)
        {
            if (jobDef == SkyrimIslandsDefOf.SkyrimIslands_InvestigateSpire)
            {
                return TexCommand.OpenLinkedQuestTex;
            }

            if (jobDef == SkyrimIslandsDefOf.SkyrimIslands_RestartSpire)
            {
                return TexCommand.DesirePower;
            }

            return TexCommand.Install;
        }
    }
}
