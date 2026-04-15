using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using SkyrimIslands.Research;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SkyrimIslands.Research
{
    public static class SkyIslandResearchUIPresenter
    {
        private const float SkyDataIconSize = 14f;
        private const float SkyDataIconSpacing = 6f;

        private static readonly AccessTools.FieldRef<MainTabWindow_Research, ResearchProjectDef> SelectedProjectRef =
            AccessTools.FieldRefAccess<MainTabWindow_Research, ResearchProjectDef>("selectedProject");

        private static readonly MethodInfo DrawProjectScrollViewMethod =
            AccessTools.Method(typeof(MainTabWindow_Research), "DrawProjectScrollView", new[] { typeof(Rect) })!;

        private static readonly MethodInfo DrawStartButtonMethod =
            AccessTools.Method(typeof(MainTabWindow_Research), "DrawStartButton", new[] { typeof(Rect) })!;

        private static readonly MethodInfo PosXMethod =
            AccessTools.Method(typeof(MainTabWindow_Research), "PosX", new[] { typeof(ResearchProjectDef) })!;

        private static readonly MethodInfo PosYMethod =
            AccessTools.Method(typeof(MainTabWindow_Research), "PosY", new[] { typeof(ResearchProjectDef) })!;

        public static void DrawProjectInfo(MainTabWindow_Research window, Rect rect)
        {
            ResearchProjectDef? selectedProject = SelectedProjectRef(window);
            GameComponent_SkyIslandResearch? research = Current.Game.GetComponent<GameComponent_SkyIslandResearch>();
            if (selectedProject == null || research == null)
            {
                return;
            }

            List<GameComponent_SkyIslandResearch.SkyIslandResearchSlot> orderedSlots = GetOrderedSlots(research);
            int slotCount = Mathf.Max(1, orderedSlots.Count);
            float progressSectionHeight = slotCount > 1 ? 75f * slotCount : 100f;

            Rect progressOutRect = rect;
            progressOutRect.yMin = rect.yMax - progressSectionHeight;
            progressOutRect.yMax = rect.yMax;

            Rect progressSectionRect = progressOutRect;
            Rect activeTitleRect = progressOutRect;
            activeTitleRect.y = progressOutRect.y - 30f;
            activeTitleRect.height = 28f;

            Rect progressInnerRect = progressOutRect.ContractedBy(10f);
            progressInnerRect.y += 5f;

            Text.Font = GameFont.Medium;
            Widgets.Label(activeTitleRect, "ActiveProjectPlural".Translate());
            Text.Font = GameFont.Small;

            Rect startButtonRect = new Rect
            {
                y = activeTitleRect.y - 65f,
                height = 55f,
                x = rect.center.x - rect.width / 4f,
                width = rect.width / 2f + 20f
            };

            Widgets.DrawMenuSection(progressSectionRect);

            float prefixWidth = GetPrefixWidth(orderedSlots);
            float rowHeight = progressInnerRect.height / slotCount;
            for (int i = 0; i < orderedSlots.Count; i++)
            {
                Rect rowRect = new Rect(progressInnerRect.x, progressInnerRect.y + rowHeight * i, progressInnerRect.width, rowHeight);
                DrawSkyProjectProgress(rowRect, orderedSlots[i], prefixWidth);
            }

            DrawStartButtonMethod.Invoke(window, new object[] { startButtonRect });
            DrawDebugButtons(rect, activeTitleRect, selectedProject);

            float y = 0f;
            DrawSkyProjectPrimaryInfo(rect, selectedProject, ref y);

            Rect scrollRect = new Rect(0f, y, rect.width, 0f)
            {
                yMax = startButtonRect.yMin - 10f
            };
            DrawProjectScrollViewMethod.Invoke(window, new object[] { scrollRect });
        }

        public static void DrawProjectListIcons(MainTabWindow_Research window, Rect rightInRect)
        {
            for (int i = 0; i < window.VisibleResearchProjects.Count; i++)
            {
                ResearchProjectDef project = window.VisibleResearchProjects[i];
                if (project.tab != window.CurTab || project.IsHidden || project is not SkyIslandResearchProjectDef skyProject)
                {
                    continue;
                }

                float posX = (float)PosXMethod.Invoke(window, new object[] { project })!;
                float posY = (float)PosYMethod.Invoke(window, new object[] { project })!;

                Rect iconRect = new Rect(posX + 4f, posY + 4f, 14f, 14f);
                Widgets.DrawBoxSolid(iconRect, skyProject.skyIslandDataType.color);
            }
        }

        private static void DrawDebugButtons(Rect rect, Rect activeTitleRect, ResearchProjectDef selectedProject)
        {
            if (Prefs.DevMode && !Find.ResearchManager.IsCurrentProject(selectedProject) && !selectedProject.IsFinished)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 120f, activeTitleRect.y, 120f, 25f), "Debug: Finish now", true, true, true, null))
                {
                    Find.ResearchManager.SetCurrentProject(selectedProject);
                    Find.ResearchManager.FinishProject(selectedProject, false, null, true);
                }

                Text.Font = GameFont.Small;
            }

            if (Prefs.DevMode && !selectedProject.TechprintRequirementMet)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 300f, activeTitleRect.y, 170f, 25f), "Debug: Apply techprint", true, true, true, null))
                {
                    Find.ResearchManager.ApplyTechprint(selectedProject, null);
                    SoundDefOf.TechprintApplied.PlayOneShotOnCamera(null);
                }

                Text.Font = GameFont.Small;
            }
        }

        private static float GetPrefixWidth(List<GameComponent_SkyIslandResearch.SkyIslandResearchSlot> slots)
        {
            float width = 75f;
            for (int i = 0; i < slots.Count; i++)
            {
                width = Mathf.Max(width, SkyDataIconSize + SkyDataIconSpacing + Text.CalcSize(GetSlotLabel(slots[i]) + ":").x);
            }

            return width;
        }

        private static void DrawSkyProjectPrimaryInfo(Rect rect, ResearchProjectDef selectedProject, ref float y)
        {
            using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft))
            {
                Rect titleRect = new Rect(0f, y, rect.width, 50f);
                Widgets.LabelCacheHeight(ref titleRect, selectedProject.LabelCap, true, false);
                y += titleRect.height;
            }

            y += 10f;

            Rect descriptionRect = new Rect(0f, y, rect.width, 0f);
            Widgets.LabelCacheHeight(ref descriptionRect, selectedProject.Description, true, false);
            y += descriptionRect.height;

            if (selectedProject is SkyIslandResearchProjectDef skyProject)
            {
                y += 8f;

                Rect typeRowRect = new Rect(0f, y, rect.width, 22f);
                Rect typeIconRect = new Rect(typeRowRect.x, typeRowRect.center.y - SkyDataIconSize * 0.5f, SkyDataIconSize, SkyDataIconSize);
                Widgets.DrawBoxSolid(typeIconRect, skyProject.skyIslandDataType.color);

                Rect typeLabelRect = typeRowRect;
                typeLabelRect.xMin = typeIconRect.xMax + SkyDataIconSpacing;
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    Widgets.Label(typeLabelRect, "研究类型：" + skyProject.skyIslandDataType.LabelCap);
                }

                y += typeRowRect.height;
            }

            y += 10f;
            Widgets.DrawLineHorizontal(rect.x - 8f, y, rect.width, Color.gray);
            y += 10f;
        }

        private static void DrawSkyProjectProgress(Rect rect, GameComponent_SkyIslandResearch.SkyIslandResearchSlot slot, float prefixWidth)
        {
            Rect progressRect = rect;
            Rect prefixRect = progressRect;
            prefixRect.width = prefixWidth;
            progressRect.xMin = prefixRect.xMax + 10f;

            Rect iconRect = new Rect(prefixRect.x, prefixRect.center.y - SkyDataIconSize * 0.5f, SkyDataIconSize, SkyDataIconSize);
            Widgets.DrawBoxSolid(iconRect, slot.dataType.color);

            Rect prefixLabelRect = prefixRect;
            prefixLabelRect.xMin = iconRect.xMax + SkyDataIconSpacing;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(prefixLabelRect, GetSlotLabel(slot) + ":");
            }

            ResearchProjectDef? project = slot.project;
            if (project == null)
            {
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    Widgets.Label(progressRect, "NoProjectSelected".Translate());
                }

                return;
            }

            progressRect = progressRect.ContractedBy(10f);
            Rect barRect = progressRect;
            GUI.DrawTexture(barRect, BaseContent.BlackTex);
            barRect = barRect.ContractedBy(3f);
            if (SkyrimIslandsTextureCache.ResearchBarBGTex != null)
            {
                GUI.DrawTexture(barRect, SkyrimIslandsTextureCache.ResearchBarBGTex);
            }

            Rect fillRect = barRect;
            fillRect.width *= project.ProgressPercent;
            Color oldColor = GUI.color;
            GUI.color = slot.dataType.color;
            GUI.DrawTexture(fillRect, SkyrimIslandsTextureCache.ResearchBarFillTex);
            GUI.color = oldColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(progressRect, project.ProgressApparentString + " / " + project.CostApparent.ToString("F0"));

            Rect titleRect = progressRect;
            titleRect.y = progressRect.y - 22f;
            titleRect.height = 22f;
            float titleWidth = Text.CalcSize(project.LabelCap).x;
            Widgets.Label(titleRect, project.LabelCap.Truncate(titleRect.width, null));
            if (titleWidth > titleRect.width)
            {
                TooltipHandler.TipRegion(titleRect, project.LabelCap);
                Widgets.DrawHighlightIfMouseover(titleRect);
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string GetSlotLabel(GameComponent_SkyIslandResearch.SkyIslandResearchSlot slot)
        {
            if (!slot.dataType.shortLabel.NullOrEmpty())
            {
                return slot.dataType.shortLabel.CapitalizeFirst();
            }

            return slot.dataType.LabelCap;
        }

        private static List<GameComponent_SkyIslandResearch.SkyIslandResearchSlot> GetOrderedSlots(GameComponent_SkyIslandResearch research)
        {
            return research.CurrentSkyProjects
                .Where(static slot => slot.dataType != null)
                .OrderBy(static slot => slot.dataType.preferredResearchViewY)
                .ToList();
        }
    }
}
