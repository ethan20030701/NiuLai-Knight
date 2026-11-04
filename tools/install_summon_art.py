#!/usr/bin/env python3
"""Compose the summon art from verified original Hollow Knight Wiki assets.

Character silhouettes are untouched original sprites. Only layout, lighting,
silk trails and SOUL particles are composed locally. Source URLs and checksums
live in wiki_art_raw/summons/sources.json; rejected generated creatures are
preserved locally in archive/summon_art_v063_rejected.
"""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps
from install_generated_art import relic_size

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'tools/wiki_art_raw/summons'
SOURCE = ROOT / 'tools/generated_art_sources/cards'
IMAGES = ROOT / 'HollowKnight/images'
W, H = 1000, 760

# (source file, centre x/y, max width/height). Shapes always come from the Wiki.
SCENES = {
 'weaver_nest': ('silk', [('Weaversong.png', 490, 285, 270, 270), ('Weaverling.png', 440, 540, 230, 200), ('Weaverling_resting.png', 220, 600, 130, 110), ('Weaverling_resting.png', 750, 610, 120, 100)]),
 'grimm_rite': ('flame', [('GrimmchildFamiliar2.png', 510, 390, 340, 340), ('Grimmchild.png', 220, 220, 170, 170), ('FlameConsumed.png', 790, 270, 100, 120), ('FlameConsumed.png', 260, 570, 80, 100)]),
 'glowing_womb': ('soul', [('Glowing_Womb.png', 280, 330, 260, 260), ('Hatchling.png', 610, 360, 240, 210), ('Hatchling_resting.png', 500, 610, 150, 130), ('Hatchling.png', 780, 580, 140, 130)]),
 'swarm_rush': ('rush', [('Knight_Action.png', 410, 460, 340, 400), ('Weaverling.png', 240, 615, 140, 120), ('GrimmchildFamiliar3.png', 660, 300, 220, 230), ('Hatchling.png', 775, 510, 180, 150)]),
 'tend_brood': ('heal', [('The_Knight.png', 500, 420, 250, 400), ('Weaverling_resting.png', 325, 610, 160, 140), ('Hatchling_resting.png', 665, 610, 170, 145), ('Soul_Catcher.png', 760, 260, 120, 140)]),
 'brood_chorus': ('chorus', [('The_Knight.png', 500, 430, 230, 370), ('Weaverling.png', 230, 590, 170, 145), ('GrimmchildFamiliar4.png', 710, 285, 250, 250), ('Hatchling.png', 770, 570, 170, 150)]),
 'weaver_volley': ('volley', [('Weaverling.png', 240, 600, 155, 135), ('Weaverling.png', 490, 470, 190, 165), ('Weaverling.png', 740, 330, 225, 195)]),
 'brood_assault': ('flame', [('GrimmchildFamiliar4.png', 535, 405, 415, 415), ('Weaverling.png', 240, 625, 145, 125), ('Hatchling.png', 810, 600, 125, 110)]),
 'hatchling_burst': ('burst', [('Hatchling.png', 540, 365, 300, 290), ('Hatchling.png', 240, 565, 190, 160), ('Glowing_Womb.png', 805, 590, 130, 130)]),
 'silken_aegis': ('shield', [('Baldur_Shell.png', 500, 395, 365, 365), ('Weaverling_resting.png', 245, 620, 130, 110), ('Weaverling_resting.png', 770, 620, 130, 110)]),
 'brood_offering': ('soul', [('Soul_Eater.png', 510, 350, 300, 300), ('Hatchling_resting.png', 290, 610, 155, 135), ('Weaverling_resting.png', 710, 610, 150, 130)]),
 'swarm_echo': ('rush', [('GrimmchildFamiliar3.png', 740, 370, 310, 310), ('Weaverling.png', 230, 610, 130, 110), ('Weaverling.png', 420, 540, 155, 135), ('Weaverling.png', 590, 455, 180, 155)]),
 'brood_instinct': ('chorus', [('Gathering_Swarm.png', 500, 265, 195, 195), ('Weaverling.png', 230, 575, 170, 150), ('GrimmchildFamiliar3.png', 510, 505, 220, 220), ('Hatchling.png', 770, 585, 180, 155)]),
 'soul_nourishment': ('heal', [('Kingsoul.png', 500, 290, 220, 280), ('Weaverling.png', 250, 610, 165, 145), ('GrimmchildFamiliar2.png', 735, 410, 180, 190), ('Hatchling_resting.png', 600, 615, 165, 140)]),
 'brood_return': ('soul', [('Glowing_Womb.png', 270, 320, 175, 175), ('Hatchling_resting.png', 425, 625, 160, 135), ('Hatchling.png', 605, 410, 235, 210), ('Weaverling_resting.png', 795, 640, 95, 85)]),
}
# Also fill the pre-existing Soul Unbound card's missing portrait.
OTHER_SCENES = {
 'soul_unbound': ('soul', [('The_Knight.png', 500, 420, 250, 400), ('Soul_Eater.png', 245, 315, 160, 160), ('Soul_Catcher.png', 790, 365, 130, 155)]),
}
LABELS = dict(zip(SCENES, ('织者之巢', '格林仪式', '发光子宫', '虫群突袭', '护育虫群', '虫群合唱',
 '织丝连袭', '虫群压境', '舍身孵化', '织壳庇护', '灵魂反哺', '虫群追击', '共生本能', '魂丝哺育', '破茧续生')))
