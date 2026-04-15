using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SkyrimIslands.World
{
    public class SkyIslandWaypointPlanner : IExposable
    {
        private List<PlanetTile> surfaceWaypoints = new List<PlanetTile>();
        private List<PlanetTile> skyWaypoints = new List<PlanetTile>();

        public IReadOnlyList<PlanetTile> SurfaceWaypoints => surfaceWaypoints;
        public IReadOnlyList<PlanetTile> SkyWaypoints => skyWaypoints;
        public bool HasRoute => surfaceWaypoints.Count > 0;
        public int Count => surfaceWaypoints.Count;

        public PlanetTile FirstSurfaceWaypoint => surfaceWaypoints.Count > 0 ? surfaceWaypoints[0] : PlanetTile.Invalid;
        public PlanetTile FirstSkyWaypoint => skyWaypoints.Count > 0 ? skyWaypoints[0] : PlanetTile.Invalid;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref surfaceWaypoints, "surfaceWaypoints", LookMode.Value);
            Scribe_Collections.Look(ref skyWaypoints, "skyWaypoints", LookMode.Value);

            surfaceWaypoints ??= new List<PlanetTile>();
            skyWaypoints ??= new List<PlanetTile>();
        }

        public bool TryAdd(PlanetTile surfaceTile, PlanetTile skyTile)
        {
            if (!skyTile.Valid)
            {
                return false;
            }

            surfaceWaypoints.Add(surfaceTile);
            skyWaypoints.Add(skyTile);
            return true;
        }

        public bool TryRemoveAt(int index)
        {
            if (index < 0 || index >= surfaceWaypoints.Count)
            {
                return false;
            }

            surfaceWaypoints.RemoveAt(index);
            if (index < skyWaypoints.Count)
            {
                skyWaypoints.RemoveAt(index);
            }

            return true;
        }

        public void Clear()
        {
            surfaceWaypoints.Clear();
            skyWaypoints.Clear();
        }

        public int MostRecentIndexAt(PlanetTile surfaceTile)
        {
            for (int i = surfaceWaypoints.Count - 1; i >= 0; i--)
            {
                if (surfaceWaypoints[i] == surfaceTile)
                {
                    return i;
                }
            }

            return -1;
        }

        public void RebuildSkyWaypoints(System.Func<PlanetTile, PlanetTile> project)
        {
            skyWaypoints.Clear();
            for (int i = 0; i < surfaceWaypoints.Count; i++)
            {
                PlanetTile skyTile = project(surfaceWaypoints[i]);
                if (skyTile.Valid)
                {
                    skyWaypoints.Add(skyTile);
                }
            }

            if (skyWaypoints.Count != surfaceWaypoints.Count)
            {
                surfaceWaypoints.Clear();
                skyWaypoints.Clear();
            }
        }
    }
}
