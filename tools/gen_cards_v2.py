#!/usr/bin/env python3
"""Card art v2 — redraws all 26 card faces in a unified style built from the
official Team Cherry renders (tools/wiki_art_raw) plus procedural effects.

Replaces the card portraits from gen_art.py (same paths/sizes, deterministic):
  images/card_portraits/big/<card>.png   1000x760
  images/card_portraits/<card>.png        250x190
"""

import math
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(REPO, "tools", "wiki_art_raw")
IMG = os.path.join(REPO, "HollowKnight", "images", "card_portraits")
DEATH = os.path.join(REPO, "HollowKnight", "images", "charui", "death")

W, H = 1000, 760

# ---------------------------------------------------------------- palette
BONE = (233, 238, 248)
PALE = (168, 199, 245)
SOUL = (210, 228, 255)
VOID = (28, 22, 46)
NAVY = (24, 30, 52)
GREEN_TOP = (18, 42, 38)
GREEN_BOT = (6, 16, 16)
ABYSS_TOP = (26, 20, 48)
ABYSS_BOT = (8, 6, 16)
SOUL_TOP = (22, 30, 58)
SOUL_BOT = (6, 8, 18)
CITY_TOP = (38, 48, 72)
CITY_BOT = (10, 13, 26)
CHARM_BG = (46, 38, 30)


# ---------------------------------------------------------------- basics
def render(name):
    im = Image.open(os.path.join(RAW, name)).convert("RGBA")
    return im.crop(im.getbbox())


HKSS = os.path.join(RAW, "hkss")


def sprite(name):
    """game sprite rip from the huijiwiki HK texture dump (tools/wiki_art_raw/hkss)"""
    im = Image.open(os.path.join(HKSS, name)).convert("RGBA")
    return im.crop(im.getbbox())


def death_frame(n):
    return Image.open(os.path.join(DEATH, f"death_f{n:03d}.png")).convert("RGBA")


def scaled(im, w, h):
    s = min(w / im.width, h / im.height)
    out = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    if s > 2.5:  # small wiki sprites go soft when blown up for card size
        out = out.filter(ImageFilter.UnsharpMask(radius=2, percent=120, threshold=2))
    return out


def vgrad(top, bot, w=W, h=H):
    t = np.linspace(0, 1, h)[:, None, None]
    top = np.array(top, dtype=np.float32) / 255.0
    bot = np.array(bot, dtype=np.float32) / 255.0
    col = top[None, None] * (1 - t) + bot[None, None] * t
    return np.broadcast_to(col, (h, w, 3)).copy()


def noise(rgb, amount, seed):
    rgb[:] += np.random.default_rng(seed).normal(0, amount, rgb.shape[:2])[..., None]


def glow(rgb, cx, cy, rx, ry, color, alpha, falloff=2.0):
    y, x = np.mgrid[0:H, 0:W]
    d = np.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
    a = np.clip(1 - d, 0, 1) ** falloff * alpha
    rgb[:] = rgb * (1 - a[..., None]) + np.array(color)[None, None] * a[..., None]


def vignette(rgb, strength=0.5):
    y, x = np.mgrid[0:H, 0:W]
    d = np.sqrt(((x - W / 2) / (W / 2)) ** 2 + ((y - H / 2) / (H / 2)) ** 2)
    m = (np.clip(d - 0.55, 0, 1) / 0.62) ** 1.5 * strength
    rgb[:] *= (1 - m)[..., None]


def wisps(n, seed, w=W, h=H, r=(2, 8), a=(0.1, 0.45)):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    for _ in range(n):
        cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        rr = rng.uniform(*r)
        aa = rng.uniform(*a)
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / rr
        m = np.clip(1 - d, 0, 1) ** 1.6
        col = np.array([0.85, 0.92, 1.0]) * rng.uniform(0.85, 1.05)
        layer = np.zeros((h, w, 4), dtype=np.float32)
        layer[..., 0], layer[..., 1], layer[..., 2] = col
        layer[..., 3] = m * aa
        img.alpha_composite(Image.fromarray((np.clip(layer * 255, 0, 255)).astype(np.uint8), "RGBA"))
    return img


def ground_shadow(canvas, cx, cy, rx, ry, alpha=0.5):
    sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    pad = int(max(rx, ry) * 3)
    y, x = np.mgrid[-pad:pad + 1, -pad:pad + 1]
    d = np.sqrt((x / rx) ** 2 + (y / ry) ** 2)
    a = (np.clip(1 - d, 0, 1) ** 1.8 * alpha * 255).astype(np.uint8)
    blob = Image.fromarray(np.dstack([np.zeros_like(a)] * 3 + [a]), "RGBA")
    sh.alpha_composite(blob, (int(cx - pad), int(cy - pad)))
    sh = sh.filter(ImageFilter.GaussianBlur(max(2, ry * 0.35)))
    canvas.alpha_composite(sh)


