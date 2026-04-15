using RimWorld;
using SkyrimIslands.Buildings.FloatingEnergySpire;
using SkyrimIslands.Research.Categories;
using SkyrimIslands.Research.Categories.Spire;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimIslands.Research
{
    public class GameComponent_SkyIslandResearch : GameComponent
    {
        private List<SkyIslandResearchSlot> currentSkyProjects = new List<SkyIslandResearchSlot>();
        private SkyResearchCategoryRegistry categoryRegistry = new SkyResearchCategoryRegistry();

        private SpireResearchCategory? SpireCategory => categoryRegistry?.Get(SkyrimIslandsDefOf.SkyrimIslandsData_SpireResearch) as SpireResearchCategory;

        public GameComponent_SkyIslandResearch(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            int oldInitialSpireQuestCreateTick = -1;
            Quest? oldActiveSpireQuest = null;
            int oldActiveSpireQuestStage = -1;
            int oldRevealedSpireResearchStage = 0;
            bool oldBootstrapInvestigationFinished = false;
            bool oldSpireSequenceFinished = false;
            int oldLastInteractionUnlockNoticeStage = -1;

            Scribe_Values.Look(ref oldInitialSpireQuestCreateTick, "initialSpireQuestCreateTick", -1);
            Scribe_References.Look(ref oldActiveSpireQuest, "activeSpireQuest");
            Scribe_Values.Look(ref oldActiveSpireQuestStage, "activeSpireQuestStage", -1);
            Scribe_Values.Look(ref oldRevealedSpireResearchStage, "revealedSpireResearchStage", 0);
            Scribe_Values.Look(ref oldBootstrapInvestigationFinished, "bootstrapInvestigationFinished", false);
            Scribe_Values.Look(ref oldSpireSequenceFinished, "spireSequenceFinished", false);
            Scribe_Values.Look(ref oldLastInteractionUnlockNoticeStage, "lastInteractionUnlockNoticeStage", -1);

            Scribe_Collections.Look(ref currentSkyProjects, "currentSkyProjects", LookMode.Deep);
            Scribe_Deep.Look(ref categoryRegistry, "categoryRegistry");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureSkyProjectSlotsInitialized();
                categoryRegistry ??= new SkyResearchCategoryRegistry();
                SpireCategory?.MigrateFromLegacy(
                    oldInitialSpireQuestCreateTick,
                    oldActiveSpireQuest,
                    oldActiveSpireQuestStage,
                    oldRevealedSpireResearchStage,
                    oldBootstrapInvestigationFinished,
                    oldSpireSequenceFinished,
                    oldLastInteractionUnlockNoticeStage);
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (categoryRegistry != null)
            {
                foreach (ISkyResearchCategory category in categoryRegistry.All)
                {
                    category.CategoryTick();
                }
            }
        }

        public void ScheduleInitialSpireQuest(Map? targetMap)
        {
            SpireCategory?.ScheduleInitialSpireQuest(targetMap);
        }

        public bool IsCurrentSpireQuestAccepted()
        {
            return SpireCategory?.IsCurrentSpireQuestAccepted() ?? false;
        }

        public IReadOnlyList<SkyIslandResearchSlot> CurrentSkyProjects
        {
            get
            {
                EnsureSkyProjectSlotsInitialized();
                return currentSkyProjects;
            }
        }

        public bool IsProjectVisible(SkyIslandResearchProjectDef project)
        {
            ISkyResearchCategory? category = categoryRegistry?.Get(project.skyIslandDataType);
            if (category != null)
            {
                return category.IsProjectVisible(project);
            }

            return true;
        }

        public bool TryGetCurrentSpireResearchProject(out SkyIslandResearchProjectDef? project)
        {
            if (SpireCategory != null)
            {
                return SpireCategory.TryGetCurrentSpireResearchProject(out project);
            }

            project = null;
            return false;
        }

        public bool ShouldUseSpireResearchWork()
        {
            return SpireCategory?.ShouldUseSpireResearchWork() ?? false;
        }

        public bool TryGetCurrentSkyResearchProject(SkyIslandDataTypeDef dataType, out SkyIslandResearchProjectDef? project)
        {
            project = GetCurrentSkyProject(dataType);
            if (project == null || project.skyIslandDataType != dataType)
            {
                project = null;
                return false;
            }

            if (!IsProjectVisible(project!) || project.IsFinished)
            {
                project = null;
                return false;
            }

            return true;
        }

        public bool ShouldUseSkyResearchWork(SkyIslandDataTypeDef dataType)
        {
            return TryGetCurrentSkyResearchProject(dataType, out _);
        }

        public bool HasAnySkyResearchSource(ThingDef sourceDef)
        {
            if (sourceDef == null)
            {
                return false;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].listerBuildings.AllBuildingsColonistOfDef(sourceDef).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAvailableSkyResearchWork(Pawn pawn)
        {
            if (pawn.MapHeld == null || categoryRegistry == null)
            {
                return false;
            }

            foreach (ISkyResearchCategory category in categoryRegistry.All)
            {
                if (category.TryGetCurrentProject(out SkyIslandResearchProjectDef project) &&
                    category.HasAvailableWork(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetCurrentSkyProject(SkyIslandResearchProjectDef project)
        {
            EnsureSkyProjectSlotsInitialized();
            for (int i = 0; i < currentSkyProjects.Count; i++)
            {
                if (currentSkyProjects[i].dataType == project.skyIslandDataType)
                {
                    currentSkyProjects[i].project = project;
                    categoryRegistry?.Get(project.skyIslandDataType)?.NotifyProjectSet(project);
                    return;
                }
            }

            currentSkyProjects.Add(new SkyIslandResearchSlot
            {
                dataType = project.skyIslandDataType,
                project = project
            });
            categoryRegistry?.Get(project.skyIslandDataType)?.NotifyProjectSet(project);
        }

        public void StopCurrentSkyProject(ResearchProjectDef project)
        {
            EnsureSkyProjectSlotsInitialized();
            for (int i = 0; i < currentSkyProjects.Count; i++)
            {
                if (currentSkyProjects[i].project == project)
                {
                    currentSkyProjects[i].project = null;
                    if (project is SkyIslandResearchProjectDef skyProject)
                    {
                        categoryRegistry?.Get(skyProject.skyIslandDataType)?.NotifyProjectStopped(skyProject);
                    }
                }
            }
        }

        public bool IsCurrentSkyProject(ResearchProjectDef project)
        {
            EnsureSkyProjectSlotsInitialized();
            for (int i = 0; i < currentSkyProjects.Count; i++)
            {
                if (currentSkyProjects[i].project == project)
                {
                    return true;
                }
            }

            return false;
        }

        public SkyIslandResearchProjectDef? GetCurrentSkyProject(SkyIslandDataTypeDef dataType)
        {
            EnsureSkyProjectSlotsInitialized();
            for (int i = 0; i < currentSkyProjects.Count; i++)
            {
                if (currentSkyProjects[i].dataType == dataType)
                {
                    return currentSkyProjects[i].project as SkyIslandResearchProjectDef;
                }
            }

            return null;
        }

        public ResearchProjectDef? GetFirstCurrentSkyProject()
        {
            EnsureSkyProjectSlotsInitialized();
            for (int i = 0; i < currentSkyProjects.Count; i++)
            {
                if (currentSkyProjects[i].project != null)
                {
                    return currentSkyProjects[i].project;
                }
            }

            return null;
        }

        public void NotifySkyProjectFinished(ResearchProjectDef project)
        {
            if (project is SkyIslandResearchProjectDef skyProject)
            {
                categoryRegistry?.Get(skyProject.skyIslandDataType)?.NotifyProjectFinished(skyProject);
            }
            StopCurrentSkyProject(project);
        }

        public SpireManualOperationKind GetCurrentManualOperation()
        {
            return SpireCategory?.GetCurrentManualOperationKind() ?? SpireManualOperationKind.None;
        }

        public JobDef? GetCurrentSpireOperation()
        {
            return SpireCategory?.GetCurrentSpireOperation();
        }

        public int GetCurrentOperationDurationTicks()
        {
            return SpireCategory?.GetCurrentOperationDurationTicks() ?? 0;
        }

        public string GetCurrentOperationLabel()
        {
            return SpireCategory?.GetCurrentOperationLabel() ?? string.Empty;
        }

        public string GetCurrentOperationDescription()
        {
            return SpireCategory?.GetCurrentOperationDescription() ?? string.Empty;
        }

        public void NotifySpireManualOperationCompleted(Building_FloatingEnergySpire spire, Pawn pawn)
        {
            SpireCategory?.NotifySpireManualOperationCompleted(spire, pawn);
        }

        public void AddSpireResearchProgress(Building_FloatingEnergySpire spire, Pawn pawn, SkyIslandResearchProjectDef project, float amount)
        {
            SpireCategory?.AddProgress(spire, pawn, project, amount);
        }

        public void AddSkyResearchProgress(Thing source, Pawn pawn, SkyIslandResearchProjectDef project, float amount)
        {
            categoryRegistry?.Get(project.skyIslandDataType)?.AddProgress(source, pawn, project, amount);
        }

        public ISkyResearchCategory? GetCategory(SkyIslandDataTypeDef dataType)
        {
            return categoryRegistry?.Get(dataType);
        }

        private void EnsureSkyProjectSlotsInitialized()
        {
            if (currentSkyProjects == null)
            {
                currentSkyProjects = new List<SkyIslandResearchSlot>();
            }

            currentSkyProjects.RemoveAll(static slot => slot == null || slot.dataType == null);

            List<SkyIslandDataTypeDef> allDataTypes = DefDatabase<SkyIslandDataTypeDef>.AllDefsListForReading
                .OrderBy(static def => def.label)
                .ToList();
            for (int i = 0; i < allDataTypes.Count; i++)
            {
                SkyIslandDataTypeDef dataType = allDataTypes[i];
                if (!currentSkyProjects.Any(slot => slot.dataType == dataType))
                {
                    currentSkyProjects.Add(new SkyIslandResearchSlot
                    {
                        dataType = dataType
                    });
                }
            }
        }

        public class SkyIslandResearchSlot : IExposable
        {
            public SkyIslandDataTypeDef dataType = null!;
            public ResearchProjectDef? project;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref dataType, "dataType");
                Scribe_Defs.Look(ref project, "project");
            }
        }
    }
}
