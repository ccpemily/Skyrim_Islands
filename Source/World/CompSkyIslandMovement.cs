using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class CompSkyIslandMovement : WorldObjectComp
    {
        private PlanetTile surfaceProjectionTile = PlanetTile.Invalid;
        private readonly SkyIslandWaypointPlanner waypointPlanner = new SkyIslandWaypointPlanner();
        private SkyIslandMovementDriver? driver;

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
        public bool HasPlannedRoute => waypointPlanner.HasRoute;

        public bool IsCenteredOnCurrentTile => driver?.GetIsCenteredOnCurrentTile(surfaceProjectionTile) ?? true;
        public SkyIslandMapParent.SkyIslandMovementState MovementState => driver?.MovementState ?? SkyIslandMapParent.SkyIslandMovementState.Idle;
        public bool IsMoveControlLocked => driver?.IsMoveControlLocked ?? false;
        public bool IsPreparingToDock => driver?.IsPreparingToDock ?? false;
        public float CurrentSpeedTilesPerDay => driver?.CurrentSpeedTilesPerDay ?? 0f;
        public int? CurrentEtaTicks => driver?.CalculateEta();
        public Vector3 CurrentSkyWorldPosition => driver?.GetSkyWorldPosition(surfaceProjectionTile) ?? Vector3.zero;
        public Vector3 CurrentSurfaceWorldPosition => driver?.GetSurfaceWorldPosition(surfaceProjectionTile) ?? Vector3.zero;
        public Vector2 CurrentSkyLongLat => driver?.GetSkyLongLat(surfaceProjectionTile) ?? Vector2.zero;
        public Vector2 CurrentSurfaceLongLat => driver?.GetSurfaceLongLat(surfaceProjectionTile) ?? Vector2.zero;

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

            switch (output.Result)
            {
                case MovementTickResult.Arrived:
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
            waypointPlanner.ExposeData();
            driver?.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureWaypointProjectionCache();
                EnsureSurfaceProjectionTile();
                if (driver != null &&
                    MovementState != SkyIslandMapParent.SkyIslandMovementState.Idle &&
                    MovementState != SkyIslandMapParent.SkyIslandMovementState.Interrupting &&
                    !driver.HasActiveTarget)
                {
                    driver.Reset();
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

            surfaceProjectionTile = surfaceLayer.GetClosestTile_NewTemp(parent.Tile, false);
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

            return skyLayer.GetClosestTile_NewTemp(surfaceTile, false);
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

        public bool TryAddPlannedSurfaceWaypoint(PlanetTile surfaceTile, bool allowConsecutiveDuplicate = false)
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
            return waypointPlanner.TryAdd(surfaceTile, skyTile);
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
            return driver!.Start(waypointPlanner.FirstSurfaceWaypoint, waypointPlanner.FirstSkyWaypoint);
        }

        public bool PauseMovementPreview()
        {
            return driver!.Interrupt(parent.Tile, SurfaceProjectionTile);
        }

        public void EnsureWaypointProjectionCache()
        {
            waypointPlanner.RebuildSkyWaypoints(GetSkyProjectionTile);
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
