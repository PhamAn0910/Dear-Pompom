using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// The JSON written by Tools/art/extract_patches.py, field for field (Unity's JsonUtility maps
    /// by field name, so do not rename anything here without changing the tool).
    /// Format and coordinate rules: docs/guides/animation-assets.md.
    /// </summary>
    [Serializable]
    public class PatchClipData
    {
        public string name = "";
        /// <summary>"patch" (background + changed tiles) or "trim" (transparent frames, no background).</summary>
        public string mode = "";
        /// <summary>Size of the original frames in pixels.</summary>
        public int width;
        public int height;
        public float fps = 12f;
        public bool loop = true;
        /// <summary>Resource name of the background picture; empty in trim mode.</summary>
        public string background = "";
        public List<string> sheets = new List<string>();
        /// <summary>Where a patch is drawn, in image pixels: origin top-left, y grows downward.</summary>
        public List<PatchRect> regions = new List<PatchRect>();
        /// <summary>Where a patch sits inside its sheet, same coordinate convention.</summary>
        public List<PatchSource> patches = new List<PatchSource>();
        public List<PatchFrame> frames = new List<PatchFrame>();

        public int FrameCount { get { return frames == null ? 0 : frames.Count; } }
        public int RegionCount { get { return regions == null ? 0 : regions.Count; } }

        /// <summary>Returns null when the clip is usable, otherwise a message saying what is wrong.</summary>
        public string Validate()
        {
            if (width <= 0 || height <= 0) return "width/height missing";
            if (!(fps > 0f)) return "fps must be positive";
            if (frames == null || frames.Count == 0) return "no frames";
            if (sheets == null || sheets.Count == 0) return "no sheets";
            if (regions == null || patches == null) return "regions/patches missing";
            if (mode == "patch" && string.IsNullOrEmpty(background)) return "patch mode needs a background";
            for (int p = 0; p < patches.Count; p++)
            {
                PatchSource source = patches[p];
                if (source == null || source.sheet < 0 || source.sheet >= sheets.Count) return "patch " + p + " points at a missing sheet";
                if (source.w <= 0 || source.h <= 0) return "patch " + p + " has no size";
            }
            for (int f = 0; f < frames.Count; f++)
            {
                if (frames[f] == null || frames[f].draw == null) return "frame " + f + " is empty";
                foreach (PatchDraw d in frames[f].draw)
                {
                    if (d == null || d.region < 0 || d.region >= regions.Count) return "frame " + f + " uses a missing region";
                    if (d.patch < 0 || d.patch >= patches.Count) return "frame " + f + " uses a missing patch";
                }
            }
            return null;
        }

        /// <summary>
        /// table[frame][region] = patch index to show in that region, or -1 to show nothing
        /// (the background is visible there). The player keeps one renderer per region and only
        /// has to compare two rows to know what changes between frames.
        /// </summary>
        public int[][] BuildFrameTable()
        {
            var table = new int[FrameCount][];
            for (int f = 0; f < table.Length; f++)
            {
                var row = new int[RegionCount];
                for (int r = 0; r < row.Length; r++) row[r] = -1;
                foreach (PatchDraw d in frames[f].draw) row[d.region] = d.patch;
                table[f] = row;
            }
            return table;
        }

        /// <summary>
        /// Centre of a region relative to the centre of the picture, in world units, with y up
        /// (Unity's convention). Multiply by the object's scale as usual.
        /// </summary>
        public void RegionCentre(int region, float pixelsPerUnit, out float x, out float y)
        {
            PatchRect r = regions[region];
            x = (r.x + r.w * 0.5f - width * 0.5f) / pixelsPerUnit;
            y = (height * 0.5f - (r.y + r.h * 0.5f)) / pixelsPerUnit;
        }

        /// <summary>
        /// A patch rectangle converted to texture coordinates (origin bottom-left), which is what
        /// UnityEngine.Sprite.Create expects.
        /// </summary>
        public void PatchTextureRect(int patch, int sheetHeight, out float x, out float y, out float w, out float h)
        {
            PatchSource p = patches[patch];
            x = p.x;
            y = sheetHeight - p.y - p.h;
            w = p.w;
            h = p.h;
        }
    }

    [Serializable]
    public class PatchRect
    {
        public int x, y, w, h;
    }

    [Serializable]
    public class PatchSource
    {
        public int sheet;
        public int x, y, w, h;
    }

    [Serializable]
    public class PatchFrame
    {
        public List<PatchDraw> draw = new List<PatchDraw>();
    }

    [Serializable]
    public class PatchDraw
    {
        public int region;
        public int patch;
    }
}
