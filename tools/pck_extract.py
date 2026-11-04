#!/usr/bin/env python3
"""Minimal Godot 4 .pck reader: list / extract files from a game pack.

Usage:
  python tools/pck_extract.py <pack.pck> --list <substring-filter>
  python tools/pck_extract.py <pack.pck> --extract <substring-filter> --out <dir>
"""

import os
import struct
import sys


def read_table(path):
    f = open(path, "rb")
    magic = f.read(4)
    assert magic == b"GDPC", f"not a pck: {magic}"
    ver, major, minor, patch = struct.unpack("<4i", f.read(16))
    flags = base = 0
    if ver >= 2:
        flags, base = struct.unpack("<iq", f.read(12))
    assert ver in (1, 2, 3), f"unsupported pck format: {ver}"
    assert not flags & 1, "encrypted directory is unsupported"
    if ver == 3:
        # Godot 4.5 places the V3 directory at its own header offset.
        # https://github.com/godotengine/godot/blob/4.5/core/io/file_access_pack.cpp
        (directory_offset,) = struct.unpack("<Q", f.read(8))
        assert directory_offset < os.path.getsize(path), "invalid directory offset"
        f.seek(directory_offset)
    else:
        f.read(16 * 4)  # reserved
    count = struct.unpack("<i", f.read(4))[0]
    assert 0 < count < 1000000, f"invalid file count: {count}"
    entries = []
    for _ in range(count):
        (plen,) = struct.unpack("<i", f.read(4))
        p = f.read(plen).rstrip(b"\0").decode("utf-8", "replace")
        if ver >= 2:
            off, size = struct.unpack("<2q", f.read(16))
            md5 = f.read(16)
            (fileflags,) = struct.unpack("<i", f.read(4))
        else:
            off, size = struct.unpack("<2i", f.read(8))
            md5 = f.read(16)
            fileflags = 0
        entries.append((p, base + off, size, fileflags))
    f.close()
    return entries, (ver, major, minor, patch, flags)


def main():
    pack = sys.argv[1]
    mode = sys.argv[2]
    needle = sys.argv[3]
    entries, meta = read_table(pack)
    print(f"pack format {meta}", file=sys.stderr)
    matches = [e for e in entries if needle.lower() in e[0].lower()]
    print(f"{len(matches)} / {len(entries)} entries match", file=sys.stderr)
    if mode == "--list":
        for p, _, size, fl in matches:
            print(f"{size:>10}  fl={fl}  {p}")
        return
    out = sys.argv[5] if len(sys.argv) > 5 else "pck_out"
    f = open(pack, "rb")
    for p, off, size, fl in matches:
        rel = p.replace("res://", "")
        dest = os.path.join(out, rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        f.seek(off)
        blob = f.read(size)
        if fl:
            raise ValueError(f"encrypted or removal entry is unsupported: {p} (flags={fl})")
        open(dest, "wb").write(blob)
        print(f"extracted {p} -> {dest} ({size} bytes, flags={fl})")


if __name__ == "__main__":
    main()
