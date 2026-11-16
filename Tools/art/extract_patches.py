#!/usr/bin/env python3
"""Turn a folder of full-screen animation frames into a small "patch clip".

Why: the 1.0 art is frame-by-frame, and every frame is a whole 1920x1080 picture although
only a few percent of the pixels change. Loading 24 such frames costs about 24 x 8 MB of
texture memory per clip. This tool keeps ONE background picture and cuts out only the
square tiles that actually change, packs them into sheets, and writes a JSON timeline.
The runtime (PatchClipPlayer, see docs/guides/animation-assets.md) draws the background and
swaps the small tiles on top. With --threshold 0 the result is pixel-identical to the source.

Two modes, chosen automatically:
  patch  frames are opaque (JPG, or PNG without transparency). The picture is cut into a grid of
         --tile pixel squares. For every grid cell the most common content becomes the background;
         a frame stores a tile only where it differs from that background. Identical tiles are
         stored once.
  trim   frames have transparency (a character on an empty canvas): no background, each frame
         is cropped to the union bounding box of all frames

Usage (from the project root):
  python3 Tools/art/extract_patches.py <frames_dir> <out_dir> --name home_morning_study --fps 12
  python3 Tools/art/extract_patches.py <frames_dir> <out_dir> --name x --verify   # prints quality numbers

Output in <out_dir>:
  <name>_base.png        background (patch mode only)
  <name>_sheet0.png ...  packed patches, power-of-two size, at most --sheet pixels wide/high
  <name>.json            the clip description (format documented in docs/guides/animation-assets.md)

Needs: pip install pillow numpy
"""
import argparse
import glob
import hashlib
import json
import os
import sys

import numpy as np
from PIL import Image

PAD = 2  # pixels of edge extrusion around each patch, so bilinear filtering never samples a neighbour


def load_frames(folder):
    files = sorted(
        f for f in glob.glob(os.path.join(folder, "*"))
        if os.path.splitext(f)[1].lower() in (".png", ".jpg", ".jpeg")
    )
    if not files:
        sys.exit("no .png/.jpg frames in " + folder)
    frames = [np.asarray(Image.open(f).convert("RGBA")) for f in files]
    shape = frames[0].shape
    for f, a in zip(files, frames):
        if a.shape != shape:
            sys.exit("frame size differs: %s is %s, expected %s" % (f, a.shape, shape))
    return files, frames


def next_pow2(n):
    p = 1
    while p < n:
        p *= 2
    return p


class ShelfPacker:
    """Very small shelf packer. Good enough here: almost every patch is one tile, so shelves fill evenly."""

    def __init__(self, max_size):
        self.max_size = max_size
        self.sheets = []  # each: dict(x, y, shelf_h, used_w, used_h, items)
        self._new_sheet()

    def _new_sheet(self):
        self.sheets.append({"x": 0, "y": 0, "shelf_h": 0, "used_w": 0, "used_h": 0})

    def add(self, w, h):
        """Returns (sheet_index, x, y) of the top-left corner of a w x h cell."""
        if w > self.max_size or h > self.max_size:
            sys.exit("a patch of %dx%d does not fit in a %d sheet; raise --sheet" % (w, h, self.max_size))
        s = self.sheets[-1]
        if s["x"] + w > self.max_size:  # next shelf
            s["x"] = 0
            s["y"] += s["shelf_h"]
            s["shelf_h"] = 0
        if s["y"] + h > self.max_size:  # next sheet
            self._new_sheet()
            s = self.sheets[-1]
        x, y = s["x"], s["y"]
        s["x"] += w
        s["shelf_h"] = max(s["shelf_h"], h)
        s["used_w"] = max(s["used_w"], x + w)
        s["used_h"] = max(s["used_h"], y + h)
        return len(self.sheets) - 1, x, y


