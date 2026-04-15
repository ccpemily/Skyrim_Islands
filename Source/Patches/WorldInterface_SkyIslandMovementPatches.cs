using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceUpdate))]
    public static class WorldInterface_WorldInterfaceUpdate_SkyIslandMovementPatch
    {
        public static void Postfix()
        {
            GameComponent_SkyIslandMovement? movement = Current.Game?.GetComponent<GameComponent_SkyIslandMovement>();
            movement?.UpdatePlanning();
        }
    }

    [HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceOnGUI))]
    public static class WorldInterface_WorldInterfaceOnGUI_SkyIslandMovementPatch
    {
        public static void Postfix()
        {
            GameComponent_SkyIslandMovement? movement = Current.Game?.GetComponent<GameComponent_SkyIslandMovement>();
            movement?.PlanningOnGUI();
        }
    }

    [HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.HandleLowPriorityInput))]
    public static class WorldInterface_HandleLowPriorityInput_SkyIslandMovementPatch
    {
        public static bool Prefix()
        {
            GameComponent_SkyIslandMovement? movement = Current.Game?.GetComponent<GameComponent_SkyIslandMovement>();
            if (movement == null || !movement.Active)
            {
                return true;
            }

            movement.ProcessInput();
            return false;
        }
    }
}
