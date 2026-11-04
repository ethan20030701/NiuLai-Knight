#!/usr/bin/env bash
set -euo pipefail
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
cd /mnt/c/Users/asus/HollowKnight
audit=tools/compatibility/bin/Debug/net9.0/CompatibilityAudit.dll
dotnet build tools/compatibility/CompatibilityAudit.csproj -v q
for branch in stable beta; do
  if [[ "$branch" == stable ]]; then
    game=/mnt/c/steamcmd/steamapps/content/app_2868840/depot_2868841/data_sts2_windows_x86_64/sts2.dll
  else
    game='/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll'
  fi
  dotnet "$audit" .build/mods/HollowKnight/HollowKnight.dll "$game" .build/mods/BaseLib/BaseLib.dll > ".build/compatibility_${branch}.json"
  cat ".build/compatibility_${branch}.json"
done
