using BaseLib.Abstracts;
using Godot;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

public static class CombatHudPreview
{
    public static async Task Run()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(NHealthBar).Assembly);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(800, 600);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.ContentScaleFactor = 1f;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.SetDefaultClearColor(new Color("243728"));
        var root = new Control { Name = "CombatHudPreview", Size = new Vector2(800, 600) };
        tree.Root.AddChild(root);
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var player = Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = player.NetId;
        var run = RunState.CreateForTest([player], seed: "HKHUD");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        var state = new CombatState(runState: run);
        state.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(state);
        player.Creature.SetMaxHpInternal(81);
        player.Creature.SetCurrentHpInternal(68);
        player.Creature.GainBlockInternal(5);
        player.PlayerCombatState!.Energy = 0;
        var soul = CustomResources<Soul>.Get(player.PlayerCombatState);
        soul.Amount = 55;

        var visuals = ((Knight)player.Character).CreateCustomVisuals();
        visuals.Position = new Vector2(480, 360);
        root.AddChild(visuals);
        var selection = new Control { Size = new Vector2(140, 250), Position = new Vector2(410, 110) };
        root.AddChild(selection);
        var health = ResourceLoader.Load<PackedScene>("res://scenes/combat/health_bar.tscn").Instantiate<NHealthBar>();
        health.Position = new Vector2(480, 378);
        root.AddChild(health);
        health.SetCreature(player.Creature);
        health.UpdateLayoutForCreatureBounds(selection);
        health.RefreshValues();

        var energyContainer = new Control { Name = "EnergyCounterContainer", Position = new Vector2(60, 440), Size = new Vector2(128, 128) };
        root.AddChild(energyContainer);
        var energy = NEnergyCounter.Create(player);
        energyContainer.AddChild(energy);
        var counter = new NSoulCounter(soul);
        counter.AttachBesideEnergy(energyContainer);

        // Measure the original character bounds separately; no health-bar patch applies to Ironclad.
        var ironclad = Player.CreateForNewRun<Ironclad>(UnlockState.all, 2);
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn = false;
        var originalVisuals = ironclad.Creature.CreateVisuals() ?? throw new Exception("Original character visuals could not load");
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn = true;
        originalVisuals.Position = new Vector2(-9000, -9000);
        root.AddChild(originalVisuals);
        await tree.ToSignal(tree.CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        var originalWidth = originalVisuals.Bounds.Size.X;
        GD.Print($"HUD ORIGINAL IRONCLAD WIDTH: {originalWidth}");

        var checks = new Dictionary<string, bool>();
        Check("knight_health_bar_240", Mathf.IsEqualApprox(health.HpBarContainer.Size.X, KnightHealthBarLayout.PlayerBarWidth));
        Check("selection_bounds_still_140", Mathf.IsEqualApprox(selection.Size.X, 140f));
        Check("soul_right_of_energy", counter.GlobalPosition.X >= energyContainer.GlobalPosition.X + energyContainer.Size.X + 20f);
        Check("soul_aligned_with_energy", Mathf.IsEqualApprox(counter.GlobalPosition.Y + counter.Size.Y / 2f, energyContainer.GlobalPosition.Y + energyContainer.Size.Y / 2f));
        var oldPosition = counter.GlobalPosition;
        energyContainer.Position += new Vector2(15, -20);
        Check("soul_follows_energy_position", (counter.GlobalPosition - oldPosition).IsEqualApprox(new Vector2(15, -20)));
        energyContainer.Position -= new Vector2(15, -20);
        energyContainer.Size = new Vector2(144, 144);
        Check("soul_follows_energy_resize", Mathf.IsEqualApprox(counter.Position.X, 168f));
        energyContainer.Size = new Vector2(128, 128);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = tree.Root.GetTexture().GetImage();
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review"));
        Directory.CreateDirectory(folder);
        image.SavePng(Path.Combine(folder, "combat_hud_layout.png"));
        File.WriteAllText(Path.Combine(folder, "combat_hud_layout_verification.json"),
            System.Text.Json.JsonSerializer.Serialize(new { checks, original_ironclad_bounds_width = originalWidth,
                knight_selection_width = selection.Size.X, knight_health_bar_width = health.HpBarContainer.Size.X,
                failed = checks.Count(x => !x.Value) }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Quit(checks.All(x => x.Value) ? 0 : 1);
        void Check(string name, bool value) { checks[name] = value; GD.Print($"HUD CHECK {name}: {value}"); }
    }
}
