#!/usr/bin/env python3
"""Build the Knight's charui art set from official Team Cherry renders.

Source images (downloaded from hollowknight.wiki, stored in wiki_art_raw/):
  The_Knight.png    - official press-kit standing art (transparent, 1070x1461)
  Knight_Action.png - official press-kit dash art (transparent, 1359x1407)

Deterministic (fixed seeds). Outputs overwrite the generated versions:
  charui/knight_combat.png            480x480   combat body (in-game idle sprite, faces LEFT)
  charui/knight_merchant.png                    shop body (same idle sprite, feet at image bottom
                                                so NMerchantCharacterFactory plants them on the mat)
  charui/knight_hero.png              1024x1152 select backdrop (standing art)
  charui/character_icon_char_name.png 128       top-bar icon (mask crop)
  charui/character_icon_char_name_outline.png   void-silhouette variant
  charui/map_marker_char_name.png     128       copy of the icon
  charui/char_select_char_name.png    132x195   select button (standing art)
  charui/char_select_char_name_locked 132x195   darkened variant
  mod_image.png                       420x420   mod list banner
  release/workshop_preview.jpg        1920x1080 workshop page banner
"""

import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(REPO, "tools", "wiki_art_raw")
IMG = os.path.join(REPO, "HollowKnight", "images")

STANDING = os.path.join(RAW, "The_Knight.png")
ACTION = os.path.join(RAW, "Knight_Action.png")
IDLE = os.path.join(RAW, "The_Knight_Idle.png")


def upscale_sharp(src, factor):
    """small in-game sprites read mushy after plain LANCZOS; sharpen after"""
    art = src.resize((max(1, round(src.width * factor)), max(1, round(src.height * factor))),
                     Image.LANCZOS)
    return art.filter(ImageFilter.UnsharpMask(radius=2, percent=90, threshold=2))


# ----------------------------------------------------------------- sources
def load_trimmed(path):
    im = Image.open(path).convert("RGBA")
    im = im.crop(im.getbbox())
    return im


def scaled(src, max_w, max_h):
    w, h = src.size
    s = min(max_w / w, max_h / h)
    return src.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)


# ----------------------------------------------------------------- helpers
def vgrad(w, h, top, bot):
    t = np.linspace(0.0, 1.0, h)[:, None, None]
    col = np.array(top)[None, None] * (1 - t) + np.array(bot)[None, None] * t
    return np.broadcast_to(col, (h, w, 3)).copy()


def radial_bg(w, h, inner, outer):
    y, x = np.mgrid[0:h, 0:w]
    d = np.sqrt((x - w / 2) ** 2 + (y - h / 2) ** 2) / (0.5 * np.hypot(w, h))
    d = np.clip(d, 0, 1)[..., None]
    return np.array(outer)[None, None] * d + np.array(inner)[None, None] * (1 - d)


def add_glow(rgb, cx, cy, rx, ry, color, alpha, falloff=2.0):
    h, w = rgb.shape[:2]
    y, x = np.mgrid[0:h, 0:w]
    d = np.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
    a = np.clip(1.0 - d, 0, 1) ** falloff * alpha
    rgb[:] = rgb * (1 - a[..., None]) + np.array(color)[None, None] * a[..., None]


def add_noise(rgb, amount, seed):
    rng = np.random.default_rng(seed)
    rgb[:] += rng.normal(0, amount, rgb.shape[:2])[..., None]


def add_wisps(rgb, n, seed, w, h, r_min=2, r_max=7, a_min=0.12, a_max=0.5,
              tint=(0.82, 0.90, 1.0)):
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:h, 0:w]
    for _ in range(n):
        cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        r = rng.uniform(r_min, r_max)
        a = rng.uniform(a_min, a_max)
        d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / r
        m = np.clip(1.0 - d, 0, 1) ** 1.6
        col = np.array(tint)[None, None] * rng.uniform(0.85, 1.05)
        rgb[:] = rgb * (1 - m[..., None] * a) + col * (m[..., None] * a)


def vignette(rgb, strength=0.45):
    h, w = rgb.shape[:2]
    y, x = np.mgrid[0:h, 0:w]
    d = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2)
    m = (np.clip(d - 0.55, 0, 1) / 0.62) ** 1.5 * strength
    rgb[:] *= (1 - m)[..., None]


