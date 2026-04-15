using RimWorld.Planet;
using UnityEngine;

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
            Interrupting
        }

        public const int MaxPlannedWaypointCount = 10;

        private CompSkyIslandMovement MovementComp => GetComponent<CompSkyIslandMovement>();

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

        public PlanetTile SurfaceProjectionTile => MovementComp.SurfaceProjectionTile;
        public override Vector3 DrawPos => MovementComp.CurrentSkyWorldPosition;
        public override Vector3 WorldCameraPosition => MovementComp.CurrentSkyWorldPosition;
        public System.Collections.Generic.IReadOnlyList<PlanetTile> PlannedSurfaceWaypoints => MovementComp.PlannedSurfaceWaypoints;
        public System.Collections.Generic.IReadOnlyList<PlanetTile> PlannedSkyWaypoints => MovementComp.PlannedSkyWaypoints;
        public bool HasPlannedRoute => MovementComp.HasPlannedRoute;
        public bool IsCenteredOnCurrentTile => MovementComp.IsCenteredOnCurrentTile;
        public SkyIslandMovementState MovementState => MovementComp.MovementState;
        public bool IsMoveControlLocked => MovementComp.IsMoveControlLocked;
        public bool IsPreparingToDock => MovementComp.IsPreparingToDock;
        public float CurrentSpeedTilesPerDay => MovementComp.CurrentSpeedTilesPerDay;
        public int? CurrentEtaTicks => MovementComp.CurrentEtaTicks;
        public Vector3 CurrentSkyWorldPosition => MovementComp.CurrentSkyWorldPosition;
        public Vector3 CurrentSurfaceWorldPosition => MovementComp.CurrentSurfaceWorldPosition;
        public Vector2 CurrentSkyLongLat => MovementComp.CurrentSkyLongLat;
        public Vector2 CurrentSurfaceLongLat => MovementComp.CurrentSurfaceLongLat;

        public void EnsureSurfaceProjectionTile() => MovementComp.EnsureSurfaceProjectionTile();
        public PlanetTile GetSkyProjectionTile(PlanetTile surfaceTile) => MovementComp.GetSkyProjectionTile(surfaceTile);
        public bool CanAddWaypointAt(PlanetTile surfaceTile) => MovementComp.CanAddWaypointAt(surfaceTile);
        public bool TryAddPlannedSurfaceWaypoint(PlanetTile surfaceTile, bool allowConsecutiveDuplicate = false) => MovementComp.TryAddPlannedSurfaceWaypoint(surfaceTile, allowConsecutiveDuplicate);
        public int MostRecentPlannedWaypointIndexAt(PlanetTile surfaceTile) => MovementComp.MostRecentPlannedWaypointIndexAt(surfaceTile);
        public bool TryRemovePlannedSurfaceWaypointAt(int index) => MovementComp.TryRemovePlannedSurfaceWaypointAt(index);
        public void ClearPlannedSurfaceWaypoints() => MovementComp.ClearPlannedSurfaceWaypoints();
        public bool StartEnginePreview() => MovementComp.StartEnginePreview();
        public bool PauseMovementPreview() => MovementComp.PauseMovementPreview();
        public void EnsureWaypointProjectionCache() => MovementComp.EnsureWaypointProjectionCache();
    }
}
