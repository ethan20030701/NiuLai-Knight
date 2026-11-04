#!/usr/bin/env python3
"""Build labelled contact sheets for visual review of the shipped card art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from card_inventory import portrait_names, retired_portrait_names

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "HollowKnight/images/card_portraits"
OUT = ROOT / "art_review"
OUT.mkdir(exist_ok=True)
available = {p.stem for p in ART.glob("*.png") if p.stem not in {"card", "v3_preview_sheet"}}
expected, retired = portrait_names(), retired_portrait_names()
assert expected <= available, f"missing active portraits: {expected - available}"
assert available <= expected | retired, f"unrecognized extra portraits: {available - expected - retired}"
names = sorted(expected)
source_names = {p.stem for p in (ROOT / "tools/generated_art_sources/cards").glob("*.png")}
assert expected <= source_names, f"missing active source art: {expected - source_names}"
assert source_names <= expected | retired, f"unrecognized extra source art: {source_names - expected - retired}"
for name in names:
    with Image.open(ART / f"{name}.png") as small, Image.open(ART / "big" / f"{name}.png") as big:
        assert small.size == (250, 190), f"{name} small: {small.size}"
        assert big.size == (1000, 760), f"{name} big: {big.size}"
font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)
for page in range((len(names) + 23) // 24):
    current = names[page * 24:(page + 1) * 24]
    sheet = Image.new("RGB", (6 * 262, 4 * 225), (27, 31, 44))
    draw = ImageDraw.Draw(sheet)
    for pos, name in enumerate(current):
        x, y = pos % 6 * 262 + 6, pos // 6 * 225 + 4
        image = Image.open(ART / (name + ".png")).convert("RGB")
        sheet.paste(image, (x, y))
        draw.text((x, y + 192), name, font=font, fill=(235, 240, 250))
    path = OUT / f"cards_{page + 1}.jpg"
    sheet.save(path, quality=92)
    print(path)
print(f"verified {len(names)} active source, small and big card portraits")
print(f"retained historical portraits (excluded from contact sheets): {', '.join(sorted(available & retired)) or 'none'}")
