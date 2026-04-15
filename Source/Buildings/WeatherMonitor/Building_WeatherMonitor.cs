using System.Collections.Generic;
using RimWorld;
using SkyrimIslands.Research;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SkyrimIslands.Buildings.WeatherMonitor
{
    public class Building_WeatherMonitor : Building, ISkyResearchSource
    {
        private const int DataCapacityTicks = 60000;
        private const int DataGatherIntervalTicks = 250;
        private const int InvestigationReward = 4;

        private int storedDataTicks;
        private CompPowerTrader? cachedPowerTraderComp;

        public float StoredDataPercent => Mathf.Clamp01(storedDataTicks / (float)DataCapacityTicks);

        public bool ReadyForInvestigation => storedDataTicks >= DataCapacityTicks;

        private CompPowerTrader? PowerTraderComp => cachedPowerTraderComp ??= GetComp<CompPowerTrader>();

        private bool PoweredOn => PowerTraderComp == null || PowerTraderComp.PowerOn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storedDataTicks, "storedDataTicks", 0);
        }

        protected override void Tick()
        {
            base.Tick();

            if (!CanAccumulateDataNow() || !this.IsHashIntervalTick(DataGatherIntervalTicks))
            {
                return;
            }

            storedDataTicks = Mathf.Min(DataCapacityTicks, storedDataTicks + DataGatherIntervalTicks);
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            string dataText = ReadyForInvestigation
                ? "云海数据：已积累完成，可执行调查。"
                : "云海数据积累：" + StoredDataPercent.ToStringPercent() + "（约 " + Mathf.Max(0, DataCapacityTicks - storedDataTicks).ToStringTicksToPeriod() + "）";
            string projectText = "当前没有选中的云海研究项目。";

            if (research != null && research.TryGetCurrentSkyResearchProject(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch, out SkyIslandResearchProjectDef? project))
            {
                projectText = "当前云海研究项目：" + project!.LabelCap;
            }

            string powerText = PoweredOn ? "供电状态：正常" : "供电状态：离线";
            return dataText + "\n" + projectText + "\n" + powerText +
                   (inspectString.NullOrEmpty() ? string.Empty : "\n" + inspectString);
        }

        public bool CanPerformResearch(Pawn pawn, SkyIslandResearchProjectDef project, out string reason)
        {
            reason = string.Empty;

            if (project.skyIslandDataType != SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch)
            {
                reason = "当前项目不需要云海研究数据。";
                return false;
            }

            if (!ReadyForInvestigation)
            {
                reason = "气象监测仪尚未积累到足够的数据。";
                return false;
            }

            if (!PoweredOn)
            {
                reason = "气象监测仪当前没有供电。";
                return false;
            }

            if (pawn.Downed || pawn.InMentalState)
            {
                reason = "该殖民者目前无法执行调查工作。";
                return false;
            }

            if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                reason = "无法抵达气象监测仪的交互位置。";
                return false;
            }

            if (!pawn.CanReserve(this))
            {
                reason = "气象监测仪当前正被其他殖民者占用。";
                return false;
            }

            return true;
        }

        public void CompleteInvestigation(Pawn pawn)
        {
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null ||
                !research.TryGetCurrentSkyResearchProject(SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch, out SkyIslandResearchProjectDef? project))
            {
                return;
            }

            research.AddSkyResearchProgress(this, pawn, project!, InvestigationReward);
            storedDataTicks = 0;
        }

        public int GetRemainingDataTicks()
        {
            return Mathf.Max(0, DataCapacityTicks - storedDataTicks);
        }

        private bool CanAccumulateDataNow()
        {
            return Spawned && !Destroyed && PoweredOn && !ReadyForInvestigation;
        }
    }
}
