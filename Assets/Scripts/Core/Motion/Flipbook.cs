using System;

namespace DearPompom.Core
{
    /// <summary>Frame arithmetic for frame-by-frame clips (PatchClipPlayer). Pure, so it is unit-tested.</summary>
    public static class Flipbook
    {
        /// <summary>
        /// Which frame to show after <paramref name="elapsedSeconds"/>. Looping clips wrap; one-shot
        /// clips hold the last frame. Never returns an index outside 0..frameCount-1.
        /// </summary>
        public static int FrameAt(double elapsedSeconds, float fps, int frameCount, bool loop)
        {
            if (frameCount <= 1 || !(fps > 0f) || !(elapsedSeconds > 0)) return 0;
            long index = (long)Math.Floor(elapsedSeconds * fps + 1e-6);
            if (loop) return (int)(index % frameCount);
            return index >= frameCount ? frameCount - 1 : (int)index;
        }

        /// <summary>True once a one-shot clip has shown its last frame for a full frame duration.</summary>
        public static bool IsFinished(double elapsedSeconds, float fps, int frameCount, bool loop)
        {
            if (loop) return false;
            if (frameCount <= 0 || !(fps > 0f)) return true;
            return elapsedSeconds * fps + 1e-6 >= frameCount;
        }
    }
}
