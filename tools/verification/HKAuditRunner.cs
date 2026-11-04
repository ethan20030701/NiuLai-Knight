using Godot;
using MegaCrit.Sts2.Core.Modding;

public partial class HKAuditRunner : Node
{
    public override async void _Ready()
    {
        GD.Print("HK AUDIT RUNNER READY");
        try
        {
            await ModManager.Initialize(new ModManagerFileIo(),
                new ModSettings { PlayerAgreedToModLoading = true }, null);
            // Godot's project assembly and sts2 can use different load contexts.
            // Run the cases in the game's context so model type identity is exact.
            var gameContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(ModManager).Assembly)!;
            GD.Print($"HK AUDIT GAME CONTEXT: {gameContext.Name}");
            var cases = gameContext.LoadFromAssemblyPath(System.IO.Path.Combine(AppContext.BaseDirectory, "HKAudit.dll"));
            await (System.Threading.Tasks.Task)cases.GetType("HKAudit")!.GetMethod("Initialize")!.Invoke(null, null)!;
        }
        catch (System.Exception e)
        {
            System.IO.File.WriteAllText("bootstrap-error.txt", e.ToString());
            GetTree().Quit(1);
        }
    }
}
