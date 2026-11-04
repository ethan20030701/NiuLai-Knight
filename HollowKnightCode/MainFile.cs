using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace HollowKnight.HollowKnightCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "HollowKnight"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        Harmony harmony = new(ModId);

        // Required gameplay/compatibility hooks reject initialization on any
        // failure; each optional visual/UI class can be disabled independently.
        PatchInstaller.Install(harmony);
    }
}