def to_image(rgb):
    return Image.fromarray(np.clip(rgb * 255 + 0.5, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def finish(rgb, wisp_count, seed, vig=0.5):
    img = to_image(rgb)
    img.alpha_composite(wisps(wisp_count, seed))
    vv = np.asarray(img.convert("RGB")).astype(np.float32) / 255.0
    vignette(vv, vig)
    return Image.fromarray(np.clip(vv * 255 + 0.5, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def save(img, card):
    img.save(os.path.join(IMG, "big", f"{card}.png"))
    img.resize((250, 190), Image.LANCZOS).save(os.path.join(IMG, f"{card}.png"))
    print("card", card)


# ---------------------------------------------------------------- motifs
def nail(d, cx, cy, angle_deg, length, base_w, tip_w):
    """the Old Nail: blade + perpendicular crossguard + pommel"""
    a = np.radians(angle_deg)
    dv = np.array([np.cos(a), -np.sin(a)])
    pv = np.array([-dv[1], dv[0]])
    base = np.array([cx, cy]) - dv * (length * 0.42)
    tip = base + dv * length
    for widen, col in [(14, (120, 160, 220, 70)), (7, (30, 36, 58, 255)), (0, BONE + (255,))]:
        pts = [tuple(base + pv * (base_w + widen)), tuple(tip + pv * (tip_w + widen)),
               tuple(tip - pv * (tip_w + widen)), tuple(base - pv * (base_w + widen))]
        d.polygon(pts, fill=col)
    bx, by = base
    for widen, col in [(10, (120, 160, 220, 60)), (5, (30, 36, 58, 255)), (0, (222, 200, 130, 255))]:
        gx, gy = pv * (base_w * 2.0 + widen)
        d.line([bx - gx, by - gy, bx + gx, by + gy], fill=col, width=16 + widen * 2)
        r = 13 + widen
        px, py = bx - dv[0] * 26, by - dv[1] * 26
        d.ellipse([px - r, py - r, px + r, py + r], fill=col)


def slash_arc(img, cx, cy, radius, width, angle_deg, color=SOUL + (230,), arc=110):
    d = ImageDraw.Draw(img)
    box = [cx - radius, cy - radius, cx + radius, cy + radius]
    for w_off, a_mul in [(0, 1.0), (-width // 3, 0.45), (width // 3, 0.25)]:
        r = radius + w_off
        d.arc([cx - r, cy - r, cx + r, cy + r], start=angle_deg - arc / 2,
              end=angle_deg + arc / 2, fill=tuple(list(color[:3]) + [int(color[3] * a_mul)]),
              width=max(2, width - abs(w_off)))


def soul_orb(img, cx, cy, r, core=(250, 252, 255)):
    y, x = np.mgrid[0:H, 0:W]
    rgb = np.asarray(img.convert("RGB")).astype(np.float32) / 255.0
    glow(rgb, cx, cy, r * 2.6, r * 2.6, (0.62, 0.76, 1.0), 0.75, falloff=2.2)
    img.paste(Image.fromarray(np.clip(rgb * 255, 0, 255).astype(np.uint8), "RGB").convert("RGBA"),
              (0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(140, 170, 220, 200))
    d.ellipse([cx - r * 0.86, cy - r * 0.86, cx + r * 0.86, cy + r * 0.86], fill=tuple(core) + (255,))


def charm_disc(img, cx, cy, r, rim=(210, 190, 140), emblem=None):
    d = ImageDraw.Draw(img)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(52, 46, 40, 255))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=rim + (255,), width=10)
    d.ellipse([cx - r * 0.82, cy - r * 0.82, cx + r * 0.82, cy + r * 0.82],
              outline=rim + (160,), width=4)
    d.ellipse([cx - 5, cy - r - 16, cx + 5, cy - r + 2], fill=rim + (255,))  # hanging loop
    if emblem:
        emblem(d, cx, cy, r)


# ---------------------------------------------------------------- scenes
def scene_old_nail():
    rgb = vgrad((34, 40, 52), (12, 13, 22))
    noise(rgb, 0.010, 1)
    glow(rgb, 500, 560, 320, 260, (0.45, 0.52, 0.5), 0.35)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 640, W, H], fill=(16, 17, 24, 255))          # ground
    rng = np.random.default_rng(4)
    for _ in range(26):                                           # grass
        gx, gh = rng.uniform(40, 960), rng.uniform(14, 34)
        d.line([gx, 645, gx + rng.uniform(-8, 8), 645 - gh],
               fill=(90, 130, 90, 220), width=3)
    nail(d, 500, 520, -90, 430, 34, 10)
    ground_shadow(img, 500, 660, 120, 22)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 5)


def scene_shell():
    rgb = vgrad((24, 34, 58), (8, 10, 22))
    noise(rgb, 0.010, 6)
    glow(rgb, 500, 360, 330, 300, (0.45, 0.62, 0.95), 0.5)
    img = to_image(rgb)
    head = scaled(render("The_Knight.png"), 520, 620)
    ground_shadow(img, 500, 690, 210, 30)
    img.alpha_composite(head, (500 - head.width // 2, 700 - head.height))
    d = ImageDraw.Draw(img)
    for r, a in [(330, 90), (280, 140), (230, 200)]:              # shield arcs
        d.arc([500 - r, 380 - r, 500 + r, 380 + r], start=-70, end=70,
              fill=(150, 195, 255, a), width=16)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 7)


def scene_focus():
    rgb = vgrad(SOUL_TOP, SOUL_BOT)
    noise(rgb, 0.010, 8)
    glow(rgb, 500, 430, 380, 340, (0.5, 0.65, 1.0), 0.5)
    img = to_image(rgb)
    rest = scaled(render("The_Knight_Resting.png"), 560, 560)
    ground_shadow(img, 500, 680, 230, 30)
    img.alpha_composite(rest, (500 - rest.width // 2, 675 - rest.height))
    for i, (ox, oy, r) in enumerate([(300, 300, 16), (700, 260, 20), (620, 180, 13),
                                     (360, 190, 12), (500, 120, 17), (240, 420, 10)]):
        soul_orb(img, ox, oy, r)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 22, 9)


def slash_scene(card, seed, pose, slash_specs, wisp_n=16, flip=False):
    rgb = vgrad(GREEN_TOP, GREEN_BOT)
    noise(rgb, 0.011, seed)
    glow(rgb, 520, 400, 360, 320, (0.4, 0.6, 0.45), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 655, W, H], fill=(9, 18, 15, 255))
    art = scaled(render(pose), 620, 620)
    if flip:
        art = art.transpose(Image.FLIP_LEFT_RIGHT)
    ground_shadow(img, 480, 700, 240, 30)
    img.alpha_composite(art, (480 - art.width // 2, 695 - art.height))
    for cx, cy, r, w, ang in slash_specs:
        slash_arc(img, cx, cy, r, w, ang)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, wisp_n, seed)


def scene_quick_slash():
    return slash_scene("quick_slash", 11, "The_Knight_Idle.png", [(500, 330, 300, 14, 20)])


def scene_great_slash():
    return nail_art_scene("great_slash", 12, "Icon_HK_Great_Slash_Art.png", 640, 520)


def scene_dash_slash():
    return nail_art_scene("dash_slash", 13, "Icon_HK_Dash_Slash_Art.png", 820, 330)


def nail_art_scene(card, seed, art_file, art_w, art_h, wisp_n=12):
    """nail-art card face built on the game's official art icon
    (tools/wiki_art_raw/hkss, greenpath gradient like the vanilla arts)"""
    rgb = vgrad(GREEN_TOP, GREEN_BOT)
    noise(rgb, 0.012, seed)
    glow(rgb, 500, 390, 380, 330, (0.45, 0.62, 0.50), 0.5)
    img = to_image(rgb)
    art = scaled(sprite(art_file), art_w, art_h)
    img.alpha_composite(art, (500 - art.width // 2, 390 - art.height // 2))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, wisp_n, seed, vig=0.42)


def spell_art_scene(seed, art_file, art_w, art_h, glow_color, wisp_n=10):
    """spell card face built on the game's official spell art icon:
    abyss gradient + themed glow + centered icon (vengeful_spirit recipe)"""
    rgb = vgrad(ABYSS_TOP, ABYSS_BOT)
    noise(rgb, 0.012, seed)
    glow(rgb, 500, 390, 380, 330, glow_color, 0.5)
    img = to_image(rgb)
    art = scaled(sprite(art_file), art_w, art_h)
    img.alpha_composite(art, (500 - art.width // 2, 390 - art.height // 2))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, wisp_n, seed, vig=0.42)


def scene_cyclone_slash():
    return nail_art_scene("cyclone_slash", 14, "Icon_HK_Cyclone_Slash_Art.png", 780, 330)
    for ang in range(0, 360, 45):
        slash_arc(img, 480, 400, 330, 16, ang, arc=48)
    return img


def scene_channelled_nail():
    rgb = vgrad((30, 36, 50), (10, 11, 20))
    noise(rgb, 0.010, 15)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    for i, (alpha, dx) in enumerate([(70, -170), (140, -85), (255, 0)]):
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        nail(ImageDraw.Draw(layer), 470 + dx, 400, 65, 380, 22, 7)
        if alpha < 255:
            layer = layer.point(lambda p: int(p * alpha / 255) if p else p)
        img.alpha_composite(layer)
    d = ImageDraw.Draw(img)
    for sx in (330, 470, 610):                                    # three hit sparks
        slash_arc(img, sx, 360, 120, 10, 40, arc=70)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 16)


def scene_dash():
    rgb = vgrad((26, 38, 56), (9, 12, 22))
    noise(rgb, 0.010, 17)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    for i in range(12):                                            # speed lines
        y = 180 + i * 34
        d.line([80 + i * 12, y, 620 + i * 22, y], fill=(180, 210, 250, 60), width=5)
    art = scaled(render("Knight_Action.png"), 660, 560)
    art = art.transpose(Image.FLIP_LEFT_RIGHT)
    img.alpha_composite(art, (330, 190))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 18)


def scene_dark_shrine():
    rgb = vgrad((24, 20, 40), (7, 6, 14))
    noise(rgb, 0.012, 19)
    glow(rgb, 500, 330, 300, 300, (0.55, 0.45, 0.9), 0.5)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 660, W, H], fill=(10, 8, 18, 255))
    d.polygon([(380, 660), (440, 300), (560, 300), (620, 660)], fill=(22, 18, 36, 255))  # shrine
    d.polygon([(430, 660), (470, 360), (530, 360), (570, 660)], fill=(30, 25, 48, 255))
    soul_orb(img, 500, 320, 34)
    for k in range(5):                                             # offering candles
        cx = 300 + k * 100
        d.rectangle([cx - 5, 620, cx + 5, 660], fill=(40, 34, 60, 255))
        glow(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, cx, 610, 26, 26,
             (0.9, 0.75, 0.5), 0.5)
        img.paste(to_image(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0), (0, 0))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 20, 20)


def scene_steady_body():
    rgb = vgrad((28, 32, 46), (10, 11, 18))
    noise(rgb, 0.010, 21)
    glow(rgb, 500, 430, 330, 300, (0.5, 0.6, 0.9), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(13, 14, 20, 255))
    art = scaled(render("The_Knight.png"), 500, 560)
    ground_shadow(img, 500, 690, 220, 26)
    img.alpha_composite(art, (500 - art.width // 2, 685 - art.height))
    d.ellipse([290, 640, 710, 720], outline=(150, 190, 240, 160), width=8)  # firm stance ring
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 22)


def charm_scene(card, seed, bg_top, emblem, glow_col=(0.7, 0.62, 0.4), m_bg=CHARM_BG):
    rgb = vgrad(bg_top, tuple(c * 0.4 for c in bg_top))
    noise(rgb, 0.011, seed)
    glow(rgb, 500, 380, 340, 320, glow_col, 0.45)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    charm_disc(img, 500, 390, 250, emblem=emblem)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, seed)


def scene_unbreakable_strength():
    def emblem(d, cx, cy, r):
        nail(d, cx, cy + r * 0.5, 90, r * 1.05, r * 0.085, r * 0.028)
    return charm_scene("unbreakable_strength", 23, (48, 42, 34), emblem)


def scene_fragile_strength():
    def emblem(d, cx, cy, r):
        d.ellipse([cx - r * 0.6, cy - r * 0.6, cx + r * 0.6, cy + r * 0.6],
                  fill=(70, 130, 80, 255))
        d.ellipse([cx - r * 0.6, cy - r * 0.6, cx + r * 0.6, cy + r * 0.6],
                  outline=(180, 230, 170, 255), width=8)
        rng = np.random.default_rng(24)
        for _ in range(7):                                          # cracks
            x0, y0 = cx + rng.uniform(-r * 0.5, r * 0.5), cy + rng.uniform(-r * 0.5, r * 0.5)
            d.line([x0, y0, x0 + rng.uniform(-60, 60), y0 + rng.uniform(-60, 60)],
                   fill=(20, 40, 24, 255), width=6)
    return charm_scene("fragile_strength", 24, (36, 52, 40), emblem, (0.5, 0.8, 0.55))


def scene_shaman_stone():
    rgb = vgrad((52, 40, 56), (18, 14, 24))
    noise(rgb, 0.011, 25)
    glow(rgb, 500, 380, 330, 310, (0.8, 0.6, 0.9), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 670, W, H], fill=(14, 10, 20, 255))
    rng = np.random.default_rng(26)
    for _ in range(7):                                             # mushroom silhouettes
        mx, mh, mw = rng.uniform(60, 940), rng.uniform(60, 160), rng.uniform(40, 90)
        my = 700
        d.rectangle([mx - 6, my - mh, mx + 6, my], fill=(30, 22, 42, 255))
        d.ellipse([mx - mw, my - mh - mw * 0.5, mx + mw, my - mh + mw * 0.4],
                  fill=(58, 44, 78, 255))
    charm_disc(img, 500, 380, 250, emblem=lambda d, cx, cy, r: (
        d.ellipse([cx - r * 0.45, cy - r * 0.45, cx + r * 0.45, cy + r * 0.45],
                  fill=(160, 120, 190, 255)),
        d.ellipse([cx - r * 0.28, cy - r * 0.28, cx + r * 0.28, cy + r * 0.28],
                  fill=(210, 180, 235, 255))))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 27)


def scene_kingsoul():
    def emblem(d, cx, cy, r):
        d.polygon([(cx - r * 0.75, cy), (cx - r * 0.15, cy - r * 0.5),
                   (cx + r * 0.15, cy - r * 0.5), (cx + r * 0.75, cy),
                   (cx + r * 0.15, cy + r * 0.45), (cx - r * 0.15, cy + r * 0.45)],
                  fill=(240, 244, 252, 255))                        # winged kingsoul
        d.ellipse([cx - r * 0.2, cy - r * 0.16, cx + r * 0.2, cy + r * 0.16],
                  fill=(120, 150, 210, 255))
    return charm_scene("kingsoul", 28, (40, 44, 66), emblem, (0.65, 0.72, 1.0))


def scene_baldur_shell():
    rgb = vgrad((24, 34, 52), (8, 11, 20))
    noise(rgb, 0.010, 29)
    glow(rgb, 500, 390, 330, 300, (0.5, 0.65, 0.9), 0.45)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(10, 13, 20, 255))
    d.ellipse([330, 260, 670, 600], fill=(90, 110, 150, 255), outline=(40, 52, 80, 255), width=10)
    d.arc([330, 260, 670, 600], start=200, end=340, fill=(190, 215, 250, 220), width=14)
    for ex, ey in [(430, 400), (570, 400)]:                        # eyes
        d.ellipse([ex - 26, ey - 20, ex + 26, ey + 20], fill=(16, 18, 30, 255))
    for r, a in [(370, 120), (420, 70)]:                           # ripple
        d.arc([500 - r, 430 - r, 500 + r, 430 + r], start=-60, end=60,
              fill=(150, 195, 255, a), width=10)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 30)


