#!/usr/bin/env python3
"""Make short combat animations from the locally cached Hollow Knight art.

The official Pure Nail render is composited with original slash and SOUL
effects. Frames share the 480x480 anchor of charui/knight_combat.png so a
runtime AnimatedSprite2D can sit directly on the Knight's body sprite.
"""

import math
import os

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "HollowKnight", "images", "vfx")
NAIL = os.path.join(ROOT, "tools", "wiki_art_raw", "hkss", "Nail 5 Pure Nail.png")
SPIRIT = os.path.join(ROOT, "tools", "wiki_art_raw", "hkss", "Icon_HK_Vengeful_Spirit.png")
SIZE = 480
SS = 2
FRAMES = 6
HAND = (267, 407)


def canvas():
    return Image.new("RGBA", (SIZE * SS, SIZE * SS), (0, 0, 0, 0))


def glow(layer, point, radius, color, alpha):
    halo = canvas()
    x, y = (round(v * SS) for v in point)
    r = round(radius * SS)
    ImageDraw.Draw(halo).ellipse((x-r, y-r, x+r, y+r), fill=(*color, alpha))
    layer.alpha_composite(halo.filter(ImageFilter.GaussianBlur(radius * SS * 0.55)))


def ring(layer, point, radius, color, alpha, width=6, start=0, end=360):
    x, y = (round(v * SS) for v in point)
    r = round(radius * SS)
    ImageDraw.Draw(layer).arc((x-r, y-r, x+r, y+r), start, end,
                              fill=(*color, alpha), width=width * SS)


def nail_frame(index, nail):
    img = canvas()
    angle = (-78, -20, -21, 8, 32, 49)[index]
    alpha = (125, 210, 245, 230, 165, 85)[index]
    # Frames 0-1 draw the weapon out from its idle position behind the left
    # shoulder. The body texture has no back-mounted grip for this whole phase.
    grip = ((202, 406), (239, 417), HAND, HAND, HAND, HAND)[index]
    glow(img, grip, 70 if index < 2 else 88, (146, 195, 255), alpha // 2)
    if index > 1:
        ring(img, HAND, 139, (108, 162, 228), alpha // 2, 22, -78, angle)
        ring(img, HAND, 139, (231, 244, 255), alpha, 7, -78, angle)
        ring(img, HAND, 116, (181, 219, 250), alpha // 2, 4, -78, angle)
    # Source render points downward. Pillow's positive rotation moves its tip
    # counterclockwise; 90-angle puts the tip at the desired screen angle.
    length = (55, 80, 175, 175, 175, 175)[index]
    weapon = nail.resize((round(39 * length / 175 * SS), length * SS), Image.Resampling.LANCZOS)
    weapon = weapon.rotate(90 - angle, Image.Resampling.BICUBIC, expand=True)
    theta = math.radians(angle)
    cx = grip[0] + (length * 0.47) * math.cos(theta)
    cy = grip[1] + (length * 0.47) * math.sin(theta)
    img.alpha_composite(weapon, (round(cx * SS - weapon.width / 2),
                                 round(cy * SS - weapon.height / 2)))
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def soul_frame(index, spirit):
    img = canvas()
    progress = index / (FRAMES - 1)
    x = 288 + progress * 95
    y = 355 - progress * 9
    alpha = round(225 * (1 - progress * 0.45))
    glow(img, (x, y), 75 + progress * 20, (140, 192, 255), alpha)
    for j in range(3):
        ring(img, (258, 358), 52 + index * 14 + j * 18,
             (187, 219, 255), max(35, 150 - index * 18 - j * 28), 4,
             208, 342)
    w = round((76 + progress * 18) * SS)
    h = round((61 + progress * 12) * SS)
    sprite = spirit.resize((w, h), Image.Resampling.LANCZOS)
    sprite.putalpha(sprite.getchannel("A").point(lambda a: a * alpha // 255))
    img.alpha_composite(sprite, (round(x * SS - w / 2), round(y * SS - h / 2)))
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def aura_frame(index, power=False):
    img = canvas()
    progress = index / (FRAMES - 1)
    center = (248, 355)
    color = (250, 207, 130) if power else (190, 220, 250)
    alpha = round(200 * (1 - progress * 0.75))
    glow(img, center, 95 + 25 * progress, color, alpha // 2)
    ring(img, center, 72 + 72 * progress, color, alpha, 7, 190, 350)
    ring(img, center, 48 + 55 * progress, (237, 246, 255), alpha // 2, 4,
         35, 160)
    draw = ImageDraw.Draw(img)
    for j in range(6):
        a = math.radians(210 + j * 30)
        r = 82 + index * 11 + (j % 2) * 14
        x = round((center[0] + math.cos(a) * r) * SS)
        y = round((center[1] + math.sin(a) * r) * SS)
        rr = max(2, 6 - index // 2) * SS
        draw.ellipse((x-rr, y-rr, x+rr, y+rr), fill=(*color, alpha))
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    nail = Image.open(NAIL).convert("RGBA")
    spirit = Image.open(SPIRIT).convert("RGBA")
    spirit = spirit.crop(spirit.getbbox())
    for i in range(FRAMES):
        nail_frame(i, nail).save(os.path.join(OUT, f"knight_nail_{i:02}.png"))
        soul_frame(i, spirit).save(os.path.join(OUT, f"knight_soul_{i:02}.png"))
        aura_frame(i).save(os.path.join(OUT, f"knight_skill_{i:02}.png"))
        aura_frame(i, power=True).save(os.path.join(OUT, f"knight_power_{i:02}.png"))
    print(f"generated {FRAMES * 4} combat effect frames in {OUT}")


if __name__ == "__main__":
    main()
