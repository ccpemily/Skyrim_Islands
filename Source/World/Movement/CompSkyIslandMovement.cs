using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World.Movement;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World.Movement
{
    public class CompSkyIslandMovement : WorldObjectComp
    {
        private PlanetTile surfaceProjectionTile = PlanetTile.Invalid;
        private readonly SkyIslandWaypointPlanner waypointPlanner = new SkyIslandWaypointPlanner();
        private SkyIslandMovementDriver? driver;

        private float departureAltitude = SkyIslandAltitude.DefaultAltitude;
        private float targetAltitude = SkyIslandAltitude.DefaultAltitude;
        private int currentGear = 1;

        private SkyIslandMapParent Owner => (SkyIslandMapParent)parent;

        public PlanetTile SurfaceProjectionTile
        {
            get
            {
                EnsureSurfaceProjectionTile();
                return surfaceProjectionTile;
            }
        }

        public IReadOnlyList<PlanetTile> PlannedSurfaceWaypoints => waypointPlanner.SurfaceWaypoints;
        public IReadOnlyList<PlanetTile> PlannedSkyWaypoints => waypointPlanner.SkyWaypoints;
        public IReadOnlyList<float> WaypointAltitudes => waypointPlanner.WaypointAltitudes;
        public bool HasPlannedRoute => waypointPlanner.HasRoute;

        public int CurrentGear => currentGear;

        public bool IsCenteredOnCurrentTile
        {
            get
            {
                if (driver == null)
                    return true;
                Vector3 dir = driver.CurrentDirection;
                SkyIslandMovementGeometry.EnsureDirection(ref dir, surfaceProjectionTile, parent.Tile);
                return SkyIslandMovementGeometry.IsCenteredOnTile(parent.Tile, dir);
            }
        }

        public SkyIslandMapParent.SkyIslandMovementState MovementState => driver?.MovementState ?? SkyIslandMapParent.SkyIslandMovementState.Idle;
        public SkyIslandMapParent.SkyIslandVerticalState VerticalState => driver?.VerticalState ?? SkyIslandMapParent.SkyIslandVerticalState.Holding;
        public bool IsMoveControlLocked => driver?.IsMoveControlLocked ?? false;
        public Vector3 CurrentVelocityDirection => driver?.CurrentVelocityDirection ?? Vector3.zero;
        public float CurrentSpeedTilesPerDay => driver?.CurrentSpeedTilesPerDay ?? 0f;
        public int? CurrentEtaTicks => driver?.CalculateEta();

        public Vector3 CurrentSkyWorldPosition
        {
            get
            {
                if (driver == null)
                    return Vector3.zero;
                Vector3 dir = driver.CurrentDirection;
                SkyIslandMovementGeometry.EnsureDirection(ref dir, surfaceProjectionTile, parent.Tile);
                return SkyIslandMovementGeometry.GetSkyWorldPosition(dir, parent.Tile, Owner.Altitude);
            }
        }

        public Vector3 CurrentSurfaceWorldPosition
        {
            get
            {
                if (driver == null)
                    return Vector3.zero;
                Vector3 dir = driver.CurrentDirection;
                SkyIslandMovementGeometry.EnsureDirection(ref dir, surfaceProjectionTile, parent.Tile);
                return SkyIslandMovementGeometry.GetSurfaceWorldPosition(dir, parent.Tile);
            }
        }

        public Vector2 CurrentSkyLongLat
        {
            get
            {
                if (driver == null)
                    return Vector2.zero;
                Vector3 dir = driver.CurrentDirection;
                SkyIslandMovementGeometry.EnsureDirection(ref dir, surfaceProjectionTile, parent.Tile);
                return SkyIslandMovementGeometry.GetSkyLongLat(dir, parent.Tile, Owner.Altitude);
            }
        }

        public Vector2 CurrentSurfaceLongLat
        {
            get
            {
                if (driver == null)
                    return Vector2.zero;
                Vector3 dir = driver.CurrentDirection;
                SkyIslandMovementGeometry.EnsureDirection(ref dir, surfaceProjectionTile, parent.Tile);
                return SkyIslandMovementGeometry.GetSurfaceLongLat(dir, parent.Tile);
            }
        }

        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            driver = new SkyIslandMovementDriver(parent);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (MovementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return;
            }

            if (MovementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting && !HasPlannedRoute)
            {
                driver!.Reset();
                return;
            }

            MovementTickOutput output = driver!.Tick(1, waypointPlanner, surfaceProjectionTile);
            surfaceProjectionTile = output.NewSurfaceProjectionTile;

            if (MovementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                Owner.Altitude = driver.CurrentAltitude;
            }

            switch (output.Result)
            {
                case MovementTickResult.Arrived:
                    Owner.Altitude = driver.TargetAltitude;
                    SendArrivalLetter();
                    TryRemovePlannedSurfaceWaypointAt(0);
                    break;
                case MovementTickResult.Interrupted:
                    SendInterruptedLetter();
                    break;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref surfaceProjectionTile, "surfaceProjectionTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref departureAltitude, "departureAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref targetAltitude, "targetAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref currentGear, "currentGear", 1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (departureAltitude > SkyIslandAltitude.MaxAltitude + 10f)
                {
                    departureAltitude = Mathf.Clamp(departureAltitude - SkyIslandAltitude.SurfaceRadius, SkyIslandAltitude.MinAltitude, SkyIslandAltitude.MaxAltitude);
                }
                if (targetAltitude > SkyIslandAltitude.MaxAltitude + 10f)
                {
                    targetAltitude = Mathf.Clamp(targetAltitude - SkyIslandAltitude.SurfaceRadius, SkyIslandAltitude.MinAltitude, SkyIslandAltitude.MaxAltitude);
                }
            }
            waypointPlanner.ExposeData();
            driver?.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                waypointPlanner.MigrateLegacyAltitudes(SkyIslandAltitude.MaxAltitude, SkyIslandAltitude.SurfaceRadius, SkyIslandAltitude.MinAltitude);

                EnsureWaypointProjectionCache();
                EnsureSurfaceProjectionTile();

                if (driver != null)
                {
                    if (MovementState != SkyIslandMapParent.SkyIslandMovementState.Idle &&
                        MovementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting &&
                        !driver.HasActiveTarget)
                    {
                        driver.Reset();
                    }

                    if (driver.DepartureAltitude == SkyIslandAltitude.DefaultAltitude &&
                        driver.TargetAltitude == SkyIslandAltitude.DefaultAltitude &&
                        (departureAltitude != SkyIslandAltitude.DefaultAltitude || targetAltitude != SkyIslandAltitude.DefaultAltitude))
                    {
                        driver.SetAltitudeContext(departureAltitude, targetAltitude);
                    }
                }
            }
        }

        public void EnsureSurfaceProjectionTile()
        {
            if (surfaceProjectionTile.Valid && surfaceProjectionTile.LayerDef == PlanetLayerDefOf.Surface)
            {
                return;
            }

            PlanetLayer surfaceLayer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            if (surfaceLayer == null || !parent.Tile.Valid)
            {
                surfaceProjectionTile = PlanetTile.Invalid;
                return;
            }

            Vector3 skyDir = Find.WorldGrid.GetTileCenter(parent.Tile).normalized;
            surfaceProjectionTile = FindClosestTileOnLayerByDirection(surfaceLayer, skyDir);
        }

        public PlanetTile GetSkyProjectionTile(PlanetTile surfaceTile)
        {
            if (!surfaceTile.Valid)
            {
                return PlanetTile.Invalid;
            }

            PlanetLayer skyLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (skyLayer == null)
            {
                return PlanetTile.Invalid;
            }

            Vector3 surfaceDir = Find.WorldGrid.GetTileCenter(surfaceTile).normalized;
            return FindClosestTileOnLayerByDirection(skyLayer, surfaceDir);
        }

        public bool CanAddWaypointAt(PlanetTile surfaceTile)
        {
            if (!surfaceTile.Valid || surfaceTile.LayerDef != PlanetLayerDefOf.Surface)
            {
                return false;
            }

            bool wouldBeFirstWaypoint = waypointPlanner.Count == 0;
            if (IsCenteredOnCurrentTile && wouldBeFirstWaypoint && surfaceTile == SurfaceProjectionTile)
            {
                return false;
            }

            return true;
        }

        public bool TryAddPlannedSurfaceWaypoint(PlanetTile surfaceTile, float altitude, bool allowConsecutiveDuplicate = false)
        {
            if (!CanAddWaypointAt(surfaceTile))
            {
                return false;
            }

            if (!allowConsecutiveDuplicate &&
                waypointPlanner.Count > 0 &&
                waypointPlanner.SurfaceWaypoints[waypointPlanner.Count - 1] == surfaceTile)
            {
                return false;
            }

            if (waypointPlanner.Count >= SkyIslandMapParent.MaxPlannedWaypointCount)
            {
                return false;
            }

            PlanetTile skyTile = GetSkyProjectionTile(surfaceTile);
            return waypointPlanner.TryAdd(surfaceTile, skyTile, altitude);
        }

        public int MostRecentPlannedWaypointIndexAt(PlanetTile surfaceTile)
        {
            return waypointPlanner.MostRecentIndexAt(surfaceTile);
        }

        public bool TryRemovePlannedSurfaceWaypointAt(int index)
        {
            bool hadFewerSkyWaypoints = waypointPlanner.SkyWaypoints.Count <= index;
            bool result = waypointPlanner.TryRemoveAt(index);
            if (result && hadFewerSkyWaypoints)
            {
                EnsureWaypointProjectionCache();
            }

            return result;
        }

        public void ClearPlannedSurfaceWaypoints()
        {
            waypointPlanner.Clear();
            driver!.Reset();
        }

        public bool StartEnginePreview()
        {
            if (!HasPlannedRoute || MovementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                return false;
            }

            EnsureWaypointProjectionCache();
            departureAltitude = Owner.Altitude;
            targetAltitude = waypointPlanner.FirstWaypointAltitude;
            return driver!.Start(waypointPlanner.FirstSurfaceWaypoint, waypointPlanner.FirstSkyWaypoint, currentGear, departureAltitude, targetAltitude);
        }

        public bool PauseMovementPreview()
        {
            return driver!.Interrupt(parent.Tile, SurfaceProjectionTile);
        }

        public void SetGear(int gear)
        {
            currentGear = Mathf.Clamp(gear, 0, SkyIslandMovementConstants.Gears.Length - 1);
            if (MovementState != SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                driver?.ChangeGear(currentGear);
            }
        }

        public void EnsureWaypointProjectionCache()
        {
            waypointPlanner.RebuildSkyWaypoints(GetSkyProjectionTile);
        }

        private static PlanetTile FindClosestTileOnLayerByDirection(PlanetLayer layer, Vector3 direction)
        {
            if (layer == null)
            {
                return PlanetTile.Invalid;
            }

            PlanetTile bestTile = PlanetTile.Invalid;
            float bestDot = -1f;
            int layerId = layer.LayerID;

            for (int i = 0; i < layer.TilesCount; i++)
            {
                PlanetTile candidate = new PlanetTile(i, layerId);
                float dot = Vector3.Dot(direction, layer.GetTileCenter(candidate).normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestTile = candidate;
                }
            }

            return bestTile;
        }

        private void SendArrivalLetter()
        {
            Find.LetterStack.ReceiveLetter(
                "空岛已抵达路径点",
                "空岛已抵达当前目标路径点，并已自动停止引擎。若要继续前往下一个路径点，请再次手动启动引擎。",
                LetterDefOf.NeutralEvent,
                parent);
        }

        private void SendInterruptedLetter()
        {
            Find.LetterStack.ReceiveLetter(
                "空岛移动已中断",
                "空岛已中断当前移动，并已减速回到最近锚定位置后停止。若要继续移动，请重新规划或再次启动引擎。",
                LetterDefOf.NeutralEvent,
                parent);
        }
    }
}
