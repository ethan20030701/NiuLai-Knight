#!/usr/bin/env python3
"""Convert the wiki's The_Knight_Death.mp4 (fixed-camera game footage) into
transparent sprite frames for the mod's death animation.

The camera never moves, so a per-pixel temporal median across all frames is
an exact background plate; per-frame colour distance from that plate becomes
the alpha matte (white knight + black void pop hard against the blue-grey
City of Tears backdrop).

Output: HollowKnight/images/charui/death/death_f###.png
  - all frames share one CANVAS and one ANCHOR: the knight's feet-centre in
    source space maps to a fixed canvas point that lines up with the combat
    sprite's feet when the AnimatedSprite2D is centred on %Visuals. Frame 0
    is the living dash pose on the same canvas, so the death start is seamless.
"""

import os
import numpy as np
from PIL import Image, ImageFilter

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(REPO, "tools", "wiki_art_raw", "raw_frames")
OUT = os.path.join(REPO, "HollowKnight", "images", "charui", "death")

FRAME_STRIDE = 2      # 60fps source -> every 2nd frame = 30fps output
DIFF_LOW = 0.12       # alpha ramp start (colour distance, 0..1)
DIFF_HIGH = 0.26      # alpha ramp end (fully opaque)
MAX_OPAQUE_FRACTION = 0.45  # fade-to-black / washout frames are dropped

# crop away the stage platform and frame edges: they only show up when the
# screen shake shifts the (otherwise static) backdrop during the burst
SRC_CROP = (6, 0, 394, 232)
# knight's feet centre in cropped-source pixels (screen shake carries through)
ANCHOR_SRC = (188, 226)
SCALE = 1.55          # matches the standing combat figure (~290px tall)
CANVAS = (640, 480)   # the 480x480 combat canvas sits centred inside
FEET_ON_CANVAS = (320, 478)  # combat feet (240,478) + canvas offset (80,0)
EDGE_FADE = 24        # alpha ramp at the canvas border (hides blob clipping)
KNIGHT_PROBE_FRAME = 6


def load_frames():
    files = sorted(f for f in os.listdir(RAW) if f.startswith("f") and f.endswith(".png"))
    return [np.asarray(Image.open(os.path.join(RAW, f)).convert("RGB"),
                       dtype=np.float32) / 255.0 for f in files]


def smoothstep(x, lo, hi):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def edge_fade(img):
    a = np.asarray(img).astype(np.float32)
    h, w = a.shape[:2]
    ramp = np.ones((h, w), dtype=np.float32)
    r = np.arange(w, dtype=np.float32)
    ramp *= np.clip(r / EDGE_FADE, 0, 1)
    ramp *= np.clip((w - 1 - r) / EDGE_FADE, 0, 1)
    a[..., 3] *= ramp
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def main():
    frames = load_frames()
    bg = np.median(np.stack(frames[::3]), axis=0)
    crop = SRC_CROP
    frames = [fr[crop[1]:crop[3], crop[0]:crop[2]] for fr in frames]
    bg = bg[crop[1]:crop[3], crop[0]:crop[2]]

    kept = frames[::FRAME_STRIDE]
    alphas = [smoothstep(np.max(np.abs(fr - bg), axis=2), DIFF_LOW, DIFF_HIGH)
              for fr in kept]
    alphas = [np.asarray(Image.fromarray((a * 255).astype(np.uint8), "L")
                         .filter(ImageFilter.GaussianBlur(0.8)),
                         dtype=np.float32) / 255.0 for a in alphas]

    pairs = [(fr, a) for fr, a in zip(kept, alphas)
             if (a > 0.02).mean() <= MAX_OPAQUE_FRACTION]  # fade-to-black etc.

    # trailing cleanup: late frames catch City-of-Tears rain shimmer as sparse
    # noise; drop end frames noisier than twice the tail baseline
    tail = [float((a > 0.02).mean()) for _, a in pairs[-10:]]
    baseline = sorted(tail)[len(tail) // 2]
    while pairs and float((pairs[-1][1] > 0.02).mean()) > 2 * baseline:
        pairs.pop()

    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        os.remove(os.path.join(OUT, f))

    # frame 0: fully transparent — the sprite sits visible over the living
    # body at all times and must render nothing until the death plays; the
    # C# side hides the living visuals on the first real frame change
    Image.new("RGBA", CANVAS, (0, 0, 0, 0)).save(
        os.path.join(OUT, "death_f000.png"), optimize=True)

    n = 1
    for fr, a in pairs:
        rgba = np.dstack([fr, a])
        img = Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA")
        img = img.resize((round(img.width * SCALE), round(img.height * SCALE)),
                         Image.LANCZOS)
        img = edge_fade(img)
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        px = FEET_ON_CANVAS[0] - round(ANCHOR_SRC[0] * SCALE)
        py = FEET_ON_CANVAS[1] - round(ANCHOR_SRC[1] * SCALE)
        canvas.alpha_composite(img, (px, py))
        canvas.save(os.path.join(OUT, f"death_f{n:03d}.png"), optimize=True)
        n += 1
    print(f"wrote {n} frames ({n} @ 30fps = {n / 30:.2f}s)")


if __name__ == "__main__":
    main()
