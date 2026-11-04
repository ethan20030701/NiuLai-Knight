export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
mkdir -p ~/sts2api/baselib-347
cd ~/sts2api/baselib-347
ilspycmd -p -o . "/mnt/c/Program Files (x86)/Steam/steamapps/workshop/content/2868840/3737335127/BaseLib/BaseLib.dll"
echo DECOMPILED
