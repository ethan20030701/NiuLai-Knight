#!/usr/bin/env python3
"""Card art v3 — sprite-upgrade pass on top of gen_cards_v2.

Replaces the procedurally-drawn subjects of 10 weak card faces with the
official game sprites fetched from hkss.huijiwiki.com (tools/fetch_hkss.py),
reusing the established scene helpers (vgrad/noise/glow/finish/ground_shadow).

Faces upgraded here (12):
  old_nail, channelled_nail, coiled_nail, soul_totem, lifeblood_cocoon,
  baldur_shell, shape_of_unn, whetstone, stalwart_shell, weaversong,
  shaman_stone, kingsoul

Sizes/paths identical to v2 (big 1000x760 + 250x190). Run:
  python tools/gen_cards_v3.py            # all 12
  python tools/gen_cards_v3.py old_nail   # single card
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# reuse everything from v2 (palette, helpers, finish/save)
import gen_cards_v2 as v2

W, H = v2.W, v2.H
HKSS = v2.HKSS
IMG = v2.IMG


# ---------------------------------------------------------------- scenes
def sprite_scene(card, seed, top, bot, glow_xy, glow_rgb, glow_a,
                 art_file, art_w, art_h, art_y_bottom=None, art_xy=None,
                 ground=True, wisp_n=14, vig=0.42, extra=None):
    """Generic sprite-on-gradient scene (ground + centered official sprite)."""
    rgb = v2.vgrad(top, bot)
    v2.noise(rgb, 0.010, seed)
    v2.glow(rgb, glow_xy[0], glow_xy[1], 360, 330, glow_rgb, glow_a)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    if ground:
        d.rectangle([0, 665, W, H], fill=tuple(int(c * 0.35) for c in bot) + (255,))
    art = v2.scaled(v2.sprite(art_file), art_w, art_h)
    if art_xy is None:
        gx, gy = 500, (art_y_bottom if art_y_bottom is not None else 680)
        pos = (gx - art.width // 2, gy - art.height)
    else:
        pos = art_xy
    if ground:
        v2.ground_shadow(img, 500, 688, max(160, art.width * 0.62), 24)
    img.alpha_composite(art, pos)
    if extra:
        extra(img, d, art, pos)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0,
                     wisp_n, seed, vig=vig)


# ---- nail renders (Nail 1..5, 80x360 vertical) -------------------------
def nail_render_scene(card, seed, art_file, top, bot, glow_rgb, sparks=(), wisp_n=14):
    def extra(img, d, art, pos):
        cx, cy = 500, 400
        for sx, sy, rr, ang in sparks:
            v2.slash_arc(img, sx, sy, rr, 9, ang, arc=60)
        v2.soul_orb(img, cx, cy, 0)  # no-op keeps helper import parity
    # nails are vertical renders: stand them slightly tilted like a planted nail
    def place(img, art, pos):
        return art
    rgb = v2.vgrad(top, bot)
    v2.noise(rgb, 0.010, seed)
    v2.glow(rgb, 500, 400, 350, 330, glow_rgb, 0.45)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=tuple(int(c * 0.35) for c in bot) + (255,))
    art = v2.scaled(v2.sprite(art_file), 300, 520)
    v2.ground_shadow(img, 500, 686, 150, 22)
    img.alpha_composite(art, (500 - art.width // 2, 680 - art.height))
    for sx, sy, rr, ang in sparks:
        v2.slash_arc(img, sx, sy, rr, 9, ang, arc=60)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0,
                     wisp_n, seed, vig=0.42)


def scene_old_nail():
    """Nail 1 Old Nail render, planted in Forgotten Crossroads dirt."""
    return nail_render_scene(
        "old_nail", 1, "Nail 1 Old Nail.png",
        (34, 40, 52), (12, 13, 22), (0.45, 0.52, 0.5),
        sparks=((430, 620, 90, 200), (570, 610, 80, 340)))


def scene_channelled_nail():
    """Nail 3 render + triple after-image slashes (the 3-hit identity)."""
    rgb = v2.vgrad((30, 36, 50), (10, 11, 20))
    v2.noise(rgb, 0.010, 15)
    v2.glow(rgb, 500, 400, 350, 330, (0.45, 0.55, 0.95), 0.45)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(9, 11, 18, 255))
    art = v2.scaled(v2.sprite("Nail 3 Channelled Nail.png"), 300, 520)
    v2.ground_shadow(img, 500, 686, 150, 22)
    img.alpha_composite(art, (500 - art.width // 2, 680 - art.height))
    # three fading slash echoes sweeping across
    for alpha, dy, rr in [(200, 0, 200), (120, 60, 260), (60, 120, 320)]:
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        v2.slash_arc(layer, 500, 400 + dy, rr, 12, 25,
                     color=(170, 200, 255, alpha), arc=80)
        img.alpha_composite(layer)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 16)


def scene_coiled_nail():
    """Nail 4 render + a coil of soul-light winding up the blade."""
    rgb = v2.vgrad((34, 30, 48), (11, 10, 20))
    v2.noise(rgb, 0.011, 46)
    v2.glow(rgb, 500, 400, 350, 330, (0.55, 0.5, 0.95), 0.45)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(12, 10, 18, 255))
    art = v2.scaled(v2.sprite("Nail 4 Coiled Nail.png"), 300, 520)
    v2.ground_shadow(img, 500, 686, 150, 22)
    img.alpha_composite(art, (500 - art.width // 2, 680 - art.height))
    # winding coil of light around the blade
    pts = []
    for t in np.linspace(0, 3.4 * np.pi, 120):
        r = 40 + t * 26
        pts.append((500 + r * np.cos(t), 400 + r * np.sin(t) * 0.9))
    d.line(pts, fill=(190, 170, 255, 140), width=7, joint="curve")
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 47)


# ---- soul totem (official Large totem render) --------------------------
def scene_soul_totem():
    rgb = v2.vgrad((22, 34, 44), (7, 12, 18))
    v2.noise(rgb, 0.011, 53)
    v2.glow(rgb, 500, 330, 300, 300, (0.50, 0.70, 0.90), 0.5)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 665, W, H], fill=(9, 14, 18, 255))
    totem = v2.scaled(v2.sprite("Soul Totem Large 1.png"), 330, 600)
    v2.ground_shadow(img, 500, 690, 200, 26)
    img.alpha_composite(totem, (500 - totem.width // 2, 680 - totem.height))
    v2.soul_orb(img, 500, 680 - totem.height - 24, 34)
    for ang in (205, 335, 30, 150):
        v2.slash_arc(img, 500, 240, 150, 8, ang, arc=55)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 54)


# ---- lifeblood cocoon (official cocoon render) -------------------------
def scene_lifeblood_cocoon():
    rgb = v2.vgrad((16, 30, 52), (5, 10, 22))
    v2.noise(rgb, 0.010, 59)
    v2.glow(rgb, 500, 390, 320, 330, (0.35, 0.60, 1.0), 0.55)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.line([0, 110, W, 84], fill=(18, 26, 40, 255), width=16)        # anchor root
    d.line([500, 92, 500, 230], fill=(84, 112, 150, 255), width=6)   # silk thread
    cocoon = v2.scaled(v2.sprite("Lifeblood Cocoon.png"), 360, 470)
    img.alpha_composite(cocoon, (500 - cocoon.width // 2, 228))
    for dx, dy in [(420, 660), (520, 680), (478, 700)]:              # lifeblood drips
        d.ellipse([dx - 8, dy, dx + 8, dy + 24], fill=(120, 180, 255, 255))
    v2.soul_orb(img, 500, 430, 22)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 60)


# ---- baldur shell (official charm icon as the emblem) ------------------
def scene_baldur_shell():
    rgb = v2.vgrad((24, 34, 52), (8, 11, 20))
    v2.noise(rgb, 0.010, 29)
    v2.glow(rgb, 500, 390, 330, 300, (0.5, 0.65, 0.9), 0.45)
    img = v2.to_image(rgb)
    v2.charm_disc(img, 500, 390, 250)
    icon = v2.scaled(v2.sprite("Baldur Shell Icon.png"), 380, 300)
    img.alpha_composite(icon, (500 - icon.width // 2, 390 - icon.height // 2))
    for r, a in [(370, 120), (420, 70)]:                             # shield ripple
        d = ImageDraw.Draw(img)
        d.arc([500 - r, 430 - r, 500 + r, 430 + r], start=-60, end=60,
              fill=(150, 195, 255, a), width=10)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 14, 30)


# ---- shape of unn (official transformed slug form) ---------------------
def scene_shape_of_unn():
    rgb = v2.vgrad((22, 40, 42), (7, 14, 15))
    v2.noise(rgb, 0.010, 31)
    v2.glow(rgb, 500, 450, 340, 300, (0.4, 0.7, 0.6), 0.4)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 655, W, H], fill=(9, 17, 16, 255))
    unn = v2.scaled(v2.sprite("Shape of Unn Default.png"), 520, 430)
    v2.ground_shadow(img, 500, 672, 260, 26)
    img.alpha_composite(unn, (500 - unn.width // 2, 665 - unn.height))
    v2.soul_orb(img, 700, 520, 14)                                   # focused soul wisp
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 16, 32)


# ---- whetstone (pure nail render mid-hone on the slab) -----------------
def scene_whetstone():
    rgb = v2.vgrad((34, 30, 40), (11, 10, 16))
    v2.noise(rgb, 0.010, 63)
    v2.glow(rgb, 500, 380, 330, 310, (0.70, 0.60, 0.85), 0.4)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 660, W, H], fill=(13, 12, 18, 255))
    d.polygon([(290, 706), (356, 566), (704, 566), (764, 706)], fill=(30, 28, 36, 255))
    d.polygon([(356, 566), (704, 566), (692, 606), (368, 606)], fill=(52, 48, 62, 255))
    nail = v2.scaled(v2.sprite("Nail 5 Pure Nail.png"), 240, 420)
    nail = nail.rotate(35, expand=True, resample=Image.BICUBIC)
    img.alpha_composite(nail, (500 - nail.width // 2, 480 - nail.height // 2))
    v2.ground_shadow(img, 520, 690, 240, 26)
    for sx in (380, 560, 650):
        v2.slash_arc(img, sx, 490, 110, 9, 30, arc=60)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 64)


# ---- stalwart shell (official masked shell + hardening ring) -----------
def scene_stalwart_shell():
    rgb = v2.vgrad((22, 32, 52), (7, 11, 22))
    v2.noise(rgb, 0.010, 65)
    v2.glow(rgb, 500, 430, 330, 320, (0.50, 0.65, 0.90), 0.45)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 668, W, H], fill=(10, 13, 22, 255))
    mask = v2.scaled(v2.sprite("HK Mask Lifeblood.png"), 360, 380)
    img.alpha_composite(mask, (500 - mask.width // 2, 200))
    v2.ground_shadow(img, 500, 668, 200, 24)
    for r, a in [(300, 150), (380, 90), (460, 50)]:                  # hardening ripple
        d.arc([500 - r, 400 - r, 500 + r, 400 + r], start=200, end=340,
              fill=(160, 200, 255, a), width=12)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 12, 66)


# ---- weaversong (official weaver render in the web) --------------------
def scene_weaversong():
    rgb = v2.vgrad((26, 24, 44), (8, 8, 18))
    v2.noise(rgb, 0.011, 55)
    v2.glow(rgb, 380, 280, 320, 300, (0.65, 0.60, 0.95), 0.4)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    ax, ay = 130, 40
    spokes = [math.radians(20 + k * 38) for k in range(9)]
    for a in spokes:
        d.line([ax, ay, ax + math.cos(a) * 900, ay + math.sin(a) * 900],
               fill=(52, 48, 82, 255), width=4)
    for rr in (130, 250, 370, 490, 610):
        pts = [(ax + math.cos(a) * rr, ay + math.sin(a) * rr) for a in spokes]
        d.line(pts, fill=(70, 64, 108, 255), width=3)
    d.line([500, 0, 500, 240], fill=(120, 110, 160, 255), width=4)
    weaver = v2.scaled(v2.sprite("Weaver Idle.png"), 430, 330)
    img.alpha_composite(weaver, (500 - weaver.width // 2, 330 - weaver.height // 2))
    v2.soul_orb(img, 760, 470, 16)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 56)


# ---- shaman stone (official snail shaman above the disc) ---------------
def scene_shaman_stone():
    rgb = v2.vgrad((52, 40, 56), (18, 14, 24))
    v2.noise(rgb, 0.011, 25)
    v2.glow(rgb, 500, 380, 330, 310, (0.8, 0.6, 0.9), 0.4)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 670, W, H], fill=(14, 10, 20, 255))
    rng = np.random.default_rng(26)
    for _ in range(7):                                   # mushroom silhouettes
        mx, mh, mw = rng.uniform(60, 940), rng.uniform(60, 160), rng.uniform(40, 90)
        my = 700
        d.rectangle([mx - 6, my - mh, mx + 6, my], fill=(30, 22, 42, 255))
        d.ellipse([mx - mw, my - mh - mw * 0.5, mx + mw, my - mh + mw * 0.4],
                  fill=(58, 44, 78, 255))
    shaman = v2.scaled(v2.sprite("Snail Shaman.png"), 420, 380)
    img.alpha_composite(shaman, (500 - shaman.width // 2, 90))
    v2.charm_disc(img, 500, 560, 190,
                  emblem=lambda d, cx, cy, r: (
                      d.ellipse([cx - r * 0.45, cy - r * 0.45, cx + r * 0.45, cy + r * 0.45],
                                fill=(160, 120, 190, 255)),
                      d.ellipse([cx - r * 0.28, cy - r * 0.28, cx + r * 0.28, cy + r * 0.28],
                                fill=(210, 180, 235, 255))))
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 27)


# ---- kingsoul (official charm-like disc with the pale winged emblem) ---
def scene_kingsoul():
    rgb = v2.vgrad((40, 44, 66), (14, 15, 26))
    v2.noise(rgb, 0.011, 28)
    v2.glow(rgb, 500, 380, 340, 320, (0.65, 0.72, 1.0), 0.5)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    v2.charm_disc(img, 500, 390, 250)
    vessel = v2.scaled(v2.sprite("HK Mask Lifeblood.png"), 300, 330)
    img.alpha_composite(vessel, (500 - vessel.width // 2, 390 - vessel.height // 2))
    for ox, oy, r in [(300, 250, 12), (700, 240, 14), (660, 520, 10), (340, 540, 11)]:
        v2.soul_orb(img, ox, oy, r)
    return v2.finish(np.asarray(img.convert("RGB")).astype(np.float32) / 255.0, 18, 28)


SCENES = {
    "old_nail": scene_old_nail,
    "channelled_nail": scene_channelled_nail,
    "coiled_nail": scene_coiled_nail,
    "soul_totem": scene_soul_totem,
    "lifeblood_cocoon": scene_lifeblood_cocoon,
    "baldur_shell": scene_baldur_shell,
    "shape_of_unn": scene_shape_of_unn,
    "whetstone": scene_whetstone,
    "stalwart_shell": scene_stalwart_shell,
    "weaversong": scene_weaversong,
    "shaman_stone": scene_shaman_stone,
    "kingsoul": scene_kingsoul,
}


def main():
    want = set(sys.argv[1:]) or None
    made = 0
    for card, fn in SCENES.items():
        if want is not None and card not in want:
            continue
        v2.save(fn(), card)
        made += 1
    print(f"done: {made} of {len(SCENES)} cards")


if __name__ == "__main__":
    main()