PALETTES = {'silk': (135, 144, 205), 'flame': (230, 62, 64), 'soul': (116, 193, 212),
 'rush': (150, 164, 216), 'heal': (194, 215, 222), 'chorus': (156, 128, 210),
 'volley': (172, 183, 219), 'burst': (193, 222, 233), 'shield': (116, 159, 213)}


def sprite(name):
    path = RAW / name
    if not path.exists():
        path = ROOT / 'tools/wiki_art_raw' / name
    im = Image.open(path).convert('RGBA')
    return im.crop(im.getbbox())


def glow(canvas, x, y, radius, color, alpha=115):
    layer = Image.new('RGBA', canvas.size)
    ImageDraw.Draw(layer).ellipse((x-radius, y-radius, x+radius, y+radius), fill=(*color, alpha))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius * .45)))


def place(canvas, name, x, y, width, height):
    im = sprite(name)
    scale = min(width / im.width, height / im.height)
    im = im.resize((round(im.width * scale), round(im.height * scale)), Image.Resampling.LANCZOS)
    im = ImageEnhance.Sharpness(im).enhance(1.25)
    canvas.alpha_composite(im, (round(x-im.width/2), round(y-im.height/2)))


def effects(canvas, style, seed):
    rng = random.Random(seed)
    color = PALETTES[style]
    layer = Image.new('RGBA', canvas.size)
    d = ImageDraw.Draw(layer)
    if style in ('silk', 'volley', 'shield', 'heal'):
        for i in range(8):
            start = (rng.randrange(-100, 350), rng.randrange(80, 730))
            end = (rng.randrange(690, 1100), rng.randrange(150, 690))
            d.line([start, (500, 400 + i*17), end], fill=(*color, 75), width=2)
    if style in ('rush', 'volley'):
        for i in range(7):
            y = 320 + i*42
            d.line([(60, y+140), (850, y-90)], fill=(*color, 70), width=3)
    if style in ('flame', 'burst'):
        for i in range(38):
            angle = rng.random()*math.tau
            r = rng.randrange(160, 330)
            x, y = 530 + math.cos(angle)*r, 410 + math.sin(angle)*r*.8
            d.line([(x, y), (x+math.cos(angle)*rng.randrange(12, 75), y+math.sin(angle)*40)], fill=(*color, 150), width=rng.randrange(2, 5))
    if style in ('chorus', 'shield'):
        d.ellipse((185, 170, 815, 670), outline=(*color, 140), width=3)
        d.ellipse((175, 160, 825, 680), outline=(*color, 65), width=2)
    for _ in range(38):
        x, y, r = rng.randrange(75, 930), rng.randrange(100, 670), rng.randrange(2, 6)
        d.ellipse((x-r, y-r, x+r, y+r), fill=(*color, rng.randrange(55, 170)))
    canvas.alpha_composite(layer)


