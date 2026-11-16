using System;

namespace DearPompom.Core
{
    /// <summary>
    /// Easing curves. Every tween in the game picks one of these; do not write a new curve inline.
    /// Which one to use is decided in docs/guides/game-feel.md ("Bảng chuyển động chuẩn").
    /// </summary>
    public enum EaseType
    {
        Linear,
        InSine, OutSine, InOutSine,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        OutQuart,
        /// <summary>Starts by pulling back slightly. For things that leave.</summary>
        InBack,
        /// <summary>Overshoots the end by about 10% and settles. For things that arrive or pop.</summary>
        OutBack,
    }

    public static class Ease
    {
        private const double BackOvershoot = 1.70158;

        /// <summary>
        /// Maps linear progress t (clamped to 0..1) to eased progress. Always returns exactly 0 at
        /// t = 0 and exactly 1 at t = 1. InBack and OutBack go slightly outside 0..1 in between.
        /// </summary>
        public static float Evaluate(EaseType type, float t)
        {
            if (!(t > 0f)) return 0f; // also catches NaN
            if (t >= 1f) return 1f;
            double x = t;
            double y;
            switch (type)
            {
                case EaseType.InSine: y = 1 - Math.Cos(x * Math.PI / 2); break;
                case EaseType.OutSine: y = Math.Sin(x * Math.PI / 2); break;
                case EaseType.InOutSine: y = -(Math.Cos(Math.PI * x) - 1) / 2; break;
                case EaseType.InQuad: y = x * x; break;
                case EaseType.OutQuad: y = 1 - (1 - x) * (1 - x); break;
                case EaseType.InOutQuad: y = x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2; break;
                case EaseType.InCubic: y = x * x * x; break;
                case EaseType.OutCubic: y = 1 - Math.Pow(1 - x, 3); break;
                case EaseType.InOutCubic: y = x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2; break;
                case EaseType.OutQuart: y = 1 - Math.Pow(1 - x, 4); break;
                case EaseType.InBack: y = (BackOvershoot + 1) * x * x * x - BackOvershoot * x * x; break;
                case EaseType.OutBack: y = 1 + (BackOvershoot + 1) * Math.Pow(x - 1, 3) + BackOvershoot * Math.Pow(x - 1, 2); break;
                default: y = x; break;
            }
            return (float)y;
        }

        /// <summary>Unclamped linear interpolation (needed because OutBack overshoots).</summary>
        public static float Lerp(float from, float to, float easedT)
        {
            return from + (to - from) * easedT;
        }
    }
}
