# HollowKnight project backup

Snapshot prepared on 2026-11-04 for the HollowKnight Slay the Spire 2 mod, version v0.6.7.

## Contents

- `HollowKnightCode/`: C# source.
- `HollowKnight/`: Godot assets, audio and localization.
- `tools/`, `docs/`, `art_review/`: project tooling, documentation and visual reviews.
- `release/`: published packages and verification records, excluding local candidate duplicates.
- `archive/`, `mods_disabled/`: retained earlier project artifacts, excluding the local migration bundle directory.

## Restore

Clone or download this repository. Compare restored files with `BACKUP_MANIFEST.json` using SHA-256.
For development, provide your own installed Slay the Spire 2, BaseLib, .NET 9 and matching Godot/MegaDot 4.5.1 environment.
Copy `Directory.Build.props.example` to `Directory.Build.props` and adapt paths to your machine before building.
Published mod files are under `release/workshop_upload/content/`.

## Backup boundary

This is a snapshot of current project files, including uncommitted changes and untracked project assets.
It does not contain the original Git history, assistant memory/configuration, personal documents, build caches,
test game runtimes, local release candidate duplicates, or `archive/migration_2026-10-09/` migration bundles.
Those files remain only in the original workspace and must be reviewed separately before deleting that workspace.
This snapshot is not yet evidence of a successful GitHub upload. Verify a fresh remote clone before local cleanup.
