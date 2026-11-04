#!/usr/bin/env bash
# Default to the downloaded stable game; later arguments can select a different game build.
set -euo pipefail
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
cd /mnt/c/Users/asus/HollowKnight
dotnet build HollowKnight.csproj -v q \
  "/p:Sts2DataDir=/mnt/c/steamcmd/steamapps/content/app_2868840/depot_2868841/data_sts2_windows_x86_64" \
  "/p:ModsPath=/mnt/c/Users/asus/HollowKnight/.build/mods/" \
  "/p:GodotPath=/nonexistent/megadot" "$@"
