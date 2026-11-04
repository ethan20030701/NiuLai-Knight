using System.Text.Json;
using Godot;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

public static class SwarmPreview
{
    public static async Task Run()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(NCard).Assembly);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(1600, 650);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.ContentScaleFactor = 1f;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.SetDefaultClearColor(new Color("111b24"));
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var player = Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = player.NetId;
        var run = RunState.CreateForTest([player], seed: "HKSWARMUI");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        var state = new CombatState(runState: run); state.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(state);
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 99;
        var cards = new CardModel[] {
            ModelDb.Card<WeaverNest>(), ModelDb.Card<TendBrood>(), ModelDb.Card<GlowingWomb>(), ModelDb.Card<GrimmRite>(), ModelDb.Card<BroodReturn>(),
            ModelDb.Card<SwarmRush>(), ModelDb.Card<WeaverVolley>(), ModelDb.Card<SwarmEcho>(), ModelDb.Card<BroodAssault>(), ModelDb.Card<HatchlingBurst>(),
            ModelDb.Card<BroodChorus>(), ModelDb.Card<BroodInstinct>(), ModelDb.Card<SoulNourishment>(), ModelDb.Card<BroodOffering>(), ModelDb.Card<SilkenAegis>(),
            ModelDb.Card<Grimmchild>(), ModelDb.Card<Weaversong>() };
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review"));
        Directory.CreateDirectory(folder);
        var checks = new Dictionary<string, bool>();
        for (int upgraded = 0; upgraded <= 1; upgraded++)
        for (int page = 0; page < (cards.Length + 4) / 5; page++)
        {
            var root = new Control(); tree.Root.AddChild(root);
            var nodes = new List<NCard>();
            var count = Math.Min(5, cards.Length - page * 5);
            for (int i = 0; i < count; i++)
            {
                var model = state.CreateCard(cards[page * 5 + i], player);
                if (upgraded == 1) model.UpgradeInternal();
                var node = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
                node.Model = model; node.Position = new Vector2((1600 - count * 320) / 2 + 160 + 320 * i, 315);
                root.AddChild(node); node.UpdateVisuals(PileType.None, CardPreviewMode.Normal); nodes.Add(node);
            }
            await tree.ToSignal(tree.CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
            foreach (var node in nodes)
            {
                var key = node.Model!.Id.Entry + (upgraded == 1 ? "+" : "");
                var label = node.GetNode<RichTextLabel>("%DescriptionLabel");
                checks[key + ".text_fits"] = label.GetContentHeight() <= label.Size.Y + 2;
                checks[key + ".no_unresolved_vars"] = !label.Text.Contains("{");
                if (node.Model is GrimmRite or GlowingWomb)
                {
                    var expected = node.Model is GrimmRite ? "22" : "8";
                    checks[key + ".soul_cost"] = node.Body.GetNode<Control>("SoulCostBadge/CostLabel").Get("text").AsString() == expected;
                }
            }
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = tree.Root.GetTexture().GetImage();
            image.SavePng(Path.Combine(folder, $"swarm_system_{page + 1}{(upgraded == 1 ? "_upgraded" : "")}.png"));
            root.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        var failed = checks.Count(x => !x.Value);
        File.WriteAllText(Path.Combine(folder, "swarm_system_ui.json"), JsonSerializer.Serialize(new { failed, checks }, new JsonSerializerOptions { WriteIndented = true }));
        tree.Quit(failed == 0 ? 0 : 1);
    }
}
