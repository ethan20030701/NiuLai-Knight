"""Verify and package the local Hornet-study candidate; never install/upload."""
from io import BytesIO
from pathlib import Path
import hashlib
import json
import shutil
import wave
import zipfile

from pck_extract import read_table
import verify_summon_package

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / '.build/mods/HollowKnight'
VERSION = 'v0.6.7-hornet-study.1'
STAMP = '2026-10-23'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert read(BUILD / 'HollowKnight.json') == read(ROOT / 'HollowKnight.json')
    assert read(BUILD / 'HollowKnight.json')['version'] == VERSION
    core = read(ROOT / f'release/HORNET_STUDY_CORE_{STAMP}.json')
    assert core['passed'] and core['checkCount'] == 53 and core['assertionCount'] == 261
    assert core['assemblySha256'] == sha(BUILD / 'HollowKnight.dll')
    runtime = {}
    for branch in ('stable', 'beta'):
        combat = read(ROOT / f'release/HORNET_STUDY_RUNTIME_{branch}_{STAMP}.json')
        api = read(ROOT / f'release/HORNET_STUDY_API_{branch}_{STAMP}.json')
        assert combat['failed'] == 0 and len(combat['results']) == 32
        assert api['missing'] == [] and api['checkedMembers'] >= 308
        staged = ROOT / f'.build/runtime-{branch}/mods/HollowKnight'
        for name in ('HollowKnight.dll', 'HollowKnight.pck', 'HollowKnight.json'):
            assert sha(staged / name) == sha(BUILD / name), (branch, name)
        runtime[branch] = {'combat': combat, 'api': api, 'dll_sha256': sha(staged / 'HollowKnight.dll')}
    media = read(ROOT / 'art_review/hornet_study_media_ui.json')
    assert media['failed'] == 0 and len(media['checks']) >= 44 and all(media['checks'].values())
    assert media['checks']['death.replay_hides_living_body']
    verify_summon_package.main()
    pack = BUILD / 'HollowKnight.pck'
    entries, _ = read_table(pack)
    lookup = {name.removeprefix('res://'): (offset, size, flags) for name, offset, size, flags in entries}
    audio = []
    with pack.open('rb') as stream:
        for entry in read(ROOT / 'HollowKnight/audio/manifest.json')['files']:
            path = ROOT / 'HollowKnight/audio' / entry['file']
            offset, size, flags = lookup[path.relative_to(ROOT).as_posix()]
            assert flags == 0
            stream.seek(offset)
            data = stream.read(size)
            assert data == path.read_bytes()
            assert hashlib.sha256(data).hexdigest() == entry['sha256']
            with wave.open(BytesIO(data)) as wav:
                assert wav.getnchannels() == 1 and wav.getframerate() == 44100 and wav.getsampwidth() == 2
                assert wav.getnframes() == entry['frames']
            audio.append({'file': entry['file'], 'bytes': size, 'sha256': entry['sha256']})
    assert len(audio) == 7
    target = ROOT / 'release/candidates' / f'HollowKnight-{VERSION}'
    target.mkdir(parents=True, exist_ok=False)
    files = []
    for name in ('HollowKnight.dll', 'HollowKnight.pdb', 'HollowKnight.pck', 'HollowKnight.json'):
        shutil.copy2(BUILD / name, target / name)
        assert sha(target / name) == sha(BUILD / name)
        files.append({'name': name, 'bytes': (target / name).stat().st_size, 'sha256': sha(target / name)})
    archive = target.parent / (target.name + '.zip')
    docs = ['HORNET_ADAPTATION_2026-10-23.md', 'HORNET_CARD_STUDY_2026-10-23.md',
            'HORNET_MEDIA_STUDY_2026-10-23.md', 'CARD_RELIC_CATALOG.md']
    with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED, compresslevel=6) as out:
        for entry in files:
            out.write(target / entry['name'], 'HollowKnight/' + entry['name'])
        for name in docs:
            out.write(ROOT / 'docs' / name, name)
        out.write(ROOT / 'HollowKnight/audio/SOURCES.md', 'AUDIO_SOURCES.md')
    with zipfile.ZipFile(archive) as check:
        assert check.testzip() is None
        for entry in files:
            assert hashlib.sha256(check.read('HollowKnight/' + entry['name'])).hexdigest() == entry['sha256']
    before = ROOT / '.build/backups/before-hornet-study-20261023.zip'
    changes = []
    with zipfile.ZipFile(before) as old:
        old_paths = set(old.namelist())
        for name in sorted(old_paths):
            path = ROOT / name
            if path.is_file() and old.read(name) != path.read_bytes():
                changes.append({'path': name, 'before_sha256': hashlib.sha256(old.read(name)).hexdigest(), 'after_sha256': sha(path)})
        current = [*ROOT.glob('HollowKnightCode/**/*.cs'), *ROOT.glob('HollowKnight/audio/**/*')]
        added = [p.relative_to(ROOT).as_posix() for p in current if p.is_file() and p.relative_to(ROOT).as_posix() not in old_paths]
    report = {
        'version': VERSION, 'published': False, 'installed_locally': False,
        'source_baseline': before.relative_to(ROOT).as_posix(),
        'scope': '10-card starter, three reward-card revisions, seven original SFX, six procedural VFX styles; retains prior swarm candidate',
        'files': files, 'archive': archive.relative_to(ROOT).as_posix(), 'archive_sha256': sha(archive),
        'runtime': runtime, 'core': core, 'native_media_ui': media,
        'assets': {'decoded_texture_checks': 228, 'localization_files': 10, 'audio': audio, 'passed': True},
        'screenshots': ['art_review/hornet_study.png', 'art_review/hornet_study_upgraded.png'],
        'manual_visual_review': 'Native Chinese normal and upgraded cards inspected; descriptions fit, 33-SOUL badge and 66-SOUL condition legible.',
        'limitations': ['No full human run/win-rate measurement', 'No human listening or live volume/focus UI test',
                        'No real network multiplayer or mid-combat save migration test',
                        'Muted audio lifecycle fixture forces the settings gate open; does not prove live settings behavior'],
        'changes_since_turn_start': changes, 'added_business_files': added,
    }
    report_path = ROOT / f'release/HORNET_STUDY_VERIFICATION_{STAMP}.json'
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Verified local candidate:', archive)
    print('Report:', report_path)


if __name__ == '__main__':
    main()