def soft_ground_shadow(canvas_rgba, cx, cy, rx, ry, alpha=0.5):
    sh = Image.new("RGBA", canvas_rgba.size, (0, 0, 0, 0))
    pad = int(max(rx, ry) * 3)
    y, x = np.mgrid[-pad:pad + 1, -pad:pad + 1]
    d = np.sqrt((x / rx) ** 2 + (y / ry) ** 2)
    a = (np.clip(1.0 - d, 0, 1) ** 1.8 * alpha * 255).astype(np.uint8)
    blob = Image.fromarray(np.dstack([np.zeros_like(a)] * 3 + [a]), "RGBA")
    sh.alpha_composite(blob, (int(cx - pad), int(cy - pad)))
    sh = sh.filter(ImageFilter.GaussianBlur(max(2, ry * 0.35)))
    canvas_rgba.alpha_composite(sh)


def paste_bottom_center(canvas, art, cx, bottom_y):
    canvas.alpha_composite(art, (int(cx - art.width / 2), int(bottom_y - art.height)))


def to_image(rgb):
    return Image.fromarray(np.clip(rgb * 255 + 0.5, 0, 255).astype(np.uint8), "RGB")


def darken(art, mul=0.32, desat=0.55):
    arr = np.asarray(art).astype(np.float32) / 255.0
    lum = arr[..., :3].mean(axis=2, keepdims=True)
    arr[..., :3] = arr[..., :3] * (1 - desat) + lum * desat
    arr[..., :3] *= mul
    return Image.fromarray(np.clip(arr * 255, 0, 255).astype(np.uint8), "RGBA")


# ----------------------------------------------------------------- pieces
def head_crop():
    """mask + horns of the standing art, trimmed tight"""
    im = load_trimmed(STANDING)
    # head occupies the top ~54% of the art's height; crop generously then trim
    crop = im.crop((0, 0, im.width, round(im.height * 0.545)))
    return crop.crop(crop.getbbox())


def cave_backdrop(w, h, seed=71):
    rgb = vgrad(w, h, (0.055, 0.066, 0.112), (0.012, 0.015, 0.026))
    add_noise(rgb, 0.010, seed)
    rng = np.random.default_rng(seed + 1)
    for _ in range(6):  # faint fog banks
        add_glow(rgb, rng.uniform(0, w), rng.uniform(0, h * 0.65),
                 rng.uniform(w * 0.18, w * 0.4), rng.uniform(h * 0.08, h * 0.2),
                 (0.30, 0.38, 0.58), rng.uniform(0.04, 0.08), falloff=1.6)
    return rgb


def finish_banner(rgb, n_wisps, seed, strength=0.45):
    add_wisps(rgb, n_wisps, seed, rgb.shape[1], rgb.shape[0])
    vignette(rgb, strength)
    return to_image(rgb)


# ----------------------------------------------------------------- outputs
def out_combat():
    # the in-game idle sprite (63x132), facing LEFT — i.e. facing the enemies
    # (the player stands on the right side of the arena). 1.9x keeps him
    # visibly smaller than vanilla characters — he is the LITTLE knight.
    art = upscale_sharp(load_trimmed(IDLE), 1.9)
    cv = Image.new("RGBA", (480, 480), (0, 0, 0, 0))
    paste_bottom_center(cv, art, 240, 478)
    cv.save(os.path.join(IMG, "charui", "knight_combat.png"))
    print("knight_combat.png (idle, faces left)")


def out_hero():
    w, h = 1024, 1152
    rgb = cave_backdrop(w, h)
    # pale soul glow behind the knight's chest
    add_glow(rgb, 512, 660, 430, 430, (0.55, 0.68, 0.92), 0.30)
    hero = to_image(rgb).convert("RGBA")
    art = scaled(load_trimmed(STANDING), 640, 1030)
    soft_ground_shadow(hero, 512, 1102, art.width * 0.40, 26, alpha=0.5)
    paste_bottom_center(hero, art, 512, 1104)
    wisps = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    rng = np.random.default_rng(23)
    yy, xx = np.mgrid[0:h, 0:w]
    for _ in range(38):
        cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        r = rng.uniform(2.5, 8)
        a = rng.uniform(0.10, 0.42)
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / r
        m = np.clip(1.0 - d, 0, 1) ** 1.6
        tint = np.array([0.84, 0.91, 1.0]) * rng.uniform(0.85, 1.05)
        layer = np.dstack([np.full((h, w), tint[0]), np.full((h, w), tint[1]),
                           np.full((h, w), tint[2]), m * a])
        wisps.alpha_composite(Image.fromarray(
            np.clip(layer * 255, 0, 255).astype(np.uint8), "RGBA"))
    hero.alpha_composite(wisps)
    vign = np.asarray(hero).astype(np.float32) / 255.0
    yy2, xx2 = np.mgrid[0:h, 0:w]
    d2 = np.sqrt(((xx2 - w / 2) / (w / 2)) ** 2 + ((yy2 - h / 2) / (h / 2)) ** 2)
    m2 = (np.clip(d2 - 0.55, 0, 1) / 0.62) ** 1.5 * 0.5
    vign = np.dstack([vign[..., :3] * (1 - m2)[..., None], np.clip(vign[..., 3] - m2 * 0, 0, 1)])
    hero = Image.fromarray(np.clip(vign * 255, 0, 255).astype(np.uint8), "RGBA")
    hero.save(os.path.join(IMG, "charui", "knight_hero.png"))
    print("knight_hero.png")


