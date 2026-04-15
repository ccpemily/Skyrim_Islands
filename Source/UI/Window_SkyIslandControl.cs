using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SkyrimIslands.MainTabs
{
    public class Window_SkyIslandControl : Window
    {
        private enum SkyIslandTab : byte
        {
            Overview,
            Movement
        }

        private readonly List<TabRecord> tabs = new List<TabRecord>();
        private Vector2 routeScrollPosition;
        private float routeViewHeight = 240f;
        private static SkyIslandTab curTab = SkyIslandTab.Overview;

        public override Vector2 InitialSize => new Vector2((float)UI.screenWidth, 620f);

        public Window_SkyIslandControl()
        {
            layer = WindowLayer.GameUI;
            closeOnCancel = true;
            closeOnClickedOutside = false;
            draggable = false;
            resizeable = false;
            preventCameraMotion = false;
        }

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(0f, (float)(UI.screenHeight - 35) - InitialSize.y, InitialSize.x, InitialSize.y).Rounded();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Find.MainTabsRoot.EscapeCurrentTab(false);
            tabs.Clear();
            tabs.Add(new TabRecord("总控面板", delegate
            {
                curTab = SkyIslandTab.Overview;
            }, () => curTab == SkyIslandTab.Overview));
            tabs.Add(new TabRecord("移动状态", delegate
            {
                curTab = SkyIslandTab.Movement;
            }, () => curTab == SkyIslandTab.Movement));
        }

        public override void DoWindowContents(Rect rect)
        {
            SkyIslandMapParent? island = GetPrimaryPlayerIsland();
            if (island == null)
            {
                Widgets.NoneLabelCenteredVertically(rect, "(当前没有玩家空岛)");
                return;
            }

            float leftWidth = Mathf.Max(270f, rect.width * 0.22f);
            Rect leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);

            const float tabHeight = 32f;
            Rect rightRect = new Rect(leftRect.xMax + 10f, rect.y, rect.width - leftRect.width - 10f, rect.height);
            Rect menuRect = rightRect;
            menuRect.yMin += tabHeight;
            Widgets.DrawMenuSection(menuRect);

            Rect tabBaseRect = new Rect(menuRect.x, menuRect.y, menuRect.width, tabHeight);
            Rect rightInnerRect = menuRect.ContractedBy(10f);
            Rect contentRect = new Rect(rightInnerRect.x + 8f, rightInnerRect.y + 4f, rightInnerRect.width - 16f, rightInnerRect.height - 4f);

            DrawSidebar(leftRect, island);
            TabDrawer.DrawTabs(tabBaseRect, tabs, 180f);

            switch (curTab)
            {
                case SkyIslandTab.Overview:
                    DrawOverviewPage(contentRect);
                    break;
                case SkyIslandTab.Movement:
                    DrawMovementPage(contentRect, island);
                    break;
            }
        }

        private static SkyIslandMapParent? GetPrimaryPlayerIsland()
        {
            for (int i = 0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
            {
                if (Find.WorldObjects.AllWorldObjects[i] is SkyIslandMapParent island &&
                    !island.Destroyed &&
                    island.Faction == Faction.OfPlayer)
                {
                    return island;
                }
            }

            return null;
        }

        private static void DrawSidebar(Rect rect, SkyIslandMapParent island)
        {
            Rect inner = rect.ContractedBy(8f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 36f), island.LabelCap);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            float y = inner.y + 42f;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), "空岛层坐标: " + FormatCoordinates(island.CurrentSkyLongLat));
            y += 24f;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), "地面投影: " + FormatCoordinates(island.CurrentSurfaceLongLat));
            y += 28f;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), "当前状态: " + GetMovementStateLabel(island));
            y += 24f;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), "当前速度: " + island.CurrentSpeedTilesPerDay.ToString("F1") + " tiles/天");
            y += 24f;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), "ETA: " + FormatEta(island.CurrentEtaTicks));
            y += 28f;

            GUI.color = Color.gray;
            Widgets.Label(new Rect(inner.x, y, inner.width, rect.height - (y - inner.y)),
                "操作提示\n\n" +
                "- 总控面板：用于汇总查看空岛系统入口\n" +
                "- 移动状态：用于查看路线与移动相关数据\n" +
                "- 后续空岛相关模块会继续并入这里");
            GUI.color = Color.white;
        }

        private static string FormatCoordinates(PlanetTile tile)
        {
            if (!tile.Valid)
            {
                return "无";
            }

            return FormatCoordinates(Find.WorldGrid.LongLatOf(tile));
        }

        private static string FormatCoordinates(Vector2 longLat)
        {
            return longLat.y.ToStringLatitude() + " / " + longLat.x.ToStringLongitude();
        }

        private static string FormatEta(int? ticks)
        {
            if (ticks == null || ticks.Value <= 0)
            {
                return "无";
            }

            return ticks.Value.ToStringTicksToPeriod(true, false, true, true, false);
        }

        private static void DrawOverviewPage(Rect rect)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, rect.height),
                "总控面板\n\n" +
                "这里用于承载整个空岛系统的总控入口，而不是具体的移动状态。\n\n" +
                "后续会继续接入：\n" +
                "- 空岛系统概况\n" +
                "- 关键功能入口跳转\n" +
                "- 资源 / 模块状态摘要\n" +
                "- 后续空岛专属建筑与系统控制");
            GUI.color = Color.white;
        }

        private void DrawMovementPage(Rect rect, SkyIslandMapParent island)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 28f), "移动状态");

            DrawMovementButtons(new Rect(rect.x, rect.y + 34f, rect.width, 72f), island);

            float infoY = rect.y + 116f;
            Widgets.Label(new Rect(rect.x, infoY, rect.width, 24f), "当前位置: " + FormatCoordinates(island.CurrentSurfaceLongLat));
            Widgets.Label(new Rect(rect.x, infoY + 24f, rect.width, 24f), "当前状态: " + GetMovementStateLabel(island));
            Widgets.Label(new Rect(rect.x, infoY + 48f, rect.width, 24f), "待执行路径点数量: " + island.PlannedSurfaceWaypoints.Count);

            string nextWaypoint = island.PlannedSurfaceWaypoints.Count > 0
                ? FormatCoordinates(island.PlannedSurfaceWaypoints[0])
                : "无";
            Widgets.Label(new Rect(rect.x, infoY + 72f, rect.width, 24f), "下一路径点: " + nextWaypoint);

            Rect routeRect = new Rect(rect.x, rect.y + 220f, rect.width, rect.height - 220f);
            DrawRoutePanel(routeRect, island);
        }

        private static string GetMovementStateLabel(SkyIslandMapParent island)
        {
            if (island.IsPreparingToDock)
            {
                return "准备泊入";
            }

            return island.MovementState switch
            {
                SkyIslandMapParent.SkyIslandMovementState.Idle => "待命",
                SkyIslandMapParent.SkyIslandMovementState.Accelerating => "加速中",
                SkyIslandMapParent.SkyIslandMovementState.Cruising => "巡航中",
                SkyIslandMapParent.SkyIslandMovementState.Decelerating => "减速中",
                SkyIslandMapParent.SkyIslandMovementState.Interrupting => "中断回锚中",
                _ => "未知"
            };
        }

        private static void DrawMovementButtons(Rect rect, SkyIslandMapParent island)
        {
            float buttonWidth = (rect.width - 12f) / 2f;
            float buttonHeight = 30f;
            Rect topLeft = new Rect(rect.x, rect.y, buttonWidth, buttonHeight);
            Rect topRight = new Rect(topLeft.xMax + 12f, rect.y, buttonWidth, buttonHeight);
            Rect bottomLeft = new Rect(rect.x, rect.y + buttonHeight + 10f, buttonWidth, buttonHeight);
            Rect bottomRight = new Rect(bottomLeft.xMax + 12f, bottomLeft.y, buttonWidth, buttonHeight);

            GameComponent_SkyIslandMovement? movement = Current.Game?.GetComponent<GameComponent_SkyIslandMovement>();
            bool controlsLocked = island.IsMoveControlLocked;

            if (controlsLocked)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(topLeft, "进入规划模式"))
            {
                if (movement != null && !controlsLocked)
                {
                    movement.StartPlanning(island);
                    Find.WindowStack.TryRemove(Find.WindowStack.WindowOfType<Window_SkyIslandControl>(), false);
                }
            }
            GUI.color = Color.white;

            if (controlsLocked)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(topRight, "清空路线"))
            {
                if (!controlsLocked)
                {
                    island.ClearPlannedSurfaceWaypoints();
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }
            GUI.color = Color.white;

            bool canStartEngine = island.HasPlannedRoute && island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Idle;
            if (!canStartEngine)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(bottomLeft, "启动引擎") && canStartEngine)
            {
                if (island.StartEnginePreview())
                {
                    Messages.Message("空岛测试移动已启动。当前版本使用固定速度进行点到点移动。", island, MessageTypeDefOf.NeutralEvent);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
            }
            GUI.color = Color.white;

            bool canInterrupt = island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Accelerating ||
                                island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Cruising ||
                                island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Decelerating;
            if (!canInterrupt)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(bottomRight, "中断移动") && canInterrupt)
            {
                bool changed = island.PauseMovementPreview();
                if (changed)
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
            }
            GUI.color = Color.white;
        }

        private void DrawRoutePanel(Rect rect, SkyIslandMapParent island)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(1f, 1f, 1f, 0.03f), new Color(1f, 1f, 1f, 0.18f), 1);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f), "路线摘要");

            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, routeViewHeight);
            Widgets.BeginScrollView(outRect, ref routeScrollPosition, viewRect);

            float y = 0f;
            DrawRouteEntry(new Rect(0f, y, viewRect.width, 62f), "当前位置", island.SurfaceProjectionTile, island.Tile);
            y += 68f;

            for (int i = 0; i < island.PlannedSurfaceWaypoints.Count; i++)
            {
                string label = $"路径点 {i + 1}";
                DrawRouteEntry(new Rect(0f, y, viewRect.width, 62f), label, island.PlannedSurfaceWaypoints[i], island.PlannedSkyWaypoints[i]);
                y += 68f;
            }

            if (!island.HasPlannedRoute)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, y, viewRect.width, 28f), "尚未规划任何路线。");
                GUI.color = Color.white;
                y += 34f;
            }

            routeViewHeight = y + 4f;
            Widgets.EndScrollView();
        }

        private static void DrawRouteEntry(Rect rect, string label, PlanetTile surfaceTile, PlanetTile skyTile)
        {
            Rect rowRect = rect;
            rowRect.height = Mathf.Max(rowRect.height, 62f);
            Widgets.DrawLightHighlight(rowRect);

            Rect inner = rowRect.ContractedBy(6f);
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f), label);

            GUI.color = Color.gray;
            Widgets.Label(new Rect(inner.x, inner.y + 20f, inner.width, 22f), "地面: " + FormatCoordinates(surfaceTile));
            Widgets.Label(new Rect(inner.x, inner.y + 38f, inner.width, 22f), "空岛: " + FormatCoordinates(skyTile));
            GUI.color = Color.white;
        }
    }
}
