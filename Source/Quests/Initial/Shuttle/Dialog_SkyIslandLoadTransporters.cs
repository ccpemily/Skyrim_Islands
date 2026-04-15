using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SkyrimIslands.Quests.Initial.Shuttle
{
    public class Dialog_SkyIslandLoadTransporters : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private readonly Map map;
        private readonly CompSkyIslandMissionTransporter transporter;
        private readonly Vector2 bottomButtonSize = new Vector2(160f, 40f);

        private List<TransferableOneWay> transferables = null!;
        private TransferableOneWayWidget pawnsTransfer = null!;
        private Tab tab;
        private float lastMassFlashTime = -9999f;
        private bool massUsageDirty = true;
        private float cachedMassUsage;
        private Vector2 itemsScrollPosition;
        private List<ItemTransferRow> itemRows = null!;
        private readonly QuickSearchWidget itemsQuickSearchWidget = new QuickSearchWidget();
        private TransferableSorterDef itemsSorter1 = TransferableSorterDefOf.Category;
        private TransferableSorterDef itemsSorter2 = TransferableSorterDefOf.MarketValue;

        public Dialog_SkyIslandLoadTransporters(Map map, CompSkyIslandMissionTransporter transporter)
        {
            this.map = map;
            this.transporter = transporter;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
        }

        public override Vector2 InitialSize => new Vector2(1024f, (float)UI.screenHeight);

        protected override float Margin => 0f;

        private float MassCapacity => transporter.MassCapacity;

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(
                        transferables,
                        IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                        true,
                        false);
                }

                return cachedMassUsage;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
            if (transporter.LoadingInProgressOrReadyToLaunch)
            {
                SetLoadedItemsToLoad();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "装载未知穿梭机");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect infoRect = new Rect(12f, 35f, inRect.width - 24f, 40f);
            List<TransferableUIUtility.ExtraInfo> info = new List<TransferableUIUtility.ExtraInfo>();
            TaggedString massLabel = string.Format("{0} / {1:F0} ", MassUsage.ToStringEnsureThreshold(MassCapacity, 0), MassCapacity) + "kg".Translate();
            string massTip = "MassCarriedSimple".Translate() + ": " + MassUsage.ToStringEnsureThreshold(MassCapacity, 2) + " " + "kg".Translate() + "\n" + "MassCapacity".Translate() + ": " + MassCapacity.ToString("F2") + " " + "kg".Translate();
            Color massColor = MassUsage > MassCapacity ? Color.red : Color.white;
            info.Add(new TransferableUIUtility.ExtraInfo("Mass".Translate(), massLabel, massColor, massTip, lastMassFlashTime));
            TransferableUIUtility.DrawExtraInfo(info, infoRect);
            inRect.yMin += 52f;

            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("人员".Translate(), delegate { tab = Tab.Pawns; }, tab == Tab.Pawns),
                new TabRecord("物品".Translate(), delegate { tab = Tab.Items; }, tab == Tab.Items)
            };
            inRect.yMin += 67f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabs, 200f);
            inRect = inRect.ContractedBy(17f);
            inRect.height += 17f;
            Widgets.BeginGroup(inRect);

            Rect contentRect = inRect.AtZero();
            DoBottomButtons(contentRect);

            Rect transferRect = contentRect;
            transferRect.yMax -= 76f;

            bool anythingChanged;
            if (tab == Tab.Pawns)
            {
                pawnsTransfer.OnGUI(transferRect, out anythingChanged);
            }
            else
            {
                DoItemsTransferGUI(transferRect, out anythingChanged);
            }

            if (anythingChanged)
            {
                CountToTransferChanged();
            }

            Widgets.EndGroup();
        }

        private void DoBottomButtons(Rect rect)
        {
            float y = rect.height - 55f - 17f;

            if (Widgets.ButtonText(new Rect(0f, y, bottomButtonSize.x, bottomButtonSize.y), "CancelButton".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(rect.width / 2f - bottomButtonSize.x / 2f, y, bottomButtonSize.x, bottomButtonSize.y), "ResetButton".Translate()))
            {
                CalculateAndRecacheTransferables();
            }

            if (Widgets.ButtonText(new Rect(rect.width - bottomButtonSize.x, y, bottomButtonSize.x, bottomButtonSize.y), "AcceptButton".Translate()))
            {
                if (TryAccept())
                {
                    Close(false);
                }
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddPawnsToTransferables();
            AddItemsToTransferables();

            if (transporter.LoadingInProgressOrReadyToLaunch)
            {
                for (int i = 0; i < transporter.innerContainer.Count; i++)
                {
                    AddToTransferables(transporter.innerContainer[i]);
                }

                foreach (Thing thing in TransporterUtility.ThingsBeingHauledTo(new List<CompTransporter> { transporter }, map))
                {
                    AddToTransferables(thing);
                }
            }

            pawnsTransfer = new TransferableOneWayWidget(
                null,
                null,
                null,
                "TransporterColonyThingCountTip".Translate(),
                true,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                true,
                () => MassCapacity - MassUsage,
                0f,
                false,
                map.Tile,
                true,
                true,
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                false);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);

            RebuildItemRows();
            CountToTransferChanged();
        }

        private void DoItemsTransferGUI(Rect inRect, out bool anythingChanged)
        {
            anythingChanged = false;

            TransferableUIUtility.DoTransferableSorters(itemsSorter1, itemsSorter2,
                delegate(TransferableSorterDef x) { itemsSorter1 = x; },
                delegate(TransferableSorterDef x) { itemsSorter2 = x; });
            itemsQuickSearchWidget.noResultsMatched = !GetFilteredAndSortedItemRows().Any();
            TransferableUIUtility.DoTransferableSearcher(itemsQuickSearchWidget, delegate { });

            Rect mainRect = new Rect(inRect.x, inRect.y + 37f, inRect.width, inRect.height - 37f);

            List<ItemTransferRow> filteredRows = GetFilteredAndSortedItemRows();
            if (filteredRows.Count == 0)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(mainRect, "NoneBrackets".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            float viewHeight = 6f + filteredRows.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(mainRect, ref itemsScrollPosition, viewRect, true);

            float minVisibleY = itemsScrollPosition.y - 30f;
            float maxVisibleY = itemsScrollPosition.y + mainRect.height;
            float curY = 6f;
            for (int i = 0; i < filteredRows.Count; i++)
            {
                if (curY > minVisibleY && curY < maxVisibleY)
                {
                    Rect rowRect = new Rect(0f, curY, viewRect.width, 30f);
                    DrawItemRow(rowRect, filteredRows[i], i, ref anythingChanged);
                }

                curY += 30f;
            }

            Widgets.EndScrollView();
        }

        private List<ItemTransferRow> GetFilteredAndSortedItemRows()
        {
            var query = itemRows.Where(r => itemsQuickSearchWidget.filter.Matches(r.transferable.Label));
            query = query.OrderBy(r => r.transferable, itemsSorter1.Comparer)
                         .ThenBy(r => r.transferable, itemsSorter2.Comparer)
                         .ThenBy(r => TransferableUIUtility.DefaultListOrderPriority(r.transferable));
            return query.ToList();
        }

        private void DrawItemRow(Rect rect, ItemTransferRow row, int index, ref bool anythingChanged)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }

            TransferableOneWay transferable = row.transferable;
            if (transferable.CountToTransfer > row.maxCount)
            {
                transferable.AdjustTo(row.maxCount);
                anythingChanged = true;
            }

            Widgets.BeginGroup(rect);

            float curX = rect.width;
            Rect disabledRect = new Rect(curX - 70f, 0f, 70f, rect.height);
            curX -= 70f;

            Rect adjustRect = new Rect(curX - 240f, 0f, 240f, rect.height);
            curX -= 240f;

            Rect countRect = new Rect(curX - 75f, 0f, 75f, rect.height);
            curX -= 75f;

            Rect massRect = new Rect(curX - 100f, 0f, 100f, rect.height);
            curX -= 100f;

            Rect infoRect = new Rect(0f, 0f, curX, rect.height);

            if (row.disabled)
            {
                DrawDisabledAdjustInterface(adjustRect);
            }
            else
            {
                int countBefore = transferable.CountToTransfer;
                DrawEnabledAdjustInterface(adjustRect, transferable, row.maxCount);
                if (countBefore != transferable.CountToTransfer)
                {
                    anythingChanged = true;
                }
            }

            Widgets.DrawHighlightIfMouseover(countRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect paddedCountRect = countRect;
            paddedCountRect.xMin += 5f;
            paddedCountRect.xMax -= 5f;
            Widgets.Label(paddedCountRect, row.maxCount.ToStringCached());
            TooltipHandler.TipRegion(countRect, "TransporterColonyThingCountTip".Translate());

            Widgets.DrawHighlightIfMouseover(massRect);
            float mass = transferable.AnyThing.GetStatValue(StatDefOf.Mass);
            Widgets.Label(massRect, mass.ToStringMass());
            TooltipHandler.TipRegionByKey(massRect, "ItemWeightTip");

            if (row.disabled)
            {
                Widgets.DrawHighlightIfMouseover(disabledRect);
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(disabledRect, "已禁用");
                GUI.color = Color.white;
                TooltipHandler.TipRegion(disabledRect, row.disabledReason);
            }

            TransferableUIUtility.DrawTransferableInfo(transferable, infoRect, Color.white);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndGroup();
        }

        private void DrawDisabledAdjustInterface(Rect rect)
        {
            Rect centerRect = new Rect(rect.center.x - 45f, rect.center.y - 12.5f, 90f, 25f).Rounded();
            GUI.color = TransferableUIUtility.ZeroCountColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(centerRect, "0");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawEnabledAdjustInterface(Rect rect, TransferableOneWay transferable, int maxCount)
        {
            rect = rect.Rounded();
            Rect centerRect = new Rect(rect.center.x - 45f, rect.center.y - 12.5f, 90f, 25f).Rounded();
            int massLimitedMax = GetMassLimitedMaxCount(transferable, maxCount);

            GUI.color = transferable.CountToTransfer == 0 ? TransferableUIUtility.ZeroCountColor : Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(centerRect, transferable.CountToTransfer.ToStringCached());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (transferable.CountToTransfer > 0)
            {
                Rect leftDoubleRect = new Rect(centerRect.x - 60f, rect.y, 30f, rect.height);
                string leftDoubleLabel = transferable.CountToTransfer > massLimitedMax ? "M<" : "<<";
                if (Widgets.ButtonText(leftDoubleRect, leftDoubleLabel))
                {
                    transferable.AdjustTo(transferable.CountToTransfer > massLimitedMax ? massLimitedMax : 0);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }

                Rect leftRect = new Rect(centerRect.x - 30f, rect.y, 30f, rect.height);
                if (Widgets.ButtonText(leftRect, "<"))
                {
                    transferable.AdjustTo(Mathf.Max(0, transferable.CountToTransfer - GenUI.CurrentAdjustmentMultiplier()));
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
            }

            if (transferable.CountToTransfer < massLimitedMax)
            {
                Rect rightRect = new Rect(centerRect.xMax, rect.y, 30f, rect.height);
                if (Widgets.ButtonText(rightRect, ">"))
                {
                    transferable.AdjustTo(Mathf.Min(massLimitedMax, transferable.CountToTransfer + GenUI.CurrentAdjustmentMultiplier()));
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }

            if (transferable.CountToTransfer < massLimitedMax)
            {
                Rect rightDoubleRect = new Rect(centerRect.xMax + 30f, rect.y, 30f, rect.height);
                string rightDoubleLabel = massLimitedMax < maxCount ? ">>M" : ">>";
                if (Widgets.ButtonText(rightDoubleRect, rightDoubleLabel))
                {
                    transferable.AdjustTo(rightDoubleLabel == ">>M" ? massLimitedMax : maxCount);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }
        }

        private int GetMassLimitedMaxCount(TransferableOneWay transferable, int rowMaxCount)
        {
            if (rowMaxCount <= 0)
            {
                return 0;
            }

            float itemMass = transferable.AnyThing.GetStatValue(StatDefOf.Mass);
            if (itemMass <= 0.0001f)
            {
                return rowMaxCount;
            }

            float freeMassForThisRow = MassCapacity - MassUsage + itemMass * transferable.CountToTransfer;
            int maxByMass = Mathf.FloorToInt(freeMassForThisRow / itemMass);
            return Mathf.Clamp(maxByMass, 0, rowMaxCount);
        }

        private void RebuildItemRows()
        {
            itemRows = new List<ItemTransferRow>();
            CompShuttle? shuttle = transporter.Shuttle;
            HashSet<Thing> loadedThings = new HashSet<Thing>(transporter.innerContainer);
            HashSet<Thing> hauledThings = new HashSet<Thing>(TransporterUtility.ThingsBeingHauledTo(new List<CompTransporter> { transporter }, map));

            List<TransferableOneWay> itemTransferables = transferables
                .Where((TransferableOneWay x) => x.ThingDef.category != ThingCategory.Pawn)
                .OrderByDescending((TransferableOneWay x) => TransferableUIUtility.DefaultListOrderPriority(x))
                .ThenBy((TransferableOneWay x) => x.Label)
                .ToList();

            for (int i = 0; i < itemTransferables.Count; i++)
            {
                TransferableOneWay transferable = itemTransferables[i];
                int reachableCount = 0;
                int disabledCount = 0;
                string disabledReason = "该物品当前无法交互。";

                for (int j = 0; j < transferable.things.Count; j++)
                {
                    Thing thing = transferable.things[j];
                    if (IsThingReachableForLoading(thing, shuttle, loadedThings, hauledThings, out string reason))
                    {
                        reachableCount += thing.stackCount;
                    }
                    else
                    {
                        disabledCount += thing.stackCount;
                        disabledReason = reason;
                    }
                }

                if (transferable.CountToTransfer > reachableCount)
                {
                    transferable.AdjustTo(reachableCount);
                }

                if (reachableCount > 0)
                {
                    itemRows.Add(new ItemTransferRow(transferable, reachableCount, false, ""));
                }

                if (disabledCount > 0)
                {
                    itemRows.Add(new ItemTransferRow(transferable, disabledCount, true, disabledReason));
                }
            }
        }

        private bool IsThingReachableForLoading(Thing thing, CompShuttle? shuttle, HashSet<Thing> loadedThings, HashSet<Thing> hauledThings, out string disabledReason)
        {
            if (loadedThings.Contains(thing) || hauledThings.Contains(thing))
            {
                disabledReason = "";
                return true;
            }

            if (thing.Destroyed)
            {
                disabledReason = "该物品已经不存在。";
                return false;
            }

            if (thing.IsForbidden(Faction.OfPlayer))
            {
                disabledReason = "该物品已被禁止，无法装载。";
                return false;
            }

            if (shuttle != null && !shuttle.IsAllowed(thing))
            {
                disabledReason = "该物品当前不允许装入穿梭机。";
                return false;
            }

            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly);
            if (map.reachability.CanReach(thing.PositionHeld, transporter.parent, PathEndMode.Touch, traverseParms))
            {
                disabledReason = "";
                return true;
            }

            if (thing.ParentHolder is Pawn_CarryTracker carryTracker &&
                carryTracker.pawn.MapHeld == map &&
                map.reachability.CanReach(carryTracker.pawn.PositionHeld, transporter.parent, PathEndMode.Touch, traverseParms))
            {
                disabledReason = "";
                return true;
            }

            disabledReason = "该物品当前无法被搬运到穿梭机。";
            return false;
        }

        private readonly struct ItemTransferRow
        {
            public readonly TransferableOneWay transferable;
            public readonly int maxCount;
            public readonly bool disabled;
            public readonly string disabledReason;

            public ItemTransferRow(TransferableOneWay transferable, int maxCount, bool disabled, string disabledReason)
            {
                this.transferable = transferable;
                this.maxCount = maxCount;
                this.disabled = disabled;
                this.disabledReason = disabledReason;
            }
        }

        private void AddPawnsToTransferables()
        {
            foreach (Pawn pawn in TransporterUtility.AllSendablePawns(new List<CompTransporter> { transporter }, map))
            {
                AddToTransferables(pawn);
            }
        }

        private void AddItemsToTransferables()
        {
            List<Thing> items = CaravanFormingUtility.AllReachableColonyItems(
                map,
                allowEvenIfOutsideHomeArea: true,
                allowEvenIfReserved: transporter.LoadingInProgressOrReadyToLaunch,
                canMinify: false);

            CompShuttle? shuttle = transporter.Shuttle;
            for (int i = 0; i < items.Count; i++)
            {
                Thing thing = items[i];
                if (shuttle == null || shuttle.IsRequired(thing) || shuttle.IsAllowed(thing))
                {
                    AddToTransferables(thing);
                }
            }
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

        private void SetLoadedItemsToLoad()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                int loadedCount = LoadedCountFor(transferables[i]);
                if (loadedCount > 0)
                {
                    transferables[i].AdjustTo(loadedCount);
                }
            }

            if (transporter.leftToLoad == null)
            {
                return;
            }

            for (int i = 0; i < transporter.leftToLoad.Count; i++)
            {
                TransferableOneWay left = transporter.leftToLoad[i];
                if (left.CountToTransfer <= 0 || !left.HasAnyThing)
                {
                    continue;
                }

                TransferableOneWay? transferable = TransferableUtility.TransferableMatchingDesperate(left.AnyThing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferable != null && transferable.CanAdjustBy(left.CountToTransferToDestination).Accepted)
                {
                    transferable.AdjustBy(left.CountToTransferToDestination);
                }
            }
        }

        private bool TryAccept()
        {
            List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(pawns))
            {
                return false;
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                TransporterUtility.InitiateLoading(new List<CompTransporter> { transporter });
            }

            AssignTransferables();
            TransporterUtility.MakeLordsAsAppropriate(pawns, new List<CompTransporter> { transporter }, map);
            Messages.Message("已开始装载未知穿梭机。", transporter.parent, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        private void AssignTransferables()
        {
            if (transporter.leftToLoad != null)
            {
                transporter.leftToLoad.Clear();
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                int loadedCount = LoadedCountFor(transferables[i]);
                int desiredCount = transferables[i].CountToTransfer;
                int pendingCount = Mathf.Max(0, desiredCount - loadedCount);
                if (pendingCount > 0)
                {
                    transporter.AddToTheToLoadList(transferables[i], pendingCount);
                }
            }
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            CompShuttle? shuttle = transporter.Shuttle;
            if (shuttle != null && shuttle.requiredColonistCount > 0 && pawns.Count((Pawn p) => p.IsColonist) < shuttle.requiredColonistCount)
            {
                Messages.Message($"至少需要装载 {shuttle.requiredColonistCount} 名殖民者。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (MassUsage > MassCapacity)
            {
                FlashMass();
                Messages.Message("所选内容超过穿梭机载重上限。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
        }

        private int LoadedCountFor(TransferableOneWay transferable)
        {
            int count = 0;
            for (int i = 0; i < transporter.innerContainer.Count; i++)
            {
                Thing loadedThing = transporter.innerContainer[i];
                if (transferable.HasAnyThing && TransferableUtility.TransferAsOne(loadedThing, transferable.AnyThing, TransferAsOneMode.PodsOrCaravanPacking))
                {
                    count += loadedThing.stackCount;
                }
            }

            return count;
        }
    }
}
