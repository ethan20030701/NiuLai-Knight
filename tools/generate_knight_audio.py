#!/usr/bin/env python3
"""Generate the Knight's original, non-vocal combat cues using only the stdlib.

Run from any directory: python tools/generate_knight_audio.py [--out DIRECTORY]
No game, workshop, recorded, or downloaded audio is used as input.
"""

import argparse
import hashlib
import json
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
TAU = math.tau
SOUNDS = {
    "nail": (0.160, 811),
    "soul": (0.300, 829),
    "guard": (0.180, 839),
    "focus": (0.480, 853),
    "hurt": (0.240, 857),
    "void": (0.420, 863),
    "power": (0.340, 877),
}


def synthesize(kind, duration, seed):
    rng = random.Random(seed)
    count = round(duration * RATE)
    samples = []
    fades = []
    low = 0.0
    phase = 0.0
    for i in range(count):
        t = i / RATE
        u = t / duration
        noise = rng.uniform(-1, 1)
        low += (noise - low) * 0.14
        high = noise - low
        if kind == "nail":
            # A quick air sweep with a short, inharmonic shell/metal contact.
            phase += TAU * (1300 - 1050 * u) / RATE
            value = 0.64 * high * math.exp(-23 * t)
            value += 0.17 * math.sin(phase) * math.exp(-42 * t)
            value += 0.12 * math.sin(TAU * 2873 * t) * math.exp(-73 * t)
        elif kind == "soul":
            # Breath-like noise over a descending hollow resonance.
            phase += TAU * (590 - 350 * u) / RATE
            env = math.sin(math.pi * u) ** 0.7 * math.exp(-3 * u)
            value = env * (0.42 * math.sin(phase) + 0.18 * math.sin(phase * 1.503) + 0.7 * low)
        elif kind == "guard":
            value = (0.35 * math.sin(TAU * 640 * t) + 0.16 * math.sin(TAU * 1019 * t)) * math.exp(-40 * t)
            value += 0.3 * low * math.exp(-25 * t)
        elif kind == "focus":
            # A restrained swell, rather than a spoken line or a music phrase.
            phase += TAU * (225 + 130 * u) / RATE
            env = math.sin(math.pi * u) ** 1.7
            value = env * (0.28 * math.sin(phase) + 0.13 * math.sin(phase * 2.002) + 0.45 * low)
        elif kind == "hurt":
            value = (0.38 * low + 0.18 * high) * math.exp(-20 * t)
            value += 0.27 * math.sin(TAU * (150 * t - 90 * t * t)) * math.exp(-32 * t)
            value += 0.10 * math.sin(TAU * 1741 * t) * math.exp(-65 * t)
        elif kind == "void":
            phase += TAU * (120 - 65 * u) / RATE
            env = math.sin(math.pi * u) ** 0.8 * math.exp(-1.8 * u)
            value = env * (0.3 * math.sin(phase) + 0.16 * math.sin(phase * 1.417) + 0.7 * low)
        else:  # power: an airy, short shell resonance; no melodic sequence.
            env = math.sin(math.pi * u) ** 0.6 * math.exp(-3 * u)
            value = env * (0.20 * math.sin(TAU * 382 * t) + 0.13 * math.sin(TAU * 617 * t) + 0.65 * low)
        # Both boundaries are exactly zero; clicks are not used as transients.
        fade = min(1.0, i / (RATE * 0.002), (count - 1 - i) / (RATE * 0.006))
        samples.append(value * max(0.0, fade))
        fades.append(max(0.0, fade))
    # Remove low-level noise bias without disturbing the zero endpoints.
    bias = sum(samples) / sum(fades)
    samples = [value - bias * fade for value, fade in zip(samples, fades)]
    scale = 0.70 / max(abs(v) for v in samples)
    return [round(v * scale * 32767) for v in samples]


def generate(out):
    out.mkdir(parents=True, exist_ok=True)
    manifest = {"generator": "tools/generate_knight_audio.py", "source": "Original procedural synthesis; no input recordings.", "sample_rate": RATE, "channels": 1, "bits_per_sample": 16, "files": []}
    for kind, (duration, seed) in SOUNDS.items():
        samples = synthesize(kind, duration, seed)
        path = out / f"knight_{kind}.wav"
        with wave.open(str(path), "wb") as stream:
            stream.setparams((1, 2, RATE, len(samples), "NONE", "not compressed"))
            stream.writeframes(struct.pack(f"<{len(samples)}h", *samples))
        manifest["files"].append({"file": path.name, "seed": seed, "frames": len(samples), "duration_seconds": len(samples) / RATE, "peak": max(abs(v) for v in samples) / 32768, "rms": math.sqrt(sum(v * v for v in samples) / len(samples)) / 32768, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    (out / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Generated {len(SOUNDS)} original mono PCM sounds in {out}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=Path(__file__).resolve().parents[1] / "HollowKnight" / "audio")
    generate(parser.parse_args().out)
