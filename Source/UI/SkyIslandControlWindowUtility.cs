using RimWorld;
using SkyrimIslands.World;
using SkyrimIslands.World.Movement;
using Verse;

namespace SkyrimIslands.MainTabs
{
    public static class SkyIslandControlWindowUtility
    {
        public static void EnsureWindowState()
        {
            bool shouldShow = HasAnyPlayerIsland();

            Window_SkyIslandControlButton? buttonWindow = Find.WindowStack.WindowOfType<Window_SkyIslandControlButton>();
            if (shouldShow)
            {
                if (buttonWindow == null)
                {
                    Find.WindowStack.Add(new Window_SkyIslandControlButton());
                }
            }
            else
            {
                if (buttonWindow != null)
                {
                    Find.WindowStack.TryRemove(buttonWindow, false);
                }

                Window_SkyIslandControl? controlWindow = Find.WindowStack.WindowOfType<Window_SkyIslandControl>();
                if (controlWindow != null)
                {
                    Find.WindowStack.TryRemove(controlWindow, false);
                }
            }
        }

        public static bool HasAnyPlayerIsland()
        {
            if (!ModsConfig.OdysseyActive || Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            GameComponent_SkyIslandMovement? movement = Current.Game?.GetComponent<GameComponent_SkyIslandMovement>();
            if (movement != null && movement.Active)
            {
                return false;
            }

            for (int i = 0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
            {
                if (Find.WorldObjects.AllWorldObjects[i] is SkyIslandMapParent island &&
                    !island.Destroyed &&
                    island.Faction == Faction.OfPlayer)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsControlWindowOpen()
        {
            return Find.WindowStack.IsOpen<Window_SkyIslandControl>();
        }

        public static void ToggleControlWindow()
        {
            Window_SkyIslandControl? controlWindow = Find.WindowStack.WindowOfType<Window_SkyIslandControl>();
            if (controlWindow != null)
            {
                Find.WindowStack.TryRemove(controlWindow, true);
                return;
            }

            OpenControlWindow();
        }

        public static void OpenControlWindow()
        {
            if (Find.WindowStack.WindowOfType<Window_SkyIslandControl>() != null)
            {
                return;
            }

            Find.WindowStack.Add(new Window_SkyIslandControl());
        }
    }
}
