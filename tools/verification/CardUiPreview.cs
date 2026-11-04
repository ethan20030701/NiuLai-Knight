using BaseLib.Abstracts;
using System.Reflection;
using Godot;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

public static class CardUiPreview
{
    public static async Task Run()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(NCard).Assembly);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(1440, 940);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.ContentScaleFactor = 1f;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RenderingServer.SetDefaultClearColor(new Color("111b24"));
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var root = new Control { Name = "CardUiPreview", Size = new Vector2(1440, 940) };
        tree.Root.AddChild(root);
        var player = Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = player.NetId;
        var run = RunState.CreateForTest([player], seed: "HKCARDUI");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        var state = new CombatState(runState: run);
        state.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(state);
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 99;

        var descending = Create<DescendingDark>(state, player, root, new Vector2(220, 235));
        var shriek = Create<AbyssShriek>(state, player, root, new Vector2(560, 235));
        var unknown = Create<AbyssShriek>(state, player, root, new Vector2(900, 235));
        unknown.Visibility = ModelVisibility.NotSeen;
        unknown.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        var great = Create<GreatSlash>(state, player, root, new Vector2(1240, 235));
        var deep = Create<DeepFocus>(state, player, root, new Vector2(440, 690));
        deep.Model!.UpgradeInternal();
        deep.Scale = new Vector2(1.1f, 1.1f);
        deep.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        var reduced = Create<VengefulSpirit>(state, player, root, new Vector2(970, 690));
        player.PlayerCombatState!.Hand.AddInternal(reduced.Model!);
        player.PlayerCombatState.Energy = 5;
        CustomResources<Soul>.Cost(reduced.Model!)!.SetThisCombat(24, reduceOnly: true);
        reduced.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        var cost = CustomResources<Soul>.Cost(reduced.Model!)!;
        SoulCardUi.Update(reduced, cost, PileType.Hand);
        await tree.ToSignal(tree.CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);

        var checks = new Dictionary<string, bool>();
        Check("badge_registered_on_reload", descending.Body.HasNode("SoulCostBadge"));
        var badge = descending.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
        Check("33_displayed", badge.GetNode<Control>("CostLabel").Get("text").AsString() == "33");
        var portraitBounds = descending.GetNode<Control>("%Portrait").GetGlobalRect();
        var bounds = badge.GetGlobalRect();
        var starBounds = descending.GetNode<Control>("%StarIcon").GetGlobalRect();
        Check("native_secondary_cost_position", bounds.GetCenter().DistanceTo(starBounds.GetCenter()) < 0.1f);
        Check("no_overlap_portrait", !bounds.Intersects(portraitBounds));
        foreach (var name in new[] { "%TitleLabel", "%TypePlaque", "%DescriptionLabel" })
            Check("no_overlap_" + name.TrimStart('%'), !bounds.Intersects(descending.GetNode<Control>(name).GetGlobalRect()));
        Check("unknown_hides_cost", !unknown.Body.GetNode<NSoulCostBadge>("SoulCostBadge").Visible);
        Check("nail_art_no_soul_badge", !great.Body.HasNode("SoulCostBadge"));
        var discountedBadge = reduced.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
        Check("modified_cost_24", discountedBadge.GetNode<Control>("CostLabel").Get("text").AsString() == "24");
        var description = reduced.Model!.GetDescriptionForPile(PileType.Hand);
        GD.Print("COST DESCRIPTION: " + description);
        Check("description_matches_24", description.Contains("24 [img]res://HollowKnight/images/charui/soul_icon_text.png[/img]"));
        var normalColor = discountedBadge.GetNode<Control>("CostLabel").GetThemeColor("font_color");
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 0;
        SoulCardUi.Update(reduced, cost, PileType.Hand);
        var poorColor = discountedBadge.GetNode<Control>("CostLabel").GetThemeColor("font_color");
        Check("insufficient_soul_changes_color", normalColor != poorColor);
        await Screenshot("soul_card_ui_insufficient.png");
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 99;
        SoulCardUi.Update(reduced, cost, PileType.Hand);
        await Screenshot("soul_card_ui.png");
        Check("rendered_description_matches_24", reduced.GetNode<Control>("%DescriptionLabel").Get("text").AsString()
            .Contains("24 [img]res://HollowKnight/images/charui/soul_icon_text.png[/img]"));
        // Check every SOUL-spending card through the real pooled card node, including manual payment.
        var coverage = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
        coverage.Position = new Vector2(-9000, -9000);
        root.AddChild(coverage);
        var inventory = new List<object>();
        var coveredSoulCards = new HashSet<string>();
        var getModel = typeof(ModelDb).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static, [typeof(Type)])!;
        // WeaverBite is a Shiv-style token that extends CustomCardModel directly
        // (not HollowKnightCard), so the pool invariant spans every concrete card
        // model in the assembly.
        var registeredTypes = typeof(VengefulSpirit).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(CustomCardModel).IsAssignableFrom(t)).OrderBy(t => t.Name).ToArray();
        var activePool = player.Character.CardPool.AllCards.ToArray();
        var poolMatchesAssembly = registeredTypes.Length == 83 && activePool.Length == 83
            && registeredTypes.ToHashSet().SetEquals(activePool.Select(card => card.GetType()));
        if (!poolMatchesAssembly)
            GD.Print($"CARD POOL MISMATCH: assembly={registeredTypes.Length} pool={activePool.Length}"
                + $" onlyInAssembly=[{string.Join(",", registeredTypes.Select(t => t.Name).Except(activePool.Select(c => c.GetType().Name)))}]"
                + $" onlyInPool=[{string.Join(",", activePool.Select(c => c.GetType().Name).Except(registeredTypes.Select(t => t.Name)))}]");
        Check("active_card_models_match_pool", poolMatchesAssembly);
        var retiredTypes = HKAudit.ExpectedRetiredCards().Select(alias => alias.RetiredType).ToHashSet();
        Check("no_retired_card_models_registered", retiredTypes.Count == 21
            && !registeredTypes.Any(type => retiredTypes.Contains(type.Name)));
        // Build the expectation from gameplay cost definitions rather than the
        // UI helper under test. These three cards pay SOUL manually in OnPlay.
        var manualCostTypes = new HashSet<Type> { typeof(SoulTotem), typeof(SoulUnbound), typeof(SoulAegis) };
        var expectedSoulCards = activePool.Where(card => CustomResources<Soul>.CanonicalCost(card) >= 0
                || manualCostTypes.Contains(card.GetType())).Select(card => card.Id.ToString()).ToHashSet();
        Check("pool_has_19_soul_cards", expectedSoulCards.Count == 19);
        foreach (var type in registeredTypes)
        {
            var canonical = (CardModel)getModel.Invoke(null, [type])!;
            if (CustomResources<Soul>.Cost(canonical) == null && !SoulCardUi.HasManualCost(canonical)) continue;
            var model = state.CreateCard(canonical, player);
            coverage.Model = model;
            coverage.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            var costBadge = coverage.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
            Check(type.Name + "_normal_badge", costBadge.Visible);
            var text = costBadge.GetNode<Control>("CostLabel").Get("text").AsString();
            Check(type.Name + "_cost", text == ExpectedCost(model));
            var upgradedText = text;
            if (model.IsUpgradable)
            {
                model.UpgradeInternal();
                coverage.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                upgradedText = costBadge.GetNode<Control>("CostLabel").Get("text").AsString();
                Check(type.Name + "_upgraded_badge", costBadge.Visible && upgradedText == ExpectedCost(model));
            }
            if (SoulCardUi.HasManualCost(model))
                Check(type.Name + "_no_registered_payment", CustomResources<Soul>.Cost(model) == null);
            Check(type.Name + "_covered_once", coveredSoulCards.Add(model.Id.ToString()));
            inventory.Add(new { card = model.Id.ToString(), cost = text, upgraded_cost = upgradedText });
        }
        Check("all_active_soul_cards_covered", coveredSoulCards.SetEquals(expectedSoulCards)
            && inventory.Count == expectedSoulCards.Count);

        // Additional actual-render examples for one-digit, fixed manual and spend-all costs.
        foreach (var node in new[] { descending, shriek, unknown, great, deep, reduced }) node.Visible = false;
        await CheckInspectScreen();
        var womb = Create<GlowingWomb>(state, player, root, new Vector2(220, 235));
        var totem = Create<SoulTotem>(state, player, root, new Vector2(560, 235));
        var unbound = Create<SoulUnbound>(state, player, root, new Vector2(900, 235));
        var aegis = Create<SoulAegis>(state, player, root, new Vector2(1240, 235));
        player.PlayerCombatState.Hand.AddInternal(totem.Model!);
        totem.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        var manualLabel = totem.Body.GetNode<NSoulCostBadge>("SoulCostBadge").GetNode<Control>("CostLabel");
        var enoughColor = manualLabel.GetThemeColor("font_color");
        CustomResources<Soul>.Get(player.PlayerCombatState).Amount = 0;
        totem.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        Check("manual_insufficient_soul_updates_color", manualLabel.GetThemeColor("font_color") != enoughColor);
        CustomResources<Soul>.Get(player.PlayerCombatState).Amount = 99;
        totem.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        Check("manual_soul_recovered_updates_color", manualLabel.GetThemeColor("font_color") == enoughColor);
        await Screenshot("soul_card_ui_all_cost_types.png");
        // Card nodes are pooled in the real game. Reusing one must clear an old resource badge.
        descending.Model = state.CreateCard<OldNail>(player);
        descending.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        Check("pooled_non_soul_card_hides_cost", !badge.Visible);
        descending.Model = null;
        Check("pooled_null_model_hides_cost", !badge.Visible);
        var folder = OutputFolder();
        File.WriteAllText(Path.Combine(folder, "soul_card_ui_verification.json"),
            System.Text.Json.JsonSerializer.Serialize(new { checks, inventory, expected_soul_cards = expectedSoulCards.OrderBy(id => id).ToArray(),
                active_model_count = activePool.Length, failed = checks.Count(x => !x.Value) },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Quit(checks.All(x => x.Value) ? 0 : 1);

        void Check(string name, bool value) { checks[name] = value; GD.Print($"CARD UI: {name} = {value}"); }

        string ExpectedCost(CardModel model) => CustomResources<Soul>.Cost(model) is { } custom
            ? custom.CostsX ? "X" : custom.GetWithModifiers(CostModifiers.All).ToString()
            : model is SoulTotem ? Soul.SpellCost.ToString() : "X";

        async Task Screenshot(string name)
        {
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = tree.Root.GetTexture().GetImage();
            if (image.SavePng(Path.Combine(OutputFolder(), name)) != Error.Ok) throw new Exception("Screenshot failed");
        }

        async Task CheckInspectScreen()
        {
            // The isolated runner has no NGame scene. Supply its native hotkey
            // owner so the inspect screen's buttons initialize normally.
            var game = new IsolatedCardPreviewGame();
            var hotkeys = new NHotkeyManager();
            var input = new NInputManager();
            var controller = new NControllerManager();
            typeof(NGame).GetProperty(nameof(NGame.Instance))!.SetValue(null, game);
            typeof(NGame).GetProperty(nameof(NGame.HotkeyManager))!.SetValue(game, hotkeys);
            typeof(NGame).GetProperty(nameof(NGame.InputManager))!.SetValue(game, input);
            typeof(NInputManager).GetProperty(nameof(NInputManager.ControllerManager))!.SetValue(input, controller);
            root.AddChild(game);
            var tips = new Control();
            typeof(NGame).GetProperty(nameof(NGame.HoverTipsContainer))!.SetValue(game, tips);
            // Reproduce the deck badge beneath the enlarged card's description.
            // Independent single-card snapshots cannot catch this draw-order bug.
            var deckCard = Create<Focus>(state, player, root, new Vector2(1040, 820));
            var deckNail = Create<OldNail>(state, player, root, new Vector2(350, 260));
            var deckSpell = Create<VengefulSpirit>(state, player, root, new Vector2(680, 260));
            var inspector = ResourceLoader.Load<PackedScene>("res://scenes/screens/inspect_card_screen.tscn")
                .Instantiate<NInspectCardScreen>();
            inspector.Visible = false;
            typeof(NGame).GetProperty(nameof(NGame.InspectCardScreen))!.SetValue(game, inspector);
            root.AddChild(inspector);
            root.AddChild(tips);
            inspector.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            inspector.Open([deckCard.Model!, deckSpell.Model!, deckNail.Model!], 0);
            await tree.ToSignal(tree.CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
            var enlarged = inspector.GetNode<NCard>("Card");
            var enlargedBadge = enlarged.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
            var deckBadge = deckCard.Body.GetNode<NSoulCostBadge>("SoulCostBadge");
            Check("deck_cost_uses_native_draw_order", deckBadge.ZIndex == deckCard.GetNode<Control>("%StarIcon").ZIndex);
            Check("inspect_cost_uses_native_draw_order", enlargedBadge.ZIndex == enlarged.GetNode<Control>("%StarIcon").ZIndex);
            Check("inspect_has_one_badge", enlarged.Body.GetChildren().OfType<NSoulCostBadge>().Count() == 1);
            Check("inspect_badge_follows_enlarged_slot", enlargedBadge.GetGlobalRect().GetCenter()
                .DistanceTo(enlarged.GetNode<Control>("%StarIcon").GetGlobalRect().GetCenter()) < 0.1f);
            var descriptionRect = enlarged.GetNode<Control>("%DescriptionLabel").GetGlobalRect();
            Check("inspect_own_badge_outside_description", !descriptionRect.Intersects(enlargedBadge.GetGlobalRect()));
            var sampleRect = deckBadge.GetGlobalRect();
            Check("repro_background_badge_overlaps_inspect_description", descriptionRect.Encloses(sampleRect));
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var withDeck = tree.Root.GetTexture().GetImage();
            withDeck.SavePng(Path.Combine(OutputFolder(), "soul_cost_inspect_layer_fixed.png"));
            var costRect = enlarged.GetNode<Control>("%EnergyIcon").GetGlobalRect()
                .Merge(enlargedBadge.GetGlobalRect()).Grow(12f);
            using var costCloseup = withDeck.GetRegion(new Rect2I((Vector2I)costRect.Position, (Vector2I)costRect.Size));
            costCloseup.SavePng(Path.Combine(OutputFolder(), "soul_cost_energy_style_closeup.png"));
            var sample = new Rect2I((Vector2I)sampleRect.Position, (Vector2I)sampleRect.Size);
            using var before = withDeck.GetRegion(sample);
            deckCard.Visible = false;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var withoutDeck = tree.Root.GetTexture().GetImage();
            using var after = withoutDeck.GetRegion(sample);
            Check("inspect_description_pixels_independent_of_background_cost", before.GetData().SequenceEqual(after.GetData()));
            // Exercise the actual inspector's navigation and upgrade callbacks.
            typeof(NInspectCardScreen).GetMethod("OnRightButtonReleased", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inspector, null);
            Check("inspect_navigation_reuses_single_badge", enlarged.Body.GetChildren().OfType<NSoulCostBadge>().Count() == 1 && enlargedBadge.Visible);
            inspector.GetNode<Control>("%Upgrade").Set("IsTicked", true);
            typeof(NInspectCardScreen).GetMethod("UpdateCardDisplay", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inspector, null);
            Check("inspect_upgrade_keeps_badge_in_slot", enlargedBadge.GetGlobalRect().GetCenter()
                .DistanceTo(enlarged.GetNode<Control>("%StarIcon").GetGlobalRect().GetCenter()) < 0.1f);
            typeof(NInspectCardScreen).GetMethod("OnRightButtonReleased", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inspector, null);
            Check("inspect_non_soul_card_hides_badge", !enlargedBadge.Visible);
            inspector.Close();
            await tree.ToSignal(tree.CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
            Check("inspect_close_restores_background_card_cost", !inspector.Visible && deckSpell.Body.GetNode<NSoulCostBadge>("SoulCostBadge").IsVisibleInTree());
            inspector.QueueFree();
            tips.QueueFree();
            deckCard.QueueFree();
            deckNail.QueueFree();
            deckSpell.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            typeof(NGame).GetProperty(nameof(NGame.Instance))!.SetValue(null, null);
            hotkeys.Free();
            controller.Free();
            input.Free();
            game.Free();
        }
    }

    static string OutputFolder()
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    static NCard Create<T>(CombatState state, Player player, Control root, Vector2 position) where T : CardModel
    {
        var model = state.CreateCard<T>(player);
        var node = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
        node.Model = model;
        node.Position = position;
        root.AddChild(node);
        node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        return node;
    }
}

// A viewport owner for native UI controls, with normal game startup disabled.
public partial class IsolatedCardPreviewGame : NGame
{
    public override void _EnterTree() { }
    public override void _Ready() { }
    public override void _ExitTree() { }
}