def scene_shape_of_unn():
    rgb = vgrad((22, 40, 42), (7, 14, 15))
    noise(rgb, 0.010, 31)
    glow(rgb, 500, 450, 340, 300, (0.4, 0.7, 0.6), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 655, W, H], fill=(9, 17, 16, 255))
    d.arc([320, 330, 720, 700], start=200, end=340, fill=(120, 170, 150, 255), width=52)  # slug
    d.arc([320, 330, 720, 700], start=205, end=335, fill=(160, 205, 180, 200), width=30)
    head = scaled(render("The_Knight.png"), 250, 260)
    img.alpha_composite(head, (430, 130))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 32)


def scene_thorns_of_agony():
    rgb = vgrad((30, 22, 26), (10, 7, 10))
    noise(rgb, 0.011, 33)
    glow(rgb, 500, 380, 300, 300, (0.7, 0.35, 0.4), 0.35)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(34)
    for _ in range(10):                                            # thorned vines
        x0, y0 = rng.uniform(0, W), 760
        pts = [(x0, y0)]
        for _ in range(5):
            pts.append((pts[-1][0] + rng.uniform(-70, 70), pts[-1][1] - rng.uniform(60, 120)))
        d.line(pts, fill=(52, 34, 40, 255), width=10, joint="curve")
        for px, py in pts[1:-1]:
            d.polygon([(px - 8, py), (px + 14, py - 26), (px + 4, py + 6)], fill=(70, 46, 52, 255))
    head = scaled(head_crop_render(), 260, 260)
    img.alpha_composite(head, (500 - 130, 250))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 35)