def out_icons():
    head = head_crop()
    for name, dark in [("character_icon_char_name.png", False),
                       ("character_icon_char_name_outline.png", True)]:
        rgb = radial_bg(128, 128,
                        (0.05, 0.058, 0.098) if not dark else (0.012, 0.014, 0.022),
                        (0.020, 0.024, 0.040) if not dark else (0.006, 0.007, 0.012))
        if not dark:
            add_glow(rgb, 64, 70, 62, 62, (0.55, 0.68, 0.92), 0.28)
        base = to_image(rgb).convert("RGBA")
        art = scaled(head, 118, 118)
        if dark:
            art = darken(art, mul=0.30, desat=0.7)
        paste_bottom_center(base, art, 64, 126)
        base.save(os.path.join(IMG, "charui", name))
    # map marker shares the icon art (same as the old generator)
    Image.open(os.path.join(IMG, "charui", "character_icon_char_name.png")).save(
        os.path.join(IMG, "charui", "map_marker_char_name.png"))
    print("character_icon + outline + map_marker")


def out_select_buttons():
    art_full = load_trimmed(STANDING)
    for name, locked in [("char_select_char_name.png", False),
                         ("char_select_char_name_locked.png", True)]:
        w, h = 132, 195
        rgb = (vgrad(w, h, (0.02, 0.022, 0.035), (0.012, 0.013, 0.020)) if locked
               else vgrad(w, h, (0.075, 0.090, 0.125), (0.035, 0.040, 0.070)))
        add_noise(rgb, 0.008, 5)
        if not locked:
            add_glow(rgb, 66, 118, 80, 90, (0.55, 0.68, 0.92), 0.30)
        btn = to_image(rgb).convert("RGBA")
        art = scaled(art_full, 108, 148)
        if locked:
            art = darken(art, mul=0.34, desat=0.65)
        soft_ground_shadow(btn, 66, 182, art.width * 0.38, 7, alpha=0.45)
        paste_bottom_center(btn, art, 66, 182)
        btn.save(os.path.join(IMG, "charui", name))
    print("char_select buttons")


def out_mod_image():
    w = h = 420
    rgb = radial_bg(w, h, (0.090, 0.105, 0.150), (0.030, 0.036, 0.060))
    add_noise(rgb, 0.010, 9)
    add_glow(rgb, 210, 240, 200, 200, (0.55, 0.68, 0.92), 0.30)
    cv = to_image(rgb).convert("RGBA")
    art = scaled(load_trimmed(STANDING), 300, 372)
    soft_ground_shadow(cv, 210, 398, art.width * 0.40, 12, alpha=0.5)
    paste_bottom_center(cv, art, 210, 400)
    wisps = np.asarray(cv.convert("RGB")).astype(np.float32) / 255.0
    add_wisps(wisps, 26, 17, w, h, r_min=1.5, r_max=4.5, a_min=0.10, a_max=0.40)
    vignette(wisps, 0.4)
    Image.fromarray(np.clip(wisps * 255 + 0.5, 0, 255).astype(np.uint8),
                    "RGB").convert("RGBA").save(os.path.join(REPO, "HollowKnight", "mod_image.png"))
    print("mod_image.png")


def out_scene_figures():
    """rest-site (sitting on a bench) and merchant (standing) figures.
    The merchant uses the same idle sprite: NMerchantCharacterFactory anchors
    the texture's bottom edge to the shop-mat anchor, which sits at the mat's
    lower rim — padding ~26% of transparent space BELOW the feet moves the
    knight up onto the middle of the mat."""
    resting = load_trimmed(os.path.join(RAW, "The_Knight_Resting.png"))
    resting = resting.resize((resting.width * 4, resting.height * 4), Image.LANCZOS)
    resting.save(os.path.join(IMG, "charui", "knight_rest_site.png"))
    idle = upscale_sharp(load_trimmed(IDLE), 2.9)
    pad = round(idle.height * 0.26)
    mat = Image.new("RGBA", (idle.width, idle.height + pad), (0, 0, 0, 0))
    mat.paste(idle, (0, 0))
    mat.save(os.path.join(IMG, "charui", "knight_merchant.png"))
    print("knight_rest_site.png + knight_merchant.png (idle, feet lifted onto the mat)")


