using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World.Movement
{
    public class SkyIslandMovementPhysics : IExposable
    {
        public Vector3 DepartureSkyDir { get; private set; } = Vector3.zero;
        public Vector3 TargetSkyDir { get; private set; } = Vector3.zero;
        public float DepartureAltitude { get; private set; } = SkyIslandAltitude.DefaultAltitude;
        public float TargetAltitude { get; private set; } = SkyIslandAltitude.DefaultAltitude;
        public float HorizontalDistanceTiles { get; private set; } = 0f;
        public float TotalDurationHours { get; private set; } = 0f;
        public float NormalDurationHours { get; private set; } = 0f;
        public float ScaledMaxSpeedH { get; private set; } = 0f;
        public float ScaledAccelH { get; private set; } = 0f;
        public float VerticalSpeed { get; private set; } = 0f;
        public float InitialSpeedH { get; private set; } = 0f;
        public int GearIndex { get; private set; } = 1;

        public void Setup(float distanceTiles, int gear, float deltaAltitude, float initialSpeed, Vector3 departureDir, Vector3 targetDir, float departureAltitude, float targetAltitude)
        {
            DepartureSkyDir = departureDir;
            TargetSkyDir = targetDir;
            DepartureAltitude = departureAltitude;
            TargetAltitude = targetAltitude;
            HorizontalDistanceTiles = distanceTiles;
            InitialSpeedH = initialSpeed;
            GearIndex = gear;

            CalculateMovementProfile(distanceTiles, gear, deltaAltitude, initialSpeed,
                out float totalDuration, out float normalDuration, out float scaledMaxSpeed, out float scaledAccel, out float verticalSpeed);

            TotalDurationHours = totalDuration;
            NormalDurationHours = normalDuration;
            ScaledMaxSpeedH = scaledMaxSpeed;
            ScaledAccelH = scaledAccel;
            VerticalSpeed = verticalSpeed;
        }

        public void SetAltitudeContext(float departure, float target)
        {
            DepartureAltitude = departure;
            TargetAltitude = target;
        }

        public void SetDepartureSkyDir(Vector3 dir)
        {
            DepartureSkyDir = dir;
        }

        public float GetCurrentAltitude(float elapsedHours)
        {
            return Mathf.Lerp(DepartureAltitude, TargetAltitude, TotalDurationHours > 0f ? Mathf.Clamp01(elapsedHours / TotalDurationHours) : 0f);
        }

        public SkyIslandMapParent.SkyIslandMovementState GetHorizontalStateAt(float elapsedHours)
        {
            float normalDistance = GetNormalDistanceFromTotal(HorizontalDistanceTiles);
            if (elapsedHours >= NormalDurationHours || normalDistance <= 0.0001f)
                return SkyIslandMapParent.SkyIslandMovementState.Docking;

            float v0 = InitialSpeedH;
            float vDock = GetDockSpeed();
            float vMax = ScaledMaxSpeedH;
            float a = ScaledAccelH;
            MotionProfile mp = new MotionProfile(v0, vMax, vDock, a, normalDistance);

            if (mp.IsShortDistance)
            {
                if (elapsedHours < mp.TAccPrime)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else if (elapsedHours < mp.TAccPrime + mp.TDecPrime)
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Docking;
            }
            else
            {
                if (elapsedHours < mp.TAcc)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else if (elapsedHours < mp.TAcc + mp.TCruise)
                    return SkyIslandMapParent.SkyIslandMovementState.Cruising;
                else if (elapsedHours < mp.NormalDuration)
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Docking;
            }
        }

        public void Reset()
        {
            DepartureSkyDir = Vector3.zero;
            TargetSkyDir = Vector3.zero;
            DepartureAltitude = SkyIslandAltitude.DefaultAltitude;
            TargetAltitude = SkyIslandAltitude.DefaultAltitude;
            HorizontalDistanceTiles = 0f;
            TotalDurationHours = 0f;
            NormalDurationHours = 0f;
            ScaledMaxSpeedH = 0f;
            ScaledAccelH = 0f;
            VerticalSpeed = 0f;
            InitialSpeedH = 0f;
            GearIndex = 1;
        }

        public void ExposeData()
        {
            Vector3 departureSkyDir = DepartureSkyDir;
            Vector3 targetSkyDir = TargetSkyDir;
            float departureAltitude = DepartureAltitude;
            float targetAltitude = TargetAltitude;
            float horizontalDistanceTiles = HorizontalDistanceTiles;
            float totalDurationHours = TotalDurationHours;
            float normalDurationHours = NormalDurationHours;
            float scaledMaxSpeedH = ScaledMaxSpeedH;
            float scaledAccelH = ScaledAccelH;
            float verticalSpeed = VerticalSpeed;
            float initialSpeedH = InitialSpeedH;
            int gearIndex = GearIndex;

            Scribe_Values.Look(ref departureSkyDir, "departureSkyDir", Vector3.zero);
            Scribe_Values.Look(ref targetSkyDir, "targetSkyDir", Vector3.zero);
            Scribe_Values.Look(ref departureAltitude, "departureAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref targetAltitude, "targetAltitude", SkyIslandAltitude.DefaultAltitude);
            Scribe_Values.Look(ref horizontalDistanceTiles, "horizontalDistanceTiles", 0f);
            Scribe_Values.Look(ref totalDurationHours, "totalDurationHours", 0f);
            Scribe_Values.Look(ref normalDurationHours, "normalDurationHours", 0f);
            Scribe_Values.Look(ref scaledMaxSpeedH, "scaledMaxSpeedH", 0f);
            Scribe_Values.Look(ref scaledAccelH, "scaledAccelH", 0f);
            Scribe_Values.Look(ref verticalSpeed, "verticalSpeed", 0f);
            Scribe_Values.Look(ref initialSpeedH, "initialSpeedH", 0f);
            Scribe_Values.Look(ref gearIndex, "gearIndex", 1);

            DepartureSkyDir = departureSkyDir;
            TargetSkyDir = targetSkyDir;
            DepartureAltitude = departureAltitude;
            TargetAltitude = targetAltitude;
            HorizontalDistanceTiles = horizontalDistanceTiles;
            TotalDurationHours = totalDurationHours;
            NormalDurationHours = normalDurationHours;
            ScaledMaxSpeedH = scaledMaxSpeedH;
            ScaledAccelH = scaledAccelH;
            VerticalSpeed = verticalSpeed;
            InitialSpeedH = initialSpeedH;
            GearIndex = gearIndex;
        }

        public void PostLoadInit(SkyIslandMapParent.SkyIslandMovementState movementState, PlanetTile parentTile)
        {
            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Idle)
            {
                Reset();
                return;
            }

            if (movementState == SkyIslandMapParent.SkyIslandMovementState.Braking)
            {
                TotalDurationHours = 0f;
                NormalDurationHours = 0f;
                ScaledMaxSpeedH = 0f;
                ScaledAccelH = 0f;
                VerticalSpeed = 0f;
                return;
            }

            if (DepartureSkyDir != Vector3.zero && TargetSkyDir != Vector3.zero)
            {
                float avgTileSize = parentTile.Valid ? parentTile.Layer.AverageTileSize : Find.WorldGrid.AverageTileSize;
                float arcAngle = GenMath.SphericalDistance(DepartureSkyDir, TargetSkyDir);
                HorizontalDistanceTiles = arcAngle * SkyIslandAltitude.SurfaceRadius / avgTileSize;

                CalculateMovementProfile(HorizontalDistanceTiles, GearIndex, TargetAltitude - DepartureAltitude, InitialSpeedH,
                    out float totalDuration, out float normalDuration, out float scaledMaxSpeed, out float scaledAccel, out float verticalSpeed);

                TotalDurationHours = totalDuration;
                NormalDurationHours = normalDuration;
                ScaledMaxSpeedH = scaledMaxSpeed;
                ScaledAccelH = scaledAccel;
                VerticalSpeed = verticalSpeed;
            }
        }

        public bool Evaluate(float elapsedHours, out Vector3 direction, out Vector3 velocityDirection, out float speed, out SkyIslandMapParent.SkyIslandMovementState state)
        {
            if (elapsedHours >= TotalDurationHours)
            {
                direction = TargetSkyDir;
                velocityDirection = Vector3.zero;
                speed = 0f;
                state = SkyIslandMapParent.SkyIslandMovementState.Idle;
                return true;
            }

            float normalDistance = GetNormalDistanceFromTotal(HorizontalDistanceTiles);

            float distanceTraveled;
            if (elapsedHours < NormalDurationHours)
            {
                distanceTraveled = GetNormalDistanceAt(elapsedHours, normalDistance);
            }
            else
            {
                float dockElapsed = elapsedHours - NormalDurationHours;
                float dockDistance = Mathf.Min(HorizontalDistanceTiles, SkyIslandMovementConstants.DockDistanceThreshold);
                float currentAltitude = Mathf.Lerp(DepartureAltitude, TargetAltitude, TotalDurationHours > 0f ? Mathf.Clamp01(elapsedHours / TotalDurationHours) : 0f);
                float remainingAltitude = TargetAltitude - currentAltitude;
                float requiredVerticalHours = Mathf.Abs(remainingAltitude) / Mathf.Max(0.0001f, VerticalSpeed);
                float dynamicDockDuration = Mathf.Max(SkyIslandMovementConstants.DockDurationHours, requiredVerticalHours);
                float dockProgress = Mathf.Clamp01(dockElapsed / dynamicDockDuration);
                distanceTraveled = normalDistance + dockProgress * dockDistance;
            }

            float t = HorizontalDistanceTiles > 0f ? distanceTraveled / HorizontalDistanceTiles : 1f;
            direction = Vector3.Slerp(DepartureSkyDir, TargetSkyDir, Mathf.Clamp01(t)).normalized;

            Vector3 axis = Vector3.Cross(DepartureSkyDir, TargetSkyDir);
            velocityDirection = axis.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axis, direction).normalized
                : Vector3.zero;

            speed = GetHorizontalSpeedAt(elapsedHours, normalDistance);

            if (elapsedHours >= NormalDurationHours)
            {
                state = SkyIslandMapParent.SkyIslandMovementState.Docking;
            }
            else
            {
                state = GetHorizontalStateAt(elapsedHours, normalDistance);
            }

            return false;
        }

        private void CalculateMovementProfile(float distanceTiles, int gear, float deltaAltitude, float initialSpeed,
            out float outTotalDuration, out float outNormalDuration, out float outScaledMaxSpeed,
            out float outScaledAccel, out float outVerticalSpeed)
        {
            GearProfile profile = SkyIslandMovementConstants.Gears[gear];
            float vMax = profile.MaxSpeedTilesPerHour;
            float a = profile.AccelerationTilesPerHourSq;
            float vDock = GetDockSpeed();
            float v0 = initialSpeed;

            float normalDistance = GetNormalDistanceFromTotal(distanceTiles);

            if (normalDistance > 0f)
            {
                float minDecelDistance = (v0 * v0 - vDock * vDock) / (2f * a);
                if (normalDistance < minDecelDistance && v0 > vDock + 0.01f)
                {
                    normalDistance = 0f;
                }
            }

            MotionProfile mp = new MotionProfile(v0, vMax, vDock, a, normalDistance);
            float tNormal = mp.NormalDuration;

            float tH = tNormal + SkyIslandMovementConstants.DockDurationHours;
            float tV = Mathf.Abs(deltaAltitude) / SkyIslandMovementConstants.VerticalSpeedKmPerHour;

            if (tH >= tV || tV <= 0f)
            {
                outTotalDuration = tH;
                outNormalDuration = tNormal;
                outScaledMaxSpeed = vMax;
                outScaledAccel = a;
                outVerticalSpeed = tH > 0f ? Mathf.Abs(deltaAltitude) / tH : 0f;
            }
            else
            {
                float scale = tH / tV;
                outTotalDuration = tV;
                outScaledMaxSpeed = vMax * scale;
                outScaledAccel = a * scale;

                MotionProfile scaledMp = new MotionProfile(v0, outScaledMaxSpeed, vDock, outScaledAccel, normalDistance);
                outNormalDuration = scaledMp.NormalDuration;

                outVerticalSpeed = SkyIslandMovementConstants.VerticalSpeedKmPerHour;
            }
        }

        private float GetNormalDistanceAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours <= 0f)
                return 0f;

            if (elapsedHours >= NormalDurationHours || normalDistance <= 0.0001f)
                return normalDistance;

            float v0 = InitialSpeedH;
            float vDock = GetDockSpeed();
            MotionProfile mp = new MotionProfile(v0, ScaledMaxSpeedH, vDock, ScaledAccelH, normalDistance);

            if (mp.IsShortDistance)
            {
                if (elapsedHours <= mp.TAccPrime)
                {
                    return v0 * elapsedHours + 0.5f * mp.A * elapsedHours * elapsedHours;
                }
                else if (elapsedHours <= mp.TAccPrime + mp.TDecPrime)
                {
                    float tIntoDec = elapsedHours - mp.TAccPrime;
                    return mp.DAccPrime + mp.VPeak * tIntoDec - 0.5f * mp.A * tIntoDec * tIntoDec;
                }
                else
                {
                    return normalDistance;
                }
            }
            else
            {
                if (elapsedHours <= mp.TAcc)
                {
                    return v0 * elapsedHours + 0.5f * mp.A * elapsedHours * elapsedHours;
                }
                else if (elapsedHours <= mp.TAcc + mp.TCruise)
                {
                    return mp.DAcc + mp.VMax * (elapsedHours - mp.TAcc);
                }
                else if (elapsedHours <= mp.NormalDuration)
                {
                    float tIntoDec = elapsedHours - mp.TAcc - mp.TCruise;
                    return normalDistance - mp.DDec + mp.VMax * tIntoDec - 0.5f * mp.A * tIntoDec * tIntoDec;
                }
                else
                {
                    return normalDistance;
                }
            }
        }

        private float GetHorizontalSpeedAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours >= NormalDurationHours || normalDistance <= 0.0001f)
            {
                float dockDistance = Mathf.Min(HorizontalDistanceTiles, SkyIslandMovementConstants.DockDistanceThreshold);
                float currentAltitude = Mathf.Lerp(DepartureAltitude, TargetAltitude, TotalDurationHours > 0f ? Mathf.Clamp01(elapsedHours / TotalDurationHours) : 0f);
                float remainingAltitude = TargetAltitude - currentAltitude;
                float requiredVerticalHours = Mathf.Abs(remainingAltitude) / Mathf.Max(0.0001f, VerticalSpeed);
                float dynamicDockDuration = Mathf.Max(SkyIslandMovementConstants.DockDurationHours, requiredVerticalHours);
                return dockDistance / dynamicDockDuration;
            }

            float v0 = InitialSpeedH;
            float vDock = GetDockSpeed();
            MotionProfile mp = new MotionProfile(v0, ScaledMaxSpeedH, vDock, ScaledAccelH, normalDistance);

            if (mp.IsShortDistance)
            {
                if (elapsedHours < mp.TAccPrime)
                    return v0 + mp.A * elapsedHours;
                else if (elapsedHours < mp.TAccPrime + mp.TDecPrime)
                    return mp.VPeak - mp.A * (elapsedHours - mp.TAccPrime);
                else
                    return vDock;
            }
            else
            {
                if (elapsedHours < mp.TAcc)
                    return v0 + mp.A * elapsedHours;
                else if (elapsedHours < mp.TAcc + mp.TCruise)
                    return mp.VMax;
                else if (elapsedHours < mp.NormalDuration)
                    return mp.VMax - mp.A * (elapsedHours - mp.TAcc - mp.TCruise);
                else
                    return vDock;
            }
        }

        private SkyIslandMapParent.SkyIslandMovementState GetHorizontalStateAt(float elapsedHours, float normalDistance)
        {
            if (elapsedHours >= NormalDurationHours || normalDistance <= 0.0001f)
                return SkyIslandMapParent.SkyIslandMovementState.Docking;

            float v0 = InitialSpeedH;
            float vDock = GetDockSpeed();
            MotionProfile mp = new MotionProfile(v0, ScaledMaxSpeedH, vDock, ScaledAccelH, normalDistance);

            if (mp.IsShortDistance)
            {
                if (elapsedHours < mp.TAccPrime)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else if (elapsedHours < mp.TAccPrime + mp.TDecPrime)
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Docking;
            }
            else
            {
                if (elapsedHours < mp.TAcc)
                    return SkyIslandMapParent.SkyIslandMovementState.Accelerating;
                else if (elapsedHours < mp.TAcc + mp.TCruise)
                    return SkyIslandMapParent.SkyIslandMovementState.Cruising;
                else if (elapsedHours < mp.NormalDuration)
                    return SkyIslandMapParent.SkyIslandMovementState.Decelerating;
                else
                    return SkyIslandMapParent.SkyIslandMovementState.Docking;
            }
        }

        private static float GetNormalDistanceFromTotal(float totalDistanceTiles)
        {
            return totalDistanceTiles > SkyIslandMovementConstants.DockDistanceThreshold
                ? totalDistanceTiles - SkyIslandMovementConstants.DockDistanceThreshold
                : 0f;
        }

        public static float GetDockSpeed()
        {
            return SkyIslandMovementConstants.DockDistanceThreshold / SkyIslandMovementConstants.DockDurationHours;
        }
    }
}
