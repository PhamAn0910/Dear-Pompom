#!/usr/bin/env python3
"""Draw the small white sprites used for particles and UI effects.

Why: the game needs sparkles, soft dots and a breathing ring, nobody can draw them in an editor,
and they must match the crayon look of the hand-drawn art. Each sprite is white with soft,
slightly grainy edges; the game tints it at runtime (Image.color / SpriteRenderer.color).

Usage (from the project root):
  python3 Tools/art/make_fx_sprites.py Assets/Resources/Fx
  python3 Tools/art/make_fx_sprites.py <any_dir> --preview   # also writes preview.png on a dark background

Output: dot.png, sparkle.png, star.png, ring.png, heart.png, petal.png (all 128x128, RGBA).
The result is deterministic (fixed random seed), so re-running never changes the files.

Needs: pip install pillow numpy
"""
import os
import sys

import numpy as np
from PIL import Image

SIZE = 128
SS = 4  # supersampling factor for smooth edges


def grid():
    n = SIZE * SS
    axis = (np.arange(n) + 0.5) / n * 2 - 1  # -1..1
    return np.meshgrid(axis, -axis)          # x to the right, y up


def soft(distance, edge=0.06):
    """1 inside (distance < 0), 0 outside, with a soft edge of the given width."""
    return np.clip(0.5 - distance / edge, 0, 1)


def shape_dot(x, y):
    r = np.sqrt(x * x + y * y)
    return np.clip(1 - r / 0.9, 0, 1) ** 1.6


def shape_sparkle(x, y):
    # Four thin points: the classic "glint". |x|^p + |y|^p < r^p with p < 1 gives concave sides.
    p = 0.55
    d = (np.abs(x) ** p + np.abs(y) ** p) ** (1 / p) - 0.92
    return soft(d, 0.10)


def shape_star(x, y):
    angle = np.arctan2(y, x) + np.pi / 2
    r = np.sqrt(x * x + y * y)
    wave = np.abs(((angle / (2 * np.pi) * 5) % 1) - 0.5) * 2  # 0 at a tip, 1 between tips
    radius = 0.88 - 0.46 * wave
    return soft(r - radius, 0.07)


def shape_ring(x, y):
    r = np.sqrt(x * x + y * y)
    return soft(np.abs(r - 0.78) - 0.07, 0.06)


def shape_heart(x, y):
    xs, ys = x * 1.25, y * 1.25 + 0.15
    d = (xs * xs + ys * ys - 0.55) ** 3 - xs * xs * ys ** 3
    # The formula is not a true distance: divide by its slope so the soft edge has an even width.
    gy, gx = np.gradient(d, 2.0 / d.shape[0])
    d = d / (np.sqrt(gx * gx + gy * gy) + 1e-6)
    return soft(d, 0.06)


def shape_petal(x, y):
    # A leaf: two circular arcs meeting in points at the top and bottom.
    d = np.maximum(np.sqrt((x - 0.55) ** 2 + y * y), np.sqrt((x + 0.55) ** 2 + y * y)) - 1.0
    return soft(d, 0.06)


SHAPES = {
    "dot": shape_dot, "sparkle": shape_sparkle, "star": shape_star,
    "ring": shape_ring, "heart": shape_heart, "petal": shape_petal,
}


def render(name, rng):
    x, y = grid()
    alpha = SHAPES[name](x, y)
    # Crayon grain: coarse noise eats into the alpha, strongest near the edges.
    coarse = rng.random((SIZE // 2, SIZE // 2))
    grain = np.asarray(Image.fromarray((coarse * 255).astype(np.uint8)).resize(alpha.shape[::-1], Image.BICUBIC)) / 255.0
    if name != "dot":
        alpha = alpha * (0.80 + 0.20 * grain) * np.clip(alpha * 3 + grain, 0, 1)
    small = Image.fromarray((np.clip(alpha, 0, 1) * 255).astype(np.uint8)).resize((SIZE, SIZE), Image.LANCZOS)
    rgba = np.zeros((SIZE, SIZE, 4), np.uint8)
    rgba[:, :, :3] = 255
    rgba[:, :, 3] = np.asarray(small)
    return Image.fromarray(rgba)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1:
        sys.exit(__doc__)
    out = args[0]
    os.makedirs(out, exist_ok=True)
    rng = np.random.default_rng(20261116)
    images = []
    for name in SHAPES:
        image = render(name, rng)
        image.save(os.path.join(out, name + ".png"))
        images.append(image)
        print("wrote " + os.path.join(out, name + ".png"))
    if "--preview" in sys.argv:
        sheet = Image.new("RGBA", (SIZE * len(images), SIZE), (60, 44, 36, 255))
        tints = [(255, 244, 200), (255, 226, 120), (255, 200, 90), (190, 230, 255), (255, 150, 170), (150, 210, 130)]
        for i, (image, tint) in enumerate(zip(images, tints)):
            colour = Image.new("RGBA", image.size, tint + (255,))
            sheet.paste(colour, (i * SIZE, 0), image)
        sheet.save(os.path.join(out, "preview.png"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
