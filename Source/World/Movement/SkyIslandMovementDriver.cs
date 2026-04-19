using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SkyrimIslands.World.Movement;

namespace SkyrimIslands.World.Movement
{
    public class SkyIslandMovementDriver : IExposable
    {
        private readonly WorldObject parent;
        private readonly SkyIslandMovementPhysics physics = new SkyIslandMovementPhysics();

        private Vector3 currentDirection = Vector3.zero;
        private Vector3 currentVelocityDirection = Vector3.zero;
        private SkyIslandMapParent.SkyIslandMovementState movementState = SkyIslandMapParent.SkyIslandMovementState.Idle;

        private PlanetTile activeTargetSurfaceTile = PlanetTile.Invalid;
        private PlanetTile activeTargetSkyTile = PlanetTile.Invalid;

        private PlanetTile interruptionAnchorSurfaceTile = PlanetTile.Invalid;
        private PlanetTile interruptionAnchorSkyTile = PlanetTile.Invalid;
        private Vector3 interruptionDepartureDir = Vector3.zero;
        private float interruptionStartSpeed = 0f;
        private float interruptionTotalDistanceTiles = 0f;
        private float interruptionElapsedHours = 0f;

        private float elapsedHours = 0f;
        private float currentSpeedH = 0f;

        private bool isBrakingForShift = false;
        private float brakingTargetSpeed = 0f;
        private float brakeStartSpeed = 0f;
        private float brakeElapsedHours = 0f;

        public SkyIslandMovementDriver(WorldObject parent)
        {
            this.parent = parent;
        }

        public SkyIslandMapParent.SkyIslandMovementState MovementState => movementState;
        public bool IsMoveControlLocked => movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting;

        public bool HasActiveTarget => activeTargetSkyTile.Valid && activeTargetSurfaceTile.Valid;
        public Vector3 CurrentDirection => currentDirection;
        public Vector3 CurrentVelocityDirection => currentVelocityDirection;

        public float CurrentAltitude => physics.GetCurrentAltitude(elapsedHours);
        public float DepartureAltitude => physics.DepartureAltitude;
        public float TargetAltitude => physics.TargetAltitude;

        public SkyIslandMapParent.SkyIslandVerticalState VerticalState
        {
            get
            {
                if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
                    return SkyIslandMapParent.SkyIslandVerticalState.Holding;

                float delta = physics.TargetAltitude - physics.DepartureAltitude;
                if (Mathf.Abs(delta) < 0.001f)
                    return SkyIslandMapParent.SkyIslandVerticalState.Holding;

                return delta > 0f ? SkyIslandMapParent.SkyIslandVerticalState.Ascending : SkyIslandMapParent.SkyIslandVerticalState.Descending;
            }
        }

        public float CurrentSpeedTilesPerDay
        {
            get
            {
                if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
                    return 0f;

                return currentSpeedH * GenDate.TicksPerDay / SkyIslandMovementConstants.HoursToTicks;
            }
        }

