namespace SkyrimIslands.World
{
    public static class SkyIslandMovementConstants
    {
        public const int TicksPerTileDistance = 80;
        public const int TicksPerTileDistanceSlow = 2500;
        public const float AnchorSnapDotThreshold = 0.9999995f;
        public const float ArrivalAngleTolerance = 0.00005f;
        public const float PathAccelerationDistanceFactor = 0.22f;
        public const float PathDecelerationDistanceFactor = 0.28f;
        public const float PathMinSpeedFactor = 0.03f;
        public const int FinalApproachTicks = 8;
        public const int InterruptionMinTicks = 24;
        public const int InterruptionDecelerationTicks = 16;
    }
}
