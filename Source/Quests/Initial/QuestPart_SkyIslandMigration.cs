using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using SkyrimIslands.Quests.Initial.Shuttle;

namespace SkyrimIslands.Quests.Initial
{
    public class QuestPart_SkyIslandMigration : QuestPartActivable
    {
        private const string MissionShuttleQuestTag = "SkyrimIslandsMissionShuttle";
        private const int LandingAnimationTicks = 220;
        private const int DelayAfterLandingTicks = 90;
        private const int ShuttleRotationAsInt = Rot4.NorthInt;

        private Thing shuttle = null!;
        private Thing incomingShuttle = null!;
        private bool landingNoticeSent;
        private int openDialogTick = -1;

        public Map sourceMap = null!;
        public IntVec3 landingCell = IntVec3.Invalid;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            if (sourceMap == null)
            {
                quest.End(QuestEndOutcome.Fail);
                return;
            }

            shuttle = ThingMaker.MakeThing(SkyrimIslandsDefOf.SkyrimIslands_MissionShuttle);
            shuttle.SetFaction(null);
            shuttle.questTags ??= new List<string>();
            shuttle.questTags.Add(MissionShuttleQuestTag);
            CompShuttle? compShuttle = shuttle.TryGetComp<CompShuttle>();
            if (compShuttle != null)
            {
                compShuttle.acceptColonists = true;
                compShuttle.acceptChildren = true;
                compShuttle.onlyAcceptColonists = false;
                compShuttle.onlyAcceptHealthy = false;
                compShuttle.allowSlaves = false;
                compShuttle.acceptColonyPrisoners = false;
                compShuttle.permitShuttle = true;
                compShuttle.requiredColonistCount = 1;
            }

            ThingDef incomingDef = DefDatabase<ThingDef>.GetNamed("ShuttleIncoming");
            if (!landingCell.IsValid)
            {
                landingCell = SkyIslandMigrationUtility.FindCenteredShuttleLandingSpot(sourceMap, Faction.OfPlayer);
            }

            shuttle.Rotation = new Rot4(ShuttleRotationAsInt);
            incomingShuttle = SkyfallerMaker.MakeSkyfaller(incomingDef, shuttle);
            incomingShuttle.Rotation = new Rot4(ShuttleRotationAsInt);
            GenPlace.TryPlaceThing(incomingShuttle, landingCell, sourceMap, ThingPlaceMode.Near);

            openDialogTick = Find.TickManager.TicksGame + LandingAnimationTicks + DelayAfterLandingTicks;
            CompSkyIslandMissionShuttleControl? shuttleControl = shuttle.TryGetComp<CompSkyIslandMissionShuttleControl>();
            shuttleControl?.InitializeForMission(openDialogTick);
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();

            if (!landingNoticeSent && openDialogTick >= 0 && Find.TickManager.TicksGame >= openDialogTick)
            {
                landingNoticeSent = true;
                quest.End(QuestEndOutcome.Success, sendLetter: false, playSound: false);
                Find.LetterStack.ReceiveLetter(
                    "穿梭机已降落",
                    "未知穿梭机已经完成降落。任务到此结束。现在可以使用它自带的装载功能装入殖民者与物资，随后自行启动发射程序前往空岛。",
                    LetterDefOf.PositiveEvent,
                    new LookTargets(landingCell, sourceMap),
                    quest: quest);
            }

            bool shuttleGone = shuttle == null || shuttle.Destroyed;
            bool incomingGone = incomingShuttle == null || incomingShuttle.Destroyed;
            if (!landingNoticeSent && shuttleGone && incomingGone)
            {
                quest.End(QuestEndOutcome.Fail);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref sourceMap, "sourceMap");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_References.Look(ref incomingShuttle, "incomingShuttle");
            Scribe_Values.Look(ref landingNoticeSent, "landingNoticeSent", false);
            Scribe_Values.Look(ref openDialogTick, "openDialogTick", -1);
            Scribe_Values.Look(ref landingCell, "landingCell");
        }
    }
}
