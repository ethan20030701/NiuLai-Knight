#!/usr/bin/env python3
"""HK sprite and scene pass for selected v0.5/v0.6 card portraits.

Uses the game and press-kit images already in tools/wiki_art_raw. Generate
after gen_art.py and gen_cards_v3.py; only the listed portraits are replaced.
The source assets stay in the repository, so this pass needs no wiki request.

    python tools/gen_cards_v4.py              # all selected portraits
    python tools/gen_cards_v4.py soul_bolt     # one portrait
"""

import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_cards_v2 as v2

W, H = v2.W, v2.H


def background(seed, top, bottom, glow_color, place="cave"):
    rgb = v2.vgrad(top, bottom)
    v2.noise(rgb, 0.009, seed)
    v2.glow(rgb, 500, 390, 420, 340, glow_color, 0.44)
    img = v2.to_image(rgb)
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(seed)
    if place in ("cave", "abyss", "greenpath", "forge"):
        rock = (12, 15, 25, 255) if place != "greenpath" else (9, 26, 22, 255)
        for x in range(0, W + 100, 75):
            length = int(rng.integers(35, 130))
            d.polygon([(x - 22, 0), (x + 24, 0), (x + 5, length)], fill=rock)
        d.polygon([(0, 710), (190, 673), (390, 701), (650, 660), (810, 694),
                   (1000, 675), (1000, 760), (0, 760)], fill=rock)
    if place == "city":
        for x, width, height in ((48, 90, 250), (220, 120, 320), (510, 95, 220),
                                 (710, 105, 340), (925, 80, 270)):
            d.rectangle((x, 760 - height, x + width, 760), fill=(18, 24, 39, 255))
            d.arc((x - 24, 760 - height - 65, x + width + 24, 760 - height + 60),
                  180, 360, fill=(72, 84, 112, 150), width=7)
        for i in range(90):
            x = int(rng.integers(0, W))
            y = int(rng.integers(0, H))
            d.line((x, y, x - 12, y + 48), fill=(95, 125, 160, 42), width=2)
    if place == "deepnest":
        for i in range(12):
            x = int(rng.integers(-50, 1050))
            d.line((x, 0, x + int(rng.integers(-90, 90)), 760),
                   fill=(12, 14, 23, 255), width=int(rng.integers(9, 25)))
    if place == "forge":
        d.rectangle((0, 665, W, H), fill=(25, 21, 26, 255))
        d.polygon(((310, 700), (690, 700), (610, 760), (390, 760)),
                  fill=(70, 42, 32, 255))
        d.line((300, 700, 700, 700), fill=(156, 91, 56, 255), width=14)
    return img


def sprite(img, name, box, center, angle=0, presskit=False):
    if name == "Baldur Shell Icon.png":
        # The source is a five-frame icon strip; use one frame as the subject.
        art = Image.open(os.path.join(v2.HKSS, name)).convert("RGBA")
        art = art.crop((0, 0, 90, 90))
        art = art.crop(art.getbbox())
    else:
        art = v2.render(name) if presskit else v2.sprite(name)
    art = v2.scaled(art, *box)
    if angle:
        art = art.rotate(angle, Image.Resampling.BICUBIC, expand=True)
    img.alpha_composite(art, (round(center[0] - art.width / 2),
                              round(center[1] - art.height / 2)))


def glow(img, center, radius, color, alpha=0.55):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y = center
    d.ellipse((x - radius, y - radius, x + radius, y + radius),
              fill=tuple(color) + (round(alpha * 255),))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius * 0.55)))


def ring(img, center, radius, color, width=10, alpha=180, start=0, end=360):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y = center
    d.arc((x - radius, y - radius, x + radius, y + radius), start, end,
          fill=tuple(color) + (alpha,), width=width)
    img.alpha_composite(layer)


def finish(img, seed, wisps=16):
    return v2.finish(np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0,
                     wisps, seed, vig=0.38)