def head_crop_render():
    im = render("The_Knight.png")
    crop = im.crop((0, 0, im.width, round(im.height * 0.545)))
    return crop.crop(crop.getbbox())


def scene_spell_burst(card, seed, frame_a, frame_b, tint_glow, extra=None):
    """spell scenes composed over a matted death-video burst frame"""
    rgb = vgrad(ABYSS_TOP, ABYSS_BOT)
    noise(rgb, 0.012, seed)
    glow(rgb, 500, 380, 360, 330, tint_glow, 0.5)
    img = to_image(rgb)
    burst = scaled(death_frame(frame_a), 830, 634)
    img.alpha_composite(burst, (500 - burst.width // 2, 380 - burst.height // 2))
    if frame_b is not None:
        second = scaled(death_frame(frame_b), 620, 475)
        img.alpha_composite(second, (500 - second.width // 2, 400 - second.height // 2))
    if extra:
        extra(ImageDraw.Draw(img))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 10, seed, vig=0.42)


def scene_vengeful_spirit():
    rgb = vgrad(ABYSS_TOP, ABYSS_BOT)
    noise(rgb, 0.012, 36)
    glow(rgb, 500, 380, 360, 330, (0.55, 0.6, 0.95), 0.5)
    img = to_image(rgb)
    burst = scaled(death_frame(6), 830, 634)
    img.alpha_composite(burst, (500 - burst.width // 2, 380 - burst.height // 2))
    art = scaled(sprite("Icon_HK_Vengeful_Spirit_Art.png"), 700, 300)
    img.alpha_composite(art, (500 - art.width // 2, 380 - art.height // 2))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 10, 36, vig=0.42)


def scene_shade_soul():
    return spell_art_scene(37, "Icon_HK_Shade_Soul_Art.png", 820, 260, (0.45, 0.30, 0.75))




def scene_howling_wraiths():
    return spell_art_scene(38, "Icon_HK_Howling_Wraiths_Art.png", 600, 480, (0.62, 0.66, 1.0), wisp_n=14)




def scene_abyss_shriek():
    return spell_art_scene(39, "Icon_HK_Abyss_Shriek_Art.png", 640, 480, (0.55, 0.42, 0.85), wisp_n=6)




def scene_desolate_dive():
    return spell_art_scene(40, "Icon_HK_Desolate_Dive_Art.png", 730, 415, (0.55, 0.65, 0.95))




def scene_descending_dark():
    return spell_art_scene(42, "Icon_HK_Descending_Dark_Art.png", 660, 395, (0.55, 0.45, 0.95))




def scene_shade_cloak():
    return spell_art_scene(44, "Icon_HK_Shade_Cloak_Art.png", 770, 340, (0.45, 0.30, 0.75))




def scene_coiled_nail():
    rgb = vgrad((34, 30, 48), (11, 10, 20))
    noise(rgb, 0.011, 46)
    glow(rgb, 500, 390, 320, 320, (0.55, 0.5, 0.95), 0.45)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    import math
    cx, cy = 500, 390
    pts = []
    for t in np.linspace(0, 4.2 * np.pi, 160):                     # coiled blade spiral
        r = 26 + t * 34
        pts.append((cx + r * np.cos(t), cy + r * np.sin(t) * 0.86))
    for w_off, col in [(10, (30, 36, 58, 255)), (0, BONE + (255,))]:
        d.line(pts, fill=col, width=16 + w_off * 2, joint="curve")
    d.ellipse([cx - 20, cy - 20, cx + 20, cy + 20], fill=(222, 200, 130, 255))
    d.ellipse([cx - 12, cy - 12, cx + 12, cy + 12], fill=(30, 36, 58, 255))
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 47)


def scene_dream_nail():
    rgb = vgrad((30, 26, 52), (9, 8, 20))
    noise(rgb, 0.010, 48)
    glow(rgb, 500, 380, 340, 320, (0.60, 0.55, 0.95), 0.45)
    img = to_image(rgb)
    dnail = scaled(sprite("Icon_HK_Dream_Nail.png"), 360, 560)   # the ornate dream nail
    ground_shadow(img, 500, 700, 180, 22)
    img.alpha_composite(dnail, (500 - dnail.width // 2, 130))
    for rr, wdt, ang in [(280, 8, 90), (350, 6, 90)]:    # dream ripples from the blade
        slash_arc(img, 500, 400, rr, wdt, ang, color=(190, 170, 255, 110), arc=360)
    for ox, oy, r in [(320, 260, 14), (700, 220, 18), (630, 350, 11), (370, 180, 12), (720, 450, 9)]:
        soul_orb(img, ox, oy, r)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 22, 49)


def scene_deep_focus():
    rgb = vgrad((16, 26, 52), (5, 7, 18))
    noise(rgb, 0.010, 50)
    glow(rgb, 500, 320, 380, 360, (0.45, 0.60, 1.0), 0.6)
    img = to_image(rgb)
    charm = scaled(sprite("Icon_HK_Focus.png"), 330, 330)   # the Focus charm, blazing
    img.alpha_composite(charm, (500 - charm.width // 2, 110))
    rest = scaled(render("The_Knight_Resting.png"), 500, 500)
    ground_shadow(img, 500, 690, 210, 28)
    img.alpha_composite(rest, (500 - rest.width // 2, 690 - rest.height))
    for ox, oy, r in [(280, 300, 12), (720, 280, 14), (660, 460, 9), (340, 460, 10)]:
        soul_orb(img, ox, oy, r)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 20, 51)


def scene_fury_of_the_fallen():
    def emblem(d, cx, cy, r):
        for side in (-1, 1):                             # angry brow slashes
            x0, y0 = cx + side * r * 0.42, cy - r * 0.36
            x1, y1 = cx + side * r * 0.06, cy - r * 0.10
            d.line([x0, y0, x1, y1], fill=(214, 78, 64, 255), width=max(8, int(r * 0.09)))
            d.ellipse([cx + side * r * 0.20 - r * 0.08, cy + r * 0.04 - r * 0.08,
                       cx + side * r * 0.20 + r * 0.08, cy + r * 0.04 + r * 0.08],
                      fill=(230, 96, 78, 255))           # burning eyes
        for a0 in (160, 270, 20):                        # rage cracks on the disc
            ar = np.radians(a0)
            x0, y0 = cx + np.cos(ar) * r * 0.16, cy + np.sin(ar) * r * 0.16
            d.line([x0, y0, x0 + np.cos(ar) * r * 0.30, y0 + np.sin(ar) * r * 0.30],
                   fill=(60, 26, 24, 255), width=5)
    return charm_scene("fury_of_the_fallen", 52, (64, 32, 30), emblem, (1.0, 0.45, 0.35))


def scene_soul_totem():
    rgb = vgrad((22, 34, 44), (7, 12, 18))
    noise(rgb, 0.011, 53)
    glow(rgb, 500, 330, 300, 300, (0.50, 0.70, 0.90), 0.5)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(9, 14, 18, 255))
    pair = Image.open(os.path.join(HKSS, "Regular_Soul_Totem_and_Sharp_Shadow_Totem.png")).convert("RGBA")
    totem = scaled(pair.crop((0, 0, pair.width // 2, pair.height)), 310, 560)
    ground_shadow(img, 500, 690, 220, 26)
    img.alpha_composite(totem, (500 - totem.width // 2, 680 - totem.height))
    soul_orb(img, 500, 680 - totem.height - 30, 34)      # soul welling at the totem's crown
    for ang in (205, 335, 30, 150):                      # energy arcing off the stone
        slash_arc(img, 500, 250, 150, 8, ang, arc=55)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 54)


def scene_weaversong():
    rgb = vgrad((26, 24, 44), (8, 8, 18))
    noise(rgb, 0.011, 55)
    glow(rgb, 380, 280, 320, 300, (0.65, 0.60, 0.95), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    ax, ay = 130, 40                                     # web anchored top-left
    spokes = [math.radians(20 + k * 38) for k in range(9)]
    for a in spokes:
        d.line([ax, ay, ax + math.cos(a) * 900, ay + math.sin(a) * 900],
               fill=(52, 48, 82, 255), width=4)
    for rr in (130, 250, 370, 490, 610):                 # snared rings
        pts = [(ax + math.cos(a) * rr, ay + math.sin(a) * rr) for a in spokes]
        d.line(pts, fill=(70, 64, 108, 255), width=3)
    d.line([500, 0, 500, 240], fill=(120, 110, 160, 255), width=4)   # the weaver drops in
    weaver = scaled(sprite("B_Little_Weaver.png"), 500, 300)
    img.alpha_composite(weaver, (500 - weaver.width // 2, 330 - weaver.height // 2))
    soul_orb(img, 760, 470, 16)                          # a wisp caught in the silk
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 56)


def scene_weaver_bite():
    rgb = vgrad((24, 20, 40), (7, 6, 16))
    noise(rgb, 0.011, 57)
    glow(rgb, 500, 380, 320, 300, (0.60, 0.50, 0.90), 0.45)
    img = to_image(rgb)
    weaver = scaled(sprite("B_Little_Weaver.png"), 700, 420)
    weaver = weaver.transpose(Image.FLIP_LEFT_RIGHT)
    img.alpha_composite(weaver, (500 - weaver.width // 2, 400 - weaver.height))
    slash_arc(img, 500, 300, 130, 10, 30, arc=60)
    slash_arc(img, 500, 300, 130, 10, 150, arc=60)
    soul_orb(img, 500, 210, 18)                          # the bitten wisp, flung
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 58)


def scene_lifeblood_cocoon():
    rgb = vgrad((16, 30, 52), (5, 10, 22))
    noise(rgb, 0.010, 59)
    glow(rgb, 500, 390, 320, 330, (0.35, 0.60, 1.0), 0.55)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.line([0, 110, W, 84], fill=(18, 26, 40, 255), width=16)        # anchor root
    d.line([500, 92, 500, 205], fill=(84, 112, 150, 255), width=6)   # silk thread
    d.ellipse([372, 195, 628, 630], fill=(52, 92, 160, 255))         # cocoon body
    d.ellipse([398, 215, 602, 612], fill=(84, 142, 224, 255))
    d.ellipse([428, 245, 572, 585], fill=(122, 176, 248, 255))
    for by in (300, 380, 460, 540):                                  # wrapped silk bands
        wdt = int(150 * (1 - abs(by - 410) / 280)) + 30
        d.line([500 - wdt // 2, by, 500 + wdt // 2, by + 14], fill=(190, 222, 255, 255), width=10)
    for dx, dy in [(432, 636), (520, 656), (478, 690)]:              # lifeblood drips
        d.ellipse([dx - 8, dy, dx + 8, dy + 24], fill=(120, 180, 255, 255))
    soul_orb(img, 500, 410, 24)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 60)


def scene_whetstone():
    rgb = vgrad((34, 30, 40), (11, 10, 16))
    noise(rgb, 0.010, 63)
    glow(rgb, 500, 380, 330, 310, (0.70, 0.60, 0.85), 0.4)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 660, W, H], fill=(13, 12, 18, 255))
    d.polygon([(290, 706), (356, 566), (704, 566), (764, 706)], fill=(30, 28, 36, 255))  # whetstone slab
    d.polygon([(356, 566), (704, 566), (692, 606), (368, 606)], fill=(52, 48, 62, 255))
    nail(d, 500, 360, 55, 390, 26, 8)                    # the nail, mid-hone
    ground_shadow(img, 520, 690, 240, 26)
    for sx in (380, 560, 650):                           # honing sparks
        slash_arc(img, sx, 480, 110, 9, 30, arc=60)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 64)


def scene_stalwart_shell():
    rgb = vgrad((22, 32, 52), (7, 11, 22))
    noise(rgb, 0.010, 65)
    glow(rgb, 500, 430, 330, 320, (0.50, 0.65, 0.90), 0.45)
    img = to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 668, W, H], fill=(10, 13, 22, 255))
    d.pieslice([330, 230, 670, 640], start=180, end=360, fill=(96, 116, 156, 255))   # shell dome
    d.arc([330, 230, 670, 640], start=200, end=340, fill=(195, 220, 252, 230), width=18)
    d.line([330, 435, 670, 435], fill=(38, 50, 78, 255), width=14)                   # ground rim
    for ang in (210, 240, 270, 300, 330):                                            # plate seams
        ar = math.radians(ang)
        d.line([500 + math.cos(ar) * 34, 435 + math.sin(ar) * 34,
                500 + math.cos(ar) * 158, 435 + math.sin(ar) * 158],
               fill=(60, 76, 110, 255), width=8)
    d.line([500, 302, 560, 380], fill=(28, 34, 54, 255), width=9)                    # the crack
    d.line([560, 380, 522, 440], fill=(28, 34, 54, 255), width=7)
    d.line([560, 380, 586, 374], fill=(190, 220, 255, 255), width=5)
    ground_shadow(img, 500, 668, 230, 24)
    for r, a in [(410, 140), (490, 80)]:                 # hardening ripple
        d.arc([500 - r, 435 - r, 500 + r, 435 + r], start=200, end=340,
              fill=(160, 200, 255, a), width=12)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 66)


def scene_nail_shockwave():
    rgb = vgrad((20, 28, 46), (6, 9, 18))
    noise(rgb, 0.011, 61)
    glow(rgb, 500, 380, 360, 330, (0.55, 0.68, 0.95), 0.5)
    img = to_image(rgb)
    art = scaled(sprite("Icon_HK_Great_Slash_Art.png"), 740, 570)   # the great slash itself
    img.alpha_composite(art, (500 - art.width // 2, 360 - art.height // 2))
    for ang in (25, 90, 155):                            # expanding wavefronts
        slash_arc(img, 500, 380, 340, 14, ang, arc=70)
    for r, a in [(250, 150), (340, 90)]:
        d = ImageDraw.Draw(img)
        d.ellipse([500 - r, 380 - r, 500 + r, 380 + r], outline=(190, 215, 255, a), width=6)
    return finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 62)


SCENES = {
    "old_nail": scene_old_nail,
    "shell": scene_shell,
    "focus": scene_focus,
    "quick_slash": scene_quick_slash,
    "great_slash": scene_great_slash,
    "dash_slash": scene_dash_slash,
    "cyclone_slash": scene_cyclone_slash,
    "channelled_nail": scene_channelled_nail,
    "coiled_nail": scene_coiled_nail,
    "dash": scene_dash,
    "dark_shrine": scene_dark_shrine,
    "steady_body": scene_steady_body,
    "unbreakable_strength": scene_unbreakable_strength,
    "fragile_strength": scene_fragile_strength,
    "shaman_stone": scene_shaman_stone,
    "kingsoul": scene_kingsoul,
    "baldur_shell": scene_baldur_shell,
    "shape_of_unn": scene_shape_of_unn,
    "thorns_of_agony": scene_thorns_of_agony,
    "vengeful_spirit": scene_vengeful_spirit,
    "shade_soul": scene_shade_soul,
    "howling_wraiths": scene_howling_wraiths,
    "abyss_shriek": scene_abyss_shriek,
    "desolate_dive": scene_desolate_dive,
    "descending_dark": scene_descending_dark,
    "shade_cloak": scene_shade_cloak,
    "dream_nail": scene_dream_nail,
    "deep_focus": scene_deep_focus,
    "fury_of_the_fallen": scene_fury_of_the_fallen,
    "soul_totem": scene_soul_totem,
    "weaversong": scene_weaversong,
    "weaver_bite": scene_weaver_bite,
    "lifeblood_cocoon": scene_lifeblood_cocoon,
    "whetstone": scene_whetstone,
    "stalwart_shell": scene_stalwart_shell,
    "nail_shockwave": scene_nail_shockwave,
}


def main():
    import sys
    want = set(sys.argv[1:]) or None
    os.makedirs(os.path.join(IMG, "big"), exist_ok=True)
    made = 0
    for card, fn in SCENES.items():
        if want is not None and card not in want:
            continue
        save(fn(), card)
        made += 1
    print(f"done: {made} of {len(SCENES)} cards")


if __name__ == "__main__":
    main()
