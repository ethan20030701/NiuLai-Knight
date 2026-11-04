"""Encode actual Godot renderer captures for reviewing the combat animation.

This does not draw or alter character art. Run the native --knight-visual-preview
first; these screenshots are the exact cutout rig used by the mod.
"""
from pathlib import Path
import json
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / 'art_review/combat_rig_frames'
proof = json.loads((folder / 'verification.json').read_text())
assert proof['rendered_frames'] == 31 and proof['feet_grounded'] and proof['nail_attached_to_hand']
frames = []
for i in range(31):
    with Image.open(folder / f'frame-{i:02}.png') as source:
        frame = source.convert('RGB')
    assert frame.size == (720, 420)
    assert len(frame.getcolors(720 * 420)) > 500, 'Renderer produced an empty scene'
    frames.append(frame)
contact_index = 3 + round(proof['contact_seconds'] * 60)
assert frames[0].tobytes() != frames[contact_index].tobytes()
for name, ms in [('combat_rig.gif', 20), ('combat_rig_slow.gif', 60)]:
    duration = [ms] * len(frames)
    duration[0], duration[-1] = 550, 700
    frames[0].save(root / 'art_review' / name, save_all=True, append_images=frames[1:], duration=duration, loop=0, disposal=2, optimize=True)
frames[contact_index].save(root / 'art_review/combat_rig_contact.png')
print('Encoded actual engine preview: normal GIF, slow GIF and contact frame.')
