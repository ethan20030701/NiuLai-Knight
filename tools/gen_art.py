#!/usr/bin/env python3
"""Generate all art assets for the HollowKnight (STS2) mod.

Hollow-Knight-flavoured vector art drawn procedurally with numpy SDF
primitives and PIL downscaling. Deterministic (fixed seeds) so re-running
regenerates stable output. Scenes are authored in their *virtual* pixel
space (e.g. 1000x760); the canvas renders at SSx supersampling and is
Lanczos-reduced on save, which keeps edges clean at every output size.

Card subjects reference Hollow Knight directly: the Knight performing nail
arts, spells (Vengeful Spirit in the City of Tears rain, Abyss Shriek in the
Abyss), soul totems, the Resting Grounds, and the five powers drawn as
Hollow-Knight-style charm medallions (Fragile Strength green and cracked).

Outputs (all under HollowKnight/):
  images/card_portraits/{id}.png        250x190   small card art
  images/card_portraits/big/{id}.png   1000x760   big card art
  images/powers/{id}.png                64x64     power icon
  images/powers/big/{id}.png           256x256    big power icon
  images/relics/soul_eater.png          94x94     relic icon
  images/relics/soul_eater_outline.png  94x94     white silhouette
  images/relics/big/soul_eater.png     256x256    big relic icon
  images/charui/*.png                              UI icons
  mod_image.png                              420x420

Run from repo root:  python tools/gen_art.py
"""

import math
import os

import numpy as np
from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMG = os.path.join(REPO, "HollowKnight", "images")

# ---------------------------------------------------------------- palette
VOID_TOP = (0.043, 0.055, 0.090)     # deep indigo night
VOID_BOT = (0.086, 0.110, 0.150)     # cave teal
PURP_TOP = (0.070, 0.043, 0.098)
PURP_BOT = (0.118, 0.078, 0.145)
GREEN_TOP = (0.045, 0.085, 0.055)    # Greenpath depths
GREEN_BOT = (0.095, 0.165, 0.095)
ABYSS_TOP = (0.020, 0.020, 0.032)
ABYSS_BOT = (0.055, 0.045, 0.085)
CITY_TOP = (0.075, 0.100, 0.155)     # City of Tears night
CITY_BOT = (0.035, 0.050, 0.085)
BONE = (0.910, 0.885, 0.825)         # pale shell white
BONE_DIM = (0.640, 0.625, 0.590)
BONE_GREEN = (0.720, 0.780, 0.620)   # fragile charm green
BONE_GREY = (0.720, 0.720, 0.640)    # shaman stone grey
ENGRAVE = (0.34, 0.32, 0.29)         # engraved symbol on charm
SOUL = (0.870, 0.955, 1.000)         # soul glow blue-white
SOUL_SOFT = (0.600, 0.780, 0.920)
BRASS = (0.780, 0.620, 0.240)
BRASS_L = (0.980, 0.880, 0.560)
DARK = (0.055, 0.058, 0.075)         # silhouette black
WISP = (0.960, 0.990, 1.000)
EMBER = (1.000, 0.720, 0.420)
VIOLET = (0.760, 0.620, 1.000)
CRIMSON = (0.980, 0.420, 0.380)
LEAF = (0.320, 0.540, 0.330)         # Greenpath foliage
WINDOW = (1.000, 0.850, 0.550)       # lit city windows


# ---------------------------------------------------------------- sdf kit
def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x + 0.5, y + 0.5


def aa(d, px=0.45):
    """coverage from signed distance (virtual-pixel units)"""
    return np.clip(0.5 - d / px, 0.0, 1.0)


def circle(x, y, cx, cy, r):
    return np.hypot(x - cx, y - cy) - r


def ellipse(x, y, cx, cy, rx, ry):
    kx, ky = (x - cx) / rx, (y - cy) / ry
    return (np.hypot(kx, ky) - 1.0) * min(rx, ry)


def seg(x, y, ax, ay, bx, by):
    """capsule distance to segment a-b (radius subtracted by caller)"""
    bxp, byp = bx - ax, by - ay
    denom = bxp * bxp + byp * byp + 1e-9
    h = np.clip(((x - ax) * bxp + (y - ay) * byp) / denom, 0.0, 1.0)
    return np.hypot(x - (ax + bxp * h), y - (ay + byp * h))


def convex(x, y, pts):
    """signed distance to convex polygon (negative inside, winding-agnostic)"""
    area = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1]
               for i in range(len(pts)))
    if area < 0:
        pts = list(reversed(pts))
    d = np.full_like(x, -1e9, dtype=np.float32)
    n = len(pts)
    for i in range(n):
        ax, ay = pts[i]
        bx, by = pts[(i + 1) % n]
        ex, ey = bx - ax, by - ay
        length = math.hypot(ex, ey) + 1e-9
        nx, ny = ey / length, -ex / length  # outward normal
        d = np.maximum(d, (x - ax) * nx + (y - ay) * ny)
    return d


def arc(x, y, cx, cy, r, a0, a1):
    """distance to an arc line centred at (cx,cy), angles in degrees ccw"""
    dx, dy = x - cx, y - cy
    dist = np.hypot(dx, dy)
    ang = np.degrees(np.arctan2(dy, dx))
    rel = (ang - a0) % 360.0
    span = (a1 - a0) % 360.0
    inside = rel <= span
    d0 = np.abs((ang - a0 + 180.0) % 360.0 - 180.0)
    d1 = np.abs((ang - a1 + 180.0) % 360.0 - 180.0)
    dang = np.radians(np.where(inside, 0.0, np.minimum(d0, d1)))
    end = np.sqrt(dist * dist + r * r - 2.0 * dist * r * np.cos(dang))
    return np.where(inside, np.abs(dist - r), end)


def rot_pt(px, py, cx, cy, deg):
    a = math.radians(deg)
    dx, dy = px - cx, py - cy
    return (cx + dx * math.cos(a) - dy * math.sin(a),
            cy + dx * math.sin(a) + dy * math.cos(a))


def rot_space(cv, cx, cy, deg):
    """coordinate grids rotated by -deg around (cx,cy)"""
    a = math.radians(-deg)
    dx, dy = cv.x - cx, cv.y - cy
    return (cx + dx * math.cos(a) - dy * math.sin(a),
            cy + dx * math.sin(a) + dy * math.cos(a))


def tapered_quad(p0, p1, w0, w1):
    """convex quad along centreline p0->p1 with half-widths w0, w1"""
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dy) + 1e-9
    nx, ny = -dy / length, dx / length
    return [(p0[0] + nx * w0, p0[1] + ny * w0), (p1[0] + nx * w1, p1[1] + ny * w1),
            (p1[0] - nx * w1, p1[1] - ny * w1), (p0[0] - nx * w0, p0[1] - ny * w0)]


# ---------------------------------------------------------------- canvas
class Cv:
    """straight-alpha RGBA float canvas, virtual coords, SSx supersampled"""

    def __init__(self, w, h, ss, bg=None):
        self.w, self.h, self.ss = w, h, ss
        y, x = np.mgrid[0:h * ss, 0:w * ss].astype(np.float32)
        self.x = (x + 0.5) / ss
        self.y = (y + 0.5) / ss
        self.rgb = np.zeros((h * ss, w * ss, 3), np.float32)
        self.a = np.zeros((h * ss, w * ss, 1), np.float32)
        if bg is not None:
            self.rgb[:] = bg
            self.a[:] = 1.0

    def paint(self, d, color, alpha=1.0):
        m = (aa(d) * alpha)[..., None]
        out_a = m + self.a * (1.0 - m)
        c = np.asarray(color, np.float32)
        self.rgb = (c * m + self.rgb * self.a * (1.0 - m)) / np.maximum(out_a, 1e-6)
        self.a = out_a

    def paint_mask(self, m, color):
        m = m[..., None]
        out_a = m + self.a * (1.0 - m)
        c = np.asarray(color, np.float32)
        self.rgb = (c * m + self.rgb * self.a * (1.0 - m)) / np.maximum(out_a, 1e-6)
        self.a = out_a

    def glow(self, cx, cy, r, color, strength=0.5, falloff=2.2):
        d = np.hypot(self.x - cx, self.y - cy) / r
        g = np.exp(-(d ** falloff)) * strength
        self.rgb = np.clip(self.rgb + np.asarray(color, np.float32) * g[..., None], 0, 1)

    def vignette(self, strength=0.4):
        h, w = self.h, self.w
        d = np.hypot((self.x - w / 2) / (w / 2), (self.y - h / 2) / (h / 2))
        m = np.clip((d - 0.75) / 0.5, 0, 1) * strength
        self.paint_mask(m, (0.01, 0.012, 0.02))

    def particles(self, n, seed, color=WISP, rmax=3.0, amax=0.22):
        rng = np.random.default_rng(seed)
        for _ in range(n):
            px, py = rng.uniform(0, self.w), rng.uniform(0, self.h)
            pr = rng.uniform(0.8, rmax)
            pa = rng.uniform(0.04, amax)
            self.paint(circle(self.x, self.y, px, py, pr), color, pa)

    def vgrad(self, top, bot):
        t = (self.y / self.h)[..., None]
        self.rgb[:] = np.asarray(top, np.float32) * (1 - t) + np.asarray(bot, np.float32) * t
        self.a[:] = 1.0

    def radial_bg(self, inner, outer):
        t = (np.hypot(self.x - self.w / 2, self.y - self.h / 2) / (self.w / 2))[..., None]
        self.rgb[:] = np.asarray(inner, np.float32) * (1 - t) + np.asarray(outer, np.float32) * t
        self.a[:] = 1.0

    def save(self, path, out_w, out_h):
        rgb = np.clip(self.rgb * 255 + 0.5, 0, 255).astype(np.uint8)
        alpha = np.clip(self.a[..., 0] * 255 + 0.5, 0, 255).astype(np.uint8)
        img = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
        if (out_w, out_h) != (self.w * self.ss, self.h * self.ss):
            img = img.resize((out_w, out_h), Image.LANCZOS)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        img.save(path)


# ---------------------------------------------------------------- motifs
def cave_bg(cv, top=VOID_TOP, bot=VOID_BOT, seed=7, ground=True):
    h, w = cv.h, cv.w
    cv.vgrad(top, bot)
    if ground:
        edge = h * 0.84 + np.sin(cv.x * 0.012 + seed) * h * 0.012 + np.sin(cv.x * 0.037) * h * 0.006
        cv.paint(cv.y - edge, DARK, 0.85)
    rng = np.random.default_rng(seed + 1)
    for _ in range(6):  # stalactites
        px = rng.uniform(0.05, 0.95) * w
        ln = rng.uniform(0.10, 0.20) * h
        wd = rng.uniform(0.006, 0.016) * w
        cv.paint(seg(cv.x, cv.y, px, 0, px + rng.uniform(-20, 20), ln) - wd,
                 (0.02, 0.025, 0.04), 0.7)
    cv.particles(30, seed + 2)
    cv.vignette(0.45)


def arch(cv, cx, cy, r, width, color=(0.05, 0.06, 0.10), alpha=0.95, a0=205, a1=335):
    cv.paint(arc(cv.x, cv.y, cx, cy, r, a0, a1) - width, color, alpha)


def nail_weapon(cv, cx, cy, length, deg, color=BONE_DIM, cracked=False, glow=True):
    """the Knight's nail: tapered blade, brass crossguard, dark hilt"""
    if glow:
        cv.glow(cx, cy, length * 0.5, SOUL_SOFT, 0.28)
    x, y = rot_space(cv, cx, cy, deg)
    half = length * 0.048
    pts = [(cx + length * 0.52, cy),
           (cx - length * 0.36, cy - half),
           (cx - length * 0.36, cy + half)]
    cv.paint(convex(x, y, pts), color)
    cv.paint(seg(x, y, cx + length * 0.50, cy, cx - length * 0.30, cy - half * 0.7)
             - length * 0.006, WISP, 0.7)
    gx = cx - length * 0.36
    cv.paint(seg(x, y, gx, cy - length * 0.075, gx, cy + length * 0.075) - length * 0.016, BRASS)
    cv.paint(seg(x, y, cx - length * 0.38, cy, cx - length * 0.55, cy) - length * 0.020,
             (0.16, 0.12, 0.10))
    if cracked:
        for t in (0.18, 0.38):
            mx = cx + length * (0.30 - t * 0.6)
            cv.paint(seg(x, y, mx, cy - length * 0.020, mx + length * 0.02, cy + length * 0.020)
                     - length * 0.004, (0.02, 0.02, 0.03), 0.8)


def hk_mask(cv, cx, cy, s, pale=BONE, eye=(0.02, 0.02, 0.03), glow=True):
    """the Knight's mask: outward-sweeping curved horns, round face, oval eyes"""
    if glow:
        cv.glow(cx, cy + s * 0.05, s * 1.15, SOUL_SOFT, 0.28)
    for side in (-1, 1):
        p0 = (cx + side * s * 0.10, cy - s * 0.24)
        p1 = (cx + side * s * 0.40, cy - s * 0.68)
        p2 = (cx + side * s * 0.34, cy - s * 1.12)
        cv.paint(convex(cv.x, cv.y, tapered_quad(p0, p1, s * 0.11, s * 0.075)), pale)
        cv.paint(convex(cv.x, cv.y, tapered_quad(p1, p2, s * 0.075, 0.0)), pale)
    cv.paint(ellipse(cv.x, cv.y, cx, cy - s * 0.08, s * 0.42, s * 0.37), pale)
    cv.paint(ellipse(cv.x, cv.y, cx, cy + s * 0.16, s * 0.28, s * 0.22), pale)
    for side in (-1, 1):
        cv.paint(ellipse(cv.x, cv.y, cx + side * s * 0.16, cy - s * 0.02,
                         s * 0.080, s * 0.150), eye, 0.96)


def knight(cv, cx, cy, s, body=DARK, glow=True):
    """the little vessel: cloaked figure with a pale mask"""
    if glow:
        cv.glow(cx, cy, s * 1.5, SOUL_SOFT, 0.22)
    pts = [(cx - s * 0.42, cy + s * 0.55), (cx - s * 0.18, cy - s * 0.10),
           (cx + s * 0.18, cy - s * 0.10), (cx + s * 0.42, cy + s * 0.55)]
    cv.paint(convex(cv.x, cv.y, pts), body)
    cv.paint(ellipse(cv.x, cv.y, cx, cy + s * 0.58, s * 0.34, s * 0.07), body)
    hk_mask(cv, cx, cy - s * 0.26, s * 0.42, pale=BONE if body is DARK else body, glow=False)


def spirit(cv, cx, cy, s, color=WISP, eye=(0.03, 0.02, 0.04), screaming=False):
    """vengeful spirit: horned ghost head with trailing wisps"""
    cv.glow(cx, cy, s * 1.5, color, 0.42)
    for i in range(3):
        t = seg(cv.x, cv.y, cx - s * 0.5 - i * s * 0.4, cy + (i - 1) * s * 0.26,
                cx - s * 0.9 - i * s * 0.55, cy + (i - 1) * s * 0.34) - s * (0.10 - i * 0.02)
        cv.paint(t, color, 0.35 - i * 0.09)
    cv.paint(circle(cv.x, cv.y, cx, cy, s * 0.42), color)
    for side in (-1, 1):
        p0 = (cx + side * s * 0.06, cy - s * 0.30)
        p1 = (cx + side * s * 0.30, cy - s * 0.62)
        p2 = (cx + side * s * 0.24, cy - s * 0.92)
        cv.paint(convex(cv.x, cv.y, tapered_quad(p0, p1, s * 0.09, s * 0.06)), color)
        cv.paint(convex(cv.x, cv.y, tapered_quad(p1, p2, s * 0.06, 0.0)), color)
    for side in (-1, 1):
        cv.paint(ellipse(cv.x, cv.y, cx + side * s * 0.15, cy - s * 0.06, s * 0.065, s * 0.105),
                 eye, 0.95)
    if screaming:
        cv.paint(ellipse(cv.x, cv.y, cx, cy + s * 0.18, s * 0.095, s * 0.15), eye, 0.95)
    for i in (-1, 0, 1):
        cv.paint(circle(cv.x, cv.y, cx + i * s * 0.22, cy + s * 0.40, s * 0.16), color)


def slash(cv, cx, cy, r, deg, width, color=WISP, alpha=0.9, a0=-70, a1=70, inner=True):
    x, y = rot_space(cv, cx, cy, deg)
    cv.glow(cx, cy, r * 1.2, color, 0.26)
    cv.paint(arc(x, y, cx, cy, r, a0, a1) - width, color, alpha)
    if inner:
        cv.paint(arc(x, y, cx, cy, r, a0 + 6, a1 - 6) - width * 0.22, (1.0, 1.0, 1.0), alpha * 0.85)


