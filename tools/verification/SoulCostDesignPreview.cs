using BaseLib.Abstracts;
using Godot;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.addons.mega_text;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

/// <summary>One review-only card. This styles its preview nodes without changing the mod's shared UI.</summary>
public static class SoulCostDesignPreview
{
    public static async Task Run()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(NCard).Assembly);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(480, 620);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.ContentScaleFactor = 1f;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.SetDefaultClearColor(new Color("111b24"));
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var root = new Control { Name = "SoulCostSinglePreview", Size = new Vector2(480, 620) };
        tree.Root.AddChild(root);
        var player = Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = player.NetId;
        var run = RunState.CreateForTest([player], seed: "SOULDRAFT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        var state = new CombatState(runState: run);
        state.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(state);
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 99;
        player.PlayerCombatState!.Energy = 5;

        var model = state.CreateCard<VengefulSpirit>(player);
        var card = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
        card.Model = model;
        card.Position = new Vector2(250, 315);
        card.Scale = new Vector2(1.25f, 1.25f);
        root.AddChild(card);
        card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        await tree.ToSignal(tree.CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

        var badge = card.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
        var icon = badge.GetChildren().OfType<TextureRect>().Single();
        icon.SelfModulate = new Color(1f, 1f, 1f, 0.60f);
        var label = badge.GetNode<MegaLabel>("CostLabel");
        label.Position = icon.Position;
        label.Size = icon.Size;
        label.MinFontSize = 20;
        label.MaxFontSize = 20;
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", StsColors.cream);
        label.AddThemeColorOverride("font_outline_color", new Color("233141"));
        label.AddThemeConstantOverride("outline_size", 2);
        label.SetTextAutoSize("33");

        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = tree.Root.GetTexture().GetImage();
        if (image == null || image.IsEmpty()) throw new Exception("Preview renderer produced no image");
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review"));
        Directory.CreateDirectory(folder);
        if (image.SavePng(Path.Combine(folder, "soul_cost_single_preview.png")) != Error.Ok)
            throw new Exception("Could not save single-card preview");
        File.WriteAllText(Path.Combine(folder, "soul_cost_single_preview.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                card = model.Id.ToString(), soul_cost = 33, icon_opacity = 0.60,
                centered_number = true, opaque_number = true,
                preview_only = true, shared_mod_ui_modified = false, installed = false
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        GD.Print("SOUL SINGLE PREVIEW: saved soul_cost_single_preview.png; review only");
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Quit();
    }
}
