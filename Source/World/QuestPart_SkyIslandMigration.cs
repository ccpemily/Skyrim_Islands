using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class QuestPart_SkyIslandMigration : QuestPartActivable
    {
        private const int LandingAnimationTicks = 220;
        private const int DelayAfterLandingTicks = 90;
        private const int ShuttleRotationAsInt = Rot4.EastInt;

        private Thing shuttle = null!;
        private bool selectionOpened;
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

            shuttle = ThingMaker.MakeThing(ThingDefOf.PassengerShuttle);
            ThingDef incomingDef = DefDatabase<ThingDef>.GetNamed("PassengerShuttleIncoming");
            if (!landingCell.IsValid)
            {
                landingCell = DropCellFinder.GetBestShuttleLandingSpot(sourceMap, Faction.OfPlayer);
            }

            shuttle.Rotation = new Rot4(ShuttleRotationAsInt);
            Thing incoming = SkyfallerMaker.MakeSkyfaller(incomingDef, shuttle);
            incoming.Rotation = new Rot4(ShuttleRotationAsInt);
            GenPlace.TryPlaceThing(incoming, landingCell, sourceMap, ThingPlaceMode.Near);

            openDialogTick = Find.TickManager.TicksGame + LandingAnimationTicks + DelayAfterLandingTicks;
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();

            if (!landingNoticeSent && openDialogTick >= 0 && Find.TickManager.TicksGame >= openDialogTick)
            {
                landingNoticeSent = true;
                Find.LetterStack.ReceiveLetter(
                    "穿梭机已降落",
                    "未知穿梭机已经完成降落。你现在可以装载人员与物资，准备启程前往空岛。",
                    LetterDefOf.PositiveEvent,
                    new LookTargets(landingCell, sourceMap),
                    quest: quest);
            }

            if (!selectionOpened && openDialogTick >= 0 && Find.TickManager.TicksGame >= openDialogTick)
            {
                selectionOpened = true;
                Find.WindowStack.Add(new Dialog_SkyIslandMigrationLoad(
                    sourceMap,
                    SkyIslandMigrationUtility.PrototypeMassCapacity,
                    OnSelectionAccepted,
                    OnSelectionCancelled));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref sourceMap, "sourceMap");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref selectionOpened, "selectionOpened", false);
            Scribe_Values.Look(ref landingNoticeSent, "landingNoticeSent", false);
            Scribe_Values.Look(ref openDialogTick, "openDialogTick", -1);
            Scribe_Values.Look(ref landingCell, "landingCell");
        }

        private void OnSelectionCancelled()
        {
            quest.End(QuestEndOutcome.Unknown);
        }

        private void OnSelectionAccepted(List<SkyIslandLoadSelection> selections)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "是否在启程后遗弃当前殖民地？\n\n选择“是”会在空岛殖民地建立后放弃当前地表殖民地；选择“否”则保留原殖民地。",
                "是",
                delegate
                {
                    SkyIslandMigrationUtility.BeginMigration(sourceMap, shuttle, selections, true, quest, landingCell);
                },
                "否",
                delegate
                {
                    SkyIslandMigrationUtility.BeginMigration(sourceMap, shuttle, selections, false, quest, landingCell);
                },
                title: "遗弃原殖民地"));
        }
    }
}
