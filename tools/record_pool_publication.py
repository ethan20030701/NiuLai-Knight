"""Record a verified SteamCMD commit and optional server round-trip hashes."""
from pathlib import Path
from datetime import datetime, timezone, timedelta
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
VERSION = 'v0.6.7'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    preflight = json.loads((ROOT / f'release/WORKSHOP_PREFLIGHT_{VERSION}.json').read_text(encoding='utf-8-sig'))
    log = Path('C:/steamcmd/logs/workshop_log.txt').read_text(encoding='utf-8-sig')
    uploaded = re.findall(r'\[([^]]+)\] \[AppID 2868840\] Uploaded new content \( ManifestID (\d+) \) for item 3796063046\.', log)
    finished = re.findall(r'\[([^]]+)\] \[AppID 2868840\] Upload finished for workshop item 3796063046 : OK', log)
    assert uploaded and finished, 'No successful upload found'
    uploaded_at, manifest = uploaded[-1]
    finished_at = finished[-1]
    prepared = datetime.fromisoformat(preflight['prepared_at']).replace(tzinfo=None)
    assert datetime.fromisoformat(uploaded_at) >= prepared, 'Upload is older than this release'
    assert datetime.fromisoformat(finished_at) >= datetime.fromisoformat(uploaded_at)
    assert sha(ROOT / 'release/workshop_upload/HollowKnight_uploader.vdf') == preflight['upload_vdf_sha256']
    for item in preflight['files']:
        for folder in (f'release/candidates/HollowKnight-{VERSION}', 'release/workshop_upload/content', '.build/mods/HollowKnight'):
            assert sha(ROOT / folder / item['name']) == item['sha256']
    proof = dict(preflight, published=True, uploaded_at=finished_at, manifest_id=manifest, upload_result='OK',
                 verified_at=datetime.now(timezone(timedelta(hours=8))).isoformat(),
                 url='https://steamcommunity.com/sharedfiles/filedetails/?id=3796063046', server_download_verified=False)
    download = Path('C:/steamcmd/steamapps/workshop/content/2868840/3796063046')
    acf = Path('C:/steamcmd/steamapps/workshop/appworkshop_2868840.acf')
    if acf.exists() and re.search(r'"manifest"\s+"' + manifest + '"', acf.read_text(encoding='utf-8-sig')):
        if all((download / item['name']).exists() and sha(download / item['name']) == item['sha256'] for item in proof['files']):
            proof['server_download_verified'] = True
            for item in proof['files']:
                item['server_download_match'] = True
    proof['verification'] = ('SteamCMD commit succeeded; all four server-downloaded files match the release by SHA256.'
                              if proof['server_download_verified'] else 'SteamCMD commit succeeded; server download hashes not yet verified.')
    (ROOT / f'release/PUBLISH_VERIFICATION_{VERSION}.json').write_text(json.dumps(proof, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(VERSION, 'upload OK; manifest', manifest, '; server round-trip:', proof['server_download_verified'])


if __name__ == '__main__':
    main()
