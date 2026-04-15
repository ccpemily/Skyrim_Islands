using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public static class SkyIslandLocalTimeUtility
    {
        private static readonly SimpleCurve SunPeekAroundDegreesFactorCurve = new SimpleCurve
        {
            new CurvePoint(70f, 1f),
            new CurvePoint(75f, 0.05f)
        };

        private static readonly SimpleCurve SunOffsetFractionFromLatitudeCurve = new SimpleCurve
        {
            new CurvePoint(70f, 0.2f),
            new CurvePoint(75f, 1.5f)
        };

        public static bool TryGetContinuousSurfaceLocation(Map? map, out Vector2 longLat)
        {
            if (map?.Parent is SkyIslandMapParent island)
            {
                longLat = island.CurrentSurfaceLongLat;
                return true;
            }

            longLat = Vector2.zero;
            return false;
        }

        public static bool TryGetContinuousSurfaceLocation(Thing? thing, out Vector2 longLat)
        {
            if (thing?.MapHeld?.Parent is SkyIslandMapParent island)
            {
                longLat = island.CurrentSurfaceLongLat;
                return true;
            }

            longLat = Vector2.zero;
            return false;
        }

        public static int DayOfYear(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayOfYear(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayOfYear(map.Tile);
        }

        public static int HourOfDay(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.HourOfDay(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.HourOfDay(map.Tile);
        }

        public static int DayOfTwelfth(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayOfTwelfth(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayOfTwelfth(map.Tile);
        }

        public static Twelfth Twelfth(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.Twelfth(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.Twelfth(map.Tile);
        }

        public static Season Season(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.Season(GenTicks.TicksAbs, longLat)
                : GenLocalDate.Season(map.Tile);
        }

        public static int Year(Map map)
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return 5500;
            }

            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.Year(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.Year(map.Tile);
        }

        public static int DayOfSeason(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayOfSeason(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayOfSeason(map.Tile);
        }

        public static int DayOfQuadrum(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayOfQuadrum(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayOfQuadrum(map.Tile);
        }

        public static int DayTick(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayTick(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayTick(map.Tile);
        }

        public static float DayPercent(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.DayPercent(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.DayPercent(map.Tile);
        }

        public static float YearPercent(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.YearPercent(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.YearPercent(map.Tile);
        }

        public static int HourInteger(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.HourInteger(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.HourInteger(map.Tile);
        }

        public static float HourFloat(Map map)
        {
            return TryGetContinuousSurfaceLocation(map, out Vector2 longLat)
                ? GenDate.HourFloat(GenTicks.TicksAbs, longLat.x)
                : GenLocalDate.HourFloat(map.Tile);
        }

        public static float CelestialSunGlow(Map map, int ticksAbs)
        {
            if (!TryGetContinuousSurfaceLocation(map, out Vector2 longLat))
            {
                return GenCelestial.CelestialSunGlow(map.Tile, ticksAbs);
            }

            return CelestialSunGlowPercent(longLat.y, GenDate.DayOfYear(ticksAbs, longLat.x), GenDate.DayPercent(ticksAbs, longLat.x));
        }

        public static void DrawDateReadoutForSkyIsland(Rect dateRect, Map map)
        {
            if (!TryGetContinuousSurfaceLocation(map, out Vector2 longLat))
            {
                return;
            }

            int ticksAbs = Find.TickManager.TicksAbs;
            int hour = GenDate.HourInteger(ticksAbs, longLat.x);
            Season season = GenDate.Season(ticksAbs, longLat);
            string seasonText = (!WorldRendererUtility.WorldSelected && Find.CurrentMap != null) ? season.LabelCap() : string.Empty;
            string dateString = GenDate.DateReadoutStringAt(ticksAbs, longLat);
            string hourString = Prefs.TwelveHourClockMode ? FormatHour12(hour) : (hour.ToString() + "LetterHour".Translate());

            Text.Font = GameFont.Small;
            float width = Mathf.Max(Mathf.Max(Text.CalcSize(hourString).x, Text.CalcSize(dateString).x), Text.CalcSize(seasonText).x) + 7f;
            dateRect.xMin = dateRect.xMax - width;

            if (Mouse.IsOver(dateRect))
            {
                Widgets.DrawHighlight(dateRect);
            }

            Widgets.BeginGroup(dateRect);
            Text.Anchor = TextAnchor.UpperRight;

            Rect rect = dateRect.AtZero();
            rect.xMax -= 7f;
            Widgets.Label(rect, hourString);
            rect.yMin += 26f;
            Widgets.Label(rect, dateString);
            rect.yMin += 26f;
            if (!seasonText.NullOrEmpty())
            {
                Widgets.Label(rect, seasonText);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndGroup();

            if (Mouse.IsOver(dateRect))
            {
                string tooltip = "DateReadoutTip".Translate(
                    GenDate.DaysPassed,
                    15,
                    season.LabelCap(),
                    15,
                    GenDate.Quadrum(ticksAbs, longLat.x).Label(),
                    BuildQuadrumTooltip(longLat.y));
                TooltipHandler.TipRegion(dateRect, new TipSignal(tooltip, 86423));
            }
        }

        private static string FormatHour12(int hour)
        {
            TaggedString suffix = hour >= 12 ? "PM".Translate() : "AM".Translate();
            if (hour == 0)
            {
                return $"12 {suffix}";
            }

            if (hour > 12)
            {
                return $"{hour - 12} {suffix}";
            }

            return $"{hour} {suffix}";
        }

        private static string BuildQuadrumTooltip(float latitude)
        {
            string result = string.Empty;
            for (int i = 0; i < 4; i++)
            {
                Quadrum quadrum = (Quadrum)i;
                result += quadrum.Label() + " - " + quadrum.GetSeason(latitude).LabelCap() + "\n";
            }

            return result.TrimEndNewlines();
        }

        private static float CelestialSunGlowPercent(float latitude, int dayOfYear, float dayPercent)
        {
            Vector3 normal = SurfaceNormal(latitude);
            Vector3 sun = SunPosition(latitude, dayOfYear, dayPercent);
            float dot = Vector3.Dot(normal.normalized, sun);
            return Mathf.Clamp01(Mathf.InverseLerp(0f, 0.7f, dot));
        }

        private static Vector3 SurfaceNormal(float latitude)
        {
            Vector3 vector = new Vector3(1f, 0f, 0f);
            return Quaternion.AngleAxis(latitude, new Vector3(0f, 0f, 1f)) * vector;
        }

        private static Vector3 SunPosition(float latitude, int dayOfYear, float dayPercent)
        {
            Vector3 target = SurfaceNormal(latitude);
            Vector3 current = SunPositionUnmodified(dayOfYear, dayPercent, new Vector3(1f, 0f, 0f), latitude);
            float factor = SunPeekAroundDegreesFactorCurve.Evaluate(latitude);
            current = Vector3.RotateTowards(current, target, 0.33161256f * factor, 9999999f);
            float latitudeFactor = Mathf.InverseLerp(60f, 0f, Mathf.Abs(latitude));
            if (latitudeFactor > 0f)
            {
                current = Vector3.RotateTowards(current, target, 6.2831855f * (17f * latitudeFactor / 360f), 9999999f);
            }

            return current.normalized;
        }

        private static Vector3 SunPositionUnmodified(float dayOfYear, float dayPercent, Vector3 initialSunPos, float latitude = 0f)
        {
            Vector3 point = initialSunPos * 100f;
            float seasonal = -Mathf.Cos(dayOfYear / 60f * Mathf.PI * 2f);
            point.y += seasonal * 100f * SunOffsetFractionFromLatitudeCurve.Evaluate(latitude);
            point = Quaternion.AngleAxis((dayPercent - 0.5f) * 360f, Vector3.up) * point;
            return point.normalized;
        }
    }
}
