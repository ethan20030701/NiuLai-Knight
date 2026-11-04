#!/usr/bin/env python3
"""Install the reviewed, locally saved illustrations at game texture sizes.

Original generated PNGs are kept under tools/generated_art_sources. Do this
after gen_cards_v5 and process_wiki_art, since those scripts produce earlier
versions of some targets.
"""
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "tools/generated_art_sources"
IMAGES = ROOT / "HollowKnight/images"


def cover(image, size):
    ratio = size[0] / size[1]
    w, h = image.size
    if w / h > ratio:
        new_w = round(h * ratio)
        image = image.crop(((w - new_w) // 2, 0, (w + new_w) // 2, h))
    else:
        new_h = round(w / ratio)
        image = image.crop((0, (h - new_h) // 2, w, (h + new_h) // 2))
    return image.resize(size, Image.Resampling.LANCZOS)


def relic_size(image, size, margin):
    image = image.crop(image.getbbox())
    image.thumbnail((size - margin * 2, size - margin * 2), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size))
    canvas.alpha_composite(image, ((size - image.width) // 2, (size - image.height) // 2))
    return canvas


def main():
    hero = Image.open(SOURCE / "knight_select_cover.png").convert("RGB")
    cover(hero, (1024, 1152)).save(IMAGES / "charui/knight_hero.png", optimize=True)
    button = cover(hero, (132, 195)).convert("RGBA")
    button.save(IMAGES / "charui/char_select_char_name.png")
    locked = np.asarray(button, dtype=np.float32).copy()
    locked[..., :3] *= 0.29
    Image.fromarray(locked.astype(np.uint8), "RGBA").save(
        IMAGES / "charui/char_select_char_name_locked.png")

    relic = Image.open(SOURCE / "soul_catcher.png").convert("RGBA")
    relic_size(relic, 94, 3).save(IMAGES / "relics/soul_catcher.png")
    relic_size(relic, 256, 8).save(IMAGES / "relics/big/soul_catcher.png")
    outline = relic_size(relic, 94, 3)
    alpha = outline.getchannel("A")
    white = Image.new("RGBA", (94, 94), (255, 255, 255, 0))
    white.putalpha(alpha)
    white.save(IMAGES / "relics/soul_catcher_outline.png")

    card_source = SOURCE / "cards"
    cards = sorted(card_source.glob("*.png")) if card_source.exists() else []
    for source in cards:
        name = source.stem
        full = cover(Image.open(source).convert("RGB"), (1000, 760))
        full.save(IMAGES / "card_portraits/big" / f"{name}.png", optimize=True)
        full.resize((250, 190), Image.Resampling.LANCZOS).save(
            IMAGES / "card_portraits" / f"{name}.png", optimize=True)
    print(f"installed cover, starter relic and {len(cards)} illustrated portraits")


if __name__ == "__main__":
    main()
