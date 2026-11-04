"""Minimal Godot 4 .pck reader: list files and extract text assets.
Usage: python tools/pck_dump.py <file.pck> [extract_substring out_dir]
"""
import struct, sys, os, hashlib

def read_pck(path):
    with open(path, "rb") as f:
        data = f.read()
    assert data[:4] == b"GDPC", "not a pck"
    off = 4
    fmt_ver, vmaj, vmin, vrev = struct.unpack_from("<IIII", data, off); off += 16
    file_base = 0
    entry_flags_size = 0
    if fmt_ver == 2:
        pack_flags, file_base = struct.unpack_from("<IQ", data, off); off += 12
        entry_flags_size = 4
    count = struct.unpack_from("<I", data, off)[0]; off += 4
    files = []
    for _ in range(count):
        plen = struct.unpack_from("<I", data, off)[0]; off += 4
        p = data[off:off+plen].rstrip(b"\0").decode("utf-8"); off += plen
        o, s = struct.unpack_from("<QQ", data, off); off += 16
        off += 16  # md5
        flags = 0
        if entry_flags_size:
            flags = struct.unpack_from("<I", data, off)[0]; off += 4
        files.append((p, file_base + o, s, flags))
    return fmt_ver, files, data

if __name__ == "__main__":
    pck, = sys.argv[1:2]
    fmt, files, data = read_pck(pck)
    print(f"format={fmt} files={len(files)}")
    if len(sys.argv) >= 4:
        sub, outdir = sys.argv[2], sys.argv[3]
        os.makedirs(outdir, exist_ok=True)
        for p, o, s, fl in files:
            if sub in p:
                dst = os.path.join(outdir, os.path.relpath(p.replace("res://", ""), sub))
                os.makedirs(os.path.dirname(dst) or ".", exist_ok=True)
                with open(dst, "wb") as w:
                    w.write(data[o:o+s])
                print("extracted", p, "->", dst)
    else:
        for p, o, s, fl in files:
            print(f"{s:>9} {p}")
