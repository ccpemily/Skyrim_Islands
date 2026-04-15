using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Research.Categories.CloudSea
{
    public class CloudSeaResearchCategory : ISkyResearchCategory
    {
        public SkyIslandDataTypeDef DataType => SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch;

        public void ExposeData()
        {
        }

        public bool IsProjectVisible(SkyIslandResearchProjectDef project)
        {
            return true;
        }

        public bool TryGetCurrentProject(out SkyIslandResearchProjectDef project)
        {
            GameComponent_SkyIslandResearch? research = Current.Game?.GetComponent<GameComponent_SkyIslandResearch>();
            if (research != null)
            {
                SkyIslandResearchProjectDef? p = research.GetCurrentSkyProject(DataType);
                if (p != null && p.skyIslandDataType == DataType && !p.IsFinished)
                {
                    project = p;
                    return true;
                }
            }

            project = null!;
            return false;
        }

        public bool HasAvailableWork(Pawn pawn)
        {
            if (!TryGetCurrentProject(out SkyIslandResearchProjectDef project) || pawn.MapHeld == null)
            {
                return false;
            }

            foreach (Thing thing in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is ISkyResearchSource source && source.CanPerformResearch(pawn, project, out _))
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
            if (project.skyIslandDataType != DataType)
            {
                return;
            }

            if (!HasAnyWeatherMonitor())
            {
                Messages.Message(
                    "当前研究需要云海研究数据，但您尚未建造任何气象监测仪。请先建造一台监测仪来积累并调查云海数据。",
                    MessageTypeDefOf.CautionInput,
                    false);
            }
        }

        public void NotifyProjectStopped(SkyIslandResearchProjectDef project)
        {
        }

        public void NotifyProjectFinished(SkyIslandResearchProjectDef project)
        {
        }

        public void CategoryTick()
        {
        }

        private static bool HasAnyWeatherMonitor()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].listerBuildings.AllBuildingsColonistOfDef(SkyrimIslandsDefOf.SkyrimIslands_WeatherMonitor).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
