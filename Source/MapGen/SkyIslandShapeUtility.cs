using System;
using UnityEngine;
using Verse;

namespace SkyrimIslands.MapGen
{
    internal static class SkyIslandShapeUtility
    {
        private const float CoreRadius = 6.0f;
        private const float PlatformOuterRadius = 12.0f;
        private const float OuterVariation = 1.35f;
        private const int SectorCount = 24;

        public static float MaxOuterRadius => PlatformOuterRadius + OuterVariation;

        public static bool IsCoreCell(IntVec3 cell, IntVec3 center)
        {
            return DistanceToCenter(cell, center) <= CoreRadius;
        }

        public static bool IsPlatformCell(IntVec3 cell, IntVec3 center, int shapeSeed)
        {
            float distance = DistanceToCenter(cell, center);
            if (distance <= CoreRadius)
            {
                return false;
            }

            return distance <= OuterRadiusAt(cell, center, shapeSeed);
        }

        public static CellRect GetShapeBounds(IntVec3 center)
        {
            int diameter = Mathf.CeilToInt(MaxOuterRadius) * 2 + 1;
            return CellRect.CenteredOn(center, diameter, diameter);
        }

        private static float OuterRadiusAt(IntVec3 cell, IntVec3 center, int shapeSeed)
        {
            Vector3 offset = cell.ToVector3Shifted() - center.ToVector3Shifted();
            float angle = Mathf.Atan2(offset.z, offset.x);
            if (angle < 0f)
            {
                angle += Mathf.PI * 2f;
            }

            float sectorFloat = angle / (Mathf.PI * 2f) * SectorCount;
            int index = Mathf.FloorToInt(sectorFloat) % SectorCount;
            int nextIndex = (index + 1) % SectorCount;
            float t = sectorFloat - Mathf.Floor(sectorFloat);

            float currentOffset = RadiusOffsetForSector(shapeSeed, index);
            float nextOffset = RadiusOffsetForSector(shapeSeed, nextIndex);
            return PlatformOuterRadius + Mathf.Lerp(currentOffset, nextOffset, t);
        }

        private static float RadiusOffsetForSector(int shapeSeed, int sectorIndex)
        {
            int seed = Gen.HashCombineInt(shapeSeed, sectorIndex * 7919);
            float normalized = Mathf.Abs(seed % 10000) / 9999f;
            return Mathf.Lerp(-OuterVariation, OuterVariation, normalized);
        }

        private static float DistanceToCenter(IntVec3 cell, IntVec3 center)
        {
            return (cell.ToVector3Shifted() - center.ToVector3Shifted()).MagnitudeHorizontal();
        }
    }
}
