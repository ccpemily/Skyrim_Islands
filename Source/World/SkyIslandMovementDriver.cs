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

        public MovementTickOutput(MovementTickResult result, PlanetTile newSurfaceProjectionTile)
        {
            Result = result;
            NewSurfaceProjectionTile = newSurfaceProjectionTile;
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
        private float currentAngularSpeed;
        private float activeSegmentTotalAngle;
        private int stateTicksElapsed;

        public SkyIslandMovementDriver(WorldObject parent)
        {
            this.parent = parent;
        }

        public SkyIslandMapParent.SkyIslandMovementState MovementState => movementState;
        public bool IsMoveControlLocked => movementState == SkyIslandMapParent.SkyIslandMovementState.Interrupting;
        public bool IsPreparingToDock =>
            movementState == SkyIslandMapParent.SkyIslandMovementState.Decelerating &&
            CurrentSpeedTilesPerDay <= 24.1f;

        public bool HasActiveTarget => activeTargetSkyTile.Valid && activeTargetSurfaceTile.Valid;
        public bool HasInterruptionAnchor => interruptionAnchorSkyTile.Valid && interruptionAnchorSurfaceTile.Valid;

        public float CurrentSpeedTilesPerDay
        {
            get
            {
                if (!parent.Tile.Valid || currentAngularSpeed <= 0f)
                {
                    return 0f;
                }

                return currentAngularSpeed * parent.Tile.Layer.Radius / parent.Tile.Layer.AverageTileSize * GenDate.TicksPerDay;
            }
        }

        public int? CalculateEta()
        {
            if (!parent.Tile.Valid)
            {
                return null;
            }

            PlanetTile targetTile = movementState switch
            {
                SkyIslandMapParent.SkyIslandMovementState.Accelerating => activeTargetSkyTile,
                SkyIslandMapParent.SkyIslandMovementState.Cruising => activeTargetSkyTile,
                SkyIslandMapParent.SkyIslandMovementState.Decelerating => activeTargetSkyTile,
                SkyIslandMapParent.SkyIslandMovementState.Interrupting => interruptionAnchorSkyTile,
                _ => PlanetTile.Invalid
            };

            return CalculateEta(targetTile);
        }

        public Vector3 GetSkyWorldPosition(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            return GetWorldPositionOnLayer(parent.Tile.Layer);
        }

        public Vector3 GetSurfaceWorldPosition(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null)
            {
                return Vector3.zero;
            }

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
            {
                return Vector2.zero;
            }

            return GetLongLatOnLayer(surfaceLayer, GetSurfaceWorldPosition(fallbackSurfaceProjectionTile));
        }

        public bool GetIsCenteredOnCurrentTile(PlanetTile fallbackSurfaceProjectionTile)
        {
            EnsureCurrentDirection(fallbackSurfaceProjectionTile);
            if (!parent.Tile.Valid || currentDirection == Vector3.zero)
            {
                return true;
            }

            return Vector3.Dot(currentDirection.normalized, Find.WorldGrid.GetTileCenter(parent.Tile).normalized) >= SkyIslandMovementConstants.AnchorSnapDotThreshold;
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

            return AdvanceMovement(ticks, planner, currentSurfaceProjectionTile);
        }

        public bool Start(PlanetTile surfaceTarget, PlanetTile skyTarget)
        {
            if (!skyTarget.Valid || movementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return false;
            }

            SetActiveTarget(surfaceTarget, skyTarget);
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Accelerating);
            return true;
        }

        public bool Interrupt(PlanetTile anchorSkyTile, PlanetTile anchorSurfaceTile)
        {
            if (movementState != SkyIslandMapParent.SkyIslandMovementState.Accelerating &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Cruising &&
                movementState != SkyIslandMapParent.SkyIslandMovementState.Decelerating)
            {
                return false;
            }

            interruptionAnchorSkyTile = anchorSkyTile;
            interruptionAnchorSurfaceTile = anchorSurfaceTile;
            SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Interrupting);
            return true;
        }

        public void Reset()
        {
            activeTargetSurfaceTile = PlanetTile.Invalid;
            activeTargetSkyTile = PlanetTile.Invalid;
            interruptionAnchorSurfaceTile = PlanetTile.Invalid;
            interruptionAnchorSkyTile = PlanetTile.Invalid;
            currentAngularSpeed = 0f;
            activeSegmentTotalAngle = 0f;
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
            Scribe_Values.Look(ref currentAngularSpeed, "currentAngularSpeed", 0f);
            Scribe_Values.Look(ref activeSegmentTotalAngle, "activeSegmentTotalAngle", 0f);
            Scribe_Values.Look(ref stateTicksElapsed, "stateTicksElapsed", 0);
        }

        private MovementTickOutput AdvanceMovement(int ticks, SkyIslandWaypointPlanner planner, PlanetTile currentSurfaceProjectionTile)
        {
            if (ticks <= 0)
            {
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            EnsureCurrentDirection(currentSurfaceProjectionTile);
            stateTicksElapsed += ticks;

            PlanetLayer skyLayer = parent.Tile.Layer;
            float cruiseAngularSpeed = (skyLayer.AverageTileSize / skyLayer.Radius) / SkyIslandMovementConstants.TicksPerTileDistance;
            float slowAngularSpeed = (skyLayer.AverageTileSize / skyLayer.Radius) / SkyIslandMovementConstants.TicksPerTileDistanceSlow;
            float interruptionDeceleration = cruiseAngularSpeed / SkyIslandMovementConstants.InterruptionDecelerationTicks;

            switch (movementState)
            {
                case SkyIslandMapParent.SkyIslandMovementState.Accelerating:
                case SkyIslandMapParent.SkyIslandMovementState.Cruising:
                case SkyIslandMapParent.SkyIslandMovementState.Decelerating:
                    return AdvanceTowardsPathTarget(ticks, cruiseAngularSpeed, slowAngularSpeed, currentSurfaceProjectionTile);
                case SkyIslandMapParent.SkyIslandMovementState.Interrupting:
                    return AdvanceTowardsInterruptionAnchor(ticks, slowAngularSpeed, interruptionDeceleration, currentSurfaceProjectionTile);
            }

            return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
        }

        private MovementTickOutput AdvanceTowardsPathTarget(int ticks, float cruiseAngularSpeed, float slowAngularSpeed, PlanetTile currentSurfaceProjectionTile)
        {
            if (!activeTargetSkyTile.Valid || !activeTargetSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            Vector3 targetDirection = Find.WorldGrid.GetTileCenter(activeTargetSkyTile).normalized;
            float remainingAngle = GenMath.SphericalDistance(currentDirection.normalized, targetDirection);
            float progress = activeSegmentTotalAngle <= 1E-06f ? 1f : Mathf.Clamp01(1f - remainingAngle / activeSegmentTotalAngle);
            float accelerationPortion = Mathf.Clamp01(progress / SkyIslandMovementConstants.PathAccelerationDistanceFactor);
            float decelerationPortion = Mathf.Clamp01(remainingAngle / Mathf.Max(activeSegmentTotalAngle * SkyIslandMovementConstants.PathDecelerationDistanceFactor, SkyIslandMovementConstants.ArrivalAngleTolerance));
            float nextSpeed = cruiseAngularSpeed * Mathf.Lerp(SkyIslandMovementConstants.PathMinSpeedFactor, 1f, Mathf.Min(accelerationPortion, decelerationPortion));

            if (remainingAngle <= activeSegmentTotalAngle * SkyIslandMovementConstants.PathDecelerationDistanceFactor)
            {
                if (movementState != SkyIslandMapParent.SkyIslandMovementState.Decelerating)
                {
                    SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Decelerating);
                }
            }
            else if (progress >= SkyIslandMovementConstants.PathAccelerationDistanceFactor)
            {
                if (movementState != SkyIslandMapParent.SkyIslandMovementState.Cruising)
                {
                    SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Cruising);
                }
            }
            else if (movementState != SkyIslandMapParent.SkyIslandMovementState.Accelerating)
            {
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Accelerating);
            }

            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Decelerating)
            {
                nextSpeed = Mathf.Min(nextSpeed, slowAngularSpeed);
            }

            currentAngularSpeed = nextSpeed;
            if (remainingAngle <= SkyIslandMovementConstants.ArrivalAngleTolerance)
            {
                currentDirection = targetDirection;
                currentAngularSpeed = 0f;
                parent.Tile = activeTargetSkyTile;
                PlanetTile arrivedSurfaceTile = activeTargetSurfaceTile;
                activeTargetSkyTile = PlanetTile.Invalid;
                activeTargetSurfaceTile = PlanetTile.Invalid;
                activeSegmentTotalAngle = 0f;
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                return new MovementTickOutput(MovementTickResult.Arrived, arrivedSurfaceTile);
            }

            float travelAngle = currentAngularSpeed * ticks;
            currentDirection = Vector3.RotateTowards(currentDirection.normalized, targetDirection, travelAngle, 999999f).normalized;
            PlanetTile newSurfaceProjection = RecalculateSurfaceProjection(currentSurfaceProjectionTile);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection);
        }

        private MovementTickOutput AdvanceTowardsInterruptionAnchor(int ticks, float slowAngularSpeed, float interruptionDeceleration, PlanetTile currentSurfaceProjectionTile)
        {
            if (!interruptionAnchorSkyTile.Valid || !interruptionAnchorSurfaceTile.Valid)
            {
                Reset();
                return new MovementTickOutput(MovementTickResult.None, currentSurfaceProjectionTile);
            }

            Vector3 targetDirection = Find.WorldGrid.GetTileCenter(interruptionAnchorSkyTile).normalized;
            float remainingAngle = GenMath.SphericalDistance(currentDirection.normalized, targetDirection);
            float nextSpeed = Mathf.Lerp(currentAngularSpeed, slowAngularSpeed, Mathf.Clamp01((float)ticks / SkyIslandMovementConstants.InterruptionDecelerationTicks));
            nextSpeed = Mathf.Min(nextSpeed, slowAngularSpeed);
            currentAngularSpeed = nextSpeed;

            if (remainingAngle <= SkyIslandMovementConstants.ArrivalAngleTolerance && stateTicksElapsed >= SkyIslandMovementConstants.InterruptionMinTicks)
            {
                currentDirection = targetDirection;
                currentAngularSpeed = 0f;
                parent.Tile = interruptionAnchorSkyTile;
                PlanetTile interruptedSurfaceTile = interruptionAnchorSurfaceTile;
                interruptionAnchorSkyTile = PlanetTile.Invalid;
                interruptionAnchorSurfaceTile = PlanetTile.Invalid;
                activeSegmentTotalAngle = 0f;
                SetMovementState(SkyIslandMapParent.SkyIslandMovementState.Idle);
                return new MovementTickOutput(MovementTickResult.Interrupted, interruptedSurfaceTile);
            }

            float travelAngle = currentAngularSpeed * ticks;
            currentDirection = Vector3.RotateTowards(currentDirection.normalized, targetDirection, travelAngle, 999999f).normalized;
            PlanetTile newSurfaceProjection = RecalculateSurfaceProjection(currentSurfaceProjectionTile);
            return new MovementTickOutput(MovementTickResult.None, newSurfaceProjection);
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

            float longitude = Mathf.Atan2(local.x, -local.z) * 57.29578f;
            float latitude = Mathf.Asin(local.y / layer.Radius) * 57.29578f;
            return new Vector2(longitude, latitude);
        }

        private void SetActiveTarget(PlanetTile surfaceTile, PlanetTile skyTile)
        {
            activeTargetSurfaceTile = surfaceTile;
            activeTargetSkyTile = skyTile;
            activeSegmentTotalAngle = GenMath.SphericalDistance(currentDirection.normalized, Find.WorldGrid.GetTileCenter(skyTile).normalized);
        }

        private void SetMovementState(SkyIslandMapParent.SkyIslandMovementState newState)
        {
            if (movementState == newState)
            {
                return;
            }

            movementState = newState;
            stateTicksElapsed = 0;
        }

        private int? CalculateEta(PlanetTile targetSkyTile)
        {
            if (!parent.Tile.Valid || !targetSkyTile.Valid)
            {
                return null;
            }

            float remainingAngle = GenMath.SphericalDistance(currentDirection.normalized, Find.WorldGrid.GetTileCenter(targetSkyTile).normalized);
            PlanetLayer skyLayer = parent.Tile.Layer;
            float cruiseAngularSpeed = (skyLayer.AverageTileSize / skyLayer.Radius) / SkyIslandMovementConstants.TicksPerTileDistance;
            float slowAngularSpeed = (skyLayer.AverageTileSize / skyLayer.Radius) / SkyIslandMovementConstants.TicksPerTileDistanceSlow;
            float effectiveAngularSpeed = currentAngularSpeed;
            if (effectiveAngularSpeed <= 1E-06f)
            {
                effectiveAngularSpeed = movementState switch
                {
                    SkyIslandMapParent.SkyIslandMovementState.Accelerating => cruiseAngularSpeed * 0.25f,
                    SkyIslandMapParent.SkyIslandMovementState.Decelerating => slowAngularSpeed,
                    SkyIslandMapParent.SkyIslandMovementState.Interrupting => slowAngularSpeed,
                    _ => 0f
                };
            }

            if (effectiveAngularSpeed <= 1E-06f)
            {
                return null;
            }

            return Mathf.CeilToInt(remainingAngle / effectiveAngularSpeed);
        }
    }
}