def out_energy_layers():
    """5 layers for BaseLib's NEnergyCounterFactory (128x128):
    L1 void backing (static), L2/L3 rune rings (rotating), L4 soul core,
    L5 shine. Knight palette: pale blue on deep void."""
    w = h = 128
    y, x = np.mgrid[0:h, 0:w]

    def layer(draw_fn):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        draw_fn(img, ImageDraw.Draw(img))
        return img

    def disc(img, d, r, fill):
        d.ellipse([64 - r, 64 - r, 64 + r, 64 + r], fill=fill)

    def glow_layer(r, color, alpha):
        d = np.sqrt((x - 64) ** 2 + (y - 64) ** 2) / r
        a = (np.clip(1.0 - d, 0, 1) ** 2 * alpha * 255).astype(np.uint8)
        rgb = (np.array(color) * 255).astype(np.uint8)
        return Image.fromarray(np.dstack([np.full((h, w), rgb[0]),
                                          np.full((h, w), rgb[1]),
                                          np.full((h, w), rgb[2]), a]), "RGBA")

    PALE = (184, 212, 255, 235)
    VOID = (13, 16, 28, 255)

    def backing(img, d):
        d.ellipse([64 - 45, 64 - 45, 64 + 45, 64 + 45], fill=(35, 42, 66, 255))
        d.ellipse([64 - 45, 64 - 45, 64 + 45, 64 + 45], outline=VOID, width=3)

    def rune_ring(img, d):
        for k in range(14):
            a = np.radians(360 / 14 * k)
            cx, cy = 64 + 52 * np.cos(a), 64 + 52 * np.sin(a)
            d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=PALE)

    def wisp_ring(img, d):
        for k in range(9):
            a = np.radians(360 / 9 * k + 12)
            cx, cy = 64 + 34 * np.cos(a), 64 + 34 * np.sin(a)
            d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=(230, 240, 255, 200))

    def soul_core(img, d):
        d.ellipse([64 - 30, 64 - 30, 64 + 30, 64 + 30], fill=(108, 140, 200, 120))
        d.ellipse([64 - 26, 64 - 26, 64 + 26, 64 + 26], fill=(235, 243, 255, 255))
        d.ellipse([64 - 16, 64 - 16, 64 + 16, 64 + 16], fill=(255, 255, 255, 255))

    def shine(img, d):
        d.ellipse([38, 30, 74, 52], fill=(255, 255, 255, 150))
        d.ellipse([84, 86, 92, 94], fill=(230, 240, 255, 190))

    names = ["energy_layer_1.png", "energy_layer_2.png", "energy_layer_3.png",
             "energy_layer_4.png", "energy_layer_5.png"]
    draws = [backing, rune_ring, wisp_ring, soul_core, shine]
    for i, (name, fn) in enumerate(zip(names, draws), start=1):
        img = glow_layer(60, (0.45, 0.6, 0.9), 0.35) if i == 4 else Image.new("RGBA", (w, h), (0, 0, 0, 0))
        fn(img, ImageDraw.Draw(img))
        img.save(os.path.join(IMG, "charui", name))
    print("energy layers")


