using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public static class SkyIslandTemperatureUtility
    {
        private const int CacheIntervalTicks = 60;
        private const int SampleNeighborDepth = 1;

        private static readonly SimpleCurve AltitudeTempOffsetCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(SkyIslandAltitude.SkyLayerHeight, -5f),
            new CurvePoint(SkyIslandAltitude.OrbitHeight, -12f)
        };

        private static readonly Dictionary<SkyIslandMapParent, TemperatureCache> cacheMap = new Dictionary<SkyIslandMapParent, TemperatureCache>();

        private struct TemperatureCache
        {
            public int ValidUntilTick;
            public float OutdoorTemp;
            public float SeasonalTemp;
        }

        public static bool TryGetSmoothedOutdoorTemp(Map map, out float temp)
        {
            if (map?.Parent is SkyIslandMapParent island && TryGetCachedOrCalculate(island, out TemperatureCache cache))
            {
                temp = cache.OutdoorTemp;
                return true;
            }

            temp = 0f;
            return false;
        }

        public static bool TryGetSmoothedSeasonalTemp(Map map, out float temp)
        {
            if (map?.Parent is SkyIslandMapParent island && TryGetCachedOrCalculate(island, out TemperatureCache cache))
            {
                temp = cache.SeasonalTemp;
                return true;
            }

            temp = 0f;
            return false;
        }

        private static bool TryGetCachedOrCalculate(SkyIslandMapParent island, out TemperatureCache cache)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (cacheMap.TryGetValue(island, out cache) && cache.ValidUntilTick > currentTick)
            {
                return true;
            }

            if (!island.SurfaceProjectionTile.Valid)
            {
                return false;
            }

            CalculateSmoothedTemperatures(island, out float outdoor, out float seasonal);
            cache = new TemperatureCache
            {
                ValidUntilTick = currentTick + CacheIntervalTicks,
                OutdoorTemp = outdoor,
                SeasonalTemp = seasonal
            };
            cacheMap[island] = cache;
            return true;
        }

        private static void CalculateSmoothedTemperatures(SkyIslandMapParent island, out float outdoor, out float seasonal)
        {
            PlanetTile centerTile = island.SurfaceProjectionTile;
            Vector2 centerLongLat = island.CurrentSurfaceLongLat;
            TileTemperaturesComp tileTemps = Find.World.tileTemperatures;

            List<PlanetTile> sampledTiles = SampleTiles(centerTile, SampleNeighborDepth);
            float totalWeight = 0f;
            float outdoorSum = 0f;
            float seasonalSum = 0f;

            for (int i = 0; i < sampledTiles.Count; i++)
            {
                PlanetTile tile = sampledTiles[i];
                if (!tile.Valid)
                {
                    continue;
                }

                Vector2 tileLongLat = Find.WorldGrid.LongLatOf(tile);
                float distance = Vector2.Distance(centerLongLat, tileLongLat);
                float weight = 1f / (1f + distance * 10f);

                outdoorSum += tileTemps.GetOutdoorTemp(tile) * weight;
                seasonalSum += tileTemps.GetSeasonalTemp(tile) * weight;
                totalWeight += weight;
            }

            if (totalWeight > 0f)
            {
                outdoor = outdoorSum / totalWeight;
                seasonal = seasonalSum / totalWeight;
            }
            else
            {
                outdoor = tileTemps.GetOutdoorTemp(centerTile);
                seasonal = tileTemps.GetSeasonalTemp(centerTile);
            }

            float altitudeOffset = AltitudeTempOffsetCurve.Evaluate(island.Altitude);
            outdoor += altitudeOffset;
            seasonal += altitudeOffset;
        }

        private static List<PlanetTile> SampleTiles(PlanetTile center, int depth)
        {
            List<PlanetTile> result = new List<PlanetTile> { center };
            if (depth <= 0)
            {
                return result;
            }

            HashSet<int> visited = new HashSet<int> { center.tileId };
            List<PlanetTile> currentFrontier = new List<PlanetTile> { center };
            List<PlanetTile> tmpNeighbors = new List<PlanetTile>();

            for (int d = 0; d < depth; d++)
            {
                List<PlanetTile> nextFrontier = new List<PlanetTile>();
                for (int i = 0; i < currentFrontier.Count; i++)
                {
                    PlanetTile tile = currentFrontier[i];
                    tmpNeighbors.Clear();
                    Find.WorldGrid.GetTileNeighbors(tile, tmpNeighbors);
                    for (int j = 0; j < tmpNeighbors.Count; j++)
                    {
                        PlanetTile neighbor = tmpNeighbors[j];
                        if (neighbor.Valid && neighbor.LayerDef == center.LayerDef && visited.Add(neighbor.tileId))
                        {
                            result.Add(neighbor);
                            nextFrontier.Add(neighbor);
                        }
                    }
                }
                currentFrontier = nextFrontier;
            }

            return result;
        }
    }
}