# ------------------------------------------------------------- HK locations
def city_rain(cv, seed=31):
    """City of Tears: tower silhouettes, lit windows, falling rain"""
    h, w = cv.h, cv.w
    cv.vgrad(CITY_TOP, CITY_BOT)
    rng = np.random.default_rng(seed)
    base = h * 0.9
    for i in range(5):
        tw = rng.uniform(0.10, 0.17) * w
        tx = rng.uniform(0.02, 0.92) * w
        th = rng.uniform(0.42, 0.92) * h
        col = (0.095, 0.115, 0.160)
        cv.paint(convex(cv.x, cv.y, [(tx, base), (tx + tw, base),
                                     (tx + tw, base - th), (tx, base - th)]), col, 0.96)
        cv.paint(convex(cv.x, cv.y, [(tx + tw * 0.1, base - th), (tx + tw * 0.9, base - th),
                                     (tx + tw * 0.5, base - th - tw * 0.5)]), col, 0.96)
        for _ in range(int(th / 55) + 2):
            wx = tx + rng.uniform(0.15, 0.85) * tw
            wy = base - rng.uniform(0.08, 0.95) * th
            cv.paint(circle(cv.x, cv.y, wx, wy, 2.4), WINDOW, rng.uniform(0.25, 0.75))
    for _ in range(70):  # rain
        rx, ry = rng.uniform(0, w), rng.uniform(0, h)
        ln = rng.uniform(18, 46)
        cv.paint(seg(cv.x, cv.y, rx, ry, rx - ln * 0.18, ry + ln), (0.62, 0.75, 0.92),
                 rng.uniform(0.10, 0.26))
    cv.vignette(0.42)


def tombstones(cv, seed=33):
    """Resting Grounds graves"""
    h, w = cv.h, cv.w
    rng = np.random.default_rng(seed)
    base = h * 0.82
    for i in range(4):
        tx = (0.16 + 0.22 * i + rng.uniform(-0.04, 0.04)) * w
        tw = rng.uniform(0.065, 0.095) * w
        th = rng.uniform(0.17, 0.27) * h
        col = (0.125, 0.135, 0.170)
        cv.paint(convex(cv.x, cv.y, [(tx - tw / 2, base), (tx + tw / 2, base),
                                     (tx + tw / 2, base - th), (tx - tw / 2, base - th)]), col)
        cv.paint(circle(cv.x, cv.y, tx, base - th, tw / 2), col)
        cv.paint(seg(cv.x, cv.y, tx, base - th + tw * 0.8, tx, base - th * 0.42), DARK, 0.6)
        cv.paint(seg(cv.x, cv.y, tx - tw * 0.26, base - th * 0.62, tx + tw * 0.26, base - th * 0.62),
                 DARK, 0.6)
    cv.particles(16, seed + 5, color=SOUL, rmax=2.2, amax=0.25)


def greenery(cv, seed=35, leaves_n=14, vines=5):
    """Greenpath foliage: hanging vines and drifting leaves"""
    h, w = cv.h, cv.w
    rng = np.random.default_rng(seed)
    for _ in range(vines):
        vx = rng.uniform(0.05, 0.95) * w
        ln = rng.uniform(0.18, 0.42) * h
        for k in range(4):
            y0, y1 = ln * k / 4, ln * (k + 1) / 4
            x0 = vx + math.sin(k * 1.9 + seed) * 16
            x1 = vx + math.sin((k + 1) * 1.9 + seed) * 16
            cv.paint(seg(cv.x, cv.y, x0, y0, x1, y1) - 3.2, (0.10, 0.20, 0.12), 0.8)
    for _ in range(leaves_n):
        lx, ly = rng.uniform(0.08, 0.92) * w, rng.uniform(0.10, 0.85) * h
        L = rng.uniform(11, 24)
        deg = rng.uniform(0, 360)
        pts = [rot_pt(lx - L, ly, lx, ly, deg), rot_pt(lx, ly - L * 0.30, lx, ly, deg),
               rot_pt(lx + L, ly, lx, ly, deg), rot_pt(lx, ly + L * 0.30, lx, ly, deg)]
        cv.paint(convex(cv.x, cv.y, pts), LEAF, rng.uniform(0.35, 0.85))
    cv.particles(10, seed + 9, color=(0.55, 0.75, 0.50), rmax=2.0, amax=0.2)


def glowbugs(cv, n=9, seed=37):
    rng = np.random.default_rng(seed)
    for _ in range(n):
        px, py = rng.uniform(0.08, 0.92) * cv.w, rng.uniform(0.15, 0.8) * cv.h
        cv.glow(px, py, 14, SOUL_SOFT, 0.5)
        cv.paint(circle(cv.x, cv.y, px, py, 2.6), WISP, 0.9)


def void_tendrils(cv, n=7, seed=39):
    h, w = cv.h, cv.w
    rng = np.random.default_rng(seed)
    for i in range(n):
        tx = (0.08 + 0.84 * i / max(n - 1, 1) + rng.uniform(-0.03, 0.03)) * w
        for k in range(4):
            y0, y1 = h * (1 - k / 4), h * (1 - (k + 1) / 4)
            x0 = tx + math.sin(k * 1.7 + i * 2.3) * 15
            x1 = tx + math.sin((k + 1) * 1.7 + i * 2.3) * 15
            cv.paint(seg(cv.x, cv.y, x0, y0, x1, y1) - 5.5, (0.025, 0.020, 0.045), 0.85)


def soul_totem(cv, cx, cy, w, h):
    """soul totem: black pillar with a glowing face glyph"""
    cv.glow(cx, cy, h * 0.85, SOUL_SOFT, 0.30)
    cv.paint(convex(cv.x, cv.y, [(cx - w * 0.5, cy + h * 0.5), (cx + w * 0.5, cy + h * 0.5),
                                 (cx + w * 0.36, cy - h * 0.40), (cx - w * 0.36, cy - h * 0.40)]),
             (0.045, 0.050, 0.078))
    cv.paint(convex(cv.x, cv.y, [(cx - w * 0.46, cy - h * 0.34), (cx + w * 0.46, cy - h * 0.34),
                                 (cx + w * 0.40, cy - h * 0.54), (cx - w * 0.40, cy - h * 0.54)]),
             (0.060, 0.066, 0.098))
    for side in (-1, 1):
        cv.paint(ellipse(cv.x, cv.y, cx + side * w * 0.17, cy - h * 0.14, w * 0.055, h * 0.095),
                 WISP, 0.95)
    cv.paint(ellipse(cv.x, cv.y, cx, cy + h * 0.10, w * 0.17, h * 0.024), WISP, 0.9)


# ------------------------------------------------------------- HK charms
def charm_hk(cv, cx, cy, r, base=BONE, symbol_fn=None, cracked=False,
             loop=BRASS, glow_color=SOUL_SOFT):
    """Hollow-Knight-style charm medallion: pale disc, loop, engraved symbol"""
    cv.glow(cx, cy, r * 2.0, glow_color, 0.40)
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(np.abs(circle(cv.x, cv.y, cx, cy - r * 1.22, r * 0.16)) - r * 0.05, loop, 0.95)
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.98), base)
    cv.paint(np.abs(dist - r * 0.86) - r * 0.055, tuple(c * 0.80 for c in base), 0.9)
    if symbol_fn:
        symbol_fn(cv, cx, cy, r)
    if cracked:
        for a0 in (-55, 15, 75):
            ar = math.radians(a0)
            x0, y0 = cx + math.cos(ar) * r * 0.95, cy + math.sin(ar) * r * 0.95
            xm, ym = cx + math.cos(ar + 0.30) * r * 0.42, cy + math.sin(ar + 0.30) * r * 0.42
            cv.paint(seg(cv.x, cv.y, x0, y0, xm, ym) - r * 0.020, (0.10, 0.11, 0.08), 0.9)
            cv.paint(seg(cv.x, cv.y, xm, ym, cx - math.cos(ar) * r * 0.55,
                         cy - math.sin(ar) * r * 0.60) - r * 0.012, (0.10, 0.11, 0.08), 0.7)


def sym_nail(cv, cx, cy, r):
    nail_weapon(cv, cx, cy, r * 1.05, 90, color=ENGRAVE, glow=False)


def sym_stance(cv, cx, cy, r):
    """wide-stance figure glyph for Steady Body"""
    cv.paint(convex(cv.x, cv.y, [(cx - 0.30 * r, cy + 0.40 * r), (cx + 0.30 * r, cy + 0.40 * r),
                                 (cx + 0.14 * r, cy - 0.04 * r), (cx - 0.14 * r, cy - 0.04 * r)]),
             ENGRAVE)
    cv.paint(circle(cv.x, cv.y, cx, cy - 0.26 * r, 0.14 * r), ENGRAVE)
    cv.paint(seg(cv.x, cv.y, cx - 0.26 * r, cy + 0.60 * r, cx - 0.10 * r, cy + 0.38 * r)
             - 0.035 * r, ENGRAVE)
    cv.paint(seg(cv.x, cv.y, cx + 0.26 * r, cy + 0.60 * r, cx + 0.10 * r, cy + 0.38 * r)
             - 0.035 * r, ENGRAVE)


def sym_thorns(cv, cx, cy, r):
    """ring of thorn spikes for Thorns of Agony"""
    for i in range(10):
        a = math.radians(i * 36)
        px, py = cx + math.cos(a) * r * 0.52, cy + math.sin(a) * r * 0.52
        pts = [(px, py),
               (cx + math.cos(a + 0.20) * r * 0.18, cy + math.sin(a + 0.20) * r * 0.18),
               (cx + math.cos(a - 0.20) * r * 0.18, cy + math.sin(a - 0.20) * r * 0.18)]
        cv.paint(convex(cv.x, cv.y, pts), ENGRAVE)
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.13), ENGRAVE)


def sym_spiral(cv, cx, cy, r):
    """spiral glyph for Shaman Stone"""
    cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.40, 90, 340) - r * 0.055, ENGRAVE)
    cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.19, 270, 490) - r * 0.048, ENGRAVE)
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.045), ENGRAVE)


def glint(cv, cx, cy, size, color=WISP, alpha=0.9):
    """four-point sparkle"""
    cv.paint(seg(cv.x, cv.y, cx - size, cy, cx + size, cy) - size * 0.12, color, alpha)
    cv.paint(seg(cv.x, cv.y, cx, cy - size, cx, cy + size) - size * 0.12, color, alpha)


def dome(cv, cx, cy, w, h, color, seg_lines=None):
    """half-ellipse dome (shell) with optional radial segment lines"""
    pts = []
    for k in range(9):
        t = math.pi * k / 8
        pts.append((cx - math.cos(t) * w / 2, cy - math.sin(t) * h))
    cv.paint(convex(cv.x, cv.y, pts), color)
    if seg_lines:
        for k in (-0.6, -0.2, 0.2, 0.6):
            cv.paint(seg(cv.x, cv.y, cx + k * w * 0.5, cy, cx + k * w * 0.42, cy - h * 0.82)
                     - 2.4, tuple(c * 0.72 for c in color), 0.9)


# ---------------------------------------------------------------- v0.3.9 school-split art
def scene_spellcharge(cv):
    """Spellcharge: the Knight channelling orbiting violet wisps"""
    cave_bg(cv, top=PURP_TOP, bot=PURP_BOT, seed=61)
    knight(cv, 460, 430, 220)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 420, 150 + i * 44, 200, 340),
                 (0.76, 0.62, 1.0), 0.5 - i * 0.1)
    for i in range(6):
        a = math.radians(i * 60 + 25)
        px, py = 500 + math.cos(a) * (120 + (i % 3) * 55), 420 + math.sin(a) * (90 + (i % 3) * 40)
        cv.paint(circle(cv.x, cv.y, px, py, 10 - (i % 3) * 2), VIOLET, 0.9)
    cv.glow(500, 420, 260, VIOLET, 0.3)
    cv.particles(12, 63, color=VIOLET, rmax=2.2, amax=0.3)


def scene_soulbell(cv):
    """Soulbell: a pale bell charm ringing soul-light ripples"""
    cave_bg(cv, seed=62)
    dome(cv, 500, 430, 150, 190, (0.90, 0.93, 0.98))
    cv.paint(circle(cv.x, cv.y, 500, 350, 16), BRASS_L)
    cv.paint(seg(cv.x, cv.y, 500, 240, 500, 342) - 5, BRASS)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 440, 200 + i * 55, 215, 325),
                 SOUL, 0.55 - i * 0.14)
    cv.glow(500, 430, 240, SOUL_SOFT, 0.35)
    cv.particles(10, 64, color=SOUL, rmax=2.0, amax=0.3)


def scene_full_soul(cv):
    """Full Soul: a vessel orb brimming with radiance, rays outward"""
    cave_bg(cv, top=(0.055, 0.075, 0.115), bot=(0.10, 0.13, 0.17), seed=65)
    for i in range(8):
        a = math.radians(i * 45 + 10)
        cv.paint(seg(cv.x, cv.y, 500 + math.cos(a) * 150, 400 + math.sin(a) * 150,
                     500 + math.cos(a) * 300, 400 + math.sin(a) * 300) - 6,
                 SOUL, 0.3)
    cv.paint(circle(cv.x, cv.y, 500, 400, 130), (0.86, 0.94, 1.0))
    cv.paint(circle(cv.x, cv.y, 500, 400, 96), SOUL)
    cv.paint(circle(cv.x, cv.y, 470, 370, 26), (1.0, 1.0, 1.0), 0.8)
    cv.glow(500, 400, 320, SOUL_SOFT, 0.4)


def scene_broken_shell(cv):
    """Broken Shell: a cracked mask with echo rings hardening around it"""
    cave_bg(cv, seed=66)
    hk_mask(cv, 500, 400, 240)
    cv.paint(seg(cv.x, cv.y, 445, 300, 470, 390) - 5, DARK)
    cv.paint(seg(cv.x, cv.y, 470, 390, 450, 470) - 5, DARK)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 410, 185 + i * 50, 190, 350),
                 SOUL_SOFT, 0.5 - i * 0.12)
    cv.glow(500, 410, 260, SOUL_SOFT, 0.28)


def scene_hum(cv):
    """Hum: the nail held high with a single clean ripple"""
    cave_bg(cv, seed=67)
    nail_weapon(cv, 480, 420, 520, -18)
    cv.paint(arc(cv.x, cv.y, 470, 410, 240, 205, 335), WISP, 0.8)
    cv.glow(470, 410, 220, SOUL_SOFT, 0.22)


def scene_broken_vessel(cv):
    """Broken Vessel: the cracked husk, leaking furious orange soul"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=68)
    knight(cv, 500, 430, 230, body=(0.13, 0.10, 0.10))
    cv.paint(seg(cv.x, cv.y, 455, 300, 480, 395) - 6, CRIMSON, 0.9)
    cv.paint(seg(cv.x, cv.y, 480, 395, 458, 470) - 6, CRIMSON, 0.9)
    cv.particles(16, 69, color=EMBER, rmax=2.6, amax=0.4)
    cv.glow(470, 400, 220, CRIMSON, 0.3)


def scene_nail_flurry(cv):
    """Nail Flurry: overlapping rain of slashes"""
    cave_bg(cv, seed=70)
    nail_weapon(cv, 420, 400, 460, -30, color=BONE_DIM)
    slash(cv, 500, 400, 300, -12, 30, WISP, 0.75, a0=-50, a1=50)
    slash(cv, 560, 440, 260, 22, 26, WISP, 0.6, a0=-45, a1=45)
    slash(cv, 460, 470, 230, 55, 22, SOUL_SOFT, 0.5, a0=-40, a1=40)
    cv.particles(10, 71, color=WISP, rmax=2.0, amax=0.25)


def scene_soul_torrent(cv):
    """Soul Torrent: a flood of wraiths pouring across the field"""
    cave_bg(cv, top=(0.05, 0.06, 0.10), bot=(0.09, 0.11, 0.16), seed=72)
    spirit(cv, 380, 330, 190, color=SOUL, screaming=True)
    spirit(cv, 560, 420, 150, color=SOUL_SOFT, screaming=False)
    spirit(cv, 700, 340, 120, color=WISP, screaming=True)
    for i in range(5):
        cv.paint(arc(cv.x, cv.y, 420, 700, 420 - i * 60, 235, 305),
                 SOUL, 0.4 - i * 0.06)
    cv.particles(20, 73, color=SOUL, rmax=2.4, amax=0.35)
    cv.glow(520, 380, 340, SOUL_SOFT, 0.3)


def icon_spellcharge(cv):
    """spellcharge: violet swirl of three charges"""
    cv.glow(cv.w / 2, cv.h / 2, 112, (0.7, 0.5, 1.0), 0.55)
    _medallion(cv, 96, (0.30, 0.20, 0.46))
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, cv.w / 2, cv.h / 2, 34 + i * 16, 200 + i * 30, 330 + i * 30),
                 (0.85, 0.72, 1.0), 0.8 - i * 0.15)
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2, 14), (0.95, 0.9, 1.0))


def icon_soulbell(cv):
    """soulbell: a small bell with two ripples"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.55, 0.8, 1.0), 0.5)
    _medallion(cv, 96, (0.18, 0.28, 0.42))
    dome(cv, cv.w / 2, cv.h / 2 + 6, 42, 52, (0.90, 0.93, 0.98))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2 - 44, 7), BRASS_L)
    for i in range(2):
        cv.paint(arc(cv.x, cv.y, cv.w / 2, cv.h / 2 + 8, 66 + i * 16, 210, 330),
                 SOUL, 0.6 - i * 0.2)


