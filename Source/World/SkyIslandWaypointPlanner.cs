using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class SkyIslandWaypointPlanner : IExposable
    {
        private List<PlanetTile> surfaceWaypoints = new List<PlanetTile>();
        private List<PlanetTile> skyWaypoints = new List<PlanetTile>();
        private List<float> waypointAltitudes = new List<float>();

        public IReadOnlyList<PlanetTile> SurfaceWaypoints => surfaceWaypoints;
        public IReadOnlyList<PlanetTile> SkyWaypoints => skyWaypoints;
        public IReadOnlyList<float> WaypointAltitudes => waypointAltitudes;
        public bool HasRoute => surfaceWaypoints.Count > 0;
        public int Count => surfaceWaypoints.Count;

        public PlanetTile FirstSurfaceWaypoint => surfaceWaypoints.Count > 0 ? surfaceWaypoints[0] : PlanetTile.Invalid;
        public PlanetTile FirstSkyWaypoint => skyWaypoints.Count > 0 ? skyWaypoints[0] : PlanetTile.Invalid;
        public float FirstWaypointAltitude => waypointAltitudes.Count > 0 ? waypointAltitudes[0] : SkyIslandAltitude.DefaultAltitude;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref surfaceWaypoints, "surfaceWaypoints", LookMode.Value);
            Scribe_Collections.Look(ref skyWaypoints, "skyWaypoints", LookMode.Value);
            Scribe_Collections.Look(ref waypointAltitudes, "waypointAltitudes", LookMode.Value);

            surfaceWaypoints ??= new List<PlanetTile>();
            skyWaypoints ??= new List<PlanetTile>();
            waypointAltitudes ??= new List<float>();
        }

        public bool TryAdd(PlanetTile surfaceTile, PlanetTile skyTile, float altitude)
        {
            if (!skyTile.Valid)
            {
                return false;
            }

            surfaceWaypoints.Add(surfaceTile);
            skyWaypoints.Add(skyTile);
            waypointAltitudes.Add(altitude);
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
            if (index < waypointAltitudes.Count)
            {
                waypointAltitudes.RemoveAt(index);
            }

            return true;
        }

        public void Clear()
        {
            surfaceWaypoints.Clear();
            skyWaypoints.Clear();
            waypointAltitudes.Clear();
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
                waypointAltitudes.Clear();
            }
        }

        public void MigrateLegacyAltitudes(float maxAltitude, float surfaceRadius, float minAltitude)
        {
            for (int i = 0; i < waypointAltitudes.Count; i++)
            {
                float alt = waypointAltitudes[i];
                if (alt > maxAltitude + 10f)
                {
                    alt = Mathf.Clamp(alt - surfaceRadius, minAltitude, maxAltitude);
                    waypointAltitudes[i] = alt;
                }
                else if (alt > maxAltitude)
                {
                    waypointAltitudes[i] = maxAltitude;
                }
            }
        }
    }
}
