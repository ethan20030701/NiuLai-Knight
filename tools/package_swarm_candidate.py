"""Package the locally verified summon-system candidate; never installs or uploads."""
from pathlib import Path
import hashlib
import json
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / '.build/mods/HollowKnight'
VERSION = 'v0.6.7-swarm.1'
STAMP = '2026-10-18'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert read(BUILD / 'HollowKnight.json')['version'] == VERSION
    core = read(ROOT / f'release/SWARM_CORE_{STAMP}.json')
    assert core['passed'] and core['checkCount'] == 50 and core['assertionCount'] == 219
    assert core['assemblySha256'] == sha(BUILD / 'HollowKnight.dll')
    runtime = {}
    for branch in ('stable', 'beta'):
        combat = read(ROOT / f'release/SWARM_RUNTIME_{branch}_{STAMP}.json')
        api = read(ROOT / f'release/SWARM_API_{branch}_{STAMP}.json')
        assert combat['failed'] == 0 and len(combat['results']) == 30
        assert api['missing'] == []
        staged = ROOT / f'.build/runtime-{branch}/mods/HollowKnight'
        for name in ('HollowKnight.dll', 'HollowKnight.pck', 'HollowKnight.json'):
            assert sha(staged / name) == sha(BUILD / name), (branch, name)
        runtime[branch] = {'combat': combat, 'api': api, 'dll_sha256': sha(staged / 'HollowKnight.dll')}
    ui = read(ROOT / 'art_review/swarm_system_ui.json')
    assert ui['failed'] == 0 and len(ui['checks']) == 72
    # Verify actual packed text and texture pixels again before shipping this exact PCK.
    import verify_summon_package
    verify_summon_package.main()
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
        out.write(ROOT / f'docs/SUMMON_SYSTEM_{STAMP}.md', 'SUMMON_SYSTEM.md')
    with zipfile.ZipFile(archive) as check:
        assert check.testzip() is None
        for entry in files:
            assert hashlib.sha256(check.read('HollowKnight/' + entry['name'])).hexdigest() == entry['sha256']
    before = ROOT / '.build/backups/before-swarm-system-20261018.zip'
    changes = []
    with zipfile.ZipFile(before) as old:
        for name in old.namelist():
            path = ROOT / name
            if path.is_file() and old.read(name) != path.read_bytes():
                changes.append({'path': name, 'before_sha256': hashlib.sha256(old.read(name)).hexdigest(), 'after_sha256': sha(path)})
    report = {
        'version': VERSION, 'published': False, 'installed_locally': False,
        'source_baseline': before.relative_to(ROOT).as_posix(),
        'scope': '17 related reward cards: 16 reworked and Brood Chorus retained; no reward-card count increase',
        'files': files, 'archive': archive.relative_to(ROOT).as_posix(), 'archive_sha256': sha(archive),
        'runtime': runtime, 'core': core, 'native_ui': ui,
        'assets': {'original_checksums': 20, 'decoded_textures': 228, 'localization_files': 10, 'passed': True},
        'screenshots': [f'art_review/swarm_system_{page}{suffix}.png' for suffix in ('', '_upgraded') for page in range(1, 5)],
        'manual_visual_review': 'Native Chinese normal/upgrade cards rendered; representative dense text and soul costs inspected.',
        'limitations': ['No full manual run or win-rate measurement', 'No real network multiplayer test', 'No mid-combat save migration test'],
        'changes_since_turn_start': changes,
    }
    report_path = ROOT / f'release/SWARM_SYSTEM_VERIFICATION_{STAMP}.json'
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Verified candidate:', archive)
    print('Report:', report_path)


if __name__ == '__main__':
    main()
