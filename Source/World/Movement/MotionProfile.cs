using UnityEngine;

namespace SkyrimIslands.World.Movement
{
    public readonly struct MotionProfile
    {
        public readonly float V0;
        public readonly float VMax;
        public readonly float VDock;
        public readonly float A;
        public readonly float NormalDistance;

        public MotionProfile(float v0, float vMax, float vDock, float a, float normalDistance)
        {
            V0 = v0;
            VMax = vMax;
            VDock = vDock;
            A = a;
            NormalDistance = normalDistance;
        }

        public float TAcc => (VMax - V0) / A;
        public float DAcc => (VMax * VMax - V0 * V0) / (2f * A);
        public float TDec => (VMax - VDock) / A;
        public float DDec => (VMax * VMax - VDock * VDock) / (2f * A);

        public bool IsShortDistance => NormalDistance <= DAcc + DDec;

        public float VPeak
        {
            get
            {
                if (IsShortDistance)
                {
                    float vPeakSq = (2f * A * NormalDistance + V0 * V0 + VDock * VDock) / 2f;
                    return Mathf.Sqrt(Mathf.Max(0f, vPeakSq));
                }
                return VMax;
            }
        }

        public float TAccPrime => (VPeak - V0) / A;
        public float DAccPrime => (VPeak * VPeak - V0 * V0) / (2f * A);
        public float TDecPrime => (VPeak - VDock) / A;

        public float TCruise => IsShortDistance ? 0f : (NormalDistance - DAcc - DDec) / VMax;

        public float NormalDuration => IsShortDistance ? TAccPrime + TDecPrime : TAcc + TDec + TCruise;
    }
}