def icon_fullsoul(cv):
    """full soul: a brimming orb over a pale medallion"""
    cv.glow(cv.w / 2, cv.h / 2, 115, (0.6, 0.85, 1.0), 0.6)
    _medallion(cv, 96, (0.20, 0.30, 0.46))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2, 42), (0.86, 0.94, 1.0))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2, 30), SOUL)
    cv.paint(circle(cv.x, cv.y, cv.w / 2 - 12, cv.h / 2 - 12, 8), (1.0, 1.0, 1.0))
    for dx in (-20, 0, 20):
        cv.paint(circle(cv.x, cv.y, cv.w / 2 + dx, cv.h / 2 + 56, 4), SOUL)


def icon_shellecho(cv):
    """shell echo: cracked shield with hardening rings"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.7, 0.8, 0.85), 0.5)
    _medallion(cv, 96, (0.24, 0.26, 0.30))
    shield = [(cv.w / 2, cv.h / 2 - 44), (cv.w / 2 + 36, cv.h / 2 - 22),
              (cv.w / 2 + 30, cv.h / 2 + 26), (cv.w / 2, cv.h / 2 + 48),
              (cv.w / 2 - 30, cv.h / 2 + 26), (cv.w / 2 - 36, cv.h / 2 - 22)]
    cv.paint(convex(cv.x, cv.y, shield), (0.88, 0.9, 0.92))
    cv.paint(seg(cv.x, cv.y, cv.w / 2 - 6, cv.h / 2 - 40, cv.w / 2 + 4, cv.h / 2 + 6) - 3, DARK)
    cv.paint(arc(cv.x, cv.y, cv.w / 2, cv.h / 2, 62, 220, 320), SOUL_SOFT, 0.7)


def relic_grubsong_eater(cv, r):
    """grubsong eater: a grub coiled around a glowing soul orb"""
    cv.glow(128, 128, 100, (1.0, 0.72, 0.42), 0.4)
    charm_hk(cv, 128, 128, 84, base=(0.26, 0.20, 0.16), symbol_fn=sym_spiral, loop=BRASS)
    cv.paint(circle(cv.x, cv.y, 128, 120, 34), SOUL)
    cv.paint(circle(cv.x, cv.y, 118, 110, 10), (1.0, 1.0, 1.0), 0.85)
    pts = [(128 - 52 + i * 16, 150 + int(16 * math.sin(i * 1.1))) for i in range(7)]
    for i in range(6):
        cv.paint(circle(cv.x, cv.y, pts[i + 1][0], pts[i + 1][1], 11 - i // 2),
                 (1.0, 0.72, 0.42))
    cv.paint(circle(cv.x, cv.y, pts[6][0] + 6, pts[6][1] - 8, 5), (0.1, 0.1, 0.1))


# ---------------------------------------------------------------- v0.4.0 companions art
def scene_dreamshield(cv):
    """Dreamshield: a woven crescent of dream circling the resting knight"""
    cave_bg(cv, top=(0.07, 0.06, 0.12), bot=(0.04, 0.045, 0.08), seed=74)
    knight(cv, 500, 430, 200)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 430, 190 + i * 46, 210, 350),
                 (0.82, 0.74, 1.0), 0.55 - i * 0.14)
    for i in range(7):
        a = math.radians(140 + i * 40)
        px, py = 500 + math.cos(a) * (150 + (i % 3) * 48), 430 + math.sin(a) * (110 + (i % 3) * 36)
        cv.paint(circle(cv.x, cv.y, px, py, 8 - (i % 3) * 2), WISP, 0.85)
    cv.glow(500, 430, 300, VIOLET, 0.25)
    cv.particles(12, 75, color=(0.85, 0.78, 1.0), rmax=2.2, amax=0.3)


def scene_soul_surge(cv):
    """Soul Surge: a rising wave of soul-light crashing over the field"""
    cave_bg(cv, top=(0.05, 0.06, 0.10), bot=(0.09, 0.11, 0.16), seed=76)
    for i in range(4):
        cv.paint(arc(cv.x, cv.y, 500, 900, 560 - i * 90, 225, 315),
                 SOUL, 0.55 - i * 0.1)
    knight(cv, 500, 480, 210)
    cv.glow(500, 560, 300, SOUL_SOFT, 0.35)
    cv.particles(18, 77, color=SOUL, rmax=2.6, amax=0.38)


def scene_grimmchild(cv):
    """Grimmchild: the little nightmare glowing in the dark"""
    cave_bg(cv, top=(0.08, 0.045, 0.07), bot=(0.05, 0.03, 0.05), seed=78)
    cv.paint(circle(cv.x, cv.y, 500, 380, 132), (0.16, 0.08, 0.10))
    cv.paint(seg(cv.x, cv.y, 430, 250, 406, 172) - 10, (0.24, 0.10, 0.12))
    cv.paint(seg(cv.x, cv.y, 570, 250, 594, 172) - 10, (0.24, 0.10, 0.12))
    cv.paint(circle(cv.x, cv.y, 452, 350, 30), CRIMSON, 0.95)
    cv.paint(circle(cv.x, cv.y, 548, 350, 30), CRIMSON, 0.95)
    cv.paint(circle(cv.x, cv.y, 446, 344, 10), (1.0, 0.92, 0.85))
    cv.paint(circle(cv.x, cv.y, 542, 344, 10), (1.0, 0.92, 0.85))
    cv.paint(circle(cv.x, cv.y, 500, 452, 13), (1.0, 0.62, 0.30))
    cv.particles(14, 79, color=EMBER, rmax=2.4, amax=0.35)
    cv.glow(500, 380, 260, CRIMSON, 0.3)


def icon_dreamshield(cv):
    """dreamshield: a violet dream crescent cradling small wisps
    (_medallion's inner disc is near-white, so the motif must be mid-saturation)"""
    cv.glow(cv.w / 2, cv.h / 2, 112, (0.75, 0.6, 1.0), 0.5)
    _medallion(cv, 96, (0.22, 0.18, 0.34))
    cx, cy = cv.w / 2, cv.h / 2 + 2
    crescent = np.maximum(circle(cv.x, cv.y, cx, cy, 48),
                          -circle(cv.x, cv.y, cx + 24, cy - 16, 42))
    cv.paint(crescent, (0.60, 0.48, 0.95))
    cv.paint(crescent, (0.86, 0.78, 1.0), 0.3)
    for dx, dy in ((-32, 28), (30, 32), (0, 46)):
        cv.paint(circle(cv.x, cv.y, cx + dx, cy + dy, 6), (0.52, 0.40, 0.88), 0.9)


def icon_grimmchild(cv):
    """grimmchild: the little nightmare face"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (1.0, 0.45, 0.35), 0.45)
    _medallion(cv, 96, (0.28, 0.12, 0.14))
    cx, cy = cv.w / 2, cv.h / 2 + 4
    cv.paint(circle(cv.x, cv.y, cx, cy, 44), (0.19, 0.09, 0.11))
    cv.paint(seg(cv.x, cv.y, cx - 26, cy - 34, cx - 38, cy - 62) - 5, (0.24, 0.10, 0.12))
    cv.paint(seg(cv.x, cv.y, cx + 26, cy - 34, cx + 38, cy - 62) - 5, (0.24, 0.10, 0.12))
    cv.paint(circle(cv.x, cv.y, cx - 16, cy - 6, 10), CRIMSON)
    cv.paint(circle(cv.x, cv.y, cx + 16, cy - 6, 10), CRIMSON)
    cv.paint(circle(cv.x, cv.y, cx - 19, cy - 9, 3), (1.0, 0.9, 0.8))
    cv.paint(circle(cv.x, cv.y, cx + 13, cy - 9, 3), (1.0, 0.9, 0.8))


# ---------------------------------------------------------------- v0.5.0 poison & engines art
POISON_GRN = (0.45, 0.85, 0.35)


def grub(cv, cx, cy, s, body=(1.0, 0.85, 0.45)):
    """a small segmented larva"""
    for i in range(4):
        rr = s * (0.30 - i * 0.05)
        cv.paint(circle(cv.x, cv.y, cx - i * s * 0.22, cy + (i % 2) * s * 0.06, rr), body, 0.95 - i * 0.08)
    cv.paint(seg(cv.x, cv.y, cx + s * 0.10, cy - s * 0.22, cx + s * 0.28, cy - s * 0.42) - s * 0.045, body)
    cv.paint(seg(cv.x, cv.y, cx + s * 0.16, cy - s * 0.14, cx + s * 0.40, cy - s * 0.26) - s * 0.035, body)
    cv.paint(circle(cv.x, cv.y, cx + s * 0.26, cy - s * 0.02, s * 0.045), DARK)


def scene_heavy_blow(cv):
    """Heavy Blow: a nail smashed down with a stone-shattering impact"""
    cave_bg(cv, seed=80)
    nail_weapon(cv, 500, 330, 560, 78, color=BONE_DIM)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 560, 120 + i * 52, 215, 325), WISP, 0.75 - i * 0.2)
    for px, py, pr in [(330, 620, 22), (690, 640, 18), (500, 690, 14)]:
        cv.paint(circle(cv.x, cv.y, px, py, pr), (0.10, 0.11, 0.14))
    cv.glow(500, 540, 240, SOUL_SOFT, 0.3)


def scene_raging_nail(cv):
    """Raging Nail: overlapping furious after-slashes"""
    cave_bg(cv, top=(0.09, 0.05, 0.06), bot=(0.05, 0.03, 0.04), seed=81)
    nail_weapon(cv, 430, 400, 500, -24, color=BONE_DIM)
    slash(cv, 540, 380, 300, -10, 28, (1.0, 0.62, 0.42), 0.8, a0=-55, a1=55)
    slash(cv, 480, 450, 260, 30, 24, (1.0, 0.72, 0.5), 0.6, a0=-50, a1=50)
    slash(cv, 560, 320, 220, 62, 20, EMBER, 0.5, a0=-45, a1=45)
    cv.particles(14, 82, color=EMBER, rmax=2.4, amax=0.4)


def scene_shell_slam(cv):
    """Shell Slam: the knight hurling its own shell as a battering ram"""
    cave_bg(cv, seed=83)
    hk_mask(cv, 500, 430, 260)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 430, 200 + i * 46, 200, 340), SOUL_SOFT, 0.55 - i * 0.13)
    cv.paint(seg(cv.x, cv.y, 640, 430, 850, 430) - 10, WISP, 0.8)
    cv.glow(500, 430, 260, SOUL_SOFT, 0.3)


def scene_mothwing_dash(cv):
    """Mothwing Dash: the knight streaking, moth-wing light trailing"""
    cave_bg(cv, seed=84)
    knight(cv, 640, 430, 190)
    for i in range(4):
        cv.paint(ellipse(cv.x, cv.y, 440 - i * 80, 430, 60, 150 - i * 24), (0.80, 0.85, 1.0), 0.42 - i * 0.08)
    cv.particles(10, 85, color=WISP, rmax=2.0, amax=0.3)


def scene_grubberfly_elegance(cv):
    """Grubberfly's Elegance: grubs basking in a holy beam"""
    cave_bg(cv, top=(0.06, 0.075, 0.055), bot=(0.04, 0.05, 0.035), seed=86)
    knight(cv, 500, 450, 210)
    cv.paint(np.maximum(ellipse(cv.x, cv.y, 500, 300, 26, 320), -(ellipse(cv.x, cv.y, 500, 300, 12, 320))),
             (1.0, 0.98, 0.80), 0.65)
    grub(cv, 300, 620, 90)
    grub(cv, 700, 640, 70)
    cv.glow(500, 380, 300, (1.0, 0.95, 0.7), 0.32)


def scene_soul_bolt(cv):
    """Soul Bolt: one tight soul lance punching through the dark"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=87)
    cv.paint(ellipse(cv.x, cv.y, 500, 400, 60, 330), SOUL)
    cv.paint(ellipse(cv.x, cv.y, 500, 400, 26, 330), (1.0, 1.0, 1.0), 0.9)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 760, 330 + i * 44, 235, 305), SOUL, 0.5 - i * 0.12)
    cv.glow(500, 400, 300, SOUL_SOFT, 0.4)


def scene_flukenest(cv):
    """Flukenest: flukes scattering over a panicking field"""
    cave_bg(cv, top=(0.07, 0.055, 0.03), bot=(0.05, 0.04, 0.03), seed=88)
    for px, py, s in [(330, 300, 64), (560, 250, 54), (700, 380, 46), (420, 470, 58), (620, 540, 50), (260, 460, 40)]:
        grub(cv, px, py, s, body=(0.95, 0.62, 0.42))
    cv.particles(16, 89, color=(1.0, 0.75, 0.5), rmax=2.6, amax=0.4)


def scene_spore_breath(cv):
    """Spore Breath: a shell mushroom puffing venomous spores"""
    cave_bg(cv, top=GREEN_TOP, bot=GREEN_BOT, seed=90)
    greenery(cv, seed=41, leaves_n=10, vines=4)
    dome(cv, 500, 500, 170, 130, (0.75, 0.55, 0.45))
    for i in range(9):
        a = math.radians(-30 - i * 15)
        cv.paint(circle(cv.x, cv.y, 500 + math.cos(a) * (200 + i * 26), 430 + math.sin(a) * (150 + i * 22),
                        12 - i * 0.8), POISON_GRN, 0.8 - i * 0.06)
    cv.glow(500, 470, 240, POISON_GRN, 0.25)


def scene_pain_brand(cv):
    """Pain Brand: a searing brand burned into the shell"""
    cave_bg(cv, top=(0.09, 0.05, 0.05), bot=(0.06, 0.03, 0.03), seed=91)
    hk_mask(cv, 500, 420, 250)
    cv.paint(arc(cv.x, cv.y, 500, 430, 120, 200, 340) - 14, CRIMSON, 0.95)
    cv.paint(arc(cv.x, cv.y, 500, 430, 78, 215, 325) - 8, (1.0, 0.55, 0.35), 0.9)
    cv.particles(12, 92, color=CRIMSON, rmax=2.2, amax=0.38)
    cv.glow(500, 420, 250, CRIMSON, 0.35)


def scene_wisp_afterimage(cv):
    """After Image: shades trailing every motion of the knight"""
    cave_bg(cv, seed=93)
    knight(cv, 620, 430, 200)
    knight(cv, 470, 450, 160, body=(0.35, 0.42, 0.55))
    knight(cv, 340, 470, 120, body=(0.20, 0.25, 0.34))
    cv.particles(10, 94, color=VIOLET, rmax=2.0, amax=0.28)


def scene_defender_crest(cv):
    """Defender's Crest: the crest emblem wreathed in heavy gas"""
    cave_bg(cv, top=(0.055, 0.075, 0.05), bot=(0.04, 0.055, 0.038), seed=95)
    charm_hk(cv, 500, 410, 170, base=BONE, symbol_fn=sym_thorns, loop=(0.40, 0.44, 0.34),
             glow_color=POISON_GRN)
    for px, py, pr in [(320, 560, 46), (680, 540, 54), (500, 640, 40), (260, 420, 34), (760, 400, 38)]:
        cv.paint(circle(cv.x, cv.y, px, py, pr), POISON_GRN, 0.22)


def scene_second_wind(cv):
    """Second Wind: a slow breath letting the excess drift away"""
    cave_bg(cv, seed=96)
    knight(cv, 500, 470, 210)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 430, 210 + i * 52, 205, 335), WISP, 0.6 - i * 0.14)
    cv.particles(14, 97, color=SOUL, rmax=2.2, amax=0.34)


def scene_soul_flow(cv):
    """Soul Flow: a stream of soul pouring from orb to vessel"""
    cave_bg(cv, top=PURP_TOP, bot=PURP_BOT, seed=98)
    cv.paint(circle(cv.x, cv.y, 320, 330, 74), (0.86, 0.94, 1.0))
    cv.paint(circle(cv.x, cv.y, 320, 330, 52), SOUL)
    for i in range(4):
        t = math.pi * (0.35 + i * 0.28)
        cv.paint(circle(cv.x, cv.y, 320 + math.cos(t) * 180 + i * 70, 330 + math.sin(t) * 120 + i * 30, 14),
                 SOUL, 0.85 - i * 0.12)
    knight(cv, 660, 480, 180)
    cv.glow(430, 380, 260, SOUL_SOFT, 0.3)


