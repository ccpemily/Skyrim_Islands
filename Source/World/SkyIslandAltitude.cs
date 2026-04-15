namespace SkyrimIslands.World
{
    public static class SkyIslandAltitude
    {
        public const float SurfaceRadius = 100f;
        public const float SkyLayerRadius = 106f;
        public const float OrbitLayerRadius = 130f;

        public const float SkyLayerHeight = SkyLayerRadius - SurfaceRadius;
        public const float OrbitHeight = OrbitLayerRadius - SurfaceRadius;

        public const float MinAltitude = SkyLayerHeight * 0.2f;
        public const float MaxAltitude = OrbitHeight * 0.8f;
        public const float DefaultAltitude = SkyLayerHeight;
    }
}
