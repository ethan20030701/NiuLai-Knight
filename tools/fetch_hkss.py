# -*- coding: utf-8 -*-
"""Slow-paced fetcher for hkss.huijiwiki.com sprites (curl-based).

python urllib is refused by the site's gateway (567/403) even with a browser
UA; curl.exe passes. So: resolve via Special:FilePath with curl, download from
the CDN with curl, pace WIKI-side requests at WIKI_WAIT seconds (measured safe
window 60-90s). CDN downloads are not rate limited.

Usage:
  python fetch_hkss.py <name1> <name2> ... [--force]
Files land in tools/wiki_art_raw/hkss/ (skipped if present unless --force).
"""
import os
import subprocess
import sys
import time
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(REPO, "tools", "wiki_art_raw", "hkss")

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36")
WIKI_WAIT = 92
RETRIES = 3


def curl_bytes(url: str, timeout: int = 60):
    """GET a URL with curl, return (ok, data)."""
    p = subprocess.run(
        ["curl.exe", "-s", "-L", "-A", UA, "--max-time", str(timeout), url],
        capture_output=True,
    )
    return p.returncode == 0, p.stdout


def fetch_one(title: str, force: bool = False) -> str:
    os.makedirs(OUT, exist_ok=True)
    safe = title.replace("文件:", "").replace("File:", "").strip()
    dst = os.path.join(OUT, safe)
    if os.path.exists(dst) and os.path.getsize(dst) > 100 and not force:
        print(f"SKIP (exists): {safe}")
        return "skip"

    import urllib.parse
    url = ("https://hkss.huijiwiki.com/wiki/Special:FilePath/" + urllib.parse.quote(safe))
    last = None
    for attempt in range(1, RETRIES + 1):
        ok, data = curl_bytes(url)
        if ok and data[:8] == b"\x89PNG\r\n\x1a\n" or ok and data[:3] == b"GIF":
            with open(dst, "wb") as f:
                f.write(data)
            print(f"OK   {safe}  ({len(data)} bytes)")
            return "ok"
        if ok:
            last = f"not image ({data[:24]!r})"
        else:
            last = f"curl exit {ok}"
        print(f"ERR  {safe}: {last}")
        if attempt < RETRIES:
            print(f"     retry {attempt + 1} after {WIKI_WAIT}s ...")
            time.sleep(WIKI_WAIT)
    print(f"FAIL {safe}: {last}")
    return "fail"


def main():
    args = [a for a in sys.argv[1:] if a != "--force"]
    force = "--force" in sys.argv[1:]
    if not args:
        print("usage: fetch_hkss.py <name|File:name> ... [--force]")
        sys.exit(1)
    ok = fail = skip = 0
    for i, n in enumerate(args):
        if i > 0:
            time.sleep(WIKI_WAIT)
        r = fetch_one(n, force)
        if r == "ok":
            ok += 1
        elif r == "skip":
            skip += 1
        else:
            fail += 1
    print(f"\ndone: ok={ok} skip={skip} fail={fail}")


if __name__ == "__main__":
    main()