def scene_web_spin(cv):
    """Web Spin: weaver threads binding the whole field"""
    cave_bg(cv, seed=99)
    for i in range(7):
        a = math.radians(i * 180 / 6)
        cv.paint(seg(cv.x, cv.y, 500, 410, 500 + math.cos(a) * 400, 410 + math.sin(a) * 320) - 3,
                 (0.85, 0.88, 0.95), 0.55)
    for rr in (90, 170, 250, 330):
        cv.paint(arc(cv.x, cv.y, 500, 410, rr, 195, 345), (0.85, 0.88, 0.95), 0.4)
    grub(cv, 500, 620, 76, body=(0.55, 0.52, 0.66))


def scene_ricochet_nail(cv):
    """Ricochet Nail: the nail rebounding between foes"""
    cave_bg(cv, seed=100)
    nail_weapon(cv, 330, 300, 300, -40, color=BONE_DIM)
    nail_weapon(cv, 670, 360, 300, 20, color=BONE_DIM)
    pts = [(330, 300), (560, 470), (670, 360)]
    for i in range(2):
        cv.paint(seg(cv.x, cv.y, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1]) - 6, WISP, 0.85)
    cv.glow(560, 470, 200, SOUL_SOFT, 0.28)


def scene_nail_envenom(cv):
    """Envenomed Nail: venom beading on the nail's edge"""
    cave_bg(cv, top=GREEN_TOP, bot=(0.05, 0.08, 0.05), seed=101)
    nail_weapon(cv, 460, 380, 560, -34, color=BONE_DIM)
    for px, py, pr in [(610, 470, 16), (655, 520, 11), (588, 540, 8)]:
        cv.paint(circle(cv.x, cv.y, px, py, pr), POISON_GRN, 0.9)
    cv.glow(610, 470, 180, POISON_GRN, 0.3)


def scene_void_surge(cv):
    """Void Surge: the abyss pouring into the vessel turn by turn"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=102)
    void_tendrils(cv, n=9, seed=44)
    knight(cv, 500, 440, 220)
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, 500, 440, 180 + i * 46, 195, 345), VIOLET, 0.5 - i * 0.12)
    cv.particles(14, 103, color=VIOLET, rmax=2.4, amax=0.36)


def scene_soul_rush(cv):
    """Soul Rush: the knight streaking with sparks of borrowed energy"""
    cave_bg(cv, top=(0.05, 0.07, 0.11), bot=(0.08, 0.10, 0.15), seed=104)
    knight(cv, 660, 440, 190)
    for i in range(5):
        cv.paint(seg(cv.x, cv.y, 520 - i * 90, 380 + (i % 3) * 50, 600 - i * 90, 380 + (i % 3) * 50) - 5,
                 SOUL, 0.7 - i * 0.1)
    cv.particles(16, 105, color=SOUL, rmax=2.2, amax=0.36)


def scene_wraith_form(cv):
    """Wraith Form: the vessel half-dissolved into shade"""
    cave_bg(cv, top=(0.05, 0.05, 0.09), bot=(0.03, 0.035, 0.06), seed=106)
    knight(cv, 500, 460, 210, body=(0.16, 0.18, 0.26))
    knight(cv, 500, 430, 180, body=(0.30, 0.34, 0.48))
    for i in range(8):
        a = math.radians(i * 45 + 12)
        cv.paint(circle(cv.x, cv.y, 500 + math.cos(a) * 210, 430 + math.sin(a) * 150, 9), VIOLET, 0.6)
    cv.glow(500, 440, 300, VIOLET, 0.3)


def scene_soul_harvest(cv):
    """Soul Harvest: a reaping arc sweeping the fallen"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=107)
    slash(cv, 500, 420, 340, -8, 34, SOUL, 0.8, a0=-60, a1=60)
    spirit(cv, 330, 320, 120, color=SOUL, screaming=False)
    spirit(cv, 660, 480, 100, color=SOUL_SOFT, screaming=True)
    knight(cv, 520, 520, 170)
    cv.particles(18, 108, color=SOUL, rmax=2.6, amax=0.4)


def icon_grubberfly(cv):
    """grubberfly's elegance: a grub in a beam of grace"""
    cv.glow(cv.w / 2, cv.h / 2, 112, (1.0, 0.95, 0.7), 0.55)
    _medallion(cv, 96, (0.24, 0.28, 0.18))
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(np.maximum(ellipse(cv.x, cv.y, cx, cy, 10, 64), -(ellipse(cv.x, cv.y, cx, cy, 4, 64))),
             (1.0, 0.97, 0.8), 0.9)
    grub(cv, cx - 30, cy + 34, 44)


def icon_pain_brand(cv):
    """pain brand: a searing crimson brand"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (1.0, 0.4, 0.3), 0.5)
    _medallion(cv, 96, (0.32, 0.12, 0.10))
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(arc(cv.x, cv.y, cx, cy, 42, 200, 340) - 10, CRIMSON)
    cv.paint(arc(cv.x, cv.y, cx, cy, 26, 215, 325) - 6, (1.0, 0.55, 0.35))


def icon_wisp_afterimage(cv):
    """after image: two offset mask outlines"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.7, 0.6, 1.0), 0.5)
    _medallion(cv, 96, (0.20, 0.20, 0.32))
    cx, cy = cv.w / 2, cv.h / 2
    hk_mask(cv, cx - 12, cy + 4, 62, pale=(0.55, 0.62, 0.78))
    hk_mask(cv, cx + 14, cy - 6, 62)


def icon_defender_crest(cv):
    """defender's crest: thorn emblem over gas puffs"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.55, 0.9, 0.45), 0.45)
    _medallion(cv, 96, (0.20, 0.28, 0.18))
    cx, cy = cv.w / 2, cv.h / 2
    charm_hk(cv, cx, cy - 6, 52, base=BONE, symbol_fn=sym_thorns, loop=(0.42, 0.44, 0.36))
    for dx, dy, pr in ((-44, 34, 12), (40, 38, 14), (0, 50, 11)):
        cv.paint(circle(cv.x, cv.y, cx + dx, cy + dy, pr), POISON_GRN, 0.4)


def icon_nail_envenom(cv):
    """envenomed nail: a nail tip with a venom drop"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.55, 0.9, 0.45), 0.5)
    _medallion(cv, 96, (0.18, 0.26, 0.16))
    cx, cy = cv.w / 2, cv.h / 2
    nail_weapon(cv, cx - 6, cy - 10, 120, 90, color=BONE_DIM)
    cv.paint(circle(cv.x, cv.y, cx + 30, cy + 34, 11), POISON_GRN)
    cv.paint(circle(cv.x, cv.y, cx + 44, cy + 46, 6), POISON_GRN, 0.7)


def icon_void_surge(cv):
    """void surge: a spiralling tendril over the medallion"""
    cv.glow(cv.w / 2, cv.h / 2, 112, (0.7, 0.5, 1.0), 0.55)
    _medallion(cv, 96, (0.16, 0.12, 0.24))
    cx, cy = cv.w / 2, cv.h / 2
    for i in range(3):
        cv.paint(arc(cv.x, cv.y, cx, cy + 6, 24 + i * 17, 200 + i * 35, 330 + i * 35),
                 (0.80, 0.66, 1.0), 0.8 - i * 0.18)
    cv.paint(circle(cv.x, cv.y, cx, cy + 6, 10), VIOLET)


def relic_gathering_swarm(cv, r):
    """gathering swarm: a swarm of tiny lights circling a geo"""
    cv.glow(128, 128, 100, (1.0, 0.85, 0.5), 0.4)
    for i in range(9):
        a = math.radians(i * 40 + 15)
        rr = r * (0.55 + (i % 3) * 0.16)
        cv.paint(circle(cv.x, cv.y, 128 + math.cos(a) * rr, 128 + math.sin(a) * rr * 0.8, r * 0.055),
                 (1.0, 0.88, 0.6), 0.85)
    cv.paint(circle(cv.x, cv.y, 128, 128, r * 0.20), (0.95, 0.85, 0.45))
    cv.paint(circle(cv.x, cv.y, 128, 128, r * 0.11), (0.80, 0.65, 0.30))


def relic_dreamcatcher(cv, r):
    """dreamcatcher: a woven net with a hanging wisp"""
    cv.glow(128, 128, 100, VIOLET, 0.4)
    cv.paint(seg(cv.x, cv.y, 128, 128 - r, 128, 128 - r * 0.55) - r * 0.04, (0.72, 0.68, 0.85))
    for rr in (0.20, 0.36, 0.52):
        cv.paint(np.abs(np.hypot(cv.x - 128, cv.y - (128 - r * 0.10)) - r * rr) - r * 0.028,
                 (0.80, 0.76, 0.92), 0.85)
    for i in range(8):
        a = math.radians(i * 45)
        cv.paint(seg(cv.x, cv.y, 128, 128 - r * 0.10,
                     128 + math.cos(a) * r * 0.52, 128 - r * 0.10 + math.sin(a) * r * 0.52) - r * 0.022,
                 (0.80, 0.76, 0.92), 0.6)
    cv.paint(circle(cv.x, cv.y, 128, 128 + r * 0.62, r * 0.10), WISP, 0.95)


def relic_unbreakable_heart(cv, r):
    """unbreakable heart: a pale heart charm, whole"""
    cv.glow(128, 128, 100, (1.0, 0.6, 0.6), 0.4)
    cx, cy = 128, 128
    # SDF union of two lobes and a point: union is MINIMUM (maximum = intersection)
    heart = np.minimum(
        np.minimum(circle(cv.x, cv.y, cx - r * 0.24, cy - r * 0.18, r * 0.30),
                   circle(cv.x, cv.y, cx + r * 0.24, cy - r * 0.18, r * 0.30)),
        convex(cv.x, cv.y, [(cx - r * 0.52, cy - r * 0.05), (cx + r * 0.52, cy - r * 0.05), (cx, cy + r * 0.62)]))
    cv.paint(heart, (0.92, 0.88, 0.90))
    cv.paint(heart, (0.85, 0.55, 0.55), 0.45)
    cv.paint(circle(cv.x, cv.y, cx, cy + r * 0.05, r * 0.07), (1.0, 1.0, 1.0), 0.9)


def relic_quick_focus(cv, r):
    """quick focus: a mask with converging focus rings"""
    cv.glow(128, 128, 100, SOUL_SOFT, 0.4)
    hk_mask(cv, 128, 128, r * 0.62)
    for rr in (0.78, 0.92):
        cv.paint(np.abs(np.hypot(cv.x - 128, cv.y - 128) - r * rr) - r * 0.025, SOUL, 0.6)


def relic_salubras_blessing(cv, r):
    """salubra's blessing: a whispering wisp over a blessing ring"""
    cv.glow(128, 128, 100, (1.0, 0.8, 0.95), 0.4)
    cv.paint(np.abs(np.hypot(cv.x - 128, cv.y - 132) - r * 0.72) - r * 0.05, (0.90, 0.75, 0.88), 0.8)
    spirit(cv, 128, 112, r * 0.72, color=(1.0, 0.88, 0.95), eye=(0.10, 0.04, 0.10))
    for dx, dy in ((-40, 60), (36, 66), (0, 78)):
        cv.paint(circle(cv.x, cv.y, 128 + dx, 132 + dy, r * 0.06), (1.0, 0.85, 0.95), 0.85)


# ---------------------------------------------------------------- v0.6.0 gold expansion art
def scene_nail_storm(cv):
    """Nail Storm: nails raining across the whole field"""
    cave_bg(cv, seed=110)
    for px, deg, ln in [(280, -60, 300), (500, -90, 340), (720, -55, 280), (610, -75, 240), (390, -80, 240)]:
        nail_weapon(cv, px, 300, ln, deg, color=BONE_DIM)
    slash(cv, 480, 500, 300, -10, 30, WISP, 0.6, a0=-50, a1=50)
    slash(cv, 620, 560, 260, 25, 26, WISP, 0.45, a0=-45, a1=45)
    cv.particles(14, 111, rmax=2.2, amax=0.3)


def scene_impregnable(cv):
    """Impregnable: the shell hardening into layer upon layer of wall"""
    cave_bg(cv, seed=112)
    knight(cv, 500, 440, 200)
    for i in range(5):
        cv.paint(arc(cv.x, cv.y, 500, 440, 170 + i * 42, 205, 335), SOUL_SOFT, 0.75 - i * 0.13)
    cv.glow(500, 440, 280, SOUL_SOFT, 0.3)


def scene_soul_aegis(cv):
    """Soul Aegis: stored soul pouring into a rising dome"""
    cave_bg(cv, top=PURP_TOP, bot=PURP_BOT, seed=113)
    knight(cv, 500, 470, 200)
    cv.paint(arc(cv.x, cv.y, 500, 620, 300, 205, 335) - 14, SOUL, 0.85)
    cv.paint(arc(cv.x, cv.y, 500, 620, 250, 210, 330) - 8, (1.0, 1.0, 1.0), 0.6)
    for i in range(6):
        a = math.radians(215 + i * 26)
        cv.paint(circle(cv.x, cv.y, 500 + math.cos(a) * 330, 620 + math.sin(a) * 330, 9), SOUL, 0.8)
    cv.glow(500, 450, 300, SOUL_SOFT, 0.35)


def scene_hallownests_echo(cv):
    """Hallownest's Echo: the kingdom's ruins ringing with one vast wave"""
    cave_bg(cv, top=(0.06, 0.065, 0.10), bot=(0.04, 0.045, 0.07), seed=114)
    arch(cv, 300, 660, 260, 34)
    arch(cv, 740, 700, 300, 30)
    tombstones(cv, seed=46)
    for i in range(4):
        cv.paint(arc(cv.x, cv.y, 500, 900, 560 - i * 95, 225, 315), SOUL, 0.6 - i * 0.11)
    cv.glow(500, 480, 340, SOUL_SOFT, 0.35)


