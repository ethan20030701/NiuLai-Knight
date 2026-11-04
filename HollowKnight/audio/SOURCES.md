# Knight combat sound sources

All `knight_*.wav` files in this directory are original procedural sound effects
created for this mod by `tools/generate_knight_audio.py`. They contain generated
oscillators and seeded noise only. They do not sample Hollow Knight, Silksong,
the Hornet workshop mod, any voice, or any music recording.

Regenerate from the repository root with:

```text
python tools/generate_knight_audio.py
```

The generator uses Python's standard library, fixed per-sound random seeds,
44.1 kHz mono signed 16-bit PCM, short fades, and a 0.70 peak ceiling. The
generated `manifest.json` records duration, level, seed, and SHA-256 for each
sound. Re-running the generator with the same Python runtime reproduces bytes.

These are non-vocal effects for Nail, SOUL, guard, Focus, damage, Void, and
power activation. The Knight remains silent. This set contains no background
music and does not replace the game's music.

Playback uses the game's existing Godot `SFX` bus (routed to `Master`). The
game updates those buses and FMOD together when its sliders change, so the
mod does not reapply or override their squared gain. Playback also follows
the game's background mute preference and owns one throttled player per
creature visual. Missing SFX bus or settings disables these optional sounds.
