namespace SkyrimIslands.World.Movement
{
    public static class SkyIslandMovementConstants
    {
        public const float HoursToTicks = 2500f;
        public const float AnchorSnapDotThreshold = 0.9999995f;
        public const float ArrivalAngleTolerance = 0.00005f;
        public const float InterruptionMinTicks = 24f;
        public const float VerticalSpeedKmPerHour = 1f;
        public const float DockDistanceThreshold = 1f;
        public const float DockDurationHours = 2f;
        public const float BrakeAccelerationTilesPerHourSq = 20f;

        public static readonly GearProfile[] Gears = new GearProfile[]
        {
            new GearProfile(2f, 2f),
            new GearProfile(5f, 3f),
            new GearProfile(10f, 4f),
            new GearProfile(20f, 6f)
        };
    }

    public readonly struct GearProfile
    {
        public readonly float MaxSpeedTilesPerHour;
        public readonly float AccelerationTilesPerHourSq;

        public GearProfile(float maxSpeedTilesPerHour, float accelerationTilesPerHourSq)
        {
            MaxSpeedTilesPerHour = maxSpeedTilesPerHour;
            AccelerationTilesPerHourSq = accelerationTilesPerHourSq;
        }
    }
}