def scene_void_feast(cv):
    """Void Feast: tendrils swallowing a small spirit whole"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=115)
    void_tendrils(cv, n=8, seed=47)
    spirit(cv, 500, 380, 150, color=(0.75, 0.62, 0.92), screaming=True)
    for px, py in [(370, 250), (640, 280), (420, 210)]:
        cv.paint(seg(cv.x, cv.y, px, py, 500, 380) - 10, (0.08, 0.05, 0.14), 0.9)
    cv.particles(10, 116, color=VIOLET, rmax=2.2, amax=0.34)


def scene_soul_pact(cv):
    """Soul Pact: a burning contract offering up a card for insight"""
    cave_bg(cv, seed=117)
    cv.paint(convex(cv.x, cv.y, [(380, 260), (620, 260), (650, 540), (350, 540)]), (0.82, 0.76, 0.62))
    for i in range(3):
        cv.paint(seg(cv.x, cv.y, 420, 330 + i * 60, 590, 330 + i * 60) - 4, (0.25, 0.20, 0.16))
    cv.paint(seg(cv.x, cv.y, 430, 560, 470, 640) - 7, EMBER, 0.9)
    cv.paint(seg(cv.x, cv.y, 570, 560, 535, 645) - 7, EMBER, 0.9)
    cv.particles(12, 118, color=EMBER, rmax=2.2, amax=0.36)
    cv.glow(500, 400, 240, (1.0, 0.75, 0.5), 0.3)


def scene_soul_echo(cv):
    """Soul Echo: every outflow ringing outward in rings"""
    cave_bg(cv, top=(0.055, 0.06, 0.10), bot=(0.04, 0.045, 0.075), seed=119)
    cv.paint(circle(cv.x, cv.y, 500, 410, 96), SOUL)
    cv.paint(circle(cv.x, cv.y, 476, 386, 20), (1.0, 1.0, 1.0), 0.85)
    for i in range(4):
        cv.paint(arc(cv.x, cv.y, 500, 410, 160 + i * 62, 195, 345), SOUL_SOFT, 0.6 - i * 0.12)
    cv.glow(500, 410, 300, SOUL_SOFT, 0.35)


def scene_refinement(cv):
    """Refinement: the nail glowing white-hot over the forge"""
    cave_bg(cv, top=(0.08, 0.06, 0.05), bot=(0.05, 0.04, 0.035), seed=120)
    nail_weapon(cv, 500, 400, 560, -20, color=(1.0, 0.85, 0.6), glow=True)
    cv.paint(convex(cv.x, cv.y, [(330, 660), (670, 660), (620, 760), (380, 760)]), (0.14, 0.12, 0.12))
    cv.particles(16, 121, color=EMBER, rmax=2.6, amax=0.4)
    cv.glow(500, 420, 300, (1.0, 0.65, 0.35), 0.4)


def icon_soul_echo(cv):
    """soul echo: rings pouring off a soul orb"""
    cv.glow(cv.w / 2, cv.h / 2, 112, (0.6, 0.85, 1.0), 0.55)
    _medallion(cv, 96, (0.18, 0.28, 0.42))
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy, 30), SOUL)
    cv.paint(circle(cv.x, cv.y, cx - 9, cy - 9, 6), (1.0, 1.0, 1.0))
    for i in range(2):
        cv.paint(arc(cv.x, cv.y, cx, cy, 46 + i * 16, 200, 340), SOUL_SOFT, 0.8 - i * 0.25)


# ---------------------------------------------------------------- v0.6.0 round-2 art
def scene_soul_battery(cv):
    """Soul Battery: a quiet pool slowly filling itself"""
    cave_bg(cv, top=PURP_TOP, bot=PURP_BOT, seed=122)
    cv.paint(ellipse(cv.x, cv.y, 500, 560, 330, 70), SOUL, 0.8)
    cv.paint(ellipse(cv.x, cv.y, 500, 555, 250, 46), (1.0, 1.0, 1.0), 0.35)
    knight(cv, 500, 360, 170)
    for i in range(5):
        cv.paint(circle(cv.x, cv.y, 420 + i * 40, 300 - i * 34, 8 - i * 0.8), SOUL, 0.85 - i * 0.1)
    cv.glow(500, 500, 280, SOUL_SOFT, 0.3)


def scene_venom_blade(cv):
    """Venom Blade: a slash trailing heavy venom"""
    cave_bg(cv, top=GREEN_TOP, bot=(0.05, 0.08, 0.05), seed=123)
    slash(cv, 500, 400, 330, -14, 34, POISON_GRN, 0.75, a0=-55, a1=55)
    for px, py, pr in [(560, 520, 14), (620, 590, 10), (520, 640, 7)]:
        cv.paint(circle(cv.x, cv.y, px, py, pr), POISON_GRN, 0.9)
    cv.glow(560, 520, 200, POISON_GRN, 0.3)


def scene_catalyst(cv):
    """Catalyst: the vial boiling over with concentrated venom"""
    cave_bg(cv, top=GREEN_TOP, bot=GREEN_BOT, seed=124)
    cv.paint(convex(cv.x, cv.y, [(430, 330), (570, 330), (620, 560), (380, 560)]), (0.55, 0.75, 0.5), 0.55)
    cv.paint(seg(cv.x, cv.y, 430, 330, 570, 330) - 5, (0.80, 0.82, 0.78))
    cv.paint(seg(cv.x, cv.y, 470, 330, 470, 270) - 5, (0.80, 0.82, 0.78))
    cv.paint(seg(cv.x, cv.y, 530, 330, 530, 270) - 5, (0.80, 0.82, 0.78))
    cv.paint(ellipse(cv.x, cv.y, 500, 480, 200, 56), POISON_GRN, 0.9)
    for px, py, pr in [(440, 430, 12), (540, 420, 15), (500, 390, 9)]:
        cv.paint(circle(cv.x, cv.y, px, py, pr), (0.85, 1.0, 0.75), 0.8)
    cv.particles(12, 125, color=POISON_GRN, rmax=2.4, amax=0.36)
    cv.glow(500, 470, 260, POISON_GRN, 0.35)


def scene_bone_break(cv):
    """Bone Break: a crushing swing with fracture lines"""
    cave_bg(cv, top=(0.08, 0.06, 0.055), bot=(0.05, 0.04, 0.04), seed=126)
    nail_weapon(cv, 470, 350, 560, 70, color=BONE_DIM)
    for a0, a1 in [(200, 250), (255, 300), (310, 350)]:
        cv.paint(arc(cv.x, cv.y, 500, 560, 150, a0, a1) - 9, WISP, 0.85)
    cv.paint(seg(cv.x, cv.y, 430, 640, 500, 590) - 5, DARK)
    cv.paint(seg(cv.x, cv.y, 560, 650, 520, 585) - 5, DARK)
    cv.glow(500, 550, 240, SOUL_SOFT, 0.3)


def scene_soul_siphon(cv):
    """Soul Siphon: essence streaming back along the nail"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=127)
    knight(cv, 620, 440, 200)
    spirit(cv, 320, 400, 140, color=(0.72, 0.60, 0.95), screaming=False)
    for i in range(5):
        cv.paint(seg(cv.x, cv.y, 420 + i * 14, 400 + math.sin(i * 1.4) * 30, 520 + i * 14, 430 + math.sin(i * 1.1) * 26) - 5,
                 SOUL, 0.75 - i * 0.09)
    cv.glow(500, 420, 240, SOUL_SOFT, 0.3)


def scene_twin_slash(cv):
    """Twin Slash: two clean parallel arcs"""
    cave_bg(cv, seed=128)
    nail_weapon(cv, 440, 430, 520, -30, color=BONE_DIM)
    slash(cv, 500, 400, 300, -12, 24, WISP, 0.8, a0=-50, a1=50)
    slash(cv, 560, 480, 280, 8, 22, SOUL_SOFT, 0.55, a0=-45, a1=45)


def scene_residual_soul(cv):
    """Residual Soul: a tiny wisp cupped in the Knight's hands"""
    cave_bg(cv, seed=129)
    knight(cv, 500, 500, 220)
    cv.paint(circle(cv.x, cv.y, 500, 300, 42), SOUL)
    cv.paint(circle(cv.x, cv.y, 486, 286, 10), (1.0, 1.0, 1.0), 0.9)
    for i in range(4):
        cv.paint(circle(cv.x, cv.y, 470 + i * 20, 240 - i * 14, 5 - i * 0.6), WISP, 0.8 - i * 0.12)
    cv.glow(500, 300, 200, SOUL_SOFT, 0.35)


def icon_soul_battery(cv):
    """soul battery: a pool with a rising drop"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.6, 0.8, 1.0), 0.5)
    _medallion(cv, 96, (0.18, 0.22, 0.38))
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(ellipse(cv.x, cv.y, cx, cy + 26, 56, 22), SOUL, 0.95)
    for i in range(3):
        cv.paint(circle(cv.x, cv.y, cx + 8, cy - 8 - i * 18, 7 - i * 1.5), SOUL, 0.9 - i * 0.15)


def relic_soul_candle(cv, r):
    """soul candle: a lit candle with a wisp curl"""
    cv.glow(128, 128, 100, (1.0, 0.8, 0.5), 0.45)
    cx, cy = 128, 128
    cv.paint(convex(cv.x, cv.y, [(cx - r * 0.22, cy - r * 0.1), (cx + r * 0.22, cy - r * 0.1),
                                 (cx + r * 0.30, cy + r * 0.75), (cx - r * 0.30, cy + r * 0.75)]),
             (0.90, 0.85, 0.70))
    cv.paint(seg(cv.x, cv.y, cx, cy - r * 0.10, cx, cy - r * 0.24) - r * 0.035, (0.35, 0.28, 0.22))
    cv.paint(ellipse(cv.x, cv.y, cx, cy - r * 0.40, r * 0.14, r * 0.26), EMBER)
    cv.paint(ellipse(cv.x, cv.y, cx, cy - r * 0.44, r * 0.07, r * 0.14), (1.0, 0.95, 0.7))
    cv.paint(circle(cv.x, cv.y, cx + r * 0.42, cy - r * 0.35, r * 0.08), WISP, 0.9)


def relic_void_brand(cv, r):
    """void brand: a seared violet sigil"""
    cv.glow(128, 128, 100, VIOLET, 0.45)
    cv.paint(np.abs(np.hypot(cv.x - 128, cv.y - 128) - r * 0.62) - r * 0.06, (0.22, 0.16, 0.30), 0.95)
    cv.paint(arc(cv.x, cv.y, 128, 128, r * 0.40, 200, 340) - r * 0.055, VIOLET, 0.95)
    cv.paint(arc(cv.x, cv.y, 128, 128, r * 0.26, 215, 325) - r * 0.04, (0.85, 0.72, 1.0), 0.9)
    cv.paint(circle(cv.x, cv.y, 128, 128, r * 0.10), (0.85, 0.72, 1.0))


def relic_hallownest_seal(cv, r):
    """hallownest seal: the kingdom's pale seal with nail emblem"""
    cv.glow(128, 128, 100, SOUL_SOFT, 0.4)
    charm_hk(cv, 128, 128, r * 0.82, base=BONE, symbol_fn=sym_nail, loop=BRASS, glow_color=SOUL_SOFT)


# ---------------------------------------------------------------- card scenes
CARD_W, CARD_H = 1000, 760


def scene_old_nail(cv):
    """the old nail lodged in a mossy stone at the King's Pass"""
    cave_bg(cv, seed=11)
    arch(cv, 500, 760, 470, 40)
    mound = ellipse(cv.x, cv.y, 500, 660, 300, 60)
    cv.paint(mound, (0.075, 0.105, 0.085))
    cv.paint(cv.y - 655, (0.055, 0.085, 0.065), 0.85)
    nail_weapon(cv, 500, 430, 560, -95, color=BONE_DIM, cracked=True)
    rng = np.random.default_rng(6)
    for _ in range(9):  # pale grass tufts
        gx = rng.uniform(0.22, 0.78) * cv.w
        gy = 640 + rng.uniform(-8, 26)
        gl = rng.uniform(18, 40)
        cv.paint(seg(cv.x, cv.y, gx, gy, gx + rng.uniform(-10, 10), gy - gl) - 2.2,
                 (0.45, 0.60, 0.40), rng.uniform(0.4, 0.8))


def scene_shell(cv):
    cave_bg(cv, seed=12)
    hk_mask(cv, 500, 400, 260)
    cv.glow(500, 400, 420, SOUL_SOFT, 0.30)


def scene_focus(cv):
    """Focus: kneeling vessel gathering SOUL among Resting Grounds graves"""
    cave_bg(cv, top=(0.050, 0.055, 0.095), bot=VOID_BOT, seed=13)
    tombstones(cv, seed=33)
    knight(cv, 500, 480, 230)
    cv.glow(500, 225, 170, SOUL, 0.45)
    cv.paint(circle(cv.x, cv.y, 500, 225, 56), WISP, 0.98)
    for i in range(5):  # soul wisps converging
        a = math.radians(60 + i * 72)
        rr = 130 - (i % 2) * 30
        cv.paint(circle(cv.x, cv.y, 500 + math.cos(a) * rr, 225 + math.sin(a) * rr * 0.55,
                        9 - (i % 3) * 2), WISP, 0.85 - (i % 3) * 0.15)


def scene_quick_slash(cv):
    """the vessel mid-slash, shade afterimage trailing"""
    cave_bg(cv, seed=14)
    knight(cv, 380, 420, 200, body=(0.10, 0.105, 0.15), glow=False)
    knight(cv, 560, 410, 200)
    slash(cv, 520, 420, 290, -22, 26, WISP, 0.95)
    slash(cv, 590, 380, 225, 26, 17, SOUL, 0.8, inner=False)
    cv.particles(18, 41, rmax=2.2, amax=0.3)


def scene_dark_shrine(cv):
    """a soul totem humming with gathered SOUL"""
    cave_bg(cv, top=PURP_TOP, bot=PURP_BOT, seed=15)
    soul_totem(cv, 500, 430, 300, 420)
    glowbugs(cv, n=9, seed=37)
    cv.particles(14, 47, color=SOUL, rmax=2.2, amax=0.3)


def scene_great_slash(cv):
    """the Great Slash nail art: one devastating crescent"""
    cave_bg(cv, seed=16)
    nail_weapon(cv, 430, 430, 560, -12, color=BONE_DIM)
    slash(cv, 520, 400, 330, -6, 40, WISP, 0.95, a0=-55, a1=55)


def scene_cyclone_slash(cv):
    """Cyclone Slash: spinning nail in a storm of Greenpath leaves"""
    cave_bg(cv, top=GREEN_TOP, bot=GREEN_BOT, seed=17)
    knight(cv, 500, 420, 200)
    for i, (r, wd, al) in enumerate([(250, 20, 0.9), (330, 14, 0.7), (180, 12, 0.6)]):
        slash(cv, 500, 400, r, i * 55, wd, WISP if i % 2 == 0 else SOUL, al)
    greenery(cv, seed=35, leaves_n=16, vines=4)


def scene_dash(cv):
    """Mothwing Cloak dash: the vessel blurs forward in a pale streak"""
    cave_bg(cv, seed=18)
    arch(cv, 500, 780, 470, 42)
    for i in range(8):
        yy = 300 + i * 52 + (i % 3) * 16
        ln = 260 + (i * 53) % 200
        cv.paint(seg(cv.x, cv.y, 700 - ln, yy, 840, yy) - 6, WISP, 0.14 + (i % 3) * 0.05)
    cv.paint(seg(cv.x, cv.y, 130, 430, 430, 430) - 30, SOUL, 0.30)
    cv.paint(seg(cv.x, cv.y, 200, 470, 420, 470) - 16, WISP, 0.45)
    knight(cv, 580, 420, 190)


def scene_dash_slash(cv):
    """Dash Slash nail art: dart through with twin cuts"""
    cave_bg(cv, seed=19)
    nail_weapon(cv, 560, 360, 520, -20, color=BONE_DIM)
    slash(cv, 500, 430, 310, -8, 20, WISP, 0.9)
    slash(cv, 500, 470, 310, -8, 20, WISP, 0.55, inner=False)
    cv.particles(16, 45, rmax=2.0, amax=0.28)


def scene_desolate_dive(cv):
    """Desolate Dive: plummet and erupt in a shockwave"""
    cave_bg(cv, top=(0.05, 0.06, 0.10), bot=VOID_BOT, seed=20)
    knight(cv, 470, 260, 170)
    for i in range(4):
        cv.paint(seg(cv.x, cv.y, 430 + i * 30, 60, 450 + i * 28, 180) - 9, WISP, 0.18)
    cv.paint(np.abs(ellipse(cv.x, cv.y, 500, 600, 300, 52)) - 24, SOUL, 0.55)
    cv.paint(np.abs(ellipse(cv.x, cv.y, 500, 600, 180, 22)) - 10, WISP, 0.4)
    for i, (px, pr) in enumerate([(280, 26), (660, 34), (790, 20), (180, 18)]):
        cv.paint(circle(cv.x, cv.y, px, 640 + (i % 2) * 26, pr), (0.08, 0.09, 0.13))
    cv.glow(500, 590, 330, SOUL_SOFT, 0.4)


def scene_vengeful_spirit(cv):
    """Vengeful Spirit over the rain of the City of Tears"""
    city_rain(cv, seed=31)
    spirit(cv, 540, 350, 250, color=(1.0, 0.88, 0.74), eye=(0.30, 0.10, 0.06))
    cv.glow(540, 350, 400, EMBER, 0.30)
    cv.particles(16, 51, color=(1.0, 0.80, 0.55), rmax=2.4, amax=0.32)


def scene_abyss_shriek(cv):
    """Abyss Shriek: screaming wraiths rise from the void"""
    cave_bg(cv, top=ABYSS_TOP, bot=ABYSS_BOT, seed=22)
    void_tendrils(cv, n=7, seed=39)
    cv.paint(ellipse(cv.x, cv.y, 500, 420, 170, 380), (0.045, 0.03, 0.08), 0.85)
    for i in range(3):
        spirit(cv, 500 + (i - 1) * 200, 560 - i * 60, 115 + (i % 2) * 30,
               color=(0.72, 0.60, 0.95), eye=(0.02, 0.01, 0.04), screaming=True)
    for i in range(6):
        xx = 260 + i * 95
        cv.paint(seg(cv.x, cv.y, xx, 700 - (i % 3) * 60, xx + 14, 240 + (i % 2) * 80) - 8,
                 VIOLET, 0.20)
    cv.glow(500, 300, 380, VIOLET, 0.30)


def scene_unbreakable_strength(cv):
    """Unbreakable Strength charm, refined by Divine"""
    cave_bg(cv, seed=23)
    charm_hk(cv, 500, 410, 170, base=BONE, symbol_fn=sym_nail, loop=BRASS,
             glow_color=SOUL_SOFT)


def scene_steady_body(cv):
    """Steady Body charm: feet planted, unmoving"""
    cave_bg(cv, seed=24)
    arch(cv, 500, 780, 460, 40)
    charm_hk(cv, 500, 410, 170, base=BONE, symbol_fn=sym_stance, loop=(0.45, 0.42, 0.38),
             glow_color=SOUL_SOFT)


def scene_fragile_strength(cv):
    """Fragile Strength charm: green, cracked, still burning with fury"""
    cave_bg(cv, top=GREEN_TOP, bot=GREEN_BOT, seed=25)
    greenery(cv, seed=36, leaves_n=12, vines=4)
    charm_hk(cv, 500, 420, 170, base=BONE_GREEN, symbol_fn=sym_nail, cracked=True,
             loop=(0.35, 0.40, 0.28), glow_color=(1.0, 0.62, 0.38))