def heavy_blow():
    img = background(80, (34, 43, 59), (11, 13, 22), (0.60, 0.69, 0.86))
    glow(img, (555, 520), 180, (180, 210, 250), 0.5)
    sprite(img, "Knight_Action.png", (650, 600), (520, 415), presskit=True)
    for r in (135, 205, 270):
        ring(img, (570, 610), r, (202, 224, 245), 7, 160, 205, 335)
    return finish(img, 80)


def shell_slam():
    img = background(83, (31, 40, 56), (12, 15, 24), (0.60, 0.75, 0.95))
    glow(img, (560, 375), 230, (170, 210, 255), 0.7)
    sprite(img, "Baldur Shell Icon.png", (470, 430), (560, 390))
    for r in (240, 290, 340):
        ring(img, (550, 430), r, (190, 220, 250), 9, 160, 210, 335)
    d = ImageDraw.Draw(img)
    for y, length in ((250, 215), (365, 280), (520, 180)):
        d.line((40, y, 40 + length, y - 28), fill=(165, 200, 240, 140), width=9)
    return finish(img, 83)


def soul_bolt():
    img = background(87, (28, 35, 61), (7, 9, 22), (0.59, 0.72, 1.0), "city")
    glow(img, (550, 360), 240, (135, 185, 255), 0.9)
    sprite(img, "Icon_HK_Vengeful_Spirit_Art.png", (800, 520), (530, 365))
    return finish(img, 87, 22)


def web_spin():
    img = background(99, (37, 37, 62), (10, 10, 22), (0.59, 0.58, 0.82), "deepnest")
    d = ImageDraw.Draw(img)
    for angle in range(0, 360, 30):
        a = math.radians(angle)
        d.line((490, 390, 490 + 610 * math.cos(a), 390 + 610 * math.sin(a)),
               fill=(173, 168, 198, 100), width=3)
    for r in (160, 270, 380):
        ring(img, (490, 390), r, (175, 170, 205), 3, 100)
    sprite(img, "Weaverling.png", (430, 410), (480, 365))
    return finish(img, 99)


def nail_envenom():
    img = background(101, (20, 52, 40), (6, 20, 19), (0.30, 0.75, 0.40), "greenpath")
    glow(img, (495, 395), 235, (105, 205, 95), 0.55)
    sprite(img, "Nail 5 Pure Nail.png", (240, 660), (500, 390), angle=-38)
    d = ImageDraw.Draw(img)
    for x, y, r in ((655, 260, 20), (700, 370, 13), (585, 570, 17), (720, 490, 10)):
        d.ellipse((x-r, y-r, x+r, y+r), fill=(116, 230, 93, 210))
        glow(img, (x, y), r * 3, (90, 215, 80), 0.4)
    return finish(img, 101)


def wraith_form():
    img = background(106, (31, 27, 52), (7, 7, 17), (0.49, 0.42, 0.75), "abyss")
    glow(img, (530, 360), 255, (124, 111, 195), 0.55)
    sprite(img, "B_Shade.png", (710, 550), (510, 365))
    for r in (270, 330):
        ring(img, (490, 400), r, (150, 139, 215), 7, 110, 190, 355)
    return finish(img, 106, 20)


def impregnable():
    img = background(112, (36, 45, 66), (12, 16, 28), (0.62, 0.77, 0.96))
    for r in (280, 350, 420):
        ring(img, (500, 510), r, (135, 172, 207), 28, 125, 205, 335)
    glow(img, (500, 390), 230, (170, 210, 245), 0.6)
    sprite(img, "Baldur Shell Icon.png", (420, 400), (500, 390))
    return finish(img, 112)


def hallownests_echo():
    img = background(114, (41, 47, 72), (9, 14, 28), (0.68, 0.75, 1.0), "city")
    for r, alpha in ((175, 175), (295, 130), (410, 85)):
        ring(img, (500, 420), r, (190, 210, 245), 9, alpha)
    glow(img, (500, 390), 260, (175, 205, 250), 0.6)
    sprite(img, "Icon_HK_Dream_Nail.png", (440, 570), (500, 365))
    return finish(img, 114, 20)


