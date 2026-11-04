#!/usr/bin/env python3
"""Download the exact original game assets catalogued from Hollow Knight Wiki.

The manifest preserves page URL, original CDN URL and checksum for each asset.
It deliberately fetches the original file rather than a Wiki thumbnail.
"""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from urllib.parse import unquote
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'tools/wiki_art_raw/summons'
MANIFEST = OUT / 'sources.json'
NAMES = (
    'Weaverling.png', 'Weaverling_resting.png',
    'GrimmchildFamiliar1.png', 'GrimmchildFamiliar2.png',
    'GrimmchildFamiliar3.png', 'GrimmchildFamiliar4.png',
    'Hatchling.png', 'Hatchling_resting.png', 'Dungling.png',
    'Weaversong.png', 'Gathering_Swarm.png', 'Glowing_Womb.png',
    'Grimmchild.png', 'Grubsong.png', 'Kingsoul.png', 'Baldur_Shell.png',
    'Soul_Catcher.png', 'Soul_Eater.png', 'FlameConsumed.png',
    'Screenshot_HK_Deepnest_03.png', 'GTpromo5.png', 'GTpromo4.png',
    'TheGrimmTroupeCoverArt.jpg',
)


def create_manifest():
    catalog = json.loads((ROOT / '.build/wiki_summon_catalog.json').read_text(encoding='utf-8'))
    by_name = {}
    for page, urls in catalog.items():
        for url in urls:
            name = unquote(url.rsplit('/', 1)[-1])
            by_name.setdefault(name, {'file': name, 'page': 'https://hollowknight.wiki/w/' + page, 'url': url})
    missing = set(NAMES) - by_name.keys()
    if missing:
        raise ValueError(f'Missing exact Wiki originals: {missing}')
    return [by_name[name] for name in NAMES]


def fetch(item):
    path = OUT / item['file']
    if not path.exists():
        result = subprocess.run(['curl.exe', '-f', '-s', '-L', '-A', 'Mozilla/5.0', '--max-time', '20', item['url'], '-o', str(path)])
        if result.returncode != 0:
            if item['file'].startswith('GTpromo') or item['file'] == 'TheGrimmTroupeCoverArt.jpg':
                print('optional background unavailable:', item['file'], flush=True)
                return None
            raise RuntimeError(f"Required Wiki original unavailable: {item['file']}")
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    if item.get('sha256') and item['sha256'] != digest:
        raise ValueError(f"Original asset checksum changed: {item['file']}")
    item['sha256'] = digest
    print(item['file'], path.stat().st_size, flush=True)
    return item


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    items = json.loads(MANIFEST.read_text(encoding='utf-8')) if MANIFEST.exists() else create_manifest()
    with ThreadPoolExecutor(max_workers=4) as pool:
        items = [item for item in pool.map(fetch, items) if item is not None]
    MANIFEST.write_text(json.dumps(items, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
