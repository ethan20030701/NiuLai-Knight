#!/usr/bin/env python3
"""Full visual-audit pass for the weak Knight card portraits.

All figures and spell shapes come from the locally cached Team Cherry press-kit
renders or Hollow Knight game sprites. This pass is run after v4 and replaces
only the portraits listed below. The layered ruins, light and motion marks are
original work; the official sprites remain recognizable at card size.
"""
from dataclasses import dataclass
from pathlib import Path
import math
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gen_cards_v2 as v2

W, H = v2.W, v2.H


@dataclass(frozen=True)
class Scene:
    subject: str
    theme: str
    effect: str
    companion: str = ""


# The original contact-sheet review found these faces too sparse, symbolic or
# small. One central illustrated subject is mandatory for every replacement.
SCENES = {
    "baldur_shell": Scene("Baldur Shell Icon.png", "cave", "shell", "@The_Knight_Idle.png"),
    "bone_break": Scene("@Knight_Action.png", "cave", "impact", "Nail 1 Old Nail.png"),
    "broken_shell": Scene("Baldur Shell Icon.png", "cave", "impact", "@The_Knight_Shade.png"),
    "broken_vessel": Scene("@The_Knight_Shade.png", "infection", "blood", "@The_Knight_Front.png"),
    "carefree_melody": Scene("Snail Shaman.png", "dream", "dream", "@The_Knight_Resting.png"),
    "catalyst": Scene("B_Little_Weaver.png", "green", "venom", "@The_Knight_Idle.png"),
    "charged_arts": Scene("@Knight_Action.png", "cave", "nail", "Nail 5 Pure Nail.png"),
    "channelled_nail": Scene("Nail 3 Channelled Nail.png", "forge", "nail", "@The_Knight_Idle.png"),
    "coiled_nail": Scene("Nail 4 Coiled Nail.png", "forge", "nail", "@The_Knight_Idle.png"),
    "dark_shrine": Scene("Soul Totem Large 1.png", "abyss", "void", "@The_Knight_Idle.png"),
    "defender_crest": Scene("Snail Shaman.png", "green", "venom", "@The_Knight_Front.png"),
    "dreamshield": Scene("Icon_HK_Dream_Nail.png", "dream", "shell", "@The_Knight_Idle.png"),
    "flukenest": Scene("B_Little_Weaver.png", "green", "swarm", "Weaverling.png"),
    "fragile_strength": Scene("Nail 2 Sharpened Nail.png", "forge", "impact", "@Knight_Action.png"),
    "full_soul": Scene("Soul Vessel.png", "soul", "soul", "@The_Knight_Front.png"),
    "fury_of_the_fallen": Scene("@Knight_Action.png", "infection", "impact", "Nail 5 Pure Nail.png"),
    "grimmchild": Scene("Snail Shaman.png", "infection", "blood", "@The_Knight_Idle.png"),
    "grubberfly_elegance": Scene("Grub.png", "dream", "nail", "@Knight_Action.png"),
    "grubsongs_melody": Scene("Grub.png", "green", "dream", "@The_Knight_Resting.png"),
    "hum": Scene("Nail 2 Sharpened Nail.png", "forge", "nail", "@The_Knight_Idle.png"),
    "impregnable": Scene("Baldur Shell Icon.png", "cave", "shell", "@The_Knight_Front.png"),
    "mothwing_dash": Scene("@Knight_Action.png", "dream", "speed", "Icon_HK_Dash_Slash_Art.png"),
    "nail_flurry": Scene("@Knight_Action.png", "cave", "nail", "Nail 3 Channelled Nail.png"),
    "nail_shockwave": Scene("Icon_HK_Great_Slash_Art.png", "cave", "impact", "@The_Knight_Idle.png"),
    "nail_storm": Scene("Icon_HK_Cyclone_Slash_Art.png", "cave", "nail", "Nail 5 Pure Nail.png"),
    "pain_brand": Scene("@The_Knight_Shade.png", "infection", "blood", "@Knight_Action.png"),
    "prides_mark": Scene("Nail 5 Pure Nail.png", "dream", "nail", "@The_Knight_Front.png"),
    "raging_nail": Scene("@Knight_Action.png", "infection", "nail", "Nail 1 Old Nail.png"),
    "ricochet_nail": Scene("Nail 4 Coiled Nail.png", "cave", "nail", "@Knight_Action.png"),
    "second_wind": Scene("@The_Knight_Resting.png", "cave", "soul", "@The_Knight_Shade.png"),
    "residual_soul": Scene("Soul Vessel.png", "soul", "soul", "@The_Knight_Front.png"),
    "sharp_assault": Scene("Icon_HK_Dash_Slash_Art.png", "cave", "speed", "@Knight_Action.png"),
    "shell_slam": Scene("Baldur Shell Icon.png", "cave", "impact", "@Knight_Action.png"),
    "soul_aegis": Scene("Icon_HK_Focus.png", "soul", "shell", "@The_Knight_Idle.png"),
    "soul_battery": Scene("Soul Vessel.png", "soul", "soul", "Soul Totem Large 1.png"),
    "soul_flow": Scene("Soul Vessel.png", "soul", "soul", "@The_Knight_Idle.png"),
    "soul_harvest": Scene("Icon_HK_Abyss_Shriek_Art.png", "abyss", "void", "@Knight_Action.png"),
    "soul_pact": Scene("Snail Shaman.png", "dream", "dream", "Soul Vessel.png"),
    "soul_rush": Scene("@Knight_Action.png", "soul", "speed", "Icon_HK_Vengeful_Spirit_Art.png"),
    "soul_siphon": Scene("B_Shade.png", "abyss", "soul", "@The_Knight_Front.png"),
    "soul_surge": Scene("Icon_HK_Vengeful_Spirit_Art.png", "soul", "soul", "@Knight_Action.png"),
    "soul_torrent": Scene("Icon_HK_Howling_Wraiths_Art.png", "soul", "soul", "@The_Knight_Front.png"),
    "soul_tyrant": Scene("Snail Shaman.png", "abyss", "void", "Soul Vessel.png"),
    "soulbell": Scene("Soul Totem Large 1.png", "dream", "dream", "@The_Knight_Resting.png"),
    "spellcharge": Scene("Icon_HK_Focus.png", "soul", "soul", "@The_Knight_Idle.png"),
    "spore_breath": Scene("Shape of Unn Default.png", "green", "venom", "@The_Knight_Idle.png"),
    "stalwart_shell": Scene("Baldur Shell Icon.png", "cave", "shell", "@The_Knight_Front.png"),
    "thorns_of_agony": Scene("@The_Knight_Shade.png", "infection", "blood", "@The_Knight_Front.png"),
    "unbreakable_strength": Scene("Nail 5 Pure Nail.png", "forge", "impact", "@Knight_Action.png"),
    "void_surge": Scene("B_Shade.png", "abyss", "void", "@The_Knight_Idle.png"),
    "wisp_afterimage": Scene("Icon_HK_Shade_Cloak_Art.png", "abyss", "speed", "@The_Knight_Idle.png"),
}

