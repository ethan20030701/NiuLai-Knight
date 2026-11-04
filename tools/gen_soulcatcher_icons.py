#!/usr/bin/env python3
"""Generate Soul Catcher relic icons for the HollowKnight (STS2) mod.

Draws a stylized charm - a magenta glass orb with a swirling soul wisp,
hung from a brass loop - with 4x supersampled anti-aliasing. Stdlib only.

Outputs into HollowKnight/images/relics/ (sizes match the template placeholders):
  soul_catcher.png          (94x94,   small icon)
  soul_catcher_outline.png  (94x94,   white silhouette)
  big/soul_catcher.png      (256x256, big icon)

Run from repo root:  python tools/gen_soulcatcher_icons.py
"""

import math
import os
import struct
import zlib
from array import array

SS = 4  # supersampling factor per axis

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(REPO_ROOT, "HollowKnight", "images", "relics")

SMALL_SIZE = 94   # from images/relics/relic.png
BIG_SIZE = 256    # from images/relics/big/relic.png

# palette (r, g, b in 0..1)
DARK = (0.075, 0.045, 0.090)      # outer silhouette edging
BRASS = (0.780, 0.620, 0.240)
BRASS_LIGHT = (0.990, 0.880, 0.550)
GLASS_IN = (0.870, 0.300, 0.560)
GLASS_OUT = (0.300, 0.070, 0.260)
RIM_LIGHT = (1.000, 0.630, 0.800)
WISP = (0.960, 0.990, 1.000)
WISP_BRIGHT = (1.000, 1.000, 1.000)
GLOW = (0.750, 0.910, 1.000)
OUTLINE = (1.0, 1.0, 1.0)         # silhouette color for *_outline.png


class Img:
    """Straight-alpha float canvas rendered at SSx resolution."""

    def __init__(self, size):
        self.size = size
        self.n = size * SS
        self.buf = array("f", bytes(16 * self.n * self.n))

    def _blend(self, x, y, col, a):
        if a <= 0.0:
            return
        i = (y * self.n + x) * 4
        pa = self.buf[i + 3]
        na = min(1.0, a + pa * (1.0 - a))
        if na <= 0.0:
            return
        keep = pa * (1.0 - a) / na
        self.buf[i] = col[0] * a / na + self.buf[i] * keep
        self.buf[i + 1] = col[1] * a / na + self.buf[i + 1] * keep
        self.buf[i + 2] = col[2] * a / na + self.buf[i + 2] * keep
        self.buf[i + 3] = na

    def _bounds(self, cx, cy, r):
        n = self.n
        return (max(0, int(math.floor((cx - r) * n))),
                min(n - 1, int(math.ceil((cx + r) * n))),
                max(0, int(math.floor((cy - r) * n))),
                min(n - 1, int(math.ceil((cy + r) * n))))

    def disc(self, cx, cy, r, col, a=1.0):
        x0, x1, y0, y1 = self._bounds(cx, cy, r)
        r2 = r * r
        for py in range(y0, y1 + 1):
            dy = (py + 0.5) / self.n - cy
            dy2 = dy * dy
            for px in range(x0, x1 + 1):
                dx = (px + 0.5) / self.n - cx
                if dx * dx + dy2 <= r2:
                    self._blend(px, py, col, a)

    def ring(self, cx, cy, r_out, thickness, col, a=1.0):
        r_in = r_out - thickness
        x0, x1, y0, y1 = self._bounds(cx, cy, r_out)
        ro2, ri2 = r_out * r_out, r_in * r_in
        for py in range(y0, y1 + 1):
            dy = (py + 0.5) / self.n - cy
            dy2 = dy * dy
            for px in range(x0, x1 + 1):
                dx = (px + 0.5) / self.n - cx
                d2 = dx * dx + dy2
                if ri2 <= d2 <= ro2:
                    self._blend(px, py, col, a)

    def ring_arc(self, cx, cy, r_out, thickness, a0, a1, col, alpha=1.0):
        r_in = r_out - thickness
        x0, x1, y0, y1 = self._bounds(cx, cy, r_out)
        ro2, ri2 = r_out * r_out, r_in * r_in
        for py in range(y0, y1 + 1):
            dy = (py + 0.5) / self.n - cy
            dy2 = dy * dy
            for px in range(x0, x1 + 1):
                dx = (px + 0.5) / self.n - cx
                d2 = dx * dx + dy2
                if ri2 <= d2 <= ro2:
                    ang = math.degrees(math.atan2(-dy, dx)) % 360.0  # 90 = up on screen
                    if a0 <= ang <= a1:
                        self._blend(px, py, col, alpha)

    def radial(self, cx, cy, r, light, col_in, col_out):
        x0, x1, y0, y1 = self._bounds(cx, cy, r)
        r2 = r * r
        for py in range(y0, y1 + 1):
            ldy = (py + 0.5) / self.n - light[1]
            for px in range(x0, x1 + 1):
                dx = (px + 0.5) / self.n - cx
                dy = (py + 0.5) / self.n - cy
                if dx * dx + dy * dy <= r2:
                    t = min(1.0, math.hypot((px + 0.5) / self.n - light[0], ldy) / r)
                    self._blend(px, py,
                                (col_in[0] + (col_out[0] - col_in[0]) * t,
                                 col_in[1] + (col_out[1] - col_in[1]) * t,
                                 col_in[2] + (col_out[2] - col_in[2]) * t), 1.0)

    def glow(self, cx, cy, r, col, a0):
        x0, x1, y0, y1 = self._bounds(cx, cy, r)
        for py in range(y0, y1 + 1):
            dy = (py + 0.5) / self.n - cy
            for px in range(x0, x1 + 1):
                dx = (px + 0.5) / self.n - cx
                d = math.hypot(dx, dy)
                if d < r:
                    self._blend(px, py, col, a0 * (1.0 - d / r) ** 2)

    def star4(self, cx, cy, r, col, thin=0.28, a=1.0):
        x0, x1, y0, y1 = self._bounds(cx, cy, r)
        for py in range(y0, y1 + 1):
            ady = abs((py + 0.5) / self.n - cy)
            for px in range(x0, x1 + 1):
                adx = abs((px + 0.5) / self.n - cx)
                if (adx / r + ady / (r * thin) <= 1.0
                        or ady / r + adx / (r * thin) <= 1.0):
                    self._blend(px, py, col, a)

    def taper(self, p0, p1, p2, r0, r1, col, steps=48):
        for i in range(steps + 1):
            t = i / steps
            x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0]
            y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]
            self.disc(x, y, r0 + (r1 - r0) * t, col)

    def save(self, path, silhouette=False):
        n = self.size
        rows = []
        cells = SS * SS
        for y in range(n):
            row = bytearray()
            for x in range(n):
                r = g = b = a = 0.0
                for sy in range(SS):
                    base = ((y * SS + sy) * self.n + x * SS) * 4
                    for sx in range(SS):
                        i = base + sx * 4
                        pa = self.buf[i + 3]
                        if pa > 0.0:
                            r += self.buf[i] * pa
                            g += self.buf[i + 1] * pa
                            b += self.buf[i + 2] * pa
                            a += pa
                mean_a = a / cells
                if silhouette:
                    # smoothstep on the coverage mask -> soft-edged silhouette
                    t = max(0.0, min(1.0, (mean_a - 0.30) / 0.40))
                    out_a = t * t * (3.0 - 2.0 * t)
                    row += bytes((255, 255, 255, _byte(out_a)))
                else:
                    if a > 1e-6:
                        r, g, b = r / a, g / a, b / a
                    row += bytes((_byte(r), _byte(g), _byte(b), _byte(mean_a)))
            rows.append(bytes(row))
        raw = b"".join(b"\x00" + row for row in rows)
        _write_png(path, n, n, raw)