def scene_thorns_of_agony(cv):
    """Thorns of Agony charm wreathed in vines"""
    cave_bg(cv, top=GREEN_TOP, bot=(0.06, 0.10, 0.07), seed=26)
    greenery(cv, seed=38, leaves_n=12, vines=6)
    charm_hk(cv, 500, 410, 170, base=BONE, symbol_fn=sym_thorns, loop=(0.45, 0.42, 0.38),
             glow_color=SOUL_SOFT)


def scene_shaman_stone(cv):
    """Shaman Stone charm amid the Fungal Wastes mushrooms"""
    cave_bg(cv, top=PURP_TOP, bot=(0.09, 0.10, 0.12), seed=27)
    for px, pw, ph in [(180, 150, 190), (820, 170, 220), (660, 110, 140), (300, 95, 120)]:
        stem_c = (0.11, 0.115, 0.145)
        cap_c = (0.16, 0.135, 0.19)
        cy_base = 680
        cv.paint(convex(cv.x, cv.y, [(px - pw * 0.16, cy_base), (px + pw * 0.16, cy_base),
                                     (px + pw * 0.10, cy_base - ph), (px - pw * 0.10, cy_base - ph)]),
                 stem_c)
        cap_pts = []
        for k in range(7):
            t = math.pi * k / 6
            cap_pts.append((px - math.cos(t) * pw * 0.5, cy_base - ph - math.sin(t) * ph * 0.34))
        cap_pts += [(px + pw * 0.5, cy_base - ph * 0.86), (px - pw * 0.5, cy_base - ph * 0.86)]
        cv.paint(convex(cv.x, cv.y, cap_pts), cap_c)
        cv.particles(6, 61, color=(0.55, 0.50, 0.65), rmax=2.0, amax=0.2)
    charm_hk(cv, 500, 400, 165, base=BONE_GREY, symbol_fn=sym_spiral,
             loop=(0.42, 0.40, 0.36), glow_color=SOUL_SOFT)


def scene_channelled_nail(cv):
    """Channelled Nail: three swift cuts from a single swing"""
    cave_bg(cv, seed=28)
    nail_weapon(cv, 470, 400, 540, -30, color=BONE_DIM)
    for i, (off, al) in enumerate([(0, 0.95), (70, 0.65), (140, 0.4)]):
        slash(cv, 500, 420 + off * 0.4, 300 - i * 12, -14 + i * 5, 13, WISP, al, inner=(i == 0))
    cv.particles(16, 63, rmax=2.0, amax=0.28)


def scene_coiled_nail(cv):
    """Coiled Nail: the blade wound in a spiral spring"""
    cave_bg(cv, seed=29)
    nail_weapon(cv, 500, 380, 620, 90, color=BONE_DIM, glow=True)
    for i, (rr, a0, wd) in enumerate([(300, 200, 13), (258, 170, 10), (214, 140, 8)]):
        cv.paint(arc(cv.x, cv.y, 500, 380, rr, a0, a0 + 235) - wd, (0.72, 0.78, 0.86),
                 0.85 - i * 0.1)
    cv.glow(500, 380, 320, SOUL_SOFT, 0.25)


def scene_kingsoul(cv):
    """Kingsoul: a radiant charm of the King's own pale essence"""
    cave_bg(cv, top=(0.075, 0.075, 0.105), bot=(0.115, 0.105, 0.13), seed=30)
    for i in range(10):  # rays
        a = math.radians(i * 36 + 12)
        r0, r1 = 205, 300 + (i % 2) * 46
        cv.paint(seg(cv.x, cv.y, 500 + math.cos(a) * r0, 400 + math.sin(a) * r0,
                     500 + math.cos(a) * r1, 400 + math.sin(a) * r1) - 7,
                 (0.98, 0.95, 0.80), 0.35)
    charm_hk(cv, 500, 400, 170, base=(0.965, 0.945, 0.865), symbol_fn=sym_spiral,
             loop=BRASS_L, glow_color=(1.0, 0.95, 0.75))
    cv.glow(500, 400, 300, (1.0, 0.94, 0.70), 0.35)


def scene_baldur_shell(cv):
    """Baldur Shell: a hard-shelled beetle curls around the vessel"""
    cave_bg(cv, seed=32)
    knight(cv, 500, 430, 210)
    dome(cv, 500, 470, 430, 260, (0.30, 0.32, 0.36), seg_lines=True)
    cv.paint(convex(cv.x, cv.y, [(700, 330), (760, 300), (742, 372)]), (0.13, 0.135, 0.17))
    glint(cv, 688, 336, 22, WISP, 0.95)
    cv.particles(12, 65, rmax=2.0, amax=0.25)


def scene_shape_of_unn(cv):
    """Shape of Unn: the vessel takes a humble, hardened little form"""
    cave_bg(cv, top=GREEN_TOP, bot=GREEN_BOT, seed=34)
    greenery(cv, seed=40, leaves_n=10, vines=4)
    cv.glow(500, 450, 260, SOUL_SOFT, 0.25)
    pts = []
    for k in range(9):
        t = math.pi * k / 8
        pts.append((500 - math.cos(t) * 190, 600 - math.sin(t) * 240))
    cv.paint(convex(cv.x, cv.y, pts), DARK)
    cv.paint(ellipse(cv.x, cv.y, 500, 602, 190, 26), DARK)
    hk_mask(cv, 500, 372, 118, glow=False)
    cv.paint(ellipse(cv.x, cv.y, 500, 626, 90, 12), (0.10, 0.105, 0.14))


def scene_howling_wraiths(cv):
    """Howling Wraiths: wailing spirits rise from the vessel in a cone"""
    cave_bg(cv, top=(0.055, 0.045, 0.095), bot=(0.11, 0.08, 0.15), seed=35)
    knight(cv, 500, 560, 200)
    for i in range(3):
        sx = 500 + (i - 1) * 230
        sy = 360 - i * 70
        spirit(cv, sx, sy, 130 + (i % 2) * 24, color=(0.85, 0.78, 1.0), eye=(0.16, 0.08, 0.24), screaming=True)
        cv.paint(seg(cv.x, cv.y, sx + 24, sy + 160, sx - 10, sy - 60) - 10, VIOLET, 0.30)
    cv.glow(500, 280, 360, VIOLET, 0.32)


def scene_shade_soul(cv):
    """Shade Soul: a darker, void-born shade tears across the field"""
    cave_bg(cv, top=(0.035, 0.030, 0.060), bot=(0.085, 0.065, 0.110), seed=36)
    void_tendrils(cv, n=6, seed=44)
    spirit(cv, 540, 360, 270, color=(0.22, 0.18, 0.30), eye=WISP)
    cv.glow(540, 360, 300, (0.55, 0.42, 0.85), 0.4)
    cv.paint(arc(cv.x, cv.y, 540, 360, 300, 200, 340) - 8, VIOLET, 0.4)
    cv.particles(18, 76, color=(0.70, 0.55, 0.95), rmax=2.4, amax=0.3)


def scene_descending_dark(cv):
    """Descending Dark: the vessel drops as darkness, shockwaves through Block"""
    cave_bg(cv, top=(0.045, 0.040, 0.075), bot=VOID_BOT, seed=37)
    knight(cv, 500, 220, 160)
    for i, (rx, ry, al) in enumerate([(340, 60, 0.5), (240, 40, 0.65), (150, 24, 0.8)]):
        cv.paint(np.abs(ellipse(cv.x, cv.y, 500, 610, rx, ry)) - 13, SOUL, al)
    cv.paint(ellipse(cv.x, cv.y, 500, 330, 26, 220), (0.06, 0.05, 0.10), 0.9)
    cv.glow(500, 600, 340, SOUL_SOFT, 0.42)


def scene_shade_cloak(cv):
    """Shade Cloak: the vessel phases, leaving void afterimages"""
    cave_bg(cv, top=(0.030, 0.028, 0.055), bot=(0.075, 0.06, 0.115), seed=38)
    for i, (ox, al) in enumerate([(-190, 0.45), (-360, 0.25)]):
        knight(cv, 560 + ox, 400, 190, body=(0.04 + i * 0.02, 0.04 + i * 0.018, 0.075 + i * 0.03), glow=False)
    knight(cv, 560, 400, 190)
    for i in range(6):
        xx = 210 + i * 58
        cv.paint(seg(cv.x, cv.y, xx, 420 + (i % 3) * 22, xx + 60, 400 + (i % 2) * 18) - 7, VIOLET, 0.35)
    cv.paint(arc(cv.x, cv.y, 560, 470, 230, 205, 335) - 9, VIOLET, 0.5)


CARD_SCENES = {
    "old_nail": scene_old_nail,
    "shell": scene_shell,
    "focus": scene_focus,
    "quick_slash": scene_quick_slash,
    "dark_shrine": scene_dark_shrine,
    "great_slash": scene_great_slash,
    "cyclone_slash": scene_cyclone_slash,
    "dash": scene_dash,
    "dash_slash": scene_dash_slash,
    "desolate_dive": scene_desolate_dive,
    "vengeful_spirit": scene_vengeful_spirit,
    "abyss_shriek": scene_abyss_shriek,
    "unbreakable_strength": scene_unbreakable_strength,
    "steady_body": scene_steady_body,
    "fragile_strength": scene_fragile_strength,
    "thorns_of_agony": scene_thorns_of_agony,
    "shaman_stone": scene_shaman_stone,
    "channelled_nail": scene_channelled_nail,
    "coiled_nail": scene_coiled_nail,
    "kingsoul": scene_kingsoul,
    "baldur_shell": scene_baldur_shell,
    "shape_of_unn": scene_shape_of_unn,
    "howling_wraiths": scene_howling_wraiths,
    "shade_soul": scene_shade_soul,
    "descending_dark": scene_descending_dark,
    "shade_cloak": scene_shade_cloak,
    "spellcharge": scene_spellcharge,
    "soulbell": scene_soulbell,
    "full_soul": scene_full_soul,
    "broken_shell": scene_broken_shell,
    "hum": scene_hum,
    "broken_vessel": scene_broken_vessel,
    "nail_flurry": scene_nail_flurry,
    "soul_torrent": scene_soul_torrent,
    "dreamshield": scene_dreamshield,
    "soul_surge": scene_soul_surge,
    "grimmchild": scene_grimmchild,
    "heavy_blow": scene_heavy_blow,
    "raging_nail": scene_raging_nail,
    "shell_slam": scene_shell_slam,
    "mothwing_dash": scene_mothwing_dash,
    "grubberfly_elegance": scene_grubberfly_elegance,
    "soul_bolt": scene_soul_bolt,
    "flukenest": scene_flukenest,
    "spore_breath": scene_spore_breath,
    "pain_brand": scene_pain_brand,
    "wisp_afterimage": scene_wisp_afterimage,
    "defender_crest": scene_defender_crest,
    "second_wind": scene_second_wind,
    "soul_flow": scene_soul_flow,
    "web_spin": scene_web_spin,
    "ricochet_nail": scene_ricochet_nail,
    "nail_envenom": scene_nail_envenom,
    "void_surge": scene_void_surge,
    "soul_rush": scene_soul_rush,
    "wraith_form": scene_wraith_form,
    "soul_harvest": scene_soul_harvest,
    "nail_storm": scene_nail_storm,
    "impregnable": scene_impregnable,
    "soul_aegis": scene_soul_aegis,
    "hallownests_echo": scene_hallownests_echo,
    "void_feast": scene_void_feast,
    "soul_pact": scene_soul_pact,
    "soul_echo": scene_soul_echo,
    "refinement": scene_refinement,
    "soul_battery": scene_soul_battery,
    "venom_blade": scene_venom_blade,
    "catalyst": scene_catalyst,
    "bone_break": scene_bone_break,
    "soul_siphon": scene_soul_siphon,
    "twin_slash": scene_twin_slash,
    "residual_soul": scene_residual_soul,
}




# ---------------------------------------------------------------- power icons
def icon_bg(cv, glow_color=SOUL_SOFT):
    cv.radial_bg((0.07, 0.085, 0.125), (0.03, 0.035, 0.06))
    cv.glow(128, 128, 110, glow_color, 0.35)


def icon_unbreakable(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_nail, loop=BRASS)


def icon_steady(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_stance, loop=(0.45, 0.42, 0.38))


def icon_fragile(cv):
    icon_bg(cv, glow_color=(1.0, 0.6, 0.35))
    charm_hk(cv, 128, 132, 72, base=BONE_GREEN, symbol_fn=sym_nail, cracked=True,
             loop=(0.35, 0.40, 0.28), glow_color=(1.0, 0.62, 0.38))


def icon_thorns(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_thorns, loop=(0.45, 0.42, 0.38))


def icon_shaman(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE_GREY, symbol_fn=sym_spiral, loop=(0.42, 0.40, 0.36))


def icon_coiled(cv):
    icon_bg(cv)
    nail_weapon(cv, 128, 124, 150, 90, color=BONE, glow=False)
    for i, (rr, a0, wd) in enumerate([(62, 200, 6), (50, 170, 5)]):
        cv.paint(arc(cv.x, cv.y, 128, 124, rr, a0, a0 + 235) - wd, (0.72, 0.78, 0.86), 0.85)


def icon_kingsoul(cv):
    icon_bg(cv, glow_color=(1.0, 0.95, 0.75))
    charm_hk(cv, 128, 130, 72, base=(0.965, 0.945, 0.865), symbol_fn=sym_spiral,
             loop=BRASS_L, glow_color=(1.0, 0.95, 0.75))


def icon_baldur(cv):
    icon_bg(cv)
    dome(cv, 128, 130, 130, 84, (0.30, 0.32, 0.36), seg_lines=True)
    cv.paint(circle(cv.x, cv.y, 128, 138, 34), (0.10, 0.105, 0.14))


def icon_shade_cloak(cv):
    icon_bg(cv, glow_color=VIOLET)
    knight(cv, 128, 124, 84)
    for i, ox in enumerate([-52, -96]):
        knight(cv, 128 + ox, 124, 84 - i * 10, body=(0.05, 0.045, 0.09), glow=False)


def sym_fury(cv, cx, cy, r):
    """angry brow slashes over burning eyes"""
    for side in (-1, 1):
        x0, y0 = cx + side * r * 0.42, cy - r * 0.34
        x1, y1 = cx + side * r * 0.06, cy - r * 0.08
        cv.paint(seg(cv.x, cv.y, x0, y0, x1, y1) - r * 0.085, CRIMSON, 0.95)
        cv.paint(circle(cv.x, cv.y, cx + side * r * 0.20, cy + r * 0.06, r * 0.09), CRIMSON, 0.9)


def icon_fury(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_fury, loop=BRASS,
             glow_color=(1.0, 0.55, 0.42))


def sym_weaver(cv, cx, cy, r):
    """a little weaver hanging from her thread"""
    cv.paint(seg(cv.x, cv.y, cx, cy - r * 0.92, cx, cy - r * 0.10) - r * 0.020,
             (0.30, 0.27, 0.44), 0.9)
    cv.paint(circle(cv.x, cv.y, cx, cy + r * 0.22, r * 0.22), (0.18, 0.15, 0.32))
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 0.04, r * 0.14), (0.26, 0.22, 0.44))
    for side in (-1, 1):
        for i in range(4):
            x0, y0 = cx + side * r * 0.10, cy - r * 0.02 + i * r * 0.11
            x1, y1 = cx + side * r * 0.55, cy + r * (i * 0.07 - 0.16)
            cv.paint(seg(cv.x, cv.y, x0, y0, x1, y1) - r * 0.018, (0.26, 0.22, 0.44), 0.9)


def icon_weaversong(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_weaver, loop=BRASS,
             glow_color=(0.78, 0.68, 1.0))


def sym_whetstone(cv, cx, cy, r):
    """a whetted blade edge over a stone"""
    cv.paint(seg(cv.x, cv.y, cx - r * 0.45, cy + r * 0.28, cx + r * 0.45, cy - r * 0.34)
             - r * 0.075, BONE, 0.95)
    cv.paint(seg(cv.x, cv.y, cx - r * 0.34, cy + r * 0.40, cx + r * 0.42, cy - r * 0.20)
             - r * 0.030, (0.45, 0.42, 0.52), 0.9)
    cv.paint(seg(cv.x, cv.y, cx - r * 0.32, cy + r * 0.52, cx + r * 0.55, cy + r * 0.52)
             - r * 0.055, (0.30, 0.27, 0.38), 0.95)


def icon_whetstone(cv):
    icon_bg(cv)
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_whetstone, loop=BRASS,
             glow_color=(0.80, 0.72, 0.95))


def sym_lifeblood(cv, cx, cy, r):
    """a pulsing lifeblood cocoon, red with pale seams"""
    RED = (0.78, 0.20, 0.24)
    RED_DK = (0.42, 0.09, 0.13)
    for k in (-1, 0, 1):
        f = 0.58 + 0.20 * (k + 1)
        cv.paint(np.abs(ellipse(cv.x, cv.y, cx, cy, r * 0.42 * f, r * 0.60 * f)) - r * 0.035,
                 RED_DK, 0.9)
    cv.paint(ellipse(cv.x, cv.y, cx, cy, r * 0.34, r * 0.50), RED)
    cv.paint(circle(cv.x, cv.y, cx - r * 0.12, cy - r * 0.22, r * 0.09), (1.0, 0.93, 0.90), 0.85)


