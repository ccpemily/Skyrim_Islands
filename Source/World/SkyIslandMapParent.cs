using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World.Movement;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class SkyIslandMapParent : SpaceMapParent
    {
        public enum SkyIslandMovementState : byte
        {
            Idle,
            Accelerating,
            Cruising,
            Decelerating,
            Braking,
            Interrupting,
            Docking
        }

        public enum SkyIslandVerticalState : byte
        {
            Holding,
            Ascending,
            Descending
        }

        public const int MaxPlannedWaypointCount = 10;

        private CompSkyIslandMovement MovementComp => GetComponent<CompSkyIslandMovement>();

        private float altitude = SkyIslandAltitude.DefaultAltitude;

        public override string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    return Name;
                }

                return "Sky Island";
            }
        }

        public float Altitude
        {
            get => altitude;
            set => altitude = value;
        }

        public PlanetTile SurfaceProjectionTile => MovementComp.SurfaceProjectionTile;
        public override Vector3 DrawPos => MovementComp.CurrentSkyWorldPosition;
        public override Vector3 WorldCameraPosition => MovementComp.CurrentSkyWorldPosition;
        public System.Collections.Generic.IReadOnlyList<PlanetTile> PlannedSurfaceWaypoints => MovementComp.PlannedSurfaceWaypoints;
        public System.Collections.Generic.IReadOnlyList<PlanetTile> PlannedSkyWaypoints => MovementComp.PlannedSkyWaypoints;
        public System.Collections.Generic.IReadOnlyList<float> WaypointAltitudes => MovementComp.WaypointAltitudes;
        public bool HasPlannedRoute => MovementComp.HasPlannedRoute;
        public bool IsCenteredOnCurrentTile => MovementComp.IsCenteredOnCurrentTile;
        public SkyIslandMovementState MovementState => MovementComp.MovementState;
        public SkyIslandVerticalState VerticalState => MovementComp.VerticalState;
        public int CurrentGear => MovementComp.CurrentGear;
        public bool IsMoveControlLocked => MovementComp.IsMoveControlLocked;
        public float CurrentSpeedTilesPerDay => MovementComp.CurrentSpeedTilesPerDay;
        public int? CurrentEtaTicks => MovementComp.CurrentEtaTicks;
        public Vector3 CurrentVelocityDirection => MovementComp.CurrentVelocityDirection;
        public Vector3 CurrentSkyWorldPosition => MovementComp.CurrentSkyWorldPosition;
        public Vector3 CurrentSurfaceWorldPosition => MovementComp.CurrentSurfaceWorldPosition;
        public Vector2 CurrentSkyLongLat => MovementComp.CurrentSkyLongLat;
        public Vector2 CurrentSurfaceLongLat => MovementComp.CurrentSurfaceLongLat;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref altitude, "altitude", SkyIslandAltitude.DefaultAltitude);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && altitude > SkyIslandAltitude.MaxAltitude + 10f)
            {
                altitude = Mathf.Clamp(altitude - SkyIslandAltitude.SurfaceRadius, SkyIslandAltitude.MinAltitude, SkyIslandAltitude.MaxAltitude);
            }
        }

        public void EnsureSurfaceProjectionTile() => MovementComp.EnsureSurfaceProjectionTile();
        public PlanetTile GetSkyProjectionTile(PlanetTile surfaceTile) => MovementComp.GetSkyProjectionTile(surfaceTile);
        public bool CanAddWaypointAt(PlanetTile surfaceTile) => MovementComp.CanAddWaypointAt(surfaceTile);
        public bool TryAddPlannedSurfaceWaypoint(PlanetTile surfaceTile, float altitude, bool allowConsecutiveDuplicate = false) => MovementComp.TryAddPlannedSurfaceWaypoint(surfaceTile, altitude, allowConsecutiveDuplicate);
        public int MostRecentPlannedWaypointIndexAt(PlanetTile surfaceTile) => MovementComp.MostRecentPlannedWaypointIndexAt(surfaceTile);
        public bool TryRemovePlannedSurfaceWaypointAt(int index) => MovementComp.TryRemovePlannedSurfaceWaypointAt(index);
        public void ClearPlannedSurfaceWaypoints() => MovementComp.ClearPlannedSurfaceWaypoints();
        public bool StartEnginePreview() => MovementComp.StartEnginePreview();
        public bool PauseMovementPreview() => MovementComp.PauseMovementPreview();
        public void EnsureWaypointProjectionCache() => MovementComp.EnsureWaypointProjectionCache();
        public void SetGear(int gear) => MovementComp.SetGear(gear);
    }
}
