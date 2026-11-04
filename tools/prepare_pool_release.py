"""Verify the completed pool candidate and stage the authorized v0.6.7 release.

Does not upload or install into the daily game. Existing release files are backed up.
"""
from collections import Counter
from datetime import datetime, timezone, timedelta
from pathlib import Path
import hashlib
import json
import re
import shutil
import zipfile

from audit_card_values import audit
from card_inventory import retirement_manifest

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / '.build/mods/HollowKnight'
VERSION = 'v0.6.7'
STAMP = '2026-11-04'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    active, retired, _, counts, rewards = audit()
    assert (len(active), rewards, len(retired)) == (83, 80, 21)
    assert counts == Counter(Basic=2, Common=22, Uncommon=37, Rare=21, Token=1)
    dll_sha = sha(BUILD / 'HollowKnight.dll')
    core_path = ROOT / 'release/POOL_CORE_2026-10-28.json'
    core = read(core_path)
    assert core['passed'] and core['failedCount'] == 0 and core['assemblySha256'] == dll_sha
    expected_core = set(re.findall(r'\bCheck\("([^"]+)"', (ROOT / 'tools/verification/core/Program.cs').read_text(encoding='utf-8-sig')))
    assert {case['name'] for case in core['checks']} == expected_core
    expected_native = set(re.findall(r'await Check\("([^"]+)"', (ROOT / 'tools/verification/HKAudit.cs').read_text(encoding='utf-8-sig')))
    runtime = {}
    for branch in ('stable', 'beta'):
        native_path = ROOT / f'release/POOL_RUNTIME_{branch}_2026-10-28.json'
        api_path = ROOT / f'release/POOL_API_{branch}_2026-10-28.json'
        native, api = read(native_path), read(api_path)
        assert native['failed'] == 0 and all(case['passed'] for case in native['results'])
        assert {case['name'] for case in native['results']} == expected_native
        assert api['missing'] == []
        staged = ROOT / f'.build/runtime-{branch}/mods/HollowKnight'
        for name in ('HollowKnight.dll', 'HollowKnight.pck', 'HollowKnight.pdb', 'HollowKnight.json'):
            assert sha(staged / name) == sha(BUILD / name), (branch, name)
        runtime[branch] = {'native_report': native_path.relative_to(ROOT).as_posix(),
                           'checks': len(native['results']), 'api_members': api['checkedMembers'],
                           'api_report': api_path.relative_to(ROOT).as_posix(), 'dll_sha256': dll_sha,
                           'game_sha256': sha(ROOT / f'.build/runtime-{branch}/data_sts2_windows_x86_64/sts2.dll')}
    ui_path = ROOT / f'release/POOL_CARD_UI_{STAMP}.json'
    ui = read(ui_path)
    assert ui['failed'] == 0 and all(ui['checks'].values())
    assert ui['active_model_count'] == 83 and len(ui['inventory']) == 19
    assert ui_path.stat().st_mtime >= (BUILD / 'HollowKnight.dll').stat().st_mtime
    assert read(BUILD / 'HollowKnight.json') == read(ROOT / 'HollowKnight.json')
    assert read(ROOT / 'HollowKnight.json')['version'] == 'v0.6.7-pool.1'
    baseline = ROOT / '.build/backups/before-pool-20261028.zip'
    with zipfile.ZipFile(baseline) as old:
        for card in active:
            assert old.read(card.path.relative_to(ROOT).as_posix()) == card.path.read_bytes(), card.name
        for card in retired:
            if card.path.parent.name == 'retired_cards_20261028':
                assert old.read('HollowKnightCode/Cards/' + card.path.name) == card.path.read_bytes()
    description = (ROOT / f'release/WORKSHOP_DESCRIPTION_{VERSION}.txt').read_text(encoding='utf-8-sig').strip()
    changenote = (ROOT / f'release/WORKSHOP_CHANGELOG_{VERSION}.txt').read_text(encoding='utf-8-sig').strip()
    assert VERSION in description and '80' in description and '83' in description
    now = datetime.now(timezone(timedelta(hours=8)))
    backup = ROOT / '.build/release_backups' / ('before-v0.6.7-' + now.strftime('%Y%m%d-%H%M%S'))
    backup.mkdir(parents=True)
    upload = ROOT / 'release/workshop_upload'
    shutil.copytree(upload, backup / 'workshop_upload')
    shutil.copytree(BUILD, backup / 'pool-candidate')
    shutil.copy2(ROOT / 'HollowKnight.json', backup / 'HollowKnight.json')
    candidate = ROOT / f'release/candidates/HollowKnight-{VERSION}'
    candidate.mkdir(parents=True, exist_ok=False)
    manifest = (ROOT / 'HollowKnight.json').read_text(encoding='utf-8-sig').replace('v0.6.7-pool.1', VERSION)
    (ROOT / 'HollowKnight.json').write_text(manifest, encoding='utf-8')
    (BUILD / 'HollowKnight.json').write_text(manifest, encoding='utf-8')
    files = []
    for name in ('HollowKnight.dll', 'HollowKnight.pdb', 'HollowKnight.pck', 'HollowKnight.json'):
        shutil.copy2(BUILD / name, candidate / name)
        shutil.copy2(BUILD / name, upload / 'content' / name)
        assert sha(candidate / name) == sha(BUILD / name) == sha(upload / 'content' / name)
        files.append({'name': name, 'bytes': (BUILD / name).stat().st_size, 'sha256': sha(BUILD / name)})
    assert {p.name for p in (upload / 'content').iterdir()} == {f['name'] for f in files}
    vdf_path = upload / 'HollowKnight_uploader.vdf'
    vdf = vdf_path.read_text(encoding='utf-8-sig')
    assert re.search(r'"publishedfileid"\s+"3796063046"', vdf)
    for key, value in [('description', description), ('changenote', changenote)]:
        assert '"' not in value, 'Use plain text without VDF quote escapes'
        vdf, count = re.subn(r'("' + key + r'"\s*)"[^"]*"', lambda m: m[1] + '"' + value + '"', vdf)
        assert count == 1, key
    vdf_path.write_text(vdf, encoding='utf-8')
    (ROOT / 'release/WORKSHOP_DESCRIPTION_STEAM.txt').write_text(description + '\n', encoding='utf-8')
    report = {'version': VERSION, 'accepted_version': 'v0.6.7-pool.1', 'published': False,
              'appid': '2868840', 'publishedfileid': '3796063046', 'prepared_at': now.isoformat(),
              'backup': backup.relative_to(ROOT).as_posix(), 'files': files,
              'only_manifest_version_changed': True, 'active_cards': 83, 'reward_cards': 80,
              'rarities': dict(counts), 'retired_cards': retirement_manifest(),
              'retained_concrete_sources_unchanged': len(active),
              'core': {'report': core_path.relative_to(ROOT).as_posix(), 'groups': core['checkCount'], 'assertions': core['assertionCount'], 'dll_sha256': dll_sha},
              'runtime': runtime, 'ui': {'report': ui_path.relative_to(ROOT).as_posix(), 'checks': len(ui['checks']), 'soul_cards': 19},
              'assets': {'checked_by': 'tools/verify_summon_package.py', 'textures': 228, 'localization_files': 10},
              'description_sha256': hashlib.sha256((description + '\n').encode()).hexdigest(),
              'upload_vdf_sha256': sha(vdf_path), 'installed_locally': False,
              'limitations': ['No full human playthrough or win-rate measurement', 'No real multiplayer or mid-combat save test',
                              'Native DTO migration covers 21 IDs, base/upgraded, floor history and one Sharp enchantment; not all enchantments or player saves']}
    for name in (f'WORKSHOP_PREFLIGHT_{VERSION}.json', f'POOL_VERIFICATION_{STAMP}.json'):
        (ROOT / 'release' / name).write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Verified and staged', VERSION, '83 active / 80 reward cards;', core['assertionCount'], 'core assertions;', len(ui['checks']), 'UI checks.')


if __name__ == '__main__':
    main()