def card_scene(name, index):
    style, objects = (SCENES | OTHER_SCENES)[name]
    # The original in-game Weaver's Den screenshot has no HUD or invented figures.
    bg = Image.open(RAW / 'Screenshot_HK_Deepnest_03.png').convert('RGB')
    bg = bg.crop((360, 210, 1840, 1080))
    canvas = ImageOps.fit(bg, (W, H), Image.Resampling.LANCZOS).convert('RGBA')
    canvas.alpha_composite(Image.new('RGBA', (W, H), (9, 12, 26, 68)))
    glow(canvas, 510, 400, 335, PALETTES[style], 95)
    if style == 'chorus':
        glow(canvas, 225, 545, 170, (140, 140, 205), 85)
        glow(canvas, 720, 290, 170, (224, 74, 79), 85)
        glow(canvas, 750, 545, 150, (143, 203, 222), 80)
    effects(canvas, style, index + 637)
    if name == 'soul_unbound':
        glow(canvas, 500, 450, 170, (200, 237, 246), 170)
    for obj in objects:
        place(canvas, *obj)
    return canvas.convert('RGB')


def pet_texture(name):
    source = sprite(name)
    factor = min(164/source.width, 148/source.height)
    source = source.resize((round(source.width*factor), round(source.height*factor)), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (256, 256))
    canvas.alpha_composite(source, ((256-source.width)//2, 220-source.height))
    return canvas


def install_icon(name, source_name, directory, small_size):
    source = sprite(source_name)
    relic_size(source.copy(), small_size, 3).save(IMAGES / directory / f'{name}.png', optimize=True)
    relic_size(source.copy(), 256, 8).save(IMAGES / directory / 'big' / f'{name}.png', optimize=True)
    if directory == 'relics':
        alpha = Image.open(IMAGES / directory / f'{name}.png').getchannel('A')
        outline = Image.new('RGBA', (small_size, small_size), (255, 255, 255, 0))
        outline.putalpha(alpha)
        outline.save(IMAGES / directory / f'{name}_outline.png', optimize=True)


def preview():
    sheet = Image.new('RGB', (1320, 970), (18, 24, 39))
    d = ImageDraw.Draw(sheet)
    font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 20)
    for i, name in enumerate(SCENES):
        x, y = 7 + (i%5)*264, 8 + (i//5)*230
        sheet.paste(Image.open(IMAGES / 'card_portraits' / f'{name}.png'), (x, y))
        d.text((x+3, y+195), LABELS[name], font=font, fill=(225, 230, 240))
    for i, (name, label) in enumerate([('weaverling', '织者幼虫'), ('grimmkin', '格林之子'),
        ('grimmkin_phase3', '成长阶段 3'), ('grimmkin_phase4', '成长阶段 4'), ('hatchling', '孵化小虫')]):
        im = Image.open(IMAGES / 'pets' / f'{name}.png').convert('RGBA')
        sheet.paste(im, (i*264, 695), im)
        d.text((i*264+65, 925), label, font=font, fill=(225, 230, 240))
    out = ROOT / 'art_review/summons_v0.6.3.jpg'
    out.parent.mkdir(exist_ok=True)
    sheet.save(out, quality=95)
    print(out)


def main():
    SOURCE.mkdir(parents=True, exist_ok=True)
    for i, name in enumerate(SCENES | OTHER_SCENES):
        full = card_scene(name, i)
        full.save(SOURCE / f'{name}.png', optimize=True)
        full.save(IMAGES / 'card_portraits/big' / f'{name}.png', optimize=True)
        full.resize((250, 190), Image.Resampling.LANCZOS).save(IMAGES / 'card_portraits' / f'{name}.png', optimize=True)
    for name, raw_name in {'weaverling': 'Weaverling.png', 'grimmkin': 'GrimmchildFamiliar2.png',
        'grimmkin_phase3': 'GrimmchildFamiliar3.png', 'grimmkin_phase4': 'GrimmchildFamiliar4.png',
        'hatchling': 'Hatchling.png'}.items():
        pet_texture(raw_name).save(IMAGES / 'pets' / f'{name}.png', optimize=True)
    for name, raw_name in {'brood_charm': 'Weaversong.png', 'swarm_bell': 'Gathering_Swarm.png'}.items():
        install_icon(name, raw_name, 'relics', 94)
    for name, raw_name in {'swarm_guard_power': 'Weaversong.png', 'grimm_maturity_power': 'Grimmchild.png',
        'brood_instinct_power': 'Gathering_Swarm.png', 'soul_nourishment_power': 'Kingsoul.png'}.items():
        install_icon(name, raw_name, 'powers', 96)
    preview()
    print('installed 15 Wiki-based summon card scenes, 1 missing old portrait, 5 canonical pet textures, 2 relics and 4 powers')


if __name__ == '__main__':
    main()
