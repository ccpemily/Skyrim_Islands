using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class Dialog_SkyIslandMigrationLoad : Window
    {
        private const float TitleHeight = 35f;
        private const float SummaryHeight = 72f;
        private const float BottomAreaHeight = 55f;
        private const float BottomAreaGap = 17f;

        private readonly Map sourceMap;
        private readonly float massCapacity;
        private readonly Action<List<SkyIslandLoadSelection>> accepted;
        private readonly Action canceled;
        private readonly Vector2 bottomButtonSize = new Vector2(160f, 40f);

        private List<TransferableOneWay> transferables = null!;
        private TransferableOneWayWidget transferWidget = null!;
        private float lastMassFlashTime = -9999f;
        private bool massUsageDirty = true;
        private float cachedMassUsage;

        public Dialog_SkyIslandMigrationLoad(Map sourceMap, float massCapacity, Action<List<SkyIslandLoadSelection>> accepted, Action canceled)
        {
            this.sourceMap = sourceMap;
            this.massCapacity = massCapacity;
            this.accepted = accepted;
            this.canceled = canceled;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnCancel = true;
            CalculateAndRecacheTransferables();
        }

        public override Vector2 InitialSize => new Vector2(1024f, Mathf.Min(UI.screenHeight, 820f));

        protected override float Margin => 0f;

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(
                        transferables,
                        IgnorePawnsInventoryMode.Ignore,
                        includePawnsMass: true,
                        ignoreSpawnedCorpsesGearAndInventory: false);
                }

                return cachedMassUsage;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(0f, 0f, inRect.width, TitleHeight);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "空岛启程装载清单");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect summaryRect = new Rect(12f, TitleHeight, inRect.width - 24f, SummaryHeight);
            Widgets.DrawMenuSection(summaryRect);
            Rect summaryInnerRect = summaryRect.ContractedBy(12f);
            Widgets.Label(
                new Rect(summaryInnerRect.x, summaryInnerRect.y, summaryInnerRect.width, 42f),
                $"选择要搭乘未知穿梭机前往空岛的殖民者、机械族、动物与物资。可合并的物品会合并显示，并可直接调整数量。总载重上限：{massCapacity.ToStringMass()}。");

            Rect massRect = new Rect(summaryInnerRect.x, summaryInnerRect.yMax - 24f, summaryInnerRect.width, 24f);
            bool massExceeded = MassUsage > massCapacity;
            GUI.color = massExceeded ? ColorLibrary.RedReadable : Color.white;
            Widgets.Label(massRect, $"当前重量：{MassUsage.ToStringMass()} / {massCapacity.ToStringMass()}");
            GUI.color = Color.white;

            Rect mainRect = new Rect(
                12f,
                summaryRect.yMax + 12f,
                inRect.width - 24f,
                inRect.height - summaryRect.yMax - 12f - BottomAreaHeight - BottomAreaGap);
            Widgets.DrawMenuSection(mainRect);

            Rect widgetRect = mainRect.ContractedBy(17f);
            widgetRect.height += 17f;
            bool anythingChanged;
            transferWidget.OnGUI(widgetRect, out anythingChanged);
            if (anythingChanged)
            {
                CountToTransferChanged();
            }

            DrawBottomButtons(inRect);
        }

        public override void OnCancelKeyPressed()
        {
            base.OnCancelKeyPressed();
            canceled();
        }

        private void DrawBottomButtons(Rect rect)
        {
            float y = rect.height - BottomAreaHeight - BottomAreaGap;

            if (Widgets.ButtonText(new Rect(0f, y, bottomButtonSize.x, bottomButtonSize.y), "CancelButton".Translate()))
            {
                canceled();
                Close();
            }

            if (Widgets.ButtonText(new Rect(rect.width / 2f - bottomButtonSize.x / 2f, y, bottomButtonSize.x, bottomButtonSize.y), "ResetButton".Translate()))
            {
                CalculateAndRecacheTransferables();
            }

            if (Widgets.ButtonText(new Rect(rect.width - bottomButtonSize.x, y, bottomButtonSize.x, bottomButtonSize.y), "AcceptButton".Translate()))
            {
                OnAcceptButton();
            }
        }

        private void OnAcceptButton()
        {
            if (MassUsage > massCapacity)
            {
                FlashMass();
                Messages.Message("所选内容超过穿梭机载重上限。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!HasSelectedColonist())
            {
                Messages.Message("至少需要选择一名殖民者。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<SkyIslandLoadSelection> selections = BuildSelections();
            if (selections.Count == 0)
            {
                Messages.Message("请至少选择一项需要转移的内容。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Close();
            accepted(selections);
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();

            foreach (Pawn colonist in sourceMap.mapPawns.FreeColonistsSpawned)
            {
                AddToTransferables(colonist);
            }

            foreach (Pawn mech in sourceMap.mapPawns.PawnsInFaction(Faction.OfPlayer).Where((Pawn pawn) => pawn.Spawned && pawn.IsColonyMech))
            {
                AddToTransferables(mech);
            }

            foreach (Pawn animal in sourceMap.mapPawns.SpawnedColonyAnimals)
            {
                AddToTransferables(animal);
            }

            foreach (Thing item in sourceMap.listerThings.AllThings.Where(IsBringablePlayerItem))
            {
                AddToTransferables(item);
            }

            transferWidget = new TransferableOneWayWidget(
                null,
                null,
                null,
                "空岛迁移装载数量",
                drawMass: true,
                ignorePawnInventoryMass: IgnorePawnsInventoryMode.Ignore,
                includePawnsMassInMassUsage: true,
                availableMassGetter: () => massCapacity - MassUsage,
                extraHeaderSpace: 0f,
                ignoreSpawnedCorpseGearAndInventoryMass: false,
                tile: sourceMap.Tile,
                drawMarketValue: true,
                drawEquippedWeapon: true,
                drawNutritionEatenPerDay: true,
                drawMechEnergy: true,
                drawItemNutrition: true,
                drawForagedFoodPerDay: false,
                drawDaysUntilRot: true,
                playerPawnsReadOnly: false,
                drawIdeo: ModsConfig.IdeologyActive,
                drawXenotype: ModsConfig.BiotechActive);

            transferWidget.AddSection("殖民者", transferables.Where((TransferableOneWay tr) => tr.AnyThing is Pawn pawn && pawn.IsFreeNonSlaveColonist));
            transferWidget.AddSection("机械族", transferables.Where((TransferableOneWay tr) => tr.AnyThing is Pawn pawn && pawn.IsColonyMech));
            transferWidget.AddSection("动物", transferables.Where((TransferableOneWay tr) => tr.AnyThing is Pawn pawn && pawn.IsAnimal));
            transferWidget.AddSection("物品", transferables.Where((TransferableOneWay tr) => tr.AnyThing.def.category == ThingCategory.Item));

            CountToTransferChanged();
        }

        private void AddToTransferables(Thing thing)
        {
            TransferableOneWay transferable = TransferableUtility.TransferableMatching<TransferableOneWay>(thing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferable == null)
            {
                transferable = new TransferableOneWay();
                transferables.Add(transferable);
            }

            if (!transferable.things.Contains(thing))
            {
                transferable.things.Add(thing);
            }
        }

        private bool HasSelectedColonist()
        {
            return transferables.Any((TransferableOneWay tr) => tr.CountToTransfer > 0 && tr.AnyThing is Pawn pawn && pawn.IsColonist);
        }

        private List<SkyIslandLoadSelection> BuildSelections()
        {
            List<SkyIslandLoadSelection> selections = new List<SkyIslandLoadSelection>();
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable.CountToTransfer <= 0)
                {
                    continue;
                }

                TransferableUtility.TransferNoSplit(
                    transferable.things,
                    transferable.CountToTransfer,
                    delegate(Thing originalThing, int toTake)
                    {
                        selections.Add(new SkyIslandLoadSelection(originalThing, toTake));
                    },
                    removeIfTakingEntireThing: false,
                    errorIfNotEnoughThings: false);
            }

            return selections;
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private bool IsBringablePlayerItem(Thing thing)
        {
            if (!thing.Spawned || thing.def.category != ThingCategory.Item || thing.Position.Fogged(sourceMap))
            {
                return false;
            }

            if (thing.def.thingCategories == null || thing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks) || thing.def.thingCategories.Contains(ThingCategoryDefOf.StoneChunks))
            {
                return false;
            }

            if (thing is Corpse || thing.def.destroyOnDrop || !thing.def.EverHaulable)
            {
                return false;
            }

            if (thing.Faction != null && thing.Faction != Faction.OfPlayer)
            {
                return false;
            }

            return true;
        }
    }
}
