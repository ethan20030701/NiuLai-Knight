#!/usr/bin/env python3
"""Preview the six local Nail frames over the corresponding attack body."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
images = root / "HollowKnight/images"
body = Image.open(images / "charui/knight_combat_attack.png").convert("RGBA")
idle = Image.open(images / "charui/knight_combat.png").convert("RGBA")
sheet = Image.new("RGB", (7 * 380, 410), (30, 36, 49))
font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 24)
draw = ImageDraw.Draw(sheet)
for i in range(7):
    scene = Image.new("RGBA", (480, 480), (25, 31, 45))
    scene.alpha_composite(idle if i == 6 else body)
    if i < 6:
        fx = Image.open(images / f"vfx/knight_nail_{i:02}.png").convert("RGBA")
        scene.alpha_composite(fx)
    scene = scene.crop((45, 100, 425, 480))
    sheet.paste(scene.convert("RGB"), (i * 380, 0))
    draw.text((i * 380 + 12, 382), "idle" if i == 6 else f"frame {i}", font=font, fill="white")
path = root / "art_review/combat_nail.jpg"
path.parent.mkdir(exist_ok=True)
sheet.save(path, quality=95)
print(path)