        public int? CalculateEta()
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
                return null;

            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Braking)
            {
                float remainingBrakeHours = (brakeStartSpeed - brakingTargetSpeed) / SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq;
                remainingBrakeHours = Mathf.Max(0f, remainingBrakeHours - brakeElapsedHours);
                return Mathf.CeilToInt(remainingBrakeHours * SkyIslandMovementConstants.HoursToTicks);
            }

            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting)
            {
                float dockSpeed = SkyIslandMovementPhysics.GetDockSpeed();
                float brakeTotalDistance = GetBrakeDistance(interruptionStartSpeed, dockSpeed);

                if (interruptionTotalDistanceTiles <= brakeTotalDistance)
                {
                    float effectiveDecel = Mathf.Min(SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq,
                        (interruptionStartSpeed * interruptionStartSpeed) / Mathf.Max(0.0001f, 2f * interruptionTotalDistanceTiles));
                    float remainingStopHours = currentSpeedH / effectiveDecel;
                    return Mathf.CeilToInt(remainingStopHours * SkyIslandMovementConstants.HoursToTicks);
                }

                if (currentSpeedH <= dockSpeed + 0.01f)
                {
                    float driftDistance = interruptionTotalDistanceTiles - brakeTotalDistance;
                    if (driftDistance <= 0.01f || currentSpeedH <= 0.01f)
                        return 0;
                    float driftHours = driftDistance / Mathf.Max(0.01f, dockSpeed);
                    return Mathf.CeilToInt(driftHours * SkyIslandMovementConstants.HoursToTicks);
                }

                float brakeHours = currentSpeedH / SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq;
                float driftDistanceAfterBrake = interruptionTotalDistanceTiles - brakeTotalDistance;
                float totalHours = brakeHours + driftDistanceAfterBrake / Mathf.Max(0.01f, dockSpeed);
                return Mathf.CeilToInt(totalHours * SkyIslandMovementConstants.HoursToTicks);
            }

            float remainingHours = Mathf.Max(0f, physics.TotalDurationHours - elapsedHours);
            return Mathf.CeilToInt(remainingHours * SkyIslandMovementConstants.HoursToTicks);
        }

        public MovementTickOutput Tick(int ticks, SkyIslandWaypointPlanner planner, PlanetTile currentSurfaceProjectionTile)
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting && !planner.HasRoute)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            return AdvanceMovement(ticks, currentSurfaceProjectionTile);
        }

        public bool Start(PlanetTile surfaceTarget, PlanetTile skyTarget, int startGear, float startDepartureAltitude, float startTargetAltitude)
        {
            if (!skyTarget.Valid || movementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
                return false;

            int gear = Mathf.Clamp(startGear, 0, SkyIslandMovementConstants.Gears.Length - 1);
            activeTargetSurfaceTile = surfaceTarget;
            activeTargetSkyTile = skyTarget;
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            isBrakingForShift = false;
            elapsedHours = 0f;

            SkyIslandMovementGeometry.EnsureDirection(ref currentDirection, surfaceTarget, parent.Tile);
            Vector3 departureSkyDir = currentDirection.normalized;
            Vector3 targetSkyDir = Find.WorldGrid.GetTileCenter(skyTarget).normalized;

            float arcAngle = GenMath.SphericalDistance(departureSkyDir, targetSkyDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            float horizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            physics.Setup(horizontalDistanceTiles, gear, startTargetAltitude - startDepartureAltitude, 0f, departureSkyDir, targetSkyDir, startDepartureAltitude, startTargetAltitude);

            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Accelerating);
            return true;
        }

        public bool ChangeGear(int newGearIndex)
        {
            if (newGearIndex < 0 || newGearIndex >= SkyIslandMovementConstants.Gears.Length)
                return false;

            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Accelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Cruising)
            {
                return false;
            }

            if (!activeTargetSkyTile.Valid)
                return false;

            GearProfile newProfile = SkyIslandMovementConstants.Gears[newGearIndex];
            float newVMax = newProfile.MaxSpeedTilesPerHour;

            Vector3 departureSkyDir = currentDirection.normalized;
            float departureAltitude = CurrentAltitude;
            int gear = newGearIndex;
            elapsedHours = 0f;
            isBrakingForShift = false;

            Vector3 targetSkyDir = Find.WorldGrid.GetTileCenter(activeTargetSkyTile).normalized;
            float arcAngle = GenMath.SphericalDistance(departureSkyDir, targetSkyDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            float horizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            if (currentSpeedH > newVMax + 0.01f)
            {
                isBrakingForShift = true;
                brakingTargetSpeed = newVMax;
                brakeStartSpeed = currentSpeedH;
                brakeElapsedHours = 0f;
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Braking);
                return true;
            }

            float initialSpeedH = currentSpeedH;
            physics.Setup(horizontalDistanceTiles, gear, physics.TargetAltitude - departureAltitude, initialSpeedH, departureSkyDir, targetSkyDir, departureAltitude, physics.TargetAltitude);

            SetMovementState(physics.GetHorizontalStateAt(0f));
            return true;
        }

        public bool Interrupt(PlanetTile anchorSkyTile, PlanetTile anchorSurfaceTile)
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Docking)
                return false;

            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Accelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Cruising &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Decelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Braking)
            {
                return false;
            }

            interruptionAnchorSkyTile = anchorSkyTile;
            interruptionAnchorSurfaceTile = anchorSurfaceTile;
            interruptionDepartureDir = currentDirection.normalized;
            interruptionStartSpeed = currentSpeedH;
            interruptionElapsedHours = 0f;
            isBrakingForShift = false;

            Vector3 anchorDir = Find.WorldGrid.GetTileCenter(anchorSkyTile).normalized;
            float arcAngle = GenMath.SphericalDistance(interruptionDepartureDir, anchorDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            interruptionTotalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Interrupting);
            return true;
        }

        public void SetAltitudeContext(float departure, float target)
        {
            physics.SetAltitudeContext(departure, target);
        }

        public void Reset()
        {
            activeTargetSurfaceTile = PlanetTile.Invalid;
            activeTargetSkyTile = PlanetTile.Invalid;
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            isBrakingForShift = false;
            currentSpeedH = 0f;
            currentVelocityDirection = Vector3.zero;
            elapsedHours = 0f;
            physics.Reset();
            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref movementState, "movementState", SkyIslandMapParent.SkyIslandMovementState.Idle);
            Scribe_Values.Look(ref currentDirection, "currentDirection", Vector3.zero);
            Scribe_Values.Look(ref activeTargetSurfaceTile, "activeTargetSurfaceTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref activeTargetSkyTile, "activeTargetSkyTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref interruptionAnchorSurfaceTile, "interruptionAnchorSurfaceTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref interruptionAnchorSkyTile, "interruptionAnchorSkyTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref interruptionDepartureDir, "interruptionDepartureDir", Vector3.zero);
            Scribe_Values.Look(ref interruptionStartSpeed, "interruptionStartSpeed", 0f);
            Scribe_Values.Look(ref interruptionElapsedHours, "interruptionElapsedHours", 0f);
            Scribe_Values.Look(ref elapsedHours, "elapsedHours", 0f);
            Scribe_Values.Look(ref currentSpeedH, "currentSpeedH", 0f);
            Scribe_Values.Look(ref isBrakingForShift, "isBrakingForShift", false);
            Scribe_Values.Look(ref brakingTargetSpeed, "brakingTargetSpeed", 0f);
            Scribe_Values.Look(ref brakeStartSpeed, "brakeStartSpeed", 0f);
            Scribe_Values.Look(ref brakeElapsedHours, "brakeElapsedHours", 0f);

            physics.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                currentVelocityDirection = Vector3.zero;

                if (movementState != SkyIslandMapParent.SkyIslandMovementState.Idle &&
                    movementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting &&
                    !HasActiveTarget)
                {
                    Reset();
                }
                else
                {
                    RebuildDerivedState();
                }
            }
        }

        private void RebuildDerivedState()
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting)
            {
                if (interruptionAnchorSkyTile.Valid)
                {
                    float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
                    Vector3 anchorDir = Find.WorldGrid.GetTileCenter(interruptionAnchorSkyTile).normalized;
                    float arcAngle = GenMath.SphericalDistance(interruptionDepartureDir, anchorDir);
                    interruptionTotalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;
                }
            }
            else
            {
                physics.PostLoadInit(movementState, parent.Tile);
            }
        }

        private MovementTickOutput AdvanceMovement(int ticks, PlanetTile currentSurfaceProjectionTile)
        {
            if (ticks <= 0)
            {
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            SkyIslandMovementGeometry.EnsureDirection(ref currentDirection, currentSurfaceProjectionTile, parent.Tile);

            switch (movementState)
            {
                case SkyIslandMapParent.SkyIslandMovementState.Accelerating:
                case SkyIslandMapParent.SkyIslandMovementState.Cruising:
                case SkyIslandMapParent.SkyIslandMovementState.Decelerating:
                case SkyIslandMapParent.SkyIslandMovementState.Docking:
                    return AdvanceTowardsTarget(ticks);
                case SkyIslandMapParent.SkyIslandMovementState.Braking:
                    return AdvanceBraking(ticks);
                case SkyIslandMapParent.SkyIslandMovementState.Interrupting:
                    return AdvanceTowardsInterruptionAnchor(ticks);
            }

            return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
        }

        private MovementTickOutput AdvanceTowardsTarget(int ticks)
        {
            if (!activeTargetSkyTile.Valid || !activeTargetSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, PlanetTile.Invalid);
            }

            float hours = ticks / SkyIslandMovementConstants.HoursToTicks;
            elapsedHours += hours;

            if (physics.Evaluate(elapsedHours, out currentDirection, out currentVelocityDirection, out currentSpeedH, out movementState))
            {
                parent.Tile = activeTargetSkyTile;
                PlanetTile arrivedSurfaceTile = activeTargetSurfaceTile;
                ClearTarget();
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                return new MovementTickOutput(MovementTickResult.Arrived, arrivedSurfaceTile);
            }

            PlanetTile newSurfaceProjection = SkyIslandMovementGeometry.RecalculateSurfaceProjection(parent, PlanetTile.Invalid, currentDirection);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection);
        }

        private MovementTickOutput AdvanceBraking(int ticks)
        {
            if (!activeTargetSkyTile.Valid || !activeTargetSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, PlanetTile.Invalid);
            }

            float hours = ticks / SkyIslandMovementConstants.HoursToTicks;
            brakeElapsedHours += hours;

            float brakeTotalHours = (brakeStartSpeed - brakingTargetSpeed) / SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq;
            currentSpeedH = Mathf.Max(brakingTargetSpeed, brakeStartSpeed - SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq * brakeElapsedHours);

            float distanceTraveled = (brakeStartSpeed + currentSpeedH) / 2f * brakeElapsedHours;
            float brakeTotalDistance = (brakeStartSpeed + brakingTargetSpeed) / 2f * brakeTotalHours;

            Vector3 targetDir = Find.WorldGrid.GetTileCenter(activeTargetSkyTile).normalized;
            float t = brakeTotalDistance > 0f ? distanceTraveled / brakeTotalDistance : 1f;
            currentDirection = Vector3.Slerp(physics.DepartureSkyDir, targetDir, Mathf.Clamp01(t)).normalized;

            Vector3 axisB = Vector3.Cross(physics.DepartureSkyDir, targetDir);
            currentVelocityDirection = axisB.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axisB, currentDirection).normalized
                : Vector3.zero;

            if (currentSpeedH <= brakingTargetSpeed + 0.01f || brakeElapsedHours >= brakeTotalHours)
            {
                currentSpeedH = brakingTargetSpeed;
                Vector3 departureSkyDir = currentDirection.normalized;
                float departureAltitude = CurrentAltitude;
                brakeElapsedHours = 0f;
                isBrakingForShift = false;

                Vector3 targetSkyDir = Find.WorldGrid.GetTileCenter(activeTargetSkyTile).normalized;
                float arcAngle = GenMath.SphericalDistance(departureSkyDir, targetSkyDir);
                float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
                float horizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

                float initialSpeedH = brakingTargetSpeed;
                physics.Setup(horizontalDistanceTiles, physics.GearIndex, physics.TargetAltitude - departureAltitude, initialSpeedH, departureSkyDir, targetSkyDir, departureAltitude, physics.TargetAltitude);

                SetMovementState(physics.GetHorizontalStateAt(0f));
            }

            PlanetTile newSurfaceProjection = SkyIslandMovementGeometry.RecalculateSurfaceProjection(parent, PlanetTile.Invalid, currentDirection);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection);
        }

        private MovementTickOutput AdvanceTowardsInterruptionAnchor(int ticks)
        {
            if (!interruptionAnchorSkyTile.Valid || !interruptionAnchorSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, PlanetTile.Invalid);
            }

            float hours = ticks / SkyIslandMovementConstants.HoursToTicks;
            interruptionElapsedHours += hours;

            Vector3 anchorDir = Find.WorldGrid.GetTileCenter(interruptionAnchorSkyTile).normalized;
            float dockSpeed = SkyIslandMovementPhysics.GetDockSpeed();

            float brakeTotalHours = (interruptionStartSpeed - dockSpeed) / SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq;
            float brakeTotalDistance = GetBrakeDistance(interruptionStartSpeed, dockSpeed);

            float distanceTraveled;
            if (interruptionTotalDistanceTiles <= brakeTotalDistance)
            {
                float effectiveDecel = Mathf.Min(SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq,
                    (interruptionStartSpeed * interruptionStartSpeed) / Mathf.Max(0.0001f, 2f * interruptionTotalDistanceTiles));
                float tStop = interruptionStartSpeed / effectiveDecel;
                if (interruptionElapsedHours >= tStop)
                {
                    currentDirection = anchorDir;
                    currentSpeedH = 0f;
                    parent.Tile = interruptionAnchorSkyTile;
                    PlanetTile interruptedSurfaceTile = interruptionAnchorSurfaceTile;
                    ClearInterruption();
                    SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                    return new MovementTickOutput(MovementTickResult.Interrupted, interruptedSurfaceTile);
                }
                currentSpeedH = Mathf.Max(0f, interruptionStartSpeed - effectiveDecel * interruptionElapsedHours);
                distanceTraveled = (interruptionStartSpeed + currentSpeedH) / 2f * interruptionElapsedHours;
            }
            else
            {
                if (interruptionElapsedHours < brakeTotalHours)
                {
                    currentSpeedH = Mathf.Max(dockSpeed, interruptionStartSpeed - SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq * interruptionElapsedHours);
                    distanceTraveled = (interruptionStartSpeed + currentSpeedH) / 2f * interruptionElapsedHours;
                }
                else
                {
                    float driftElapsed = interruptionElapsedHours - brakeTotalHours;
                    currentSpeedH = dockSpeed;
                    distanceTraveled = brakeTotalDistance + dockSpeed * driftElapsed;
                }

                float t = interruptionTotalDistanceTiles > 0f ? distanceTraveled / interruptionTotalDistanceTiles : 1f;

                if (t >= 1f || currentSpeedH <= 0.01f)
                {
                    currentDirection = anchorDir;
                    currentSpeedH = 0f;
                    parent.Tile = interruptionAnchorSkyTile;
                    PlanetTile interruptedSurfaceTile = interruptionAnchorSurfaceTile;
                    ClearInterruption();
                    SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                    return new MovementTickOutput(MovementTickResult.Interrupted, interruptedSurfaceTile);
                }
            }

            float tDir = interruptionTotalDistanceTiles > 0f ? distanceTraveled / interruptionTotalDistanceTiles : 1f;
            currentDirection = Vector3.Slerp(interruptionDepartureDir, anchorDir, Mathf.Clamp01(tDir)).normalized;

            Vector3 axisI = Vector3.Cross(interruptionDepartureDir, anchorDir);
            currentVelocityDirection = axisI.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axisI, currentDirection).normalized
                : Vector3.zero;

            PlanetTile newSurfaceProjection = SkyIslandMovementGeometry.RecalculateSurfaceProjection(parent, PlanetTile.Invalid, currentDirection);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection);
        }

        private static float GetBrakeDistance(float startSpeed, float endSpeed)
        {
            float t = (startSpeed - endSpeed) / SkyIslandMovementConstants.BrakeAccelerationTilesPerHourSq;
            return (startSpeed + endSpeed) / 2f * t;
        }

        private void SetMovementState(SkyIslandMapParent.SkyIslandMovementState newState)
        {
            if (movementState == newState)
                return;

            movementState = newState;
        }

        private void ClearTarget()
        {
            activeTargetSurfaceTile = PlanetTile.Invalid;
            activeTargetSkyTile = PlanetTile.Invalid;
        }

        private void ClearInterruption()
        {
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
        }

    }
}