def icon_lifeblood(cv):
    icon_bg(cv, glow_color=(0.95, 0.45, 0.45))
    charm_hk(cv, 128, 130, 72, base=(0.93, 0.87, 0.85), symbol_fn=sym_lifeblood,
             loop=BRASS, glow_color=(0.95, 0.45, 0.45))


def sym_shadecloak(cv, cx, cy, r):
    """a cloak of shadow with a violet rim"""
    VOID = (0.05, 0.045, 0.085)
    VIOLET = (0.62, 0.40, 0.85)
    pts = [(cx - r * 0.44, cy + r * 0.55), (cx - r * 0.20, cy - r * 0.38),
           (cx + r * 0.20, cy - r * 0.38), (cx + r * 0.44, cy + r * 0.55)]
    cv.paint(convex(cv.x, cv.y, pts), VOID, 0.96)
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 0.40, r * 0.17), VOID, 0.96)
    cv.paint(seg(cv.x, cv.y, cx - r * 0.44, cy + r * 0.55, cx - r * 0.20, cy - r * 0.38)
             - r * 0.030, VIOLET, 0.9)
    cv.paint(seg(cv.x, cv.y, cx + r * 0.44, cy + r * 0.55, cx + r * 0.20, cy - r * 0.38)
             - r * 0.030, VIOLET, 0.9)


def icon_shadecloak(cv):
    icon_bg(cv, glow_color=(0.55, 0.35, 0.80))
    charm_hk(cv, 128, 130, 72, base=BONE, symbol_fn=sym_shadecloak,
             loop=BRASS, glow_color=(0.55, 0.35, 0.80))



def _medallion(cv, r, tint):
    """shared charm disc used by the new power icons"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.95), (0.10, 0.12, 0.18))
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.80), tint)
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.55), (0.95, 0.95, 0.98))


def icon_pride(cv):
    """pride's mark: a tall crown above the medallion"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (1.0, 0.85, 0.45), 0.5)
    _medallion(cv, 96, (0.75, 0.60, 0.20))
    cx = cv.w / 2
    for i, (dx, hh) in enumerate([(-40, 55), (0, 75), (40, 55)]):
        cv.paint(convex(cv.x, cv.y, [
            (cx + dx - 14, 96), (cx + dx + 14, 96),
            (cx + dx + 6, 96 - hh), (cx + dx - 6, 96 - hh)]), (1.0, 0.92, 0.55))


def icon_charged(cv):
    """charged arts: a blade with three stacked charge pips"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.5, 0.75, 1.0), 0.5)
    _medallion(cv, 96, (0.20, 0.32, 0.52))
    cv.paint(seg(cv.x, cv.y, cv.w / 2 - 34, cv.h / 2 + 30,
                 cv.w / 2 + 30, cv.h / 2 - 34) - 9, (0.85, 0.92, 1.0))
    for i, y in enumerate((52, 76, 100)):
        cv.paint(circle(cv.x, cv.y, 84 - i * 6, y, 9), (0.55, 0.8, 1.0))


def icon_tyrant(cv):
    """soul tyrant: a crown over a void orb"""
    cv.glow(cv.w / 2, cv.h / 2, 115, (0.6, 0.4, 0.95), 0.6)
    _medallion(cv, 96, (0.24, 0.14, 0.40))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2, 40), (0.55, 0.35, 0.9))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, cv.h / 2, 22), (0.9, 0.8, 1.0))
    for dx in (-38, 0, 38):
        cv.paint(convex(cv.x, cv.y, [(cv.w / 2 + dx - 12, 78),
                                     (cv.w / 2 + dx, 52),
                                     (cv.w / 2 + dx + 12, 78)]), (0.9, 0.8, 1.0))


def icon_melody(cv):
    """carefree melody: a music note over a warm shell"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (1.0, 0.75, 0.55), 0.55)
    _medallion(cv, 96, (0.42, 0.26, 0.18))
    cv.paint(seg(cv.x, cv.y, cv.w / 2 + 18, 52, cv.w / 2 + 18, 140) - 7,
             (1.0, 0.88, 0.66))
    cv.paint(ellipse(cv.x, cv.y, cv.w / 2 + 4, 142, 22, 15), (1.0, 0.88, 0.66))
    cv.paint(ellipse(cv.x, cv.y, cv.w / 2 + 34, 58, 12, 9), (1.0, 0.88, 0.66))


def icon_gmelf(cv):
    """grubsong's melody: a small grub coiled on the medallion"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (1.0, 0.62, 0.35), 0.5)
    _medallion(cv, 96, (0.30, 0.20, 0.12))
    pts = [(cv.w / 2 - 44 + i * 15, cv.h / 2 + int(28 * math.sin(i * 0.9)))
           for i in range(7)]
    for i in range(6):
        cv.paint(circle(cv.x, cv.y, pts[i + 1][0], pts[i + 1][1], 13 - i),
                 (1.0, 0.72, 0.42))
    cv.paint(circle(cv.x, cv.y, pts[6][0] + 8, pts[6][1] - 10, 6), (0.1, 0.1, 0.1))


def icon_hits(cv):
    """hit counter: four tally strokes"""
    cv.glow(cv.w / 2, cv.h / 2, 105, (0.65, 0.85, 1.0), 0.4)
    _medallion(cv, 96, (0.16, 0.22, 0.30))
    for i in range(4):
        x = cv.w / 2 - 30 + i * 20
        cv.paint(seg(cv.x, cv.y, x, cv.h / 2 + 26, x + 8, cv.h / 2 - 26) - 6,
                 (0.75, 0.88, 1.0))
    cv.paint(seg(cv.x, cv.y, cv.w / 2 - 34, 58, cv.w / 2 + 30, 122) - 5,
             (1.0, 0.55, 0.4))


def icon_shrine(cv):
    """shrine blessing: a small shrine with a glowing gem"""
    cv.glow(cv.w / 2, cv.h / 2, 110, (0.5, 0.9, 0.8), 0.5)
    _medallion(cv, 96, (0.14, 0.30, 0.28))
    cv.paint(convex(cv.x, cv.y, [(cv.w / 2 - 44, 118), (cv.w / 2, 58),
                                 (cv.w / 2 + 44, 118)]), (0.85, 0.95, 0.9))
    cv.paint(circle(cv.x, cv.y, cv.w / 2, 112, 14), (0.4, 1.0, 0.85))




POWER_ICONS = {
    "unbreakable_strength_power": icon_unbreakable,
    "steady_body_power": icon_steady,
    "fragile_strength_power": icon_fragile,
    "thorns_of_agony_power": icon_thorns,
    "shaman_stone_power": icon_shaman,
    "coiled_nail_power": icon_coiled,
    "kingsoul_power": icon_kingsoul,
    "baldur_shell_power": icon_baldur,
    "fury_of_the_fallen_power": icon_fury,
    "weaversong_power": icon_weaversong,
    "whetstone_power": icon_whetstone,
    "lifeblood_cocoon_power": icon_lifeblood,
    "prides_mark_power": icon_pride,
    "charged_arts_power": icon_charged,
    "soul_tyrant_power": icon_tyrant,
    "carefree_melody_power": icon_melody,
    "grubsongs_melody_power": icon_gmelf,
    "hit_counter_power": icon_hits,
    "shrine_blessing_power": icon_shrine,
    "spellcharge_power": icon_spellcharge,
    "soulbell_power": icon_soulbell,
    "full_soul_power": icon_fullsoul,
    "shell_echo_power": icon_shellecho,
    "dreamshield_power": icon_dreamshield,
    "grimmchild_power": icon_grimmchild,
    "grubberfly_elegance_power": icon_grubberfly,
    "pain_brand_power": icon_pain_brand,
    "wisp_afterimage_power": icon_wisp_afterimage,
    "defender_crest_power": icon_defender_crest,
    "nail_envenom_power": icon_nail_envenom,
    "void_surge_power": icon_void_surge,
    "soul_echo_power": icon_soul_echo,
    "soul_battery_power": icon_soul_battery,
}


# ---------------------------------------------------------------- relics
def soul_eater(cv, r):
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.20) - r * 0.07, (0.52, 0.48, 0.54))
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.08), (0.55, 0.51, 0.57))
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(np.abs(dist - r * 0.97) - r * 0.09, (0.52, 0.48, 0.54))
    d = dist / r
    t = np.clip(d, 0, 1)[..., None]
    col = (np.asarray((0.06, 0.03, 0.09), np.float32) * (1 - t)
           + np.asarray((0.14, 0.05, 0.17), np.float32) * t)
    cv.paint_mask(aa(circle(cv.x, cv.y, cx, cy, r * 0.90)), col)
    for i in range(3):
        a = math.radians(120 * i + 40)
        cv.paint(circle(cv.x, cv.y, cx + math.cos(a) * r * 0.42, cy + math.sin(a) * r * 0.42,
                        r * (0.16 - i * 0.03)), CRIMSON, 0.85 - i * 0.15)
    cv.paint(circle(cv.x, cv.y, cx + r * 0.08, cy - r * 0.06, r * 0.13), (1.0, 0.78, 0.72), 0.9)
    cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.97, 190, 320) - r * 0.05, (0.70, 0.65, 0.70), 0.8)


def sharpened_nail(cv, r):
    """the nail laid on a whetstone, edge keen and gleaming"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(convex(cv.x, cv.y, [(cx - r * 0.95, cy + r * 0.55), (cx + r * 0.95, cy + r * 0.55),
                                 (cx + r * 0.75, cy + r * 0.95), (cx - r * 0.75, cy + r * 0.95)]),
             (0.20, 0.22, 0.27))
    nail_weapon(cv, cx, cy - r * 0.18, r * 1.85, -28, color=BONE, glow=False)
    glint(cv, cx + r * 0.42, cy - r * 0.52, r * 0.16, WISP, 0.95)
    glint(cv, cx - r * 0.30, cy + r * 0.05, r * 0.10, WISP, 0.8)


def soul_vessel(cv, r):
    """a distilled vessel, brimming with gathered SOUL"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(seg(cv.x, cv.y, cx, cy - r * 1.30, cx, cy - r * 0.62) - r * 0.13, (0.36, 0.34, 0.40))
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.34, r * 0.15), (0.42, 0.40, 0.46))
    cv.glow(cx, cy, r * 1.9, SOUL, 0.5)
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(circle(cv.x, cv.y, cx, cy, r), (0.10, 0.10, 0.16))
    # glowing soul pool filling the lower vessel
    pool = np.maximum(circle(cv.x, cv.y, cx, cy + r * 0.32, r * 0.86), cv.y - (cy + r * 0.10))
    cv.paint_mask(aa(pool) * aa(circle(cv.x, cv.y, cx, cy, r * 0.90)), SOUL)
    cv.paint(np.abs(dist - r * 0.97) - r * 0.09, (0.44, 0.42, 0.50))
    cv.paint(circle(cv.x, cv.y, cx - r * 0.30, cy - r * 0.30, r * 0.10), WISP, 0.9)


def dream_totem(cv, r):
    """a small totem with serene, dreaming eyes"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.7, VIOLET, 0.4)
    cv.paint(convex(cv.x, cv.y, [(cx - r * 0.62, cy + r * 0.95), (cx + r * 0.62, cy + r * 0.95),
                                 (cx + r * 0.48, cy - r * 0.55), (cx - r * 0.48, cy - r * 0.55)]),
             (0.05, 0.045, 0.075))
    cv.paint(convex(cv.x, cv.y, [(cx - r * 0.57, cy - r * 0.48), (cx + r * 0.57, cy - r * 0.48),
                                 (cx + r * 0.52, cy - r * 0.78), (cx - r * 0.52, cy - r * 0.78)]),
             (0.065, 0.058, 0.095))
    for side in (-1, 1):  # closed dreaming eyes
        cv.paint(arc(cv.x, cv.y, cx + side * r * 0.24, cy - r * 0.05, r * 0.15, 200, 340) - r * 0.045,
                 (0.86, 0.80, 1.0), 0.95)
    cv.paint(circle(cv.x, cv.y, cx, cy + r * 0.34, r * 0.06), (0.86, 0.80, 1.0), 0.9)


def spell_twister(cv, r):
    """a twisted charm: swirling violet glass"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.20) - r * 0.07, (0.45, 0.40, 0.52))
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.08), (0.48, 0.43, 0.55))
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(np.abs(dist - r * 0.97) - r * 0.09, (0.52, 0.45, 0.62))
    d = dist / r
    t = np.clip(d, 0, 1)[..., None]
    col = (np.asarray((0.10, 0.06, 0.16), np.float32) * (1 - t)
           + np.asarray((0.30, 0.18, 0.44), np.float32) * t)
    cv.paint_mask(aa(circle(cv.x, cv.y, cx, cy, r * 0.90)), col)
    cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.44, 90, 390) - r * 0.10, VIOLET, 0.95)
    cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.24, 270, 530) - r * 0.085, (0.86, 0.78, 1.0), 0.9)
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.055), WISP, 0.95)


def nailmasters_glory(cv, r):
    """a gold-honed nail crest of the three masters"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.20) - r * 0.07, BRASS)
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.08), BRASS_L)
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(np.abs(dist - r * 0.97) - r * 0.09, BRASS)
    d = dist / r
    t = np.clip(d, 0, 1)[..., None]
    col = (np.asarray((0.10, 0.09, 0.07), np.float32) * (1 - t)
           + np.asarray((0.24, 0.20, 0.12), np.float32) * t)
    cv.paint_mask(aa(circle(cv.x, cv.y, cx, cy, r * 0.90)), col)
    nail_weapon(cv, cx, cy - r * 0.05, r * 1.15, 90, color=BONE, glow=False)
    for side in (-1, 1):  # master's wing accents
        cv.paint(arc(cv.x, cv.y, cx, cy, r * 0.55, 205 if side < 0 else 295, 265 if side < 0 else 355) - r * 0.055,
                 BRASS_L, 0.85)


def sharp_shadow(cv, r):
    """a cloak clasp with a slit of the Abyss"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.20) - r * 0.07, (0.30, 0.28, 0.36))
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 1.28, r * 0.08), (0.33, 0.31, 0.39))
    dist = np.hypot(cv.x - cx, cv.y - cy)
    cv.paint(np.abs(dist - r * 0.97) - r * 0.09, (0.36, 0.34, 0.44))
    d = dist / r
    t = np.clip(d, 0, 1)[..., None]
    col = (np.asarray((0.03, 0.028, 0.05), np.float32) * (1 - t)
           + np.asarray((0.08, 0.07, 0.13), np.float32) * t)
    cv.paint_mask(aa(circle(cv.x, cv.y, cx, cy, r * 0.90)), col)
    cv.glow(cx, cy, r * 0.9, VIOLET, 0.35)
    cv.paint(ellipse(cv.x, cv.y, cx, cy, r * 0.12, r * 0.42), WISP, 0.95)
    cv.paint(circle(cv.x, cv.y, cx + r * 0.05, cy - r * 0.08, r * 0.05), (0.1, 0.06, 0.2), 0.9)


def grubsong(cv, r):
    """a little grub, humming a warm song"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.8, (1.0, 0.92, 0.70), 0.5)
    for i in range(4):  # segmented body
        seg_r = r * (0.50 - i * 0.07)
        cv.paint(circle(cv.x, cv.y, cx - i * r * 0.34, cy + r * 0.05 * i, seg_r),
                 (0.88, 0.82, 0.66) if i % 2 == 0 else (0.78, 0.71, 0.55))
    head = (cx + r * 0.44, cy)
    cv.paint(circle(cv.x, cv.y, head[0] + r * 0.06, head[1], r * 0.34), (0.93, 0.88, 0.74))
    for side in (-1, 1):  # sleepy eyes
        cv.paint(arc(cv.x, cv.y, head[0] + r * 0.06 + side * r * 0.12, head[1] - r * 0.04, r * 0.09, 200, 340)
                 - r * 0.028, (0.25, 0.20, 0.14), 0.95)
    cv.paint(ellipse(cv.x, cv.y, head[0] + r * 0.06, head[1] + r * 0.12, r * 0.045, r * 0.030),
             (0.25, 0.20, 0.14), 0.9)


