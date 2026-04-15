using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using SkyrimIslands.MainTabs;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SkyrimIslands.World
{
    public class GameComponent_SkyIslandMovement : GameComponent
    {
        private SkyIslandMapParent? planningIsland;

        public GameComponent_SkyIslandMovement(Game game)
        {
        }

        public bool Active => planningIsland != null;

        public bool IsPlanning(SkyIslandMapParent island)
        {
            return planningIsland == island;
        }

        public void StartPlanning(SkyIslandMapParent island)
        {
            if (Current.ProgramState != ProgramState.Playing || island.Destroyed || !island.Tile.Valid)
            {
                return;
            }

            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
            {
                Messages.Message("未找到地面层，无法为浮空岛规划路径。", island, MessageTypeDefOf.RejectInput);
                return;
            }

            island.EnsureSurfaceProjectionTile();
            if (!island.SurfaceProjectionTile.Valid)
            {
                Messages.Message("当前浮空岛没有有效的地面投影位置，无法规划路径。", island, MessageTypeDefOf.RejectInput);
                return;
            }

            planningIsland = island;

            CameraJumper.TryShowWorld();
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            Find.TickManager.Pause();

            PlanetLayer.Selected = surfaceLayer;
            Find.WorldSelector.Select(island, false);
            Find.WorldCameraDriver.JumpTo(island.SurfaceProjectionTile);
            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
        }

        public void StopPlanning(bool returnToSkyLayer)
        {
            SkyIslandMapParent? island = planningIsland;
            planningIsland = null;

            if (!returnToSkyLayer || island == null || island.Destroyed)
            {
                return;
            }

            CameraJumper.TryShowWorld();
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            PlanetLayer.Selected = island.Tile.Layer;
            Find.WorldSelector.Select(island, false);
            Find.WorldCameraDriver.JumpTo(island.Tile);
            SkyIslandControlWindowUtility.OpenControlWindow();
            SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
        }

        public void UpdatePlanning()
        {
            if (planningIsland == null)
            {
                DrawWorldRoutePreviews();
                return;
            }

            if (planningIsland.Destroyed)
            {
                planningIsland = null;
                return;
            }

            if (!WorldRendererUtility.WorldSelected)
            {
                StopPlanning(true);
                return;
            }

            PlanetLayer? surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
            {
                planningIsland = null;
                return;
            }

            if (PlanetLayer.Selected != surfaceLayer)
            {
                PlanetLayer.Selected = surfaceLayer;
                Find.WorldSelector.Select(planningIsland, false);
            }

            DrawWorldRoutePreviews(planningIsland);
            SkyIslandMovementRenderUtility.DrawPlanningOverlay(planningIsland);
        }

        public void PlanningOnGUI()
        {
            if (planningIsland == null || !WorldRendererUtility.WorldSelected)
            {
                return;
            }

            GenUI.DrawMouseAttachment(SkyrimIslandsTextureCache.WaypointMouseAttachmentTex);
            DrawPlanningHintWindow();
        }

        public void ProcessInput()
        {
            if (planningIsland == null || !WorldRendererUtility.WorldSelected || Mouse.IsInputBlockedNow)
            {
                return;
            }

            if (KeyBindingDefOf.Cancel.KeyDownEvent)
            {
                StopPlanning(true);
                Event.current.Use();
                return;
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 1)
            {
                return;
            }

            PlanetTile tile = GenWorld.MouseTile(false);
            if (!tile.Valid || tile.LayerDef != PlanetLayerDefOf.Surface)
            {
                return;
            }

            int existingIndex = planningIsland.MostRecentPlannedWaypointIndexAt(tile);
            if (existingIndex >= 0)
            {
                ShowExistingWaypointMenu(tile, existingIndex);
                Event.current.Use();
                return;
            }

            TryAddWaypointFromPlanning(tile, false);
        }

        private void ShowExistingWaypointMenu(PlanetTile tile, int existingIndex)
        {
            if (planningIsland == null)
            {
                return;
            }

            bool isLastWaypoint = existingIndex == planningIsland.PlannedSurfaceWaypoints.Count - 1;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("删除原有路径点", delegate
            {
                if (planningIsland != null && planningIsland.TryRemovePlannedSurfaceWaypointAt(existingIndex))
                {
                    Find.WorldSelector.Select(planningIsland, false);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }));

            if (isLastWaypoint)
            {
                options.Add(new FloatMenuOption("在重叠位置新建路径点（不可用）", null));
            }
            else if (planningIsland.CanAddWaypointAt(tile))
            {
                options.Add(new FloatMenuOption("在重叠位置新建路径点", delegate
                {
                    TryAddWaypointFromPlanning(tile, true);
                }));
            }
            else
            {
                options.Add(new FloatMenuOption("在重叠位置新建路径点（不可用）", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void TryAddWaypointFromPlanning(PlanetTile tile, bool allowConsecutiveDuplicate)
        {
            if (planningIsland == null)
            {
                return;
            }

            if (!planningIsland.CanAddWaypointAt(tile))
            {
                Messages.Message("空岛当前锚定在这块地面投影上，不能把当前所在 tile 设为路径点。", planningIsland, MessageTypeDefOf.RejectInput);
                return;
            }

            if (planningIsland.PlannedSurfaceWaypoints.Count >= SkyIslandMapParent.MaxPlannedWaypointCount)
            {
                Messages.Message("空岛路径点数量不能超过 " + SkyIslandMapParent.MaxPlannedWaypointCount + " 个。", planningIsland, MessageTypeDefOf.RejectInput);
                return;
            }

            if (planningIsland.TryAddPlannedSurfaceWaypoint(tile, allowConsecutiveDuplicate))
            {
                Find.WorldSelector.Select(planningIsland, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else if (allowConsecutiveDuplicate)
            {
                Messages.Message("无法在当前位置新建重叠路径点。", planningIsland, MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("无法添加该路径点。", planningIsland, MessageTypeDefOf.RejectInput);
            }

            if (planningIsland != null)
            {
                Find.WorldSelector.Select(planningIsland, false);
                if (Event.current != null)
                {
                    Event.current.Use();
                }
            }
        }

        private static void DrawWorldRoutePreviews(SkyIslandMapParent? skipIsland = null)
        {
            if (!WorldRendererUtility.WorldSelected)
            {
                return;
            }

            PlanetLayer? selectedLayer = PlanetLayer.Selected;
            if (selectedLayer == null)
            {
                return;
            }

            foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
            {
                if (worldObject is not SkyIslandMapParent island ||
                    island == skipIsland ||
                    island.Destroyed ||
                    !island.HasPlannedRoute)
                {
                    continue;
                }

                if (selectedLayer.Def == PlanetLayerDefOf.Surface)
                {
                    SkyIslandMovementRenderUtility.DrawSurfaceRoutePreview(island);
                    continue;
                }

                if (selectedLayer == island.Tile.Layer)
                {
                    SkyIslandMovementRenderUtility.DrawSkyRoutePreview(island);
                }
            }
        }

        private void DrawPlanningHintWindow()
        {
            SkyIslandMapParent? island = planningIsland;
            if (island == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            int waypointCount = island.PlannedSurfaceWaypoints.Count;
            Rect rect = new Rect(((float)UI.screenWidth - 520f) / 2f, (float)UI.screenHeight - 130f, 520f, 92f);
            Find.WindowStack.ImmediateWindow(287134551, rect, WindowLayer.Dialog, delegate
            {
                Widgets.DrawWindowBackground(rect.AtZero());
                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Small;

                float y = 8f;
                Widgets.Label(new Rect(10f, y, rect.width - 20f, 24f), "空岛移动规划");
                y += 24f;

                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(16f, y, rect.width - 32f, 22f), "右键地面 tile：添加路径点；右键已有路径点：打开路径点菜单");
                y += 20f;
                Widgets.Label(new Rect(16f, y, rect.width - 32f, 22f), "ESC：退出规划并返回空岛层视角");
                y += 20f;
                GUI.color = Color.white;
                Widgets.Label(new Rect(16f, y, rect.width - 32f, 22f), "已规划路径点: " + waypointCount);

                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }, false, false, 1f);
        }
    }
}