def void_feast():
    img = background(115, (25, 23, 43), (4, 5, 14), (0.42, 0.34, 0.63), "abyss")
    glow(img, (625, 365), 205, (145, 135, 220), 0.5)
    sprite(img, "B_Shade.png", (710, 530), (450, 355))
    sprite(img, "HK Mask Lifeblood.png", (190, 185), (700, 400))
    d = ImageDraw.Draw(img)
    for y in (285, 380, 485):
        d.line((330, y, 620, 400), fill=(10, 9, 25, 255), width=26)
    return finish(img, 115, 18)


def soul_echo():
    img = background(119, (30, 38, 65), (7, 10, 23), (0.60, 0.77, 1.0))
    for r, alpha in ((195, 205), (295, 155), (405, 95)):
        ring(img, (500, 385), r, (180, 215, 255), 10, alpha)
    glow(img, (500, 385), 230, (160, 200, 255), 0.5)
    sprite(img, "Soul Vessel.png", (385, 470), (500, 370))
    return finish(img, 119, 20)


def refinement():
    img = background(120, (53, 40, 41), (16, 13, 20), (0.89, 0.60, 0.38), "forge")
    glow(img, (510, 495), 280, (235, 139, 73), 0.65)
    sprite(img, "Nail 5 Pure Nail.png", (270, 670), (500, 350), angle=-35)
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(121)
    for _ in range(34):
        x, y = int(rng.integers(300, 700)), int(rng.integers(240, 660))
        r = int(rng.integers(2, 7))
        d.ellipse((x-r, y-r, x+r, y+r), fill=(245, 170, 90, 175))
    return finish(img, 120, 10)


def soul_battery():
    img = background(122, (29, 36, 67), (9, 12, 29), (0.55, 0.72, 1.0))
    d = ImageDraw.Draw(img)
    d.ellipse((175, 565, 825, 715), fill=(55, 99, 155, 185))
    ring(img, (500, 625), 330, (160, 205, 255), 9, 170, 20, 160)
    glow(img, (500, 380), 230, (150, 200, 255), 0.55)
    sprite(img, "Soul Vessel.png", (360, 445), (500, 390))
    return finish(img, 122)


def venom_blade():
    img = background(123, (23, 54, 38), (5, 18, 18), (0.36, 0.81, 0.43), "greenpath")
    ring(img, (500, 390), 300, (110, 212, 105), 30, 170, 205, 335)
    glow(img, (500, 385), 220, (100, 220, 105), 0.65)
    sprite(img, "Nail 4 Coiled Nail.png", (255, 640), (500, 390), angle=-55)
    d = ImageDraw.Draw(img)
    for x, y, r in ((710, 335, 21), (675, 500, 13), (585, 590, 9)):
        d.ellipse((x-r, y-r, x+r, y+r), fill=(127, 225, 109, 215))
    return finish(img, 123)


SCENES = {
    "heavy_blow": heavy_blow,
    "shell_slam": shell_slam,
    "soul_bolt": soul_bolt,
    "web_spin": web_spin,
    "nail_envenom": nail_envenom,
    "wraith_form": wraith_form,
    "impregnable": impregnable,
    "hallownests_echo": hallownests_echo,
    "void_feast": void_feast,
    "soul_echo": soul_echo,
    "refinement": refinement,
    "soul_battery": soul_battery,
    "venom_blade": venom_blade,
}


def main():
    wanted = set(sys.argv[1:]) or set(SCENES)
    unknown = wanted - set(SCENES)
    if unknown:
        raise SystemExit(f"unknown portrait(s): {', '.join(sorted(unknown))}")
    for card, scene in SCENES.items():
        if card in wanted:
            v2.save(scene(), card)
    print(f"done: {len(wanted)} of {len(SCENES)} portraits")


if __name__ == "__main__":
    main()
