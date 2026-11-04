#!/usr/bin/env python3
"""Verify the built PCK resolves and contains the current artwork and text.

The quick packer imports PNGs as lossless WebP inside Godot CTEX resources.
Compare decoded visible pixels, not merely resource names in the binary.
"""
from io import BytesIO
from pathlib import Path
import hashlib
import json
import re
from PIL import Image
from install_summon_art import SCENES
from pck_extract import read_table
from card_inventory import card_definitions, portrait_names, retired_portrait_names

ROOT = Path(__file__).resolve().parents[1]
MOD = ROOT / '.build/mods/HollowKnight'


def visible_pixels(image):
    image = image.convert('RGBA')
    # Transparent source RGB can be discarded by a lossless image importer.
    return (image.getchannel('A').tobytes(), Image.alpha_composite(
        Image.new('RGBA', image.size, (0, 0, 0, 255)), image).convert('RGB').tobytes())


def main():
    manifest = ROOT / 'tools/wiki_art_raw/summons/sources.json'
    originals = json.loads(manifest.read_text(encoding='utf-8'))
    for original in originals:
        data = (manifest.parent / original['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == original['sha256'], original['file']
        with Image.open(BytesIO(data)) as image:
            image.verify()
    expected_manifest = json.loads((ROOT / 'HollowKnight.json').read_text(encoding='utf-8-sig'))
    assert json.loads((MOD / 'HollowKnight.json').read_text(encoding='utf-8-sig')) == expected_manifest

    pack = MOD / 'HollowKnight.pck'
    entries, version = read_table(pack)
    lookup = {name.removeprefix('res://'): (offset, size, flags) for name, offset, size, flags in entries}
    assert len(lookup) == len(entries), 'Duplicate packed resource paths'
    with pack.open('rb') as stream:
        def payload(name):
            offset, size, flags = lookup[name.removeprefix('res://')]
            assert flags == 0, (name, flags)
            assert offset + size <= pack.stat().st_size, name
            stream.seek(offset)
            data = stream.read(size)
            assert len(data) == size, name
            return data

        all_portraits = sorted((ROOT / 'HollowKnight/images/card_portraits').glob('*.png'))
        all_portraits = [p for p in all_portraits if p.stem not in {'card', 'v3_preview_sheet'}]
        active_names, retired_names = portrait_names(), retired_portrait_names()
        available = {p.stem for p in all_portraits}
        assert active_names <= available, f'Missing active portraits: {active_names - available}'
        assert available <= active_names | retired_names, f'Unrecognized portrait files: {available - active_names - retired_names}'
        portraits = [p for p in all_portraits if p.stem in active_names]
        historical_portraits = [p for p in all_portraits if p.stem in retired_names]
        # Historical art may stay packed without restoring retired CardModel classes.
        portraits += historical_portraits
        images = portraits + [p.parent / 'big' / p.name for p in portraits]
        images.append(ROOT / 'HollowKnight/images/charui/soul_meter.png')
        images += list((ROOT / 'HollowKnight/images/pets').glob('*.png'))
        assert len(list((ROOT / 'HollowKnight/images/pets').glob('*.png'))) == 5
        for name in ('brood_charm', 'swarm_bell'):
            images += [ROOT / f'HollowKnight/images/relics/{name}.png',
                       ROOT / f'HollowKnight/images/relics/{name}_outline.png',
                       ROOT / f'HollowKnight/images/relics/big/{name}.png']
        for name in ('swarm_guard_power', 'grimm_maturity_power', 'brood_instinct_power', 'soul_nourishment_power'):
            images += [ROOT / f'HollowKnight/images/powers/{name}.png',
                       ROOT / f'HollowKnight/images/powers/big/{name}.png']
        for path in images:
            relative = path.relative_to(ROOT).as_posix()
            if relative in lookup:
                data = payload(relative)
            else:
                remap = payload(relative + '.import')
                assert f'source_file="res://{relative}"'.encode() in remap, relative
                imported = re.search(rb'path="res://([^"]+)"', remap)[1].decode()
                ctex = payload(imported)
                assert ctex[:4] == b'GST2', imported
                start = ctex.find(b'RIFF')
                assert start >= 0 and ctex[start+8:start+12] == b'WEBP', imported
                data = ctex[start:]
            with Image.open(BytesIO(data)) as packed, Image.open(path) as source:
                assert packed.size == source.size, relative
                assert visible_pixels(packed) == visible_pixels(source), f'Packed pixels differ: {relative}'
        text_count = 0
        for language in ('zhs', 'eng'):
            for category in ('cards', 'powers', 'relics', 'monsters', 'static_hover_tips'):
                relative = f'HollowKnight/localization/{language}/{category}.json'
                expected = json.loads((ROOT / relative).read_text(encoding='utf-8-sig'))
                actual = json.loads(payload(relative).decode('utf-8-sig'))
                assert actual == expected, relative
                text_count += 1
    dll = (MOD / 'HollowKnight.dll').read_bytes()
    for card in card_definitions():
        assert card.name.encode() + b'\0' in dll, f'Missing compiled active card: {card.name}'
    for name in ('SwarmGuardPower', 'GrimmMaturityPower', 'SoulNourishmentPower', 'BroodInstinctPower',
                 'WeaverlingPet', 'GrimmkinPet', 'HatchlingPet', 'BroodCharm', 'SwarmBell'):
        assert name.encode() + b'\0' in dll, f'Missing compiled model: {name}'
    for name in SCENES:
        model = ''.join(word.title() for word in name.split('_'))
        assert model.encode() + b'\0' in dll, f'Missing compiled card: {model}'
    print(f'Verified {len(originals)} original asset checksums, {len(images)} packed textures by decoded pixels,')
    print(f'{text_count} packed localization files, 15 compiled summon cards and 9 related models; PCK format {version[0]}.')
    print(f'Active cards/portraits: {len(active_names)}; retained retired portraits: {len(historical_portraits)} (not card models).')


if __name__ == '__main__':
    main()
