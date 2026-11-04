export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
cd /mnt/c/Users/asus/HollowKnight || exit 1
dotnet restore --force >/dev/null 2>&1 || echo "RESTORE FAILED"
grep -o '"Alchyr.Sts2.BaseLib/[^"]*"' .godot/mono/temp/obj/project.assets.json | head -1
dotnet build --no-incremental -v q \
  "/p:Sts2DataDir=/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64" \
  "/p:ModsPath=/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/"
