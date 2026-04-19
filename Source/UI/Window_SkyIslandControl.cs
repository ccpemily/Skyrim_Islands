using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using SkyrimIslands.World.Movement;
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
        private bool isDraggingMinimap;
        private static SkyIslandTab curTab = SkyIslandTab.Overview;

        public override Vector2 InitialSize => new Vector2((float)UI.screenWidth * 0.9f, 620f);

        public Window_SkyIslandControl()
        {
            layer = WindowLayer.GameUI;
            closeOnCancel = true;
            closeOnClickedOutside = true;
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

            const float barWidth = 28f;
            float leftWidth = Mathf.Max(270f, Mathf.Round(rect.width * 0.22f));
            Rect barRect = new Rect(rect.x, rect.y, barWidth, rect.height);
            Rect leftRect = new Rect(rect.x + barWidth, rect.y, leftWidth, rect.height);

            const float tabHeight = 32f;
            Rect rightRect = new Rect(Mathf.Round(leftRect.xMax + 10f), rect.y, Mathf.Round(rect.width - leftWidth - barWidth - 10f), rect.height);
            Rect menuRect = rightRect;
            menuRect.yMin += tabHeight;
            Widgets.DrawMenuSection(menuRect);

            Rect tabBaseRect = new Rect(menuRect.x, menuRect.y, menuRect.width, tabHeight);
            Rect rightInnerRect = menuRect.ContractedBy(10f);
            Rect contentRect = new Rect(rightInnerRect.x + 8f, rightInnerRect.y + 4f, rightInnerRect.width - 16f, rightInnerRect.height - 4f);

            DrawAltitudeBar(barRect, island);
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

        private static void DrawAltitudeBar(Rect rect, SkyIslandMapParent island)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.12f, 0.9f));

            float minY = rect.yMax - (SkyIslandAltitude.MinAltitude / SkyIslandAltitude.OrbitHeight) * rect.height;
            float maxY = rect.yMax - (SkyIslandAltitude.MaxAltitude / SkyIslandAltitude.OrbitHeight) * rect.height;
            Rect validRect = new Rect(rect.x + 4f, maxY, rect.width - 8f, Mathf.Max(2f, minY - maxY));
            Widgets.DrawBoxSolid(validRect, new Color(0.22f, 0.45f, 0.6f, 0.5f));

            float markerY = rect.yMax - (Mathf.Clamp(island.Altitude, 0f, SkyIslandAltitude.OrbitHeight) / SkyIslandAltitude.OrbitHeight) * rect.height;
            Rect markerRect = new Rect(rect.x + 2f, markerY - 1.5f, rect.width - 4f, 3f);
            Widgets.DrawBoxSolid(markerRect, Color.white);

            Widgets.DrawBox(rect);

            TooltipHandler.TipRegion(rect, new TipSignal(() => BuildAltitudeTooltip(island), 9001001));
        }

        private static string BuildAltitudeTooltip(SkyIslandMapParent island)
        {
            float absoluteRadius = SkyIslandAltitude.SurfaceRadius + island.Altitude;
            return $"当前高度: {island.Altitude:F1} (半径 {absoluteRadius:F1})\n可用范围: {SkyIslandAltitude.MinAltitude:F1} ~ {SkyIslandAltitude.MaxAltitude:F1}\n轨道层顶: {SkyIslandAltitude.OrbitHeight:F0} (半径 {SkyIslandAltitude.OrbitLayerRadius:F0})";
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
            float minimapSize = Mathf.Min(rect.width / 2f - 10f, rect.height - 68f);
            float minimapY = rect.y + (rect.height - minimapSize) * 0.5f;
            const float gearBarHeight = 32f;
            float rightColumnX = rect.x + minimapSize + 20f;
            float rightColumnWidth = rect.width - minimapSize - 20f;

            Rect minimapRect = new Rect(rect.x, minimapY, minimapSize, minimapSize);
            HandleMinimapInput(minimapRect);
            SkyIslandMinimapUtility.DrawMinimap(minimapRect, island);
            DrawMinimapResetButton(minimapRect);

            DrawMovementButtons(new Rect(rightColumnX, rect.y + 34f, rightColumnWidth, 36f), island);

            Rect gearBarRect = new Rect(rightColumnX, rect.y + 34f + 36f + 10f, rightColumnWidth, gearBarHeight);
            DrawGearBar(gearBarRect, island);

            float infoY = gearBarRect.yMax + 14f;
            Widgets.Label(new Rect(rightColumnX, infoY, rightColumnWidth, 24f), "当前位置: " + FormatCoordinates(island.CurrentSurfaceLongLat));
            Widgets.Label(new Rect(rightColumnX, infoY + 24f, rightColumnWidth, 24f), "当前状态: " + GetMovementStateLabel(island));
            Widgets.Label(new Rect(rightColumnX, infoY + 48f, rightColumnWidth, 24f), "待执行路径点数量: " + island.PlannedSurfaceWaypoints.Count);

            string nextWaypoint = island.PlannedSurfaceWaypoints.Count > 0
                ? FormatCoordinates(island.PlannedSurfaceWaypoints[0])
                : "无";
            Widgets.Label(new Rect(rightColumnX, infoY + 72f, rightColumnWidth, 24f), "下一路径点: " + nextWaypoint);

            float routeY = infoY + 110f;
            Rect routeRect = new Rect(rightColumnX, routeY, rightColumnWidth, rect.height - routeY);
            DrawRoutePanel(routeRect, island);
        }

        private void HandleMinimapInput(Rect minimapRect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && minimapRect.Contains(e.mousePosition))
            {
                isDraggingMinimap = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDraggingMinimap)
            {
                SkyIslandMinimapUtility.MinimapYaw += e.delta.x * 0.01f;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && isDraggingMinimap)
            {
                isDraggingMinimap = false;
                e.Use();
            }
        }

        private static void DrawMinimapResetButton(Rect minimapRect)
        {
            Rect buttonRect = new Rect(minimapRect.x + 4f, minimapRect.y + 4f, 26f, 24f);
            if (Widgets.ButtonText(buttonRect, "\u21ba"))
            {
                SkyIslandMinimapUtility.ResetYaw();
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }
        }

        private static string GetMovementStateLabel(SkyIslandMapParent island)
        {
            string horizontal = island.MovementState switch
            {
                SkyIslandMapParent.SkyIslandMovementState.Idle => "待命",
                SkyIslandMapParent.SkyIslandMovementState.Accelerating => "加速中",
                SkyIslandMapParent.SkyIslandMovementState.Cruising => "巡航中",
                SkyIslandMapParent.SkyIslandMovementState.Decelerating => "减速中",
                SkyIslandMapParent.SkyIslandMovementState.Braking => "制动中",
                SkyIslandMapParent.SkyIslandMovementState.Docking => "泊入中",
                SkyIslandMapParent.SkyIslandMovementState.Interrupting => "中断回锚中",
                _ => "未知"
            };

            string vertical = island.VerticalState switch
            {
                SkyIslandMapParent.SkyIslandVerticalState.Ascending => "上升中",
                SkyIslandMapParent.SkyIslandVerticalState.Descending => "下降中",
                SkyIslandMapParent.SkyIslandVerticalState.Holding => "维持高度",
                _ => "维持高度"
            };

            return $"{horizontal}（{vertical}）";
        }

        private static void DrawGearBar(Rect rect, SkyIslandMapParent island)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.08f, 0.08f, 0.08f, 0.9f), new Color(0.35f, 0.35f, 0.35f, 0.5f), 1);

            float segmentWidth = rect.width / 5f;
            bool isIdle = island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Idle;
            string[] gearLabels = { "前进一", "前进二", "前进三", "前进四" };

            float lineCenterY = rect.center.y;
            float lineHalfHeight = rect.height * 0.28f;
            float lineTop = lineCenterY - lineHalfHeight;
            float lineBottom = lineCenterY + lineHalfHeight;

            for (int i = 0; i < 5; i++)
            {
                bool isInterrupt = i == 0;
                bool isSelected = isInterrupt
                    ? (isIdle || island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting)
                    : (!isIdle && island.CurrentGear == i - 1);

                bool canInteract;
                if (isInterrupt)
                {
                    canInteract = island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Accelerating ||
                                  island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Cruising ||
                                  island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Decelerating ||
                                  island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Braking;
                }
                else
                {
                    canInteract = isIdle ||
                                  island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Accelerating ||
                                  island.MovementState == SkyIslandMapParent.SkyIslandMovementState.Cruising;
                }

                Rect segRect = new Rect(rect.x + i * segmentWidth, rect.y, segmentWidth, rect.height);

                Color lineColor;
                float lineThickness;

                if (isInterrupt)
                {
                    lineColor = isSelected
                        ? new Color(1f, 0.15f, 0.15f, 1f)
                        : new Color(0.8f, 0.1f, 0.1f, canInteract ? 0.85f : 0.35f);
                    lineThickness = isSelected ? 4f : 2f;
                }
                else
                {
                    lineColor = isSelected
                        ? new Color(0.25f, 1f, 0.4f, 1f)
                        : new Color(0.75f, 0.75f, 0.75f, canInteract ? 0.85f : 0.35f);
                    lineThickness = isSelected ? 4f : 2f;
                }

                float centerX = segRect.center.x;

                Widgets.DrawLine(
                    new Vector2(centerX, lineTop),
                    new Vector2(centerX, lineBottom),
                    lineColor,
                    lineThickness);

                if (Mouse.IsOver(segRect))
                {
                    Widgets.DrawBoxSolid(segRect, new Color(1f, 1f, 1f, 0.06f));
                }

                string tooltip = isInterrupt ? "中断移动" : gearLabels[i - 1];
                TooltipHandler.TipRegion(segRect, new TipSignal(tooltip, 3000000 + i));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(segRect) && canInteract)
                {
                    if (isInterrupt)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "确认要中断当前移动吗？空岛将制动减速，随后飘向最近的锚定点停止。",
                            "确认",
                            delegate
                            {
                                bool changed = island.PauseMovementPreview();
                                if (changed)
                                {
                                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                                }
                            },
                            "取消",
                            null,
                            title: "中断移动"));
                    }
                    else
                    {
                        int newGear = i - 1;
                        if (isIdle && !island.HasPlannedRoute)
                        {
                            Messages.Message("没有规划任何路线。", island, MessageTypeDefOf.RejectInput);
                        }
                        else if (isIdle && island.HasPlannedRoute)
                        {
                            string targetLabel = gearLabels[newGear];
                            Find.WindowStack.Add(new Dialog_MessageBox(
                                $"确认要切换到 {targetLabel} 吗？",
                                "确认",
                                delegate
                                {
                                    island.SetGear(newGear);
                                    if (island.StartEnginePreview())
                                    {
                                        Messages.Message("空岛移动已启动。", island, MessageTypeDefOf.NeutralEvent);
                                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                                    }
                                },
                                "取消",
                                null,
                                title: "切换档位"));
                        }
                        else
                        {
                            string targetLabel = gearLabels[newGear];
                            Find.WindowStack.Add(new Dialog_MessageBox(
                                $"确认要切换到 {targetLabel} 吗？",
                                "确认",
                                delegate
                                {
                                    island.SetGear(newGear);
                                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                                },
                                "取消",
                                null,
                                title: "切换档位"));
                        }
                    }
                    Event.current.Use();
                }
            }
        }

        private static void DrawMovementButtons(Rect rect, SkyIslandMapParent island)
        {
            float buttonWidth = (rect.width - 12f) / 2f;
            float buttonHeight = 30f;
            Rect topLeft = new Rect(rect.x, rect.y, buttonWidth, buttonHeight);
            Rect topRight = new Rect(topLeft.xMax + 12f, rect.y, buttonWidth, buttonHeight);

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
            DrawRouteEntry(new Rect(0f, y, viewRect.width, 78f), "当前位置", island.SurfaceProjectionTile, island.Tile, island.Altitude);
            y += 84f;

            for (int i = 0; i < island.PlannedSurfaceWaypoints.Count; i++)
            {
                string label = $"路径点 {i + 1}";
                float waypointAltitude = island.WaypointAltitudes.Count > i ? island.WaypointAltitudes[i] : SkyIslandAltitude.DefaultAltitude;
                DrawRouteEntry(new Rect(0f, y, viewRect.width, 78f), label, island.PlannedSurfaceWaypoints[i], island.PlannedSkyWaypoints[i], waypointAltitude);
                y += 84f;
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

        private static void DrawRouteEntry(Rect rect, string label, PlanetTile surfaceTile, PlanetTile skyTile, float altitude)
        {
            Rect rowRect = rect;
            rowRect.height = Mathf.Max(rowRect.height, 78f);
            Widgets.DrawLightHighlight(rowRect);

            Rect inner = rowRect.ContractedBy(6f);
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f), label);

            GUI.color = Color.gray;
            Widgets.Label(new Rect(inner.x, inner.y + 20f, inner.width, 22f), "地面: " + FormatCoordinates(surfaceTile));
            Widgets.Label(new Rect(inner.x, inner.y + 36f, inner.width, 22f), "空岛: " + FormatCoordinates(skyTile));
            Widgets.Label(new Rect(inner.x, inner.y + 52f, inner.width, 22f), "高度: " + altitude.ToString("F1"));
            GUI.color = Color.white;
        }
    }
}