def void_heart(cv, r):
    """the black heart of the Void, rimmed in pale light"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 2.0, VIOLET, 0.5)
    lobe_l = circle(cv.x, cv.y, cx - r * 0.30, cy - r * 0.28, r * 0.44)
    lobe_r = circle(cv.x, cv.y, cx + r * 0.30, cy - r * 0.28, r * 0.44)
    tri = convex(cv.x, cv.y, [(cx - r * 0.62, cy - r * 0.18),
                              (cx + r * 0.62, cy - r * 0.18),
                              (cx, cy + r * 0.80)])
    heart = np.minimum(np.minimum(lobe_l, lobe_r), tri)
    cv.paint(np.abs(heart) - r * 0.05, BONE, 0.95)
    cv.paint(heart, (0.030, 0.028, 0.055))
    cv.paint(circle(cv.x, cv.y, cx - r * 0.12, cy - r * 0.20, r * 0.05), WISP, 0.9)


def awakened_dream(cv, r):
    """a sleeping eye, opening; dream-wisps drift free"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.7, SOUL, 0.28)
    cv.paint(circle(cv.x, cv.y, cx, cy, r), (0.11, 0.11, 0.18))
    cv.paint(circle(cv.x, cv.y, cx, cy, r) - circle(cv.x, cv.y, cx, cy, r * 0.92),
             (0.44, 0.42, 0.50))
    cv.paint(arc(cv.x, cv.y, cx, cy + r * 0.12, r * 0.66, 195, 345) - r * 0.040,
             (0.78, 0.75, 0.68), 0.95)
    cv.paint(circle(cv.x, cv.y, cx, cy - r * 0.02, r * 0.30), SOUL_SOFT)
    dist_iris = np.hypot(cv.x - cx, cv.y - (cy - r * 0.02))
    cv.paint(np.abs(dist_iris - r * 0.25) - r * 0.055, SOUL)
    cv.paint(circle(cv.x, cv.y, cx - r * 0.09, cy - r * 0.12, r * 0.08), (1.0, 1.0, 1.0), 0.95)
    for a0 in (150, 195, 240):
        a = math.radians(a0)
        wx, wy = cx + math.cos(a) * r * 0.80, cy - r * 0.50 + math.sin(a) * r * 0.26
        cv.paint(circle(cv.x, cv.y, wx, wy, r * 0.05), WISP, 0.9)




def sprintmasters_gift(cv, r):
    """a wind-swept cloak pin: three motion arcs over a pale brooch"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.8, (0.75, 0.92, 1.0), 0.5)
    # brooch body: a cloak clasp
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.78), (0.16, 0.20, 0.30))
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.62), (0.72, 0.84, 0.96))
    cv.paint(circle(cv.x, cv.y, cx, cy, r * 0.48), (0.36, 0.52, 0.70))
    # three dash arcs crossing the brooch (sprint trails)
    for i, (rr, a0) in enumerate([(0.95, 150), (0.72, 165), (1.18, 140)]):
        wdt = r * (0.075 - i * 0.015)
        cv.paint(arc(cv.x, cv.y, cx, cy, r * rr, a0, a0 + 60) - wdt * 0.5, WISP, 0.85 - i * 0.18)
    # the pin: a tiny nail silhouette through the clasp
    cv.paint(seg(cv.x, cv.y, cx - r * 0.30, cy + r * 0.34, cx + r * 0.30, cy - r * 0.34)
             - r * 0.045, BONE_DIM)


def hiveblood(cv, r):
    """a hex-comb charm dripping amber blood"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.8, (1.0, 0.78, 0.38), 0.55)
    # three hex cells
    def hexagon(px, py, pr, rot=0.5):
        pts = []
        for k in range(6):
            a = rot + k * np.pi / 3
            pts.append((px + np.cos(a) * pr, py + np.sin(a) * pr))
        return convex(cv.x, cv.y, pts)
    amber = (0.92, 0.62, 0.22)
    amber_lit = (1.0, 0.82, 0.45)
    dark = (0.24, 0.15, 0.08)
    # cell 1: top-left, full
    cv.paint(hexagon(cx - r * 0.52, cy - r * 0.34, r * 0.46) - r * 0.055, dark)
    cv.paint(hexagon(cx - r * 0.52, cy - r * 0.34, r * 0.40) - r * 0.045, amber_lit)
    # cell 2: top-right, full
    cv.paint(hexagon(cx + r * 0.50, cy - r * 0.30, r * 0.44) - r * 0.055, dark)
    cv.paint(hexagon(cx + r * 0.50, cy - r * 0.30, r * 0.38) - r * 0.045, amber)
    # cell 3: bottom-center, half-drained with a drip
    cv.paint(hexagon(cx, cy + r * 0.42, r * 0.52) - r * 0.055, dark)
    pool = np.maximum(hexagon(cx, cy + r * 0.42, r * 0.46) - r * 0.045,
                      cv.y - (cy + r * 0.30))
    cv.paint_mask(aa(pool), amber_lit)
    cv.paint(ellipse(cv.x, cv.y, cx + r * 0.10, cy + r * 0.92, r * 0.06, r * 0.11), amber_lit)
    cv.paint(ellipse(cv.x, cv.y, cx - r * 0.16, cy + r * 1.06, r * 0.05, r * 0.09), amber)


def lifeblood_core(cv, r):
    """a beating blue heart in a dark vessel shell"""
    cx, cy = cv.w / 2, cv.h / 2
    cv.glow(cx, cy, r * 1.9, (0.35, 0.62, 1.0), 0.6)
    # outer shell: broken vessel silhouette (two shell lobes)
    cv.paint(circle(cv.x, cv.y, cx - r * 0.52, cy - r * 0.18, r * 0.44), (0.14, 0.16, 0.24))
    cv.paint(circle(cv.x, cv.y, cx + r * 0.52, cy - r * 0.18, r * 0.44), (0.14, 0.16, 0.24))
    cv.paint(circle(cv.x, cv.y, cx - r * 0.52, cy - r * 0.18, r * 0.34), (0.24, 0.30, 0.42))
    cv.paint(circle(cv.x, cv.y, cx + r * 0.52, cy - r * 0.18, r * 0.34), (0.24, 0.30, 0.42))
    # the core: pulsing blue heart with a bright valve
    heart = (np.hypot((cv.x - cx) / (r * 0.62), ((cv.y - cy) / (r * 0.55)))
             - 1.0 + 0.18 * np.cos(np.arctan2(cv.y - cy, cv.x - cx) * 2.0))
    cv.paint(heart, (0.18, 0.42, 0.86))
    core = np.hypot(cv.x - cx, cv.y - cy) - r * 0.30
    cv.paint(core, (0.55, 0.76, 1.0))
    valve = circle(cv.x, cv.y, cx, cy - r * 0.30, r * 0.10)
    cv.paint(valve, (0.85, 0.93, 1.0), 0.95)
    # lifeblood drips
    cv.paint(ellipse(cv.x, cv.y, cx - r * 0.14, cy + r * 0.86, r * 0.06, r * 0.12),
             (0.45, 0.70, 1.0))
    cv.paint(ellipse(cv.x, cv.y, cx + r * 0.18, cy + r * 1.02, r * 0.05, r * 0.09),
             (0.45, 0.70, 1.0), 0.85)



RELICS = {
    "sprintmasters_gift": sprintmasters_gift,
    "hiveblood": hiveblood,
    "lifeblood_core": lifeblood_core,
    "grubsong_eater": relic_grubsong_eater,
    "soul_eater": soul_eater,
    "sharpened_nail": sharpened_nail,
    "soul_vessel": soul_vessel,
    "dream_totem": dream_totem,
    "spell_twister": spell_twister,
    "nailmasters_glory": nailmasters_glory,
    "sharp_shadow": sharp_shadow,
    "grubsong": grubsong,
    "void_heart": void_heart,
    "awakened_dream": awakened_dream,
    "gathering_swarm": relic_gathering_swarm,
    "dreamcatcher": relic_dreamcatcher,
    "unbreakable_heart": relic_unbreakable_heart,
    "quick_focus": relic_quick_focus,
    "salubras_blessing": relic_salubras_blessing,
    "soul_candle": relic_soul_candle,
    "void_brand": relic_void_brand,
    "hallownest_seal": relic_hallownest_seal,
}


# ---------------------------------------------------------------- charui
def energy_orb(cv):
    w = cv.w
    cx = cy = w / 2
    r = w * 0.34
    cv.glow(cx, cy, r * 2.0, SOUL, 0.85, falloff=2.6)
    cv.paint(circle(cv.x, cv.y, cx, cy, r), WISP, 0.95)
    cv.paint(circle(cv.x, cv.y, cx - r * 0.2, cy - r * 0.22, r * 0.45), (1.0, 1.0, 1.0), 0.9)
    cv.paint(np.maximum(circle(cv.x, cv.y, cx, cy, r), -circle(cv.x, cv.y, cx, cy, r * 0.78)),
             SOUL_SOFT, 0.8)


def _paint_soul_orb(cv, glow):
    """User-requested SOUL glyph: the Knight's pale shell as a round mask —
    white orb, two void eyes, fully transparent background. Shared between
    the 96px combat counter and the 24px inline card-text icon."""
    w = cv.w
    cx = cy = w / 2
    r = w * 0.40
    if glow:
        cv.glow(cx, cy, r * 1.85, SOUL, 0.45, falloff=2.6)
    orb = circle(cv.x, cv.y, cx, cy, r)
    cv.paint(orb, (0.97, 0.98, 1.00))
    # soft shading on the lower half so the sphere reads round
    cv.paint(np.maximum(orb, circle(cv.x, cv.y, cx, cy - r * 0.50, r * 1.05)),
             (0.80, 0.84, 0.92), 0.45)
    # two void eyes, vertical ellipses just below the midline
    for sx in (-1, 1):
        cv.paint(ellipse(cv.x, cv.y, cx + sx * r * 0.42, cy + r * 0.12,
                         r * 0.17, r * 0.30), (0.015, 0.015, 0.03))


def soul_counter_orb(cv):
    _paint_soul_orb(cv, glow=True)


def soul_text_icon(cv):
    _paint_soul_orb(cv, glow=False)


CHARUI = {
    "soul_orb": (soul_counter_orb, 96),
    # inline card-text glyph: rendered at native size by [img]; card
    # description text is ~20px, so sit just above it (16px was too small,
    # 96px was enormous)
    "soul_icon_text": (soul_text_icon, 24),
}


def knight_figure(cv, cx, cy, s, body=DARK, rim=SOUL_SOFT, nail=True, glow=True):
    """the Little Knight standing: cloak with pale rim light, nail at the side"""
    if glow:
        cv.glow(cx, cy, s * 1.35, SOUL_SOFT, 0.30)
    if nail:
        nail_weapon(cv, cx - s * 0.46, cy - s * 0.10, s * 1.30, 62, color=(0.32, 0.34, 0.40), glow=False)
    pts = [(cx - s * 0.40, cy + s * 0.62), (cx - s * 0.22, cy - s * 0.16),
           (cx + s * 0.22, cy - s * 0.16), (cx + s * 0.40, cy + s * 0.62)]
    cv.paint(convex(cv.x, cv.y, pts), body)
    cv.paint(ellipse(cv.x, cv.y, cx, cy + s * 0.64, s * 0.37, s * 0.06), body)
    cv.paint(seg(cv.x, cv.y, cx - s * 0.38, cy + s * 0.58, cx - s * 0.22, cy - s * 0.14)
             - s * 0.014, rim, 0.75)
    cv.paint(seg(cv.x, cv.y, cx + s * 0.38, cy + s * 0.58, cx + s * 0.22, cy - s * 0.14)
             - s * 0.014, rim, 0.75)
    cv.paint(ellipse(cv.x, cv.y, cx - s * 0.14, cy + s * 0.70, s * 0.10, s * 0.05), (0.03, 0.032, 0.05))
    cv.paint(ellipse(cv.x, cv.y, cx + s * 0.14, cy + s * 0.70, s * 0.10, s * 0.05), (0.03, 0.032, 0.05))
    hk_mask(cv, cx, cy - s * 0.34, s * 0.46, glow=glow)


def render_knight_combat():
    # figure spans ~1.605*s tall and ~1.53*s wide (nail included) - crop tight,
    # feet at the bottom edge (the factory anchors the sprite bottom at the origin)
    s = 290
    w, h = 480, 480
    cv = Cv(w, h, ss=4)
    knight_figure(cv, w // 2, int(0.855 * s) + 6, s)
    cv.save(f"{IMG}/charui/knight_combat.png", w, h)
    return 1


def render_knight_hero():
    cv = Cv(1024, 1152, ss=2)
    cave_bg(cv, top=(0.050, 0.060, 0.100), bot=VOID_BOT, seed=71)
    arch(cv, 512, 1180, 980, 70)
    cv.glow(512, 620, 460, SOUL_SOFT, 0.30)
    knight_figure(cv, 512, 620, 560)
    cv.particles(34, 72)
    cv.vignette(0.5)
    cv.save(f"{IMG}/charui/knight_hero.png", 1024, 1152)
    return 1


def render_workshop_banner(out_path):
    """1920x1080 Steam Workshop preview: knight right, SOUL orb + glow left"""
    cv = Cv(1920, 1080, ss=1)
    cave_bg(cv, top=(0.048, 0.058, 0.098), bot=VOID_BOT, seed=71, ground=True)
    arch(cv, 620, 1150, 900, 64)
    cv.glow(560, 430, 330, SOUL, 0.75, falloff=2.4)
    cv.paint(circle(cv.x, cv.y, 560, 430, 96), WISP, 0.97)
    cv.paint(circle(cv.x, cv.y, 505, 378, 42), (1.0, 1.0, 1.0), 0.92)
    for i in range(6):
        a = math.radians(40 + i * 60)
        rr = 190 - (i % 2) * 40
        cv.paint(circle(cv.x, cv.y, 560 + math.cos(a) * rr, 430 + math.sin(a) * rr * 0.55,
                        15 - (i % 3) * 3), WISP, 0.85 - (i % 3) * 0.15)
    knight_figure(cv, 1300, 560, 640)
    cv.particles(46, 73)
    cv.vignette(0.5)
    rgb = np.clip(cv.rgb * 255 + 0.5, 0, 255).astype(np.uint8)
    img = Image.fromarray(rgb, "RGB").convert("RGB")
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path, quality=92)
    return 1


def render_icon_outline():
    cv = Cv(128, 128, ss=4)
    cv.radial_bg((0.012, 0.014, 0.022), (0.008, 0.009, 0.014))
    hk_mask(cv, 64, 68, 34, pale=(0.05, 0.055, 0.085), glow=False)
    cv.save(f"{IMG}/charui/character_icon_char_name_outline.png", 128, 128)
    return 1


# ---------------------------------------------------------------- main
def main():
    import sys
    want = set(sys.argv[1:]) or None
    made = 0

    for cid, fn in CARD_SCENES.items():
        if want is not None and cid not in want:
            continue
        cv = Cv(CARD_W, CARD_H, ss=3)
        fn(cv)
        cv.save(f"{IMG}/card_portraits/big/{cid}.png", CARD_W, CARD_H)
        cv.save(f"{IMG}/card_portraits/{cid}.png", 250, 190)
        made += 2
        print("card  ", cid)

    for pid, fn in POWER_ICONS.items():
        if want is not None and pid not in want:
            continue
        cv = Cv(256, 256, ss=3)
        fn(cv)
        cv.save(f"{IMG}/powers/big/{pid}.png", 256, 256)
        cv.save(f"{IMG}/powers/{pid}.png", 64, 64)
        made += 2
        print("power ", pid)

    for rid, fn in RELICS.items():
        if want is not None and rid not in want:
            continue
        cv = Cv(256, 256, ss=4)
        fn(cv, 86)
        cv.save(f"{IMG}/relics/big/{rid}.png", 256, 256)
        cv.save(f"{IMG}/relics/{rid}.png", 94, 94)
        alpha = (np.clip(cv.a[..., 0], 0, 1) * 255).astype(np.uint8)
        sil_img = Image.fromarray(np.dstack([
            np.full_like(alpha, 255), np.full_like(alpha, 255),
            np.full_like(alpha, 255), alpha]), "RGBA").resize((94, 94), Image.LANCZOS)
        sil_img.save(f"{IMG}/relics/{rid}_outline.png")
        made += 3
        print("relic ", rid)

    for uid, (fn, size) in CHARUI.items():
        if want is not None and uid not in want:
            continue
        cv = Cv(size, size, ss=4)
        fn(cv)
        cv.save(f"{IMG}/charui/{uid}.png", size, size)
        made += 1
        print("charui", uid, size)

    # All character art (combat/select/hero/icons/merchant/rest/mod_image/
    # workshop banner) belongs to tools/process_wiki_art.py — gen_art used to
    # draw its own stylized versions here and whichever script ran last
    # silently overwrote the other. Only card faces / power & relic icons /
    # energy orbs / soul counter icons are generated from here now.

    cv = Cv(74, 74, ss=4)
    energy_orb(cv)
    cv.save(f"{IMG}/charui/big_energy.png", 74, 74)
    cv = Cv(24, 24, ss=6)
    energy_orb(cv)
    cv.save(f"{IMG}/charui/text_energy.png", 24, 24)
    made += 2
    print("charui")

    print(f"done: {made} files")


if __name__ == "__main__":
    main()
