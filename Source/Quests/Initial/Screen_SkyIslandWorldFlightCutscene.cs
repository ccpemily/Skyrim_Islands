using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SkyrimIslands.World;

namespace SkyrimIslands.Quests.Initial
{
    public class Screen_SkyIslandWorldFlightCutscene : Window
    {
        private const float FlightCameraAltitude = 135f;
        private static readonly AccessTools.FieldRef<WorldCameraDriver, float> DesiredAltitudeRef =
            AccessTools.FieldRefAccess<WorldCameraDriver, float>("desiredAltitude");

        private readonly TravellingTransporters travellingShuttle;
        private readonly SkyIslandMapParent island;

        public Screen_SkyIslandWorldFlightCutscene(TravellingTransporters travellingShuttle, SkyIslandMapParent island)
        {
            this.travellingShuttle = travellingShuttle;
            this.island = island;
            doWindowBackground = false;
            doCloseButton = false;
            doCloseX = false;
            closeOnCancel = false;
            forcePause = false;
            preventCameraMotion = false;
            drawShadow = false;
            absorbInputAroundWindow = false;
            layer = WindowLayer.Super;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

        protected override float Margin => 0f;

        public override void PreOpen()
        {
            base.PreOpen();
            CameraJumper.TryShowWorld();
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            PlanetLayer.Selected = island.Tile.Layer;
            SetFlightCameraAltitude();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            if (travellingShuttle == null || travellingShuttle.Destroyed || !travellingShuttle.Spawned)
            {
                if (!WorldRendererUtility.WorldSelected)
                {
                    Close(false);
                }

                return;
            }

            if (!WorldRendererUtility.WorldSelected)
            {
                CameraJumper.TryShowWorld();
            }

            PlanetLayer.Selected = island.Tile.Layer;
            Find.WorldCameraDriver.JumpTo(travellingShuttle.DrawPos);
        }

        public override void DoWindowContents(Rect inRect)
        {
        }

        private static void SetFlightCameraAltitude()
        {
            WorldCameraDriver cameraDriver = Find.WorldCameraDriver;
            float altitude = Mathf.Max(WorldCameraDriver.MinAltitude + 5f, FlightCameraAltitude);
            cameraDriver.altitude = altitude;
            DesiredAltitudeRef(cameraDriver) = altitude;
        }
    }
}