PALETTES = {
    "cave": ((28, 38, 54), (7, 11, 23), (176, 207, 245)),
    "soul": ((25, 39, 67), (5, 10, 26), (170, 213, 255)),
    "abyss": ((24, 20, 44), (4, 4, 14), (145, 127, 216)),
    "dream": ((42, 38, 65), (9, 9, 28), (209, 197, 246)),
    "green": ((23, 50, 40), (4, 18, 18), (139, 209, 143)),
    "infection": ((55, 31, 37), (15, 7, 17), (249, 144, 99)),
    "forge": ((56, 40, 41), (13, 12, 22), (250, 184, 118)),
}


def source(name):
    if name.startswith("@"):
        return v2.render(name[1:])
    art = v2.sprite(name)
    if name == "Baldur Shell Icon.png":
        # This is a five-frame sprite strip, not five separate shell objects.
        art = art.crop((0, 0, min(90, art.width), art.height))
        art = art.crop(art.getbbox())
    return art


def atmosphere(theme, seed):
    top, bottom, accent = PALETTES[theme]
    rgb = v2.vgrad(top, bottom)
    rng = np.random.default_rng(seed)
    v2.noise(rgb, 0.009, seed)
    v2.glow(rgb, 510, 355, 440, 360, np.array(accent) / 255, 0.55)
    im = v2.to_image(rgb)
    ruins = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(ruins)
    # Staggered Hallownest arches, with the main scene visible through them.
    for x, width, height in ((-65, 195, 570), (225, 130, 350), (690, 155, 395), (930, 180, 590)):
        shade = (7, 10, 23, 75) if theme != "green" else (6, 20, 19, 92)
        d.rectangle((x, H - height, x + width, H), fill=shade)
        d.arc((x - 10, H - height - 80, x + width + 10, H - height + 95),
              180, 360, fill=(*accent, 43), width=8)
        d.line((x + 12, H - height, x + 12, H), fill=(*accent, 23), width=5)
    for x in range(-40, W + 40, 110):
        length = int(rng.integers(30, 95))
        d.polygon(((x - 24, 0), (x + 31, 0), (x + 4, length)), fill=(4, 6, 14, 145))
    d.polygon(((0, 690), (180, 657), (340, 681), (540, 650), (760, 675), (1000, 646),
               (1000, 760), (0, 760)), fill=(6, 9, 19, 150))
    ruins = ruins.filter(ImageFilter.GaussianBlur(3))
    im.alpha_composite(ruins)
    # Small distant lights and drifting dust preserve depth at thumbnail size.
    d = ImageDraw.Draw(im)
    for i in range(65):
        x, y = int(rng.integers(25, 975)), int(rng.integers(60, 660))
        r = int(rng.integers(1, 4))
        d.ellipse((x-r, y-r, x+r, y+r), fill=(*accent, int(rng.integers(25, 100))))
    return im, accent