def _byte(v):
    return max(0, min(255, int(round(v * 255))))


def _write_png(path, w, h, raw):
    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(png)


def draw(size):
    """All coordinates are fractions of the canvas (0..1)."""
    im = Img(size)

    # dark edging behind everything for a crisp silhouette
    im.ring(0.5, 0.155, 0.097, 0.058, DARK)
    im.disc(0.5, 0.575, 0.352, DARK)

    # brass pendant loop with a top-left sheen
    im.ring(0.5, 0.155, 0.085, 0.034, BRASS)
    im.ring_arc(0.5, 0.155, 0.085, 0.026, 115, 205, BRASS_LIGHT)

    # glass orb: radial gradient toward the upper-left light, pale pink rim
    im.radial(0.5, 0.575, 0.34, (0.46, 0.50), GLASS_IN, GLASS_OUT)
    im.ring(0.5, 0.575, 0.336, 0.024, RIM_LIGHT)
    im.ring_arc(0.5, 0.575, 0.285, 0.020, 120, 205, WISP_BRIGHT, alpha=0.30)

    # soul wisp: glowing comma shape curled around the orb's center
    im.glow(0.48, 0.560, 0.24, GLOW, 0.32)
    im.disc(0.505, 0.615, 0.165, GLASS_OUT, a=0.40)  # shadow so the wisp floats
    im.taper((0.650, 0.690), (0.670, 0.385), (0.475, 0.545), 0.022, 0.135, WISP)
    im.disc(0.475, 0.565, 0.135, WISP)
    im.disc(0.440, 0.520, 0.060, WISP_BRIGHT)
    im.disc(0.615, 0.705, 0.040, WISP_BRIGHT, a=0.18)  # faint bounce light

    # sparkles
    im.star4(0.735, 0.370, 0.048, WISP_BRIGHT)
    im.star4(0.285, 0.430, 0.032, WISP_BRIGHT)

    return im


def main():
    small = draw(SMALL_SIZE)
    small.save(os.path.join(OUT_DIR, "soul_catcher.png"))
    small.save(os.path.join(OUT_DIR, "soul_catcher_outline.png"), silhouette=True)
    draw(BIG_SIZE).save(os.path.join(OUT_DIR, "big", "soul_catcher.png"))
    print("written to", OUT_DIR)


if __name__ == "__main__":
    main()