def out_hand_icons():
    """multiplayer rock-paper-scissors hands, knight style (256x256):
    point = bone nail, rock = SOUL orb, paper = the mask, scissors = crossed nails"""
    S = 256
    os.makedirs(os.path.join(IMG, "ui", "hands"), exist_ok=True)
    NAVY = (38, 42, 61, 255)
    BONE = (238, 243, 252, 255)
    GLOW = (150, 190, 245, 90)

    def nail_polygon(cx, cy, angle_deg, length=150, base_w=15, tip_w=4):
        a = np.radians(angle_deg)
        d = np.array([np.cos(a), -np.sin(a)])
        p = np.array([-d[1], d[0]])
        base = np.array([cx, cy]) - d * (length * 0.42)
        tip = base + d * length
        pts = [tuple(base + p * base_w), tuple(tip + p * tip_w),
               tuple(tip - p * tip_w), tuple(base - p * base_w)]
        return pts

    def draw_nail(d, cx, cy, angle):
        a = np.radians(angle)
        dvec = np.array([np.cos(a), -np.sin(a)])
        pvec = np.array([-dvec[1], dvec[0]])
        bx, by = cx - dvec[0] * 66, cy - dvec[1] * 66  # hilt centre
        for widen, col in [(9, GLOW), (5, NAVY), (0, BONE)]:
            d.polygon(nail_polygon(cx, cy, angle, base_w=15 + widen, tip_w=4 + widen), fill=col)
            gx, gy = pvec * (26 + widen)
            d.line([bx - gx, by - gy, bx + gx, by + gy], fill=col, width=12 + widen * 2)
            r = 10 + widen
            d.ellipse([bx - dvec[0] * 20 - r, by - dvec[1] * 20 - r,
                       bx - dvec[0] * 20 + r, by - dvec[1] * 20 + r], fill=col)

    def new_icon():
        return Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # point: nail thrusting up-right
    icon = new_icon()
    draw_nail(ImageDraw.Draw(icon), 118, 138, 45)
    icon.save(os.path.join(IMG, "ui", "hands", "multiplayer_hand_ironclad_point.png"))

    # rock: the SOUL orb
    icon = new_icon()
    yy, xx = np.mgrid[0:S, 0:S]
    dist = np.sqrt((xx - 128) ** 2 + (yy - 128) ** 2) / 95
    a = (np.clip(1 - dist, 0, 1) ** 1.8 * 160).astype(np.uint8)
    glow = np.zeros((S, S, 4), dtype=np.uint8)
    glow[..., 0], glow[..., 1], glow[..., 2], glow[..., 3] = 150, 190, 245, a
    icon.alpha_composite(Image.fromarray(glow, "RGBA"))
    d = ImageDraw.Draw(icon)
    d.ellipse([52, 52, 204, 204], fill=(108, 140, 200, 160))
    d.ellipse([58, 58, 198, 198], fill=(235, 243, 255, 255))
    d.ellipse([74, 74, 182, 182], fill=(255, 255, 255, 255))
    icon.save(os.path.join(IMG, "ui", "hands", "multiplayer_hand_ironclad_rock.png"))

    # paper: the mask itself, flat
    icon = new_icon()
    head = scaled(head_crop(), 190, 190)
    icon.alpha_composite(head, ((S - head.width) // 2, (S - head.height) // 2))
    icon.save(os.path.join(IMG, "ui", "hands", "multiplayer_hand_ironclad_paper.png"))

    # scissors: two crossed nails
    icon = new_icon()
    dd = ImageDraw.Draw(icon)
    draw_nail(dd, 128, 118, 65)
    draw_nail(dd, 128, 118, 115)
    icon.save(os.path.join(IMG, "ui", "hands", "multiplayer_hand_ironclad_scissors.png"))
    print("hand icons")


def out_workshop_banner():
    w, h = 1920, 1080
    rgb = cave_backdrop(w, h, seed=71)
    # SOUL orb on the left, echoing the old hand-drawn banner
    add_glow(rgb, 470, 400, 330, 330, (0.62, 0.78, 1.0), 0.75, falloff=2.4)
    y, x = np.mgrid[0:h, 0:w]
    for cx, cy, r, a in [(470, 400, 96, 0.97), (415, 348, 42, 0.92)]:
        d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / r
        m = np.clip(1.0 - d, 0, 1) ** 1.2 * a
        col = np.where(d[..., None] < 1, np.array([1.0, 1.0, 1.0]), np.array([0.85, 0.92, 1.0]))
        rgb[:] = rgb * (1 - m[..., None]) + col * m[..., None]
    rng = np.random.default_rng(31)
    for i in range(6):
        a_ = np.radians(40 + i * 60)
        rr = 190 - (i % 2) * 40
        add_glow(rgb, 470 + np.cos(a_) * rr, 400 + np.sin(a_) * rr * 0.55,
                 26 - (i % 3) * 5, 26 - (i % 3) * 5, (0.85, 0.92, 1.0),
                 0.85 - (i % 3) * 0.15)
    art = scaled(load_trimmed(STANDING), 620, 900)
    banner = to_image(rgb).convert("RGBA")
    soft_ground_shadow(banner, 1390, 1000, art.width * 0.38, 20, alpha=0.5)
    paste_bottom_center(banner, art, 1390, 1002)
    banner = np.asarray(banner.convert("RGB")).astype(np.float32) / 255.0
    add_wisps(banner, 46, 73, w, h)
    vignette(banner, 0.5)
    out = Image.fromarray(np.clip(banner * 255 + 0.5, 0, 255).astype(np.uint8), "RGB")
    path = os.path.join(REPO, "release", "workshop_preview.jpg")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out.save(path, quality=92)
    print("workshop_preview.jpg")


def main():
    out_combat()
    out_hero()
    out_icons()
    out_select_buttons()
    out_mod_image()
    out_scene_figures()
    out_energy_layers()
    out_hand_icons()
    out_workshop_banner()
    print("done")


if __name__ == "__main__":
    main()