def composite_subject(im, name, center, box, alpha=255, angle=0):
    art = v2.scaled(source(name), *box)
    if angle:
        art = art.rotate(angle, Image.Resampling.BICUBIC, expand=True)
    if alpha != 255:
        art.putalpha(art.getchannel("A").point(lambda a: a * alpha // 255))
    x, y = round(center[0] - art.width / 2), round(center[1] - art.height / 2)
    im.alpha_composite(art, (x, y))


def motion(im, effect, accent, seed):
    rng = np.random.default_rng(seed + 509)
    fx = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(fx)
    if effect in ("nail", "impact", "speed"):
        for i in range(3 if effect != "impact" else 5):
            dx = i * 38
            alpha = 190 - i * 38
            d.arc((320 + dx, 152 + dx // 2, 865 + dx, 650 + dx // 2),
                  190, 316, fill=(*accent, alpha), width=15 - 2 * i)
        if effect == "impact":
            for i in range(18):
                a = math.tau * i / 18 + rng.uniform(-0.12, 0.12)
                r = rng.integers(145, 330)
                x, y = 620 + r * math.cos(a), 405 + r * math.sin(a)
                d.line((620, 405, x, y), fill=(*accent, int(rng.integers(65, 145))), width=4)
        if effect == "speed":
            for i in range(9):
                y = 220 + i * 40
                d.line((30, y, 240 + i % 3 * 45, y - 12), fill=(*accent, 90), width=5)
    elif effect in ("soul", "dream", "void"):
        for i in range(16):
            a = math.tau * i / 16
            r = 160 + 65 * math.sin(i * 2.4)
            x, y = 510 + r * math.cos(a), 390 + r * math.sin(a)
            rr = 8 + i % 4 * 4
            d.ellipse((x-rr, y-rr, x+rr, y+rr), fill=(*accent, 65 + i % 4 * 30))
        for r, a in ((165, 130), (255, 85), (340, 50)):
            d.arc((510-r, 390-r, 510+r, 390+r), 195, 340, fill=(*accent, a), width=5)
    elif effect == "shell":
        for r, a in ((210, 175), (265, 115), (325, 65)):
            d.arc((500-r, 425-r, 500+r, 425+r), 190, 355, fill=(*accent, a), width=20)
    elif effect in ("venom", "swarm", "blood"):
        for i in range(34):
            x, y = rng.integers(90, 940), rng.integers(140, 640)
            rr = rng.integers(3, 18)
            d.ellipse((x-rr, y-rr, x+rr, y+rr), fill=(*accent, int(rng.integers(45, 145))))
        if effect == "blood":
            for i in range(9):
                x = 90 + i * 105
                d.line((x, 710, x + rng.integers(-60, 60), 170), fill=(*accent, 55), width=5)
    im.alpha_composite(fx.filter(ImageFilter.GaussianBlur(1.6)))


def draw_scene(card, spec):
    seed = 800 + sorted(SCENES).index(card) * 11
    im, accent = atmosphere(spec.theme, seed)
    # A close foreground subject and a smaller supporting figure establish the
    # card's story; no empty circles or miniature pictograms as focal points.
    halo = Image.new("RGBA", (W, H))
    ImageDraw.Draw(halo).ellipse((190, 80, 850, 700), fill=(*accent, 90))
    im.alpha_composite(halo.filter(ImageFilter.GaussianBlur(130)))
    if spec.companion:
        composite_subject(im, spec.companion, (770, 480), (330, 370), alpha=160,
                          angle=-17 if "Nail" in spec.companion else 0)
        center, box = (400, 385), (605, 545)
    else:
        center, box = (510, 375), (720, 575)
    if "Nail" in spec.subject:
        box = (335, 600)
    if spec.subject.startswith("Icon_HK") and "_Art" in spec.subject:
        box = (770, 540)
    if spec.subject.startswith("@The_Knight"):
        box = (510, 575)
    composite_subject(im, spec.subject, center, box,
                      angle=-28 if "Nail" in spec.subject else 0)
    motion(im, spec.effect, accent, seed)
    # Floor vignette and light specks tie the disparate official source sprites
    # into the same dark, inked Hallownest scene.
    return v2.finish(np.asarray(im.convert("RGB"), dtype=np.float32) / 255,
                     10, seed, vig=0.34)


def main():
    wanted = set(sys.argv[1:]) or set(SCENES)
    unknown = wanted - set(SCENES)
    if unknown:
        raise SystemExit(f"unknown portraits: {', '.join(sorted(unknown))}")
    for name in sorted(wanted):
        v2.save(draw_scene(name, SCENES[name]), name)
    print(f"v5: {len(wanted)} of {len(SCENES)} weak portraits redrawn")


if __name__ == "__main__":
    main()
