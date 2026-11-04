"""Package the locally verified cleanup candidate without installing/uploading."""
from pathlib import Path
from collections import Counter
import hashlib
import json
import re
import shutil
import zipfile

from card_inventory import active_cards, retired_cards
from pck_extract import read_table
import verify_summon_package

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / '.build/mods/HollowKnight'
VERSION = 'v0.6.7-cleanup.1'
STAMP = '2026-10-24'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert read(BUILD / 'HollowKnight.json') == read(ROOT / 'HollowKnight.json')
    assert read(BUILD / 'HollowKnight.json')['version'] == VERSION
    active, retired = active_cards(), retired_cards()
    counts = Counter(card.rarity for card in active)
    assert len(active) == 101 and sum(card.is_reward for card in active) == 98 and len(retired) == 3
    core = read(ROOT / f'release/CLEANUP_CORE_{STAMP}.json')
    expected_core = set(re.findall(r'\bCheck\("([^"]+)"', (ROOT / 'tools/verification/core/Program.cs').read_text(encoding='utf-8-sig')))
    assert core['passed'] and core['failedCount'] == 0
    assert {case['name'] for case in core['checks']} == expected_core
    assert core['assemblySha256'] == sha(BUILD / 'HollowKnight.dll')
    expected_native = set(re.findall(r'await Check\("([^"]+)"', (ROOT / 'tools/verification/HKAudit.cs').read_text(encoding='utf-8-sig')))
    runtime = {}
    for branch in ('stable', 'beta'):
        combat = read(ROOT / f'release/CLEANUP_RUNTIME_{branch}_{STAMP}.json')
        api = read(ROOT / f'release/CLEANUP_API_{branch}_{STAMP}.json')
        assert combat['failed'] == 0 and all(case['passed'] for case in combat['results'])
        assert {case['name'] for case in combat['results']} == expected_native
        assert api['missing'] == []
        staged = ROOT / f'.build/runtime-{branch}/mods/HollowKnight'
        for name in ('HollowKnight.dll', 'HollowKnight.pck', 'HollowKnight.json'):
            assert sha(staged / name) == sha(BUILD / name), (branch, name)
        runtime[branch] = {'combat': combat, 'api': api, 'dll_sha256': sha(staged / 'HollowKnight.dll')}
    media = read(ROOT / 'art_review/cleanup_media_ui.json')
    assert (ROOT / 'art_review/cleanup_media_ui.json').stat().st_mtime >= (BUILD / 'HollowKnight.dll').stat().st_mtime
    assert media['failed'] == 0 and all(media['checks'].values())
    for key in ('counter.ui_failure_preserves_real_payment', 'counter.ui_failure_does_not_break_later_gain',
                'death.replay_hides_living_body', 'audio.one_player_per_owner'):
        assert media['checks'][key], key
    hud = read(ROOT / f'release/CLEANUP_HUD_{STAMP}.json')
    assert hud['failed'] == 0 and all(hud['checks'].values())
    card_ui = read(ROOT / f'release/CLEANUP_CARD_UI_{STAMP}.json')
    for name in (f'CLEANUP_HUD_{STAMP}.json', f'CLEANUP_CARD_UI_{STAMP}.json'):
        assert (ROOT / 'release' / name).stat().st_mtime >= (BUILD / 'HollowKnight.dll').stat().st_mtime
    assert card_ui['failed'] == 0 and all(card_ui['checks'].values())
    assert len(card_ui['inventory']) == 19
    assert card_ui['active_model_count'] == 101
    assert {row['card'] for row in card_ui['inventory']} == set(card_ui['expected_soul_cards'])
    verify_summon_package.main()
    entries, _ = read_table(BUILD / 'HollowKnight.pck')
    lookup = {name.removeprefix('res://'): (offset, size, flags) for name, offset, size, flags in entries}
    audio = []
    with (BUILD / 'HollowKnight.pck').open('rb') as pack:
        for entry in read(ROOT / 'HollowKnight/audio/manifest.json')['files']:
            relative = 'HollowKnight/audio/' + entry['file']
            offset, size, flags = lookup[relative]
            assert flags == 0
            pack.seek(offset)
            data = pack.read(size)
            assert data == (ROOT / relative).read_bytes()
            assert hashlib.sha256(data).hexdigest() == entry['sha256']
            audio.append({'name': relative, 'sha256': entry['sha256'], 'bytes': size})
    baseline = ROOT / '.build/backups/before-cleanup-20261024.zip'
    changes = []
    added = []
    with zipfile.ZipFile(baseline) as old:
        retained_sources = [f'HollowKnightCode/Cards/{name}.cs' for name in ('VengefulSpirit', 'QuickSlash', 'AbyssShriek')]
        for name in retained_sources:
            assert old.read(name) == (ROOT / name).read_bytes(), name
        new_files = [
            'HollowKnightCode/Compatibility/RetiredCards.cs',
            'HollowKnightCode/Compatibility/PatchInstaller.cs',
            'HollowKnightCode/Patches/RetiredCardSavePatch.cs',
            'tools/card_inventory.py', 'tools/package_cleanup_candidate.py',
            'tools/verification/PatchInstallerChecks.cs', 'docs/CLEANUP_2026-10-24.md',
            'archive/retired_cards_20261024/SoulBolt.cs',
            'archive/retired_cards_20261024/hum.cs',
            'archive/retired_cards_20261024/HallownestsEcho.cs',
            'archive/retired_cards_20261024/retired_cards.json',
        ]
        for name in new_files:
            assert name not in old.namelist(), name
            added.append({'path': name, 'sha256': sha(ROOT / name)})
        for name in old.namelist():
            path = ROOT / name
            if not path.exists():
                changes.append({'path': name, 'status': 'archived or removed from active sources'})
            elif old.read(name) != path.read_bytes():
                changes.append({'path': name, 'before_sha256': hashlib.sha256(old.read(name)).hexdigest(), 'after_sha256': sha(path)})
    target = ROOT / 'release/candidates' / f'HollowKnight-{VERSION}'
    target.mkdir(parents=True, exist_ok=False)
    files = []
    for name in ('HollowKnight.dll', 'HollowKnight.pdb', 'HollowKnight.pck', 'HollowKnight.json'):
        shutil.copy2(BUILD / name, target / name)
        assert sha(target / name) == sha(BUILD / name)
        files.append({'name': name, 'bytes': (target / name).stat().st_size, 'sha256': sha(target / name)})
    archive = target.parent / (target.name + '.zip')
    with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED, compresslevel=6) as out:
        for entry in files:
            out.write(target / entry['name'], 'HollowKnight/' + entry['name'])
        for name in ('CLEANUP_2026-10-24.md', 'CARD_RELIC_CATALOG.md'):
            out.write(ROOT / 'docs' / name, name)
        out.write(ROOT / 'HollowKnight/audio/SOURCES.md', 'AUDIO_SOURCES.md')
    with zipfile.ZipFile(archive) as check:
        assert check.testzip() is None
        for entry in files:
            assert hashlib.sha256(check.read('HollowKnight/' + entry['name'])).hexdigest() == entry['sha256']
    report = {
        'version': VERSION, 'published': False, 'installed_locally': False,
        'active_cards': len(active), 'reward_cards': sum(card.is_reward for card in active), 'rarities': dict(counts),
        'retired_cards': read(ROOT / 'archive/retired_cards_20261024/retired_cards.json'),
        'source_baseline': baseline.relative_to(ROOT).as_posix(), 'existing_files_changed_since_baseline': changes,
        'new_cleanup_files': added,
        'retained_card_sources_unchanged': retained_sources,
        'files': files, 'archive': archive.relative_to(ROOT).as_posix(), 'archive_sha256': sha(archive),
        'core': core, 'runtime': runtime, 'native_media_ui': media, 'native_hud': hud, 'native_card_ui': card_ui,
        'assets': {'textures': 228, 'localization_files': 10, 'audio': audio, 'passed': True},
        'screenshots': ['art_review/cleanup_cards.png', 'art_review/cleanup_cards_upgraded.png'],
        'review': 'Chinese six-card base/upgrade layouts inspected; retained card values, SOUL badges and bank threshold visible.',
        'limitations': ['No full human run or win-rate measurement', 'No actual player saves edited or loaded',
                        'Native serialized-card migration covers upgrades, deck history and one real Sharp enchantment, not every save property/enchantment',
                        'No human listening/live audio settings UI test; muted lifecycle fixture forces settings gate',
                        'No real network multiplayer or mid-combat save migration test'],
    }
    destination = ROOT / f'release/CLEANUP_VERIFICATION_{STAMP}.json'
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Verified local candidate:', archive)
    print('Verification:', destination)


if __name__ == '__main__':
    main()
