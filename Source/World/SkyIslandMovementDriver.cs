using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public enum MovementTickResult
    {
        None,
        Arrived,
        Interrupted
    }

    public readonly struct MovementTickOutput
    {
        public readonly MovementTickResult Result;
        public readonly PlanetTile NewSurfaceProjectionTile;
        public readonly float Progress;

        public MovementTickOutput(MovementTickResult result, PlanetTile newSurfaceProjectionTile, float progress = 0f)
        {
            Result = result;
            NewSurfaceProjectionTile = newSurfaceProjectionTile;
            Progress = progress;
        }
    }

    public class SkyIslandMovementDriver : IExposable
    {
        private static readonly List<PlanetTile> tmpNeighbors = new List<PlanetTile>();

        private readonly WorldObject parent;

        private Vector3 currentDirection = Vector3.zero;
        private SkyIslandMapParent.SkyIslandMovementState movementState = SkyIslandMapParent.SkyIslandMovementState.Idle;

        private PlanetTile activeTargetSurfaceTile = PlanetTile.Invalid;
        private PlanetTile activeTargetSkyTile = PlanetTile.Invalid;

        private PlanetTile interruptionAnchorSurfaceTile = PlanetTile.Invalid;
        private PlanetTile interruptionAnchorSkyTile = PlanetTile.Invalid;
        private Vector3 interruptionDepartureDir = Vector3.zero;
        private float interruptionStartSpeed = 0f;
        private float interruptionTotalDistanceTiles = 0f;
        private float interruptionElapsedHours = 0f;

        private Vector3 departureSkyDir = Vector3.zero;
        private Vector3 targetSkyDir = Vector3.zero;
        private float departureAltitude = SkyIslandAltitude.DefaultAltitude;
        private float targetAltitude = SkyIslandAltitude.DefaultAltitude;
        private int gearIndex = 1;
        private float totalDurationHours = 0f;
        private float elapsedHours = 0f;
        private float scaledMaxSpeedH = 0f;
        private float scaledAccelH = 0f;
        private float verticalSpeed = 0f;
        private float normalDurationHours = 0f;
        private float horizontalDistanceTiles = 0f;
        private float currentSpeedH = 0f;

        public SkyIslandMovementDriver(WorldObject parent)
        {
            this.parent = parent;
        }

        public SkyIslandMapParent.SkyIslandMovementState MovementState => movementState;
        public bool IsMoveControlLocked => movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting;
        public bool IsPreparingToDock => movementState == SkyIslandMapParent.SkyIslandMovementState.Docking;

        public bool HasActiveTarget => activeTargetSkyTile.Valid && activeTargetSurfaceTile.Valid;
        public bool HasInterruptionAnchor => interruptionAnchorSkyTile.Valid && interruptionAnchorSurfaceTile.Valid;
        public Vector3 CurrentDirection => currentDirection == Vector3.zero ? Vector3.zero : currentDirection.normalized;

        public float CurrentAltitude => Mathf.Lerp(departureAltitude, targetAltitude, totalDurationHours > 0f ? Mathf.Clamp01(elapsedHours / totalDurationHours) : 0f);
        public float DepartureAltitude => departureAltitude;
        public float TargetAltitude => targetAltitude;

        public SkyIslandMapParent.SkyIslandVerticalState VerticalState
        {
            get
            {
                if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
                    return SkyIslandMapParent.SkyIslandVerticalState.Holding;

                float delta = targetAltitude - departureAltitude;
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

            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting)
            {
                if (currentSpeedH <= 0.01f)
                    return 0;

                float decel = SkyIslandMovementConstants.Gears[gearIndex].AccelerationTilesPerHourSq;
                float interruptionHours = currentSpeedH / decel;
                return Mathf.CeilToInt(interruptionHours * SkyIslandMovementConstants.HoursToTicks);
            }

            float remainingHours = Mathf.Max(0f, totalDurationHours - elapsedHours);
            return Mathf.CeilToInt(remainingHours * SkyIslandMovementConstants.HoursToTicks);
        }

        public Vector3 GetSkyWorldPosition(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            float radius = SkyIslandAltitude.SurfaceRadius + ((SkyIslandMapParent)parent).Altitude;
            Vector3 direction = currentDirection == Vector3.zero ? Find.WorldGrid.GetTileCenter(parent.Tile).normalized : currentDirection.normalized;
            return parent.Tile.Layer.Origin + direction * radius;
        }

        public Vector3 GetSurfaceWorldPosition(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
                return Vector3.zero;

            return GetWorldPositionOnLayer(surfaceLayer);
        }

        public Vector2 GetSkyLongLat(PlanetTile fallbackSurfaceProjectionTile)
        {
            return GetLongLatOnLayer(parent.Tile.Layer, GetSkyWorldPosition(fallbackSurfaceProjectionTile));
        }

        public Vector2 GetSurfaceLongLat(PlanetTile fallbackSurfaceProjectionTile)
        {
            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
                return Vector2.zero;

            return GetLongLatOnLayer(surfaceLayer, GetSurfaceWorldPosition(fallbackSurfaceProjectionTile));
        }

        public bool GetIsCenteredOnCurrentTile(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            if (!parent.Tile.Valid || currentDirection == Vector3.zero)
                return true;

            return Vector3.Dot(currentDirection.normalized, Find.WorldGrid.GetTileCenter(parent.Tile).normalized) >= SkyIslandMovementConstants.AnchorSnapDotThreshold;
        }

        public MovementTickOutput Tick(int ticks, SkyIslandWaypointPlanner planner, PlanetTile currentSurfaceProjectionTile)
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile, 1f);
            }

            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting && !planner.HasRoute)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile, 1f);
            }

            return AdvanceMovement(ticks, planner, currentSurfaceProjectionTile);
        }

        public bool Start(PlanetTile surfaceTarget, PlanetTile skyTarget, int startGear, float startDepartureAltitude, float startTargetAltitude)
        {
            if (!skyTarget.Valid || movementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
                return false;

            gearIndex = Mathf.Clamp(startGear, 0, SkyIslandMovementConstants.Gears.Length - 1);
            activeTargetSurfaceTile = surfaceTarget;
            activeTargetSkyTile = skyTarget;
            departureAltitude = startDepartureAltitude;
            targetAltitude = startTargetAltitude;
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            elapsedHours = 0f;

            EnsureCurrentDirection(surfaceTarget);
            departureSkyDir = currentDirection.normalized;
            targetSkyDir = Find.WorldGrid.GetTileCenter(skyTarget).normalized;

            float arcAngle = GenMath.SphericalDistance(departureSkyDir, targetSkyDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            horizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            CalculateMovementProfile(horizontalDistanceTiles, gearIndex, targetAltitude - departureAltitude,
                out totalDurationHours, out normalDurationHours, out scaledMaxSpeedH, out scaledAccelH, out verticalSpeed);

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

            departureSkyDir = currentDirection.normalized;
            departureAltitude = CurrentAltitude;
            gearIndex = newGearIndex;
            elapsedHours = 0f;

            targetSkyDir = Find.WorldGrid.GetTileCenter(activeTargetSkyTile).normalized;
            float arcAngle = GenMath.SphericalDistance(departureSkyDir, targetSkyDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            horizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            CalculateMovementProfile(horizontalDistanceTiles, gearIndex, targetAltitude - departureAltitude,
                out totalDurationHours, out normalDurationHours, out scaledMaxSpeedH, out scaledAccelH, out verticalSpeed);

            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Accelerating);
            return true;
        }

        public bool Interrupt(PlanetTile anchorSkyTile, PlanetTile anchorSurfaceTile)
        {
            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Accelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Cruising &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Decelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Docking)
            {
                return false;
            }

            interruptionAnchorSkyTile = anchorSkyTile;
            interruptionAnchorSurfaceTile = anchorSurfaceTile;
            interruptionDepartureDir = currentDirection.normalized;
            interruptionStartSpeed = currentSpeedH;
            interruptionElapsedHours = 0f;

            Vector3 anchorDir = Find.WorldGrid.GetTileCenter(anchorSkyTile).normalized;
            float arcAngle = GenMath.SphericalDistance(interruptionDepartureDir, anchorDir);
            float avgTileSize = parent.Tile.Valid ? parent.Tile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
            interruptionTotalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Interrupting);
            return true;
        }

        public void SetAltitudeContext(float departure, float target)
        {
            departureAltitude = departure;
            targetAltitude = target;
        }

        public void Reset()
        {
            activeTargetSurfaceTile = PlanetTile.Invalid;
            activeTargetSkyTile = PlanetTile.Invalid;
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            currentSpeedH = 0f;
            elapsedHours = 0f;
            totalDurationHours = 0f;
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
            Scribe_Values.Look(ref interruptionTotalDistanceTiles, "interruptionTotalDistanceTiles", 0f);
            Scribe_Values.Look(ref interruptionElapsedHours, "interruptionElapsedHours", 0f);
            Scribe_Values.Look(ref departureSkyDir, "departureSkyDir", Vector3.zero);
            Scribe_Values.Look(ref targetSkyDir, "targetSkyDir", Vector3.zero);
            Scribe_Values.Look(ref departureAltitude, "departureAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref targetAltitude, "targetAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref gearIndex, "gearIndex", 1);
            Scribe_Values.Look(ref totalDurationHours, "totalDurationHours", 0f);
            Scribe_Values.Look(ref elapsedHours, "elapsedHours", 0f);
            Scribe_Values.Look(ref scaledMaxSpeedH, "scaledMaxSpeedH", 0f);
            Scribe_Values.Look(ref scaledAccelH, "scaledAccelH", 0f);
            Scribe_Values.Look(ref verticalSpeed, "verticalSpeed", 0f);
            Scribe_Values.Look(ref normalDurationHours, "normalDurationHours", 0f);
            Scribe_Values.Look(ref horizontalDistanceTiles, "horizontalDistanceTiles", 0f);
            Scribe_Values.Look(ref currentSpeedH, "currentSpeedH", 0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (movementState != SkyIslandMapParent.SkyIslandMovementState.Idle &&
                    movementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting &&
                    !HasActiveTarget)
                {
                    Reset();
                }
            }
        }

        private MovementTickOutput AdvanceMovement(int ticks, SkyIslandWaypointPlanner planner, PlanetTile currentSurfaceProjectionTile)
        {
            if (ticks <= 0)
            {
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile,
                    movementState == SkyIslandMapParent.SkyIslandMovementState.Idle ? 1f : 0f);
            }

            EnsureCurrentDirection(currentSurfaceProjectionTile);

            switch (movementState)
            {
                case SkyIslandMapParent.SkyIslandMovementState.Accelerating:
                case SkyIslandMapParent.SkyIslandMovementState.Cruising:
                case SkyIslandMapParent.SkyIslandMovementState.Decelerating:
                case SkyIslandMapParent.SkyIslandMovementState.Docking:
                    return AdvanceTowardsTarget(ticks, currentSurfaceProjectionTile);
                case SkyIslandMapParent.SkyIslandMovementState.Interrupting:
                    return AdvanceTowardsInterruptionAnchor(ticks, currentSurfaceProjectionTile);
            }

            return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile, 0f);
        }

        private MovementTickOutput AdvanceTowardsTarget(int ticks, PlanetTile currentSurfaceProjectionTile)
        {
            if (!activeTargetSkyTile.Valid || !activeTargetSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile, 1f);
            }

            float hours = ticks / SkyIslandMovementConstants.HoursToTicks;
            elapsedHours += hours;

            if (elapsedHours >= totalDurationHours)
            {
                currentDirection = targetSkyDir;
                currentSpeedH = 0f;
                parent.Tile = activeTargetSkyTile;
                PlanetTile arrivedSurfaceTile = activeTargetSurfaceTile;
                ClearTarget();
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                return new MovementTickOutput(MovementTickResult.Arrived, arrivedSurfaceTile, 1f);
            }

            float normalDistance = horizontalDistanceTiles > SkyIslandMovementConstants.DockDistanceThreshold
                ? horizontalDistanceTiles - SkyIslandMovementConstants.DockDistanceThreshold
                : 0f;

            float distanceTraveled;
            if (elapsedHours < normalDurationHours)
            {
                distanceTraveled = GetNormalDistanceAt(elapsedHours, normalDistance);
            }
            else
            {
                float dockElapsed = elapsedHours - normalDurationHours;
                float dockProgress = Mathf.Clamp01(dockElapsed / SkyIslandMovementConstants.DockDurationHours);
                float dockDistance = Mathf.Min(horizontalDistanceTiles, SkyIslandMovementConstants.DockDistanceThreshold);
                distanceTraveled = normalDistance + dockProgress * dockDistance;
            }

            float t = horizontalDistanceTiles > 0f ? distanceTraveled / horizontalDistanceTiles : 1f;
            currentDirection = Vector3.Slerp(departureSkyDir, targetSkyDir, Mathf.Clamp01(t)).normalized;
            currentSpeedH = GetHorizontalSpeedAt(elapsedHours, normalDistance);

            if (elapsedHours >= normalDurationHours)
            {
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Docking);
            }
            else
            {
                SetMovementState(GetHorizontalStateAt(elapsedHours, normalDistance));
            }

            PlanetTile newSurfaceProjection = RecalculateSurfaceProjection(currentSurfaceProjectionTile);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection, t);
        }

        private MovementTickOutput AdvanceTowardsInterruptionAnchor(int ticks, PlanetTile currentSurfaceProjectionTile)
        {
            if (!interruptionAnchorSkyTile.Valid || !interruptionAnchorSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile, 1f);
            }

            float hours = ticks / SkyIslandMovementConstants.HoursToTicks;
            interruptionElapsedHours += hours;

            Vector3 anchorDir = Find.WorldGrid.GetTileCenter(interruptionAnchorSkyTile).normalized;
            float decel = SkyIslandMovementConstants.Gears[gearIndex].AccelerationTilesPerHourSq;

            currentSpeedH = Mathf.Max(0f, interruptionStartSpeed - decel * interruptionElapsedHours);
            float distanceTraveled = interruptionStartSpeed * interruptionElapsedHours - 0.5f * decel * interruptionElapsedHours * interruptionElapsedHours;

            float t = interruptionTotalDistanceTiles > 0f ? distanceTraveled / interruptionTotalDistanceTiles : 1f;
            currentDirection = Vector3.Slerp(interruptionDepartureDir, anchorDir, Mathf.Clamp01(t)).normalized;

            if (t >= 1f || currentSpeedH <= 0.01f)
            {
                currentDirection = anchorDir;
                currentSpeedH = 0f;
                parent.Tile = interruptionAnchorSkyTile;
                PlanetTile interruptedSurfaceTile = interruptionAnchorSurfaceTile;
                ClearInterruption();
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                return new MovementTickOutput(MovementTickResult.Interrupted, interruptedSurfaceTile, 1f);
            }

            PlanetTile newSurfaceProjection = RecalculateSurfaceProjection(currentSurfaceProjectionTile);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection, t);
        }

        private void CalculateMovementProfile(float distanceTiles, int gear, float deltaAltitude,
            out float outTotalDuration, out float outNormalDuration, out float outScaledMaxSpeed,
            out float outScaledAccel, out float outVerticalSpeed)
        {
            GearProfile profile = SkyIslandMovementConstants.Gears[gear];
            float vMax = profile.MaxSpeedTilesPerHour;
            float a = profile.AccelerationTilesPerHourSq;

            float normalDistance = distanceTiles > SkyIslandMovementConstants.DockDistanceThreshold
                ? distanceTiles - SkyIslandMovementConstants.DockDistanceThreshold
                : 0f;

            float tAcc = vMax / a;
            float dAcc = 0.5f * a * tAcc * tAcc;
            float tNormal;
            if (normalDistance <= 2f * dAcc)
            {
                tNormal = 2f * Mathf.Sqrt(normalDistance / a);
            }
            else
            {
                tNormal = 2f * tAcc + (normalDistance - 2f * dAcc) / vMax;
            }

            float tH = tNormal + SkyIslandMovementConstants.DockDurationHours;
            float tV = Mathf.Abs(deltaAltitude) / SkyIslandMovementConstants.VerticalSpeedKmPerHour;

            if (tH >= tV || tV <= 0f)
            {
                outTotalDuration = tH;
                outNormalDuration = tNormal;
                outScaledMaxSpeed = vMax;
                outScaledAccel = a;
                outVerticalSpeed = tH > 0f ? Mathf.Abs(deltaAltitude) / tH : 0f;
            }
            else
            {
                float scale = tH / tV;
                outTotalDuration = tV;
                outScaledMaxSpeed = vMax * scale;
                outScaledAccel = a * scale;

                float tAccScaled = outScaledMaxSpeed / outScaledAccel;
                float dAccScaled = 0.5f * outScaledAccel * tAccScaled * tAccScaled;
                if (normalDistance <= 2f * dAccScaled)
                {
                    outNormalDuration = 2f * Mathf.Sqrt(normalDistance / outScaledAccel);
                }
                else
                {
                    outNormalDuration = 2f * tAccScaled + (normalDistance - 2f * dAccScaled) / outScaledMaxSpeed;
                }

                outVerticalSpeed = SkyIslandMovementConstants.VerticalSpeedKmPerHour;
            }
        }

        private float GetNormalDistanceAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours <= 0f)
                return 0f;

            if (elapsedHours >= normalDurationHours)
                return normalDistance;

            float tAcc = scaledMaxSpeedH / scaledAccelH;
            float dAcc = 0.5f * scaledAccelH * tAcc * tAcc;

            if (normalDistance <= 2f * dAcc)
            {
                float halfTime = normalDurationHours / 2f;
                if (elapsedHours <= halfTime)
                {
                    return 0.5f * scaledAccelH * elapsedHours * elapsedHours;
                }
                else
                {
                    float tDec = elapsedHours - halfTime;
                    float peakSpeed = scaledAccelH * halfTime;
                    float distAtPeak = 0.5f * normalDistance;
                    return distAtPeak + peakSpeed * tDec - 0.5f * scaledAccelH * tDec * tDec;
                }
            }
            else
            {
                float tCruise = normalDurationHours - 2f * tAcc;
                if (elapsedHours <= tAcc)
                {
                    return 0.5f * scaledAccelH * elapsedHours * elapsedHours;
                }
                else if (elapsedHours <= tAcc + tCruise)
                {
                    return dAcc + scaledMaxSpeedH * (elapsedHours - tAcc);
                }
                else
                {
                    float tDec = elapsedHours - tAcc - tCruise;
                    return normalDistance - dAcc + scaledMaxSpeedH * tDec - 0.5f * scaledAccelH * tDec * tDec;
                }
            }
        }

        private float GetHorizontalSpeedAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours >= normalDurationHours)
            {
                float dockDistance = Mathf.Min(horizontalDistanceTiles, SkyIslandMovementConstants.DockDistanceThreshold);
                return dockDistance / SkyIslandMovementConstants.DockDurationHours;
            }

            float tAcc = scaledMaxSpeedH / scaledAccelH;
            float dAcc = 0.5f * scaledAccelH * tAcc * tAcc;

            if (normalDistance <= 2f * dAcc)
            {
                float halfTime = normalDurationHours / 2f;
                if (elapsedHours < halfTime)
                    return scaledAccelH * elapsedHours;
                else
                    return scaledAccelH * (normalDurationHours - elapsedHours);
            }
            else
            {
                float tCruise = normalDurationHours - 2f * tAcc;
                if (elapsedHours < tAcc)
                    return scaledAccelH * elapsedHours;
                else if (elapsedHours < tAcc + tCruise)
                    return scaledMaxSpeedH;
                else
                    return scaledAccelH * (normalDurationHours - elapsedHours);
            }
        }

        private SkyIslandMapParent.SkyIslandMovementState GetHorizontalStateAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours >= normalDurationHours)
                return SkyIslandMapParent.SkyIslandMovementState.Decelerating;

            float tAcc = scaledMaxSpeedH / scaledAccelH;
            float dAcc = 0.5f * scaledAccelH * tAcc * tAcc;

            if (normalDistance <= 2f * dAcc)
            {
                float halfTime = normalDurationHours / 2f;
                if (elapsedHours < halfTime)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
            }
            else
            {
                float tCruise = normalDurationHours - 2f * tAcc;
                if (elapsedHours < tAcc)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else if (elapsedHours < tAcc + tCruise)
                    return SkyIslandMapParent.SkyIslandMovementState.Cruising;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
            }
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

        private PlanetTile RecalculateSurfaceProjection(PlanetTile fallbackSurfaceProjectionTile)
        {
            if (currentDirection == Vector3.zero)
            {
                return fallbackSurfaceProjectionTile;
            }

            parent.Tile = FindClosestNeighboringTile(parent.Tile, parent.Tile.Layer, currentDirection);

            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer != null)
            {
                PlanetTile anchorSurfaceTile = fallbackSurfaceProjectionTile.Valid
                    ? fallbackSurfaceProjectionTile
                    : surfaceLayer.GetClosestTile_NewTemp(parent.Tile, false);
                return FindClosestNeighboringTile(anchorSurfaceTile, surfaceLayer, currentDirection);
            }

            return fallbackSurfaceProjectionTile;
        }

        private static PlanetTile FindClosestNeighboringTile(PlanetTile currentTile, PlanetLayer layer, Vector3 direction)
        {
            if (!currentTile.Valid || currentTile.Layer != layer)
            {
                return currentTile;
            }

            PlanetTile bestTile = currentTile;
            float bestDot = Vector3.Dot(Find.WorldGrid.GetTileCenter(currentTile).normalized, direction);

            bool improved;
            int safety = 8;
            do
            {
                improved = false;
                tmpNeighbors.Clear();
                layer.GetTileNeighbors(bestTile, tmpNeighbors);
                for (int i = 0; i < tmpNeighbors.Count; i++)
                {
                    float dot = Vector3.Dot(Find.WorldGrid.GetTileCenter(tmpNeighbors[i]).normalized, direction);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestTile = tmpNeighbors[i];
                        improved = true;
                    }
                }
            }
            while (improved && --safety > 0);

            return bestTile;
        }

        private void EnsureCurrentDirection(PlanetTile fallbackSurfaceProjectionTile)
        {
            if (currentDirection != Vector3.zero)
            {
                return;
            }

            PlanetTile anchorTile = fallbackSurfaceProjectionTile.Valid ? fallbackSurfaceProjectionTile : parent.Tile;
            if (!anchorTile.Valid)
            {
                return;
            }

            currentDirection = Find.WorldGrid.GetTileCenter(anchorTile).normalized;
        }

        private Vector3 GetWorldPositionOnLayer(PlanetLayer layer)
        {
            Vector3 direction = currentDirection == Vector3.zero ? Find.WorldGrid.GetTileCenter(parent.Tile).normalized : currentDirection.normalized;
            return layer.Origin + direction * layer.Radius;
        }

        private static Vector2 GetLongLatOnLayer(PlanetLayer layer, Vector3 worldPosition)
        {
            Vector3 local = worldPosition - layer.Origin;
            if (local == Vector3.zero)
            {
                return Vector2.zero;
            }

            float magnitude = local.magnitude;
            float longitude = Mathf.Atan2(local.x, -local.z) * 57.29578f;
            float latitude = Mathf.Asin(local.y / magnitude) * 57.29578f;
            return new Vector2(longitude, latitude);
        }
    }
}