def extrude(patch):
    """Pad a patch by PAD pixels on every side by repeating its edge pixels."""
    return np.pad(patch, ((PAD, PAD), (PAD, PAD), (0, 0)), mode="edge")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("frames_dir")
    ap.add_argument("out_dir")
    ap.add_argument("--name", required=True, help="clip id, lower_snake_case; becomes the file name prefix")
    ap.add_argument("--fps", type=float, default=12.0)
    ap.add_argument("--no-loop", action="store_true")
    ap.add_argument("--tile", type=int, default=64, help="tile size in pixels (patch mode); keep it a multiple of 8")
    ap.add_argument("--threshold", type=int, default=0, help="0-255; a tile is stored when any pixel differs from the background by more than this. 0 = lossless")
    ap.add_argument("--sheet", type=int, default=2048, help="maximum sheet size (power of two)")
    ap.add_argument("--verify", action="store_true", help="rebuild every frame from the output and print the error")
    args = ap.parse_args()

    files, frames = load_frames(args.frames_dir)
    h, w = frames[0].shape[:2]
    has_alpha = any((a[:, :, 3] < 250).any() for a in frames)
    mode = "trim" if has_alpha else "patch"
    os.makedirs(args.out_dir, exist_ok=True)

    packer = ShelfPacker(args.sheet)
    placed = {}        # content hash -> (sheet, x, y, w, h)
    pixels = []        # (sheet, x, y, padded_patch_array)
    timeline = []      # per frame: list of {"region": i, "patch": k}
    regions = []       # [x0, y0, x1, y1] in image pixels; a tile cell (patch mode) or the trim box
    patches = []       # list of {"sheet", "x", "y", "w", "h"} (inner rectangle, without padding)

    def place(patch):
        key = hashlib.md5(patch.tobytes()).hexdigest()
        if key not in placed:
            ph, pw = patch.shape[:2]
            sheet, x, y = packer.add(pw + 2 * PAD, ph + 2 * PAD)
            pixels.append((sheet, x, y, extrude(patch)))
            patches.append({"sheet": sheet, "x": x + PAD, "y": y + PAD, "w": pw, "h": ph})
            placed[key] = len(patches) - 1
        return placed[key]

    if mode == "patch":
        rgb = [a[:, :, :3] for a in frames]
        t = args.tile
        base = np.zeros((h, w, 3), np.uint8)
        regions = []
        timeline = [[] for _ in frames]
        opaque = lambda crop: np.dstack([crop, np.full(crop.shape[:2], 255, np.uint8)])
        for y0 in range(0, h, t):
            for x0 in range(0, w, t):
                y1, x1 = min(y0 + t, h), min(x0 + t, w)
                crops = [a[y0:y1, x0:x1] for a in rgb]
                keys = [c.tobytes() for c in crops]
                # Background = the content this cell shows in the most frames.
                common = max(set(keys), key=keys.count)
                base_crop = crops[keys.index(common)]
                base[y0:y1, x0:x1] = base_crop
                region = -1
                for f, crop in enumerate(crops):
                    if keys[f] == common:
                        continue
                    if args.threshold > 0:
                        diff = np.abs(crop.astype(np.int16) - base_crop.astype(np.int16)).max()
                        if diff <= args.threshold:
                            continue
                    if region < 0:
                        regions.append([x0, y0, x1, y1])
                        region = len(regions) - 1
                    timeline[f].append({"region": region, "patch": place(opaque(crop))})
        Image.fromarray(base).save(os.path.join(args.out_dir, args.name + "_base.png"))
    else:
        alpha_union = np.zeros((h, w), bool)
        for a in frames:
            alpha_union |= a[:, :, 3] > 8
        ys, xs = np.where(alpha_union)
        regions = [[int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]]
        x0, y0, x1, y1 = regions[0]
        for a in frames:
            timeline.append([{"region": 0, "patch": place(a[y0:y1, x0:x1])}])
        base = None

    # Write sheets (power-of-two so GPU block compression never has to rescale them).
    sheet_sizes = []
    for index, s in enumerate(packer.sheets):
        sw, sh = next_pow2(max(s["used_w"], 4)), next_pow2(max(s["used_h"], 4))
        canvas = np.zeros((sh, sw, 4), np.uint8)
        for sheet, x, y, patch in pixels:
            if sheet == index:
                canvas[y:y + patch.shape[0], x:x + patch.shape[1]] = patch
        # Opaque clips are saved without an alpha channel, so Unity picks the cheaper opaque
        # GPU format (half the memory of a format with alpha).
        image = Image.fromarray(canvas if mode == "trim" else canvas[:, :, :3])
        image.save(os.path.join(args.out_dir, "%s_sheet%d.png" % (args.name, index)))
        sheet_sizes.append([sw, sh])

    clip = {
        "name": args.name,
        "mode": mode,
        "width": w,
        "height": h,
        "fps": args.fps,
        "loop": not args.no_loop,
        "background": (args.name + "_base") if mode == "patch" else "",
        "sheets": ["%s_sheet%d" % (args.name, i) for i in range(len(sheet_sizes))],
        # Rectangles use image coordinates: origin top-left, y grows downward.
        "regions": [{"x": r[0], "y": r[1], "w": r[2] - r[0], "h": r[3] - r[1]} for r in regions],
        "patches": patches,
        "frames": [{"draw": entry} for entry in timeline],
    }
    with open(os.path.join(args.out_dir, args.name + ".json"), "w", encoding="utf-8") as handle:
        json.dump(clip, handle, separators=(",", ":"))  # compact: a clip can hold thousands of entries

    before = len(frames) * w * h * 4
    after = sum(sw * sh * 4 for sw, sh in sheet_sizes) + (w * h * 4 if mode == "patch" else 0)
    print("%s: mode=%s frames=%d regions=%d unique_patches=%d sheets=%s" % (
        args.name, mode, len(frames), len(regions), len(patches), sheet_sizes))
    busiest = max(len(entry) for entry in timeline)
    print("  most patches drawn in one frame: %d" % busiest)
    print("  uncompressed texture memory: %.1f MB -> %.1f MB (%.0f%% saved)" % (
        before / 1048576.0, after / 1048576.0, 100.0 * (1 - after / float(before))))

    if args.verify:
        sheets = [np.asarray(Image.open(os.path.join(args.out_dir, n + ".png")).convert("RGBA")) for n in clip["sheets"]]
        worst_psnr, worst_max = 999.0, 0
        for original, entry in zip(frames, timeline):
            canvas = np.dstack([base, np.full((h, w), 255, np.uint8)]).copy() if mode == "patch" else np.zeros((h, w, 4), np.uint8)
            for d in entry:
                r, p = clip["regions"][d["region"]], patches[d["patch"]]
                canvas[r["y"]:r["y"] + r["h"], r["x"]:r["x"] + r["w"]] = \
                    sheets[p["sheet"]][p["y"]:p["y"] + p["h"], p["x"]:p["x"] + p["w"]]
            visible = original[:, :, 3:4] > 8 if mode == "trim" else True
            err = np.abs(canvas[:, :, :3].astype(np.int16) - original[:, :, :3].astype(np.int16)) * visible
            mse = float((err.astype(np.float64) ** 2).mean())
            psnr = 99.0 if mse == 0 else 10 * np.log10(255.0 ** 2 / mse)
            worst_psnr, worst_max = min(worst_psnr, psnr), max(worst_max, int(err.max()))
        print("  verify: worst frame PSNR %.1f dB, largest single-pixel error %d/255" % (worst_psnr, worst_max))
        print("  (PSNR above 40 dB is visually identical; a large single-pixel error is fine if PSNR is high)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
