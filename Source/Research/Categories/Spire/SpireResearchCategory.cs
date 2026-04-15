using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Categories.Spire.Operations;
using SkyrimIslands.Research.Operations;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.Spire
{
    public class SpireResearchCategory : ISkyResearchCategory
    {
        private const int InitialSpireQuestDelayTicks = 1500;
        private const string SpireQuestTagPrefix = "SkyrimIslandsSpireQuestStage";

        private int initialSpireQuestCreateTick = -1;
        private Quest? activeSpireQuest;
        private int activeSpireQuestStage = -1;
        private int revealedSpireResearchStage;
        private bool bootstrapInvestigationFinished;
        private bool spireSequenceFinished;
        private int lastInteractionUnlockNoticeStage = -1;

        private readonly List<ISpireManualOperation> operations = new List<ISpireManualOperation>
        {
            new BootstrapInvestigationOp(),
            new AdvanceToStage1Op(),
            new AdvanceToStage2Op(),
            new FinalRestartOp()
        };

        public SkyIslandDataTypeDef DataType => SkyrimIslandsDefOf.SkyrimIslandsData_SpireResearch;

        public void ExposeData()
        {
            Scribe_Values.Look(ref initialSpireQuestCreateTick, "initialSpireQuestCreateTick", -1);
            Scribe_References.Look(ref activeSpireQuest, "activeSpireQuest");
            Scribe_Values.Look(ref activeSpireQuestStage, "activeSpireQuestStage", -1);
            Scribe_Values.Look(ref revealedSpireResearchStage, "revealedSpireResearchStage", 0);
            Scribe_Values.Look(ref bootstrapInvestigationFinished, "bootstrapInvestigationFinished", false);
            Scribe_Values.Look(ref spireSequenceFinished, "spireSequenceFinished", false);
            Scribe_Values.Look(ref lastInteractionUnlockNoticeStage, "lastInteractionUnlockNoticeStage", -1);
        }

        public void MigrateFromLegacy(
            int oldInitialSpireQuestCreateTick,
            Quest oldActiveSpireQuest,
            int oldActiveSpireQuestStage,
            int oldRevealedSpireResearchStage,
            bool oldBootstrapInvestigationFinished,
            bool oldSpireSequenceFinished,
            int oldLastInteractionUnlockNoticeStage)
        {
            if (initialSpireQuestCreateTick < 0 && oldInitialSpireQuestCreateTick >= 0)
            {
                initialSpireQuestCreateTick = oldInitialSpireQuestCreateTick;
            }
            if (activeSpireQuest == null && oldActiveSpireQuest != null)
            {
                activeSpireQuest = oldActiveSpireQuest;
            }
            if (activeSpireQuestStage < 0 && oldActiveSpireQuestStage >= 0)
            {
                activeSpireQuestStage = oldActiveSpireQuestStage;
            }
            if (revealedSpireResearchStage == 0 && oldRevealedSpireResearchStage > 0)
            {
                revealedSpireResearchStage = oldRevealedSpireResearchStage;
            }
            if (!bootstrapInvestigationFinished && oldBootstrapInvestigationFinished)
            {
                bootstrapInvestigationFinished = oldBootstrapInvestigationFinished;
            }
            if (!spireSequenceFinished && oldSpireSequenceFinished)
            {
                spireSequenceFinished = oldSpireSequenceFinished;
            }
            if (lastInteractionUnlockNoticeStage < 0 && oldLastInteractionUnlockNoticeStage >= 0)
            {
                lastInteractionUnlockNoticeStage = oldLastInteractionUnlockNoticeStage;
            }
        }

        public bool IsProjectVisible(SkyIslandResearchProjectDef project)
        {
            if (project == SkyrimIslandsDefOf.SkyrimIslands_SpireTech_0)
            {
                return bootstrapInvestigationFinished || project.IsFinished;
            }

            return project.stage <= revealedSpireResearchStage || project.IsFinished;
        }

        public bool TryGetCurrentProject(out SkyIslandResearchProjectDef project)
        {
            if (TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? p))
            {
                project = p!;
                return true;
            }

            project = null!;
            return false;
        }

        public bool HasAvailableWork(Pawn pawn)
        {
            if (!TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project) || pawn.MapHeld == null)
            {
                return false;
            }

            foreach (Thing thing in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is ISkyResearchSource source && source.CanPerformResearch(pawn, project!, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddProgress(Thing source, Pawn pawn, SkyIslandResearchProjectDef project, float amount)
        {
            if (amount <= 0f || project.IsFinished)
            {
                return;
            }

            Find.ResearchManager.AddProgress(project, amount, pawn);
            if (source.MapHeld != null)
            {
                MoteMaker.ThrowText(
                    source.DrawPos,
                    source.MapHeld,
                    project.skyIslandDataType.shortLabel + " +" + amount.ToString("0.00"),
                    3f);
            }
        }

        public void NotifyProjectSet(SkyIslandResearchProjectDef project)
        {
        }

        public void NotifyProjectStopped(SkyIslandResearchProjectDef project)
        {
        }

        public void NotifyProjectFinished(SkyIslandResearchProjectDef project)
        {
        }

        public void CategoryTick()
        {
            TryCreateInitialSpireQuest();
            TryNotifyInteractionUnlocked();
        }

        public void ScheduleInitialSpireQuest(Map? targetMap)
        {
            if (targetMap == null || spireSequenceFinished || activeSpireQuest != null || initialSpireQuestCreateTick >= 0)
            {
                return;
            }

            if (SkyIslandSpireUtility.FindFloatingEnergySpire(targetMap) == null)
            {
                return;
            }

            initialSpireQuestCreateTick = Find.TickManager.TicksGame + InitialSpireQuestDelayTicks;
        }

        public bool IsCurrentSpireQuestAccepted()
        {
            return activeSpireQuest != null && activeSpireQuest.EverAccepted && activeSpireQuest.State == QuestState.Ongoing;
        }

        public bool TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project)
        {
            project = GetCurrentSkyProject(DataType);
            if (project == null || project.skyIslandDataType != DataType)
            {
                project = null;
                return false;
            }

            if (!IsProjectVisible(project) || project.IsFinished || activeSpireQuestStage != project.stage || activeSpireQuestStage <= 0)
            {
                project = null;
                return false;
            }

            return true;
        }

        public bool ShouldUseSpireResearchWork()
        {
            return TryGetCurrentSpireResearchProject(out _);
        }

        public SpireManualOperationKind GetCurrentManualOperationKind()
        {
            if (!IsCurrentSpireQuestAccepted())
            {
                return SpireManualOperationKind.None;
            }

            if (activeSpireQuestStage == 0 && !SkyrimIslandsDefOf.SkyrimIslands_SpireTech_0.IsFinished)
            {
                return SpireManualOperationKind.BootstrapInvestigation;
            }

            if (activeSpireQuestStage == 1 && SkyrimIslandsDefOf.SkyrimIslands_SpireTech_1.IsFinished)
            {
                return SpireManualOperationKind.AdvanceToStage1;
            }

            if (activeSpireQuestStage == 2 && SkyrimIslandsDefOf.SkyrimIslands_SpireTech_2.IsFinished)
            {
                return SpireManualOperationKind.AdvanceToStage2;
            }

            if (activeSpireQuestStage == 3 && SkyrimIslandsDefOf.SkyrimIslands_SpireTech_3.IsFinished)
            {
                return SpireManualOperationKind.FinalRestart;
            }

            return SpireManualOperationKind.None;
        }

        public ISpireManualOperation? GetCurrentManualOperation()
        {
            SpireManualOperationKind kind = GetCurrentManualOperationKind();
            if (kind == SpireManualOperationKind.None)
            {
                return null;
            }

            return operations.FirstOrDefault(op => op.Kind == kind);
        }

        public JobDef? GetCurrentSpireOperation()
        {
            return GetCurrentManualOperation()?.JobDef;
        }

        public int GetCurrentOperationDurationTicks()
        {
            return GetCurrentManualOperation()?.DurationTicks ?? 0;
        }

        public string GetCurrentOperationLabel()
        {
            return GetCurrentManualOperation()?.Label ?? string.Empty;
        }

        public string GetCurrentOperationDescription()
        {
            return GetCurrentManualOperation()?.Description ?? string.Empty;
        }

        public void NotifySpireManualOperationCompleted(Building_FloatingEnergySpire spire, Pawn pawn)
        {
            ISpireManualOperation? op = GetCurrentManualOperation();
            if (op != null)
            {
                op.OnCompleted(spire, pawn, this);
            }
        }

        public void CompleteBootstrapInvestigation(Building_FloatingEnergySpire spire, Pawn pawn)
        {
            if (bootstrapInvestigationFinished)
            {
                return;
            }

            bootstrapInvestigationFinished = true;
            lastInteractionUnlockNoticeStage = -1;
            UnlockTech(SkyrimIslandsDefOf.SkyrimIslands_SpireTech_0, false);
            revealedSpireResearchStage = 1;
            CompleteActiveSpireQuest();

            Find.LetterStack.ReceiveLetter(
                "尖塔0级科技建立",
                pawn.LabelShortCap + " 完成了对浮空能量尖塔的初步调查。空岛科技页中已经揭示 `尖塔1阶段科技`，研究人员现在可以在尖塔上持续开展研究。",
                LetterDefOf.PositiveEvent,
                spire);

            CreateStageQuest(1, spire, autoAccept: true);
        }

        public void CompleteStageAdvance(int completedStage, Building_FloatingEnergySpire spire, Pawn pawn, FloatingEnergySpireState newState)
        {
            spire.ApplyState(newState);
            CompleteActiveSpireQuest();
            revealedSpireResearchStage = completedStage + 1;
            lastInteractionUnlockNoticeStage = -1;

            Find.LetterStack.ReceiveLetter(
                "尖塔进入新阶段",
                pawn.LabelShortCap + " 已推动浮空能量尖塔进入新的运行阶段。下一阶段科技已经在空岛科技页中揭示，并且新的主线任务已自动接取。",
                LetterDefOf.PositiveEvent,
                spire);

            CreateStageQuest(completedStage + 1, spire, autoAccept: true);
        }

        public void CompleteFinalRestart(Building_FloatingEnergySpire spire, Pawn pawn)
        {
            spire.ApplyState(FloatingEnergySpireState.FullyRestarted);
            CompleteActiveSpireQuest();
            spireSequenceFinished = true;
            lastInteractionUnlockNoticeStage = -1;

            Find.LetterStack.ReceiveLetter(
                "尖塔完全重启",
                pawn.LabelShortCap + " 完成了浮空能量尖塔的最终重启，空岛主控重新上线。当前示例骨架的尖塔主线已全部跑通。",
                LetterDefOf.PositiveEvent,
                spire);
        }

        private void TryCreateInitialSpireQuest()
        {
            if (spireSequenceFinished || initialSpireQuestCreateTick < 0 || Find.TickManager.TicksGame < initialSpireQuestCreateTick)
            {
                return;
            }

            Building_FloatingEnergySpire? spire = SkyIslandSpireUtility.FindFloatingEnergySpire(SkyIslandSpireUtility.GetStartingSkyIslandMap());
            if (spire == null)
            {
                return;
            }

            CreateStageQuest(0, spire, autoAccept: false);
            initialSpireQuestCreateTick = -1;
        }

        private void TryNotifyInteractionUnlocked()
        {
            if (!IsCurrentSpireQuestAccepted() || activeSpireQuestStage <= 0 || activeSpireQuestStage == lastInteractionUnlockNoticeStage)
            {
                return;
            }

            ResearchProjectDef? project = GetProjectForStage(activeSpireQuestStage);
            if (project == null || !project.IsFinished)
            {
                return;
            }

            Building_FloatingEnergySpire? spire = SkyIslandSpireUtility.FindFloatingEnergySpire(SkyIslandSpireUtility.GetStartingSkyIslandMap());
            if (spire == null)
            {
                return;
            }

            lastInteractionUnlockNoticeStage = activeSpireQuestStage;
            Find.LetterStack.ReceiveLetter(
                "尖塔阶段可推进",
                "当前阶段的尖塔科技已经完成。现在可以前往浮空能量尖塔，执行新的阶段推进操作。",
                LetterDefOf.NeutralEvent,
                spire,
                quest: activeSpireQuest);
        }

        private void CreateStageQuest(int stage, Building_FloatingEnergySpire spire, bool autoAccept)
        {
            Quest quest = Quest.MakeRaw();
            quest.root = SkyrimIslandsDefOf.SkyrimIslands_SpireRecoveryQuest;
            quest.acceptanceExpireTick = -1;
            quest.name = GetQuestName(stage);
            quest.description = GetQuestDescription(stage);
            quest.tags.Add(SpireQuestTagPrefix + stage);
            if (autoAccept)
            {
                quest.SetInitiallyAccepted();
            }

            spire.questTags ??= new List<string>();
            string stageTag = SpireQuestTagPrefix + stage;
            if (!spire.questTags.Contains(stageTag))
            {
                spire.questTags.Add(stageTag);
            }

            Find.QuestManager.Add(quest);
            Find.LetterStack.ReceiveLetter(
                GetQuestLetterLabel(stage),
                GetQuestLetterText(stage),
                LetterDefOf.NeutralEvent,
                spire,
                quest: quest);

            activeSpireQuest = quest;
            activeSpireQuestStage = stage;
        }

        private void CompleteActiveSpireQuest()
        {
            if (activeSpireQuest != null &&
                (activeSpireQuest.State == QuestState.Ongoing || activeSpireQuest.State == QuestState.NotYetAccepted))
            {
                activeSpireQuest.End(QuestEndOutcome.Success, sendLetter: false, playSound: false);
            }
        }

        private static SkyIslandResearchProjectDef? GetCurrentSkyProject(SkyIslandDataTypeDef dataType)
        {
            GameComponent_SkyIslandResearch? research = Current.Game?.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null)
            {
                return null;
            }

            return research.GetCurrentSkyProject(dataType);
        }

        private static void UnlockTech(ResearchProjectDef project, bool showCompletionDialog)
        {
            if (project.IsFinished)
            {
                return;
            }

            Find.ResearchManager.FinishProject(project, doCompletionDialog: showCompletionDialog, researcher: null, doCompletionLetter: true);
        }

        private static ResearchProjectDef? GetProjectForStage(int stage)
        {
            return stage switch
            {
                1 => SkyrimIslandsDefOf.SkyrimIslands_SpireTech_1,
                2 => SkyrimIslandsDefOf.SkyrimIslands_SpireTech_2,
                3 => SkyrimIslandsDefOf.SkyrimIslands_SpireTech_3,
                _ => null
            };
        }

        private static string GetQuestName(int stage)
        {
            return stage switch
            {
                1 => "尖塔第一阶段研究",
                2 => "尖塔第二阶段研究",
                3 => "尖塔最终重启前研究",
                _ => "尖塔初步调查"
            };
        }

        private static string GetQuestDescription(int stage)
        {
            return stage switch
            {
                1 => "已建立尖塔的基础研究链路。\n\n现在可以在空岛科技页中选择 `尖塔1阶段科技` 作为当前项目。分配研究工作的小人会在尖塔可调查时自动前往工作。科技完成后，回到尖塔执行阶段推进操作。",
                2 => "尖塔已进入第一阶段运行状态。\n\n现在可以研究 `尖塔2阶段科技`。科技完成后，回到尖塔执行下一次阶段推进操作。",
                3 => "尖塔已进入第二阶段运行状态。\n\n现在可以研究 `尖塔3阶段科技`。科技完成后，回到尖塔执行最终重启。",
                _ => "接受任务后，将开放对浮空能量尖塔的初步调查互动。完成这次调查后会解锁 `尖塔0阶段科技`，并揭示后续的尖塔科研路线。"
            };
        }

        private static string GetQuestLetterLabel(int stage)
        {
            return stage switch
            {
                1 => "尖塔第一阶段任务已接取",
                2 => "尖塔第二阶段任务已接取",
                3 => "尖塔最终阶段任务已接取",
                _ => "尖塔传来回响"
            };
        }

        private static string GetQuestLetterText(int stage)
        {
            return stage switch
            {
                1 => "新的尖塔任务已自动接取。现在可以在空岛科技页中选择 `尖塔1阶段科技`，并让研究人员前往尖塔开展工作。",
                2 => "新的尖塔任务已自动接取。现在可以研究 `尖塔2阶段科技`，完成后再回到尖塔推进阶段。",
                3 => "新的尖塔任务已自动接取。现在可以研究 `尖塔3阶段科技`，为最终重启做准备。",
                _ => "空岛中央的浮空能量尖塔在你们抵达后传出了断续回响。接受任务后即可开启第一步调查。"
            };
        }
    }
}
