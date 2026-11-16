using System;

namespace DearPompom.Core
{
    /// <summary>
    /// A damped spring that follows a moving target. Use it when the target can change while the
    /// motion is still running (hover scale, a panel the player drags, the camera): a tween would
    /// restart and jerk, a spring keeps its velocity and bends smoothly.
    ///
    /// The step is the exact solution of the spring equation, so the result does not depend on
    /// the frame rate and never explodes after a long frame (a WebGL tab that was in the background).
    /// </summary>
    public struct Spring
    {
        public float Value;
        public float Velocity;

        public Spring(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        /// <param name="target">Where the value should settle.</param>
        /// <param name="frequency">Oscillations per second. 2 is soft, 4 is snappy, 8 is very stiff.</param>
        /// <param name="damping">1 = no overshoot. 0.5 = a visible bounce. Clamped to 0.05..1.</param>
        /// <param name="deltaTime">Seconds since the last step. Negative values are treated as 0.</param>
        public void Step(float target, float frequency, float damping, float deltaTime)
        {
            if (!(deltaTime > 0f) || !(frequency > 0f)) return;
            double zeta = damping < 0.05f ? 0.05 : (damping > 1f ? 1.0 : damping);
            double omega = 2.0 * Math.PI * frequency;
            double t = deltaTime;
            double x0 = Value - target;
            double v0 = Velocity;
            double x, v;
            if (zeta >= 0.999)
            {
                // Critically damped: x = (x0 + (v0 + w x0) t) e^(-w t)
                double c = v0 + omega * x0;
                double e = Math.Exp(-omega * t);
                x = (x0 + c * t) * e;
                v = (v0 - c * omega * t) * e;
            }
            else
            {
                double a = zeta * omega;
                double b = omega * Math.Sqrt(1.0 - zeta * zeta);
                double e = Math.Exp(-a * t);
                double cos = Math.Cos(b * t), sin = Math.Sin(b * t);
                x = e * (x0 * cos + ((v0 + a * x0) / b) * sin);
                v = e * (v0 * cos - ((a * v0 + omega * omega * x0) / b) * sin);
            }
            Value = (float)(target + x);
            Velocity = (float)v;
        }

        /// <summary>True when the value is close enough to stop updating.</summary>
        public bool IsSettled(float target, float epsilon = 0.001f)
        {
            return Math.Abs(Value - target) < epsilon && Math.Abs(Velocity) < epsilon;
        }
    }

    public static class Smooth
    {
        /// <summary>
        /// Frame-rate independent "move a fraction of the way each frame". Replaces the common but
        /// wrong <c>Lerp(current, target, speed * deltaTime)</c>. After <paramref name="halfLife"/>
        /// seconds the remaining distance is halved, at any frame rate.
        /// </summary>
        public static float Damp(float current, float target, float halfLife, float deltaTime)
        {
            if (!(halfLife > 0f)) return target;
            if (!(deltaTime > 0f)) return current;
            double keep = Math.Pow(0.5, deltaTime / (double)halfLife);
            return (float)(target + (current - target) * keep);
        }
    }
}
