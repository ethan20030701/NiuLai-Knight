#!/usr/bin/env python3
"""Build the Knight's attack body from the cached official idle sprite.

The little gray Nail grip is part of the official idle bitmap. Removing that
grip for the attack phase lets the animated Nail be the only visible weapon.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "tools/wiki_art_raw/The_Knight_Idle.png"
OUTPUT = ROOT / "HollowKnight/images/charui/knight_combat_attack.png"

art = Image.open(SOURCE).convert("RGBA")
mask = Image.new("L", art.size)
# Covers the back-mounted grip and its dark outline, while preserving the
# head and the cloak that begins beneath the grip.
ImageDraw.Draw(mask).polygon(
    [(0, 76), (8, 77), (12, 80), (19, 83), (25, 87), (22, 95), (0, 96)],
    fill=255,
)
alpha = art.getchannel("A")
alpha.paste(0, (0, 0), mask)
art.putalpha(alpha)
art = art.resize((round(art.width * 1.9), round(art.height * 1.9)), Image.Resampling.LANCZOS)
art = art.filter(ImageFilter.UnsharpMask(radius=2, percent=90, threshold=2))
canvas = Image.new("RGBA", (480, 480))
canvas.alpha_composite(art, (240 - art.width // 2, 478 - art.height))
canvas.save(OUTPUT)
print(OUTPUT)
