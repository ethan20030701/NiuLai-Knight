export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
mkdir -p ~/sts2api/hornet
cd ~/sts2api/hornet
ilspycmd -p -o . "/mnt/c/Program Files (x86)/Steam/steamapps/workshop/content/2868840/3747589529/HornetMod.dll"
echo "EXIT=$?"
find . -name '*.cs' | wc -l
