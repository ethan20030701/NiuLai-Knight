#!/usr/bin/env bash
# Build the mod inside WSL. From Windows Git Bash run:
#   wsl.exe bash /mnt/c/Users/asus/HollowKnight/tools/build_wsl.sh
# (do NOT inline this via wsl.exe bash -c '...' — wsl.exe re-joins args and
#  the unquoted expansion of $PATH with "Program Files (x86)" breaks parsing)
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

cd /mnt/c/Users/asus/HollowKnight || exit 1

exec dotnet build -v q \
  "/p:Sts2DataDir=/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64" \
  "/p:ModsPath=/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/"
