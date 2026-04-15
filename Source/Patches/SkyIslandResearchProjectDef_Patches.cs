using HarmonyLib;
using RimWorld;
using SkyrimIslands.Research;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(ResearchProjectDef), "get_IsHidden")]
    public static class ResearchProjectDef_IsHidden_SkyIslandsPatch
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__instance is not SkyIslandResearchProjectDef skyProject)
            {
                return;
            }

            GameComponent_SkyIslandResearch? research = Current.Game?.GetComponent<GameComponent_SkyIslandResearch>();
            if (research == null)
            {
                return;
            }

            __result = __result || !research.IsProjectVisible(skyProject);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.SetCurrentProject))]
    public static class ResearchManager_SetCurrentProject_SkyIslandsPatch
    {
        public static bool Prefix(ResearchProjectDef proj)
        {
            if (proj is not SkyIslandResearchProjectDef skyProject)
            {
                return true;
            }

            Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.SetCurrentSkyProject(skyProject);
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.StopProject))]
    public static class ResearchManager_StopProject_SkyIslandsPatch
    {
        public static bool Prefix(ResearchProjectDef proj)
        {
            if (proj is not SkyIslandResearchProjectDef)
            {
                return true;
            }

            Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.StopCurrentSkyProject(proj);
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.IsCurrentProject))]
    public static class ResearchManager_IsCurrentProject_SkyIslandsPatch
    {
        public static void Postfix(ResearchProjectDef proj, ref bool __result)
        {
            if (__result || proj is not SkyIslandResearchProjectDef)
            {
                return;
            }

            __result = Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.IsCurrentSkyProject(proj) ?? false;
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_SkyIslandsPatch
    {
        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj is not SkyIslandResearchProjectDef)
            {
                return;
            }

            Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.NotifySkyProjectFinished(proj);
        }
    }

    [HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.ShouldSkip))]
    public static class WorkGiver_Researcher_ShouldSkip_SkyIslandsPatch
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            if (Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.HasAvailableSkyResearchWork(pawn) ?? false)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.HasJobOnThing))]
    public static class WorkGiver_Researcher_HasJobOnThing_SkyIslandsPatch
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (Current.Game.GetComponent<GameComponent_SkyIslandResearch>()?.HasAvailableSkyResearchWork(pawn) ?? false)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "UpdateSelectedProject")]
    public static class MainTabWindow_Research_UpdateSelectedProject_SkyIslandsPatch
    {
        private static readonly AccessTools.FieldRef<MainTabWindow_Research, ResearchProjectDef> SelectedProjectRef =
            AccessTools.FieldRefAccess<MainTabWindow_Research, ResearchProjectDef>("selectedProject");

        public static bool Prefix(MainTabWindow_Research __instance, ResearchManager researchManager)
        {
            if (__instance.CurTab != SkyrimIslandsDefOf.SkyrimIslands_SkyResearchTab)
            {
                return true;
            }

            SelectedProjectRef(__instance) = null!;
            return false;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "DoBeginResearch")]
    public static class MainTabWindow_Research_DoBeginResearch_SkyIslandsPatch
    {
        public static bool Prefix(ResearchProjectDef projectToStart)
        {
            if (projectToStart is not SkyIslandResearchProjectDef skyProject)
            {
                return true;
            }

            SoundDefOf.ResearchStart.PlayOneShotOnCamera();
            Find.ResearchManager.SetCurrentProject(projectToStart);
            TutorSystem.Notify_Event("StartResearchProject");

            return false;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "DrawProjectInfo")]
    public static class MainTabWindow_Research_DrawProjectInfo_SkyIslandsPatch
    {
        public static bool Prefix(MainTabWindow_Research __instance, Rect rect)
        {
            if (__instance.CurTab != SkyrimIslandsDefOf.SkyrimIslands_SkyResearchTab)
            {
                return true;
            }

            SkyIslandResearchUIPresenter.DrawProjectInfo(__instance, rect);
            return false;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "ListProjects")]
    public static class MainTabWindow_Research_ListProjects_SkyIslandsPatch
    {
        public static void Postfix(MainTabWindow_Research __instance, Rect rightInRect)
        {
            if (__instance.CurTab != SkyrimIslandsDefOf.SkyrimIslands_SkyResearchTab)
            {
                return;
            }

            SkyIslandResearchUIPresenter.DrawProjectListIcons(__instance, rightInRect);
        }
    }
}
