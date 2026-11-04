using System.Reflection;
using System.Text.Json;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

public static class HornetStudyPreview
{
    public static async Task Run()
    {
        bool cleanup = OS.GetCmdlineUserArgs().Contains("--knight-cleanup-preview");
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(NCard).Assembly);
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(OldNail).Assembly);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(cleanup ? 2010 : 1360, 900);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        RenderingServer.SetDefaultClearColor(new Color("111923"));
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var player = Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = player.NetId;
        var run = RunState.CreateForTest([player], seed: "HKHORNETSTUDY");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        var state = new CombatState(runState: run); state.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(state);
        CustomResources<Soul>.Get(player.PlayerCombatState!).Amount = 66;
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review"));
        Directory.CreateDirectory(folder);
        var checks = new Dictionary<string, bool>();
        CardModel[] cards = cleanup
            ? [ModelDb.Card<VengefulSpirit>(), ModelDb.Card<QuickSlash>(), ModelDb.Card<AbyssShriek>(),
               ModelDb.Card<ChannelledNail>(), ModelDb.Card<DarkShrine>(), ModelDb.Card<MothwingDash>()]
            : [ModelDb.Card<VengefulSpirit>(), ModelDb.Card<ChannelledNail>(), ModelDb.Card<DarkShrine>(), ModelDb.Card<MothwingDash>()];
        string artifact = cleanup ? "cleanup_cards" : "hornet_study";
        for (var upgraded = 0; upgraded <= 1; upgraded++)
        {
            var root = new Node2D { Name = "StudyPreview" }; tree.Root.AddChild(root);
            Label(root, "THE KNIGHT  /  SOUL, NAIL AND SPELLS", new Vector2(55, 28), 24);
            Label(root, upgraded == 0 ? "BASE CARDS  ·  TEN-CARD STARTER INCLUDES VENGEFUL SPIRIT" : "UPGRADES  ·  SOUL REMAINS 11 / 33 / 99", new Vector2(55, 68), 16);
            var nodes = new List<NCard>();
            for (var i = 0; i < cards.Length; i++)
            {
                var model = state.CreateCard(cards[i], player);
                if (upgraded == 1) model.UpgradeInternal();
                var node = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
                node.Model = model; node.Position = new Vector2(190 + i * 325, 335);
                root.AddChild(node); node.UpdateVisuals(PileType.None, CardPreviewMode.Normal); nodes.Add(node);
            }
            await tree.ToSignal(tree.CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
            foreach (var node in nodes)
            {
                var key = node.Model!.Id.Entry + (upgraded == 1 ? "+" : "");
                var text = node.GetNode<RichTextLabel>("%DescriptionLabel");
                checks[key + ".text_fits"] = text.GetContentHeight() <= text.Size.Y + 2;
                checks[key + ".variables_resolved"] = !text.Text.Contains('{');
                if (node.Model is VengefulSpirit or AbyssShriek)
                    checks[key + ".soul_cost_33"] = node.Body.GetNode<Control>("SoulCostBadge/CostLabel").Get("text").AsString() == "33";
                if (node.Model is MothwingDash)
                    checks[key + ".threshold_visible"] = text.Text.Contains("66");
            }
            var effects = new List<KnightImpact>();
            var styles = Enum.GetValues<KnightImpact.Style>();
            for (var i = 0; i < styles.Length; i++)
            {
                var position = new Vector2(130 + i * (cleanup ? 340 : 220), 745);
                Label(root, styles[i].ToString().ToUpperInvariant(), position + new Vector2(-35, 83), 16);
                var effect = KnightImpact.Create(styles[i]); effect.Position = position;
                root.AddChild(effect); effects.Add(effect);
            }
            await tree.ToSignal(tree.CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var screenshot = tree.Root.GetTexture().GetImage();
            checks[$"render.{upgraded}.image"] = screenshot != null && !screenshot.IsEmpty();
            screenshot!.SavePng(Path.Combine(folder, $"{artifact}{(upgraded == 1 ? "_upgraded" : "")}.png"));
            await tree.ToSignal(tree.CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
            checks[$"effects.{upgraded}.all_freed"] = effects.All(effect => !GodotObject.IsInstanceValid(effect));
            root.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        foreach (var kind in new[] { "nail", "soul", "guard", "focus", "hurt", "void", "power" })
        {
            using var wav = AudioStreamWav.LoadFromFile($"res://HollowKnight/audio/knight_{kind}.wav");
            checks[$"audio.{kind}.decoded"] = wav != null && wav.MixRate == 44100 && !wav.Stereo
                && wav.GetLength() is > 0.1 and < 0.6 && wav.Data.Length > 100;
        }
        checks["audio.test_mode_muted"] = KnightAudio.SettingsGain() == 0f;
        var playerType = typeof(KnightAudio).Assembly.GetType("HollowKnight.HollowKnightCode.Character.KnightAudioPlayer")!;
        using var cue = (AudioStreamPlayer)Activator.CreateInstance(playerType)!;
        var accept = playerType.GetMethod("Accept")!;
        playerType.GetField("_kind", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cue, "nail");
        playerType.GetField("_started", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cue, 1000UL);
        playerType.GetField("_priority", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cue, 1);
        bool Accept(string kind, int priority, ulong cd, ulong now) => (bool)accept.Invoke(cue, [kind, priority, cd, now])!;
        checks["audio.same_kind_throttled"] = !Accept("nail", 1, 130, 1050) && Accept("nail", 1, 130, 1130);
        checks["audio.high_priority_interrupts"] = Accept("hurt", 4, 180, 1050) && !Accept("guard", 1, 160, 1050);
        checks["audio.expired_priority_window"] = Accept("guard", 1, 160, 1100);
        var knight = player.Character;
        checks["audio.no_inherited_combat_voices"] = knight.AttackSfx == KnightAudio.ProceduralCue
            && knight.CastSfx == KnightAudio.ProceduralCue && knight.DeathSfx == KnightAudio.ProceduralCue;
        var oneShots = typeof(NAudioManager).GetMethods().Where(m => m.Name == nameof(NAudioManager.PlayOneShot)).ToArray();
        checks["audio.marker_filtered_on_both_overloads"] = oneShots.Length == 2 && oneShots.All(method =>
            Harmony.GetPatchInfo(method)?.Prefixes.Any(p => p.PatchMethod.DeclaringType?.Name == "KnightInheritedAudioPatch") == true);
        var filter = typeof(KnightAudio).Assembly.GetType("HollowKnight.HollowKnightCode.Patches.KnightInheritedAudioPatch")!
            .GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!;
        checks["audio.filter_leaves_other_events"] = (bool)filter.Invoke(null, ["event:/sfx/characters/ironclad/ironclad_attack"])!
            && !(bool)filter.Invoke(null, [KnightAudio.ProceduralCue])!;
        await CheckAudioOwnership(tree, checks);
        var replayRoot = new Node2D(); tree.Root.AddChild(replayRoot);
        var knightVisuals = ((Knight)player.Character).CreateCustomVisuals(); replayRoot.AddChild(knightVisuals);
        var body = knightVisuals.GetNode<Sprite2D>("%Visuals");
        var death = knightVisuals.GetNode<AnimatedSprite2D>("KnightDeathAnim");
        death.Play("Dead");
        await tree.ToSignal(tree.CreateTimer(2.85), SceneTreeTimer.SignalName.Timeout);
        checks["death.first_sequence_completes"] = !death.IsPlaying() && !body.Visible;
        body.Visible = true;
        death.Play("Dead");
        await tree.ToSignal(tree.CreateTimer(0.12), SceneTreeTimer.SignalName.Timeout);
        checks["death.replay_hides_living_body"] = !body.Visible;
        replayRoot.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (cleanup)
        {
            // The SOUL display listens inside actual payment/gain events.
            // Deliberately invalidate its label and verify the real payment
            // still succeeds, disables this UI, and allows subsequent gains.
            var uiRoot = new Control(); tree.Root.AddChild(uiRoot);
            var resource = CustomResources<Soul>.Get(player.PlayerCombatState!);
            resource.Amount = 66;
            var counter = new NSoulCounter(resource); uiRoot.AddChild(counter);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var brokenLabel = (Label)typeof(NSoulCounter).GetField("_label", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(counter)!;
            brokenLabel.Free();
            var spent = await resource.Spend<Soul>(state, null, 1, false);
            checks["counter.ui_failure_preserves_real_payment"] = spent && resource.Amount == 65 && !counter.Visible;
            Soul.Gain(player, 1);
            checks["counter.ui_failure_does_not_break_later_gain"] = resource.Amount == 66;
            uiRoot.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        var failed = checks.Count(c => !c.Value);
        File.WriteAllText(Path.Combine(folder, cleanup ? "cleanup_media_ui.json" : "hornet_study_media_ui.json"), JsonSerializer.Serialize(
            new { failed, checks, note = "Native rendering/decoding and muted playback lifecycle. Lifecycle fixture forces the settings gate open; actual live volume/focus controls, human listening and network co-op remain untested." },
            new JsonSerializerOptions { WriteIndented = true }));
        tree.Quit(failed == 0 ? 0 : 1);
    }

    private static async Task CheckAudioOwnership(SceneTree tree, Dictionary<string, bool> checks)
    {
        // Exercise actual Godot playback with its SFX bus muted. Only the
        // settings gate is opened for this fixture; sound/loading/player code
        // is real. This does not claim to test the live settings screen.
        var bus = AudioServer.GetBusIndex("SFX");
        var createdBus = bus < 0;
        if (createdBus)
        {
            AudioServer.AddBus(); bus = AudioServer.BusCount - 1;
            AudioServer.SetBusName(bus, "SFX");
        }
        var wasMuted = AudioServer.IsBusMute(bus);
        AudioServer.SetBusMute(bus, true);
        var harmony = new Harmony("hollowknight.media-lifecycle-fixture");
        var gain = typeof(KnightAudio).GetMethod(nameof(KnightAudio.SettingsGain))!;
        var root = new Node2D(); tree.Root.AddChild(root);
        var first = new Sprite2D(); var second = new Sprite2D();
        root.AddChild(first); root.AddChild(second);
        try
        {
            harmony.Patch(gain, prefix: new HarmonyMethod(typeof(HornetStudyPreview), nameof(OpenAudioGate)));
            KnightAudio.Play(first, "nail"); KnightAudio.Play(first, "nail");
            KnightAudio.Play(second, "soul");
            var a = first.GetNodeOrNull<AudioStreamPlayer>("KnightAudio");
            var b = second.GetNodeOrNull<AudioStreamPlayer>("KnightAudio");
            checks["audio.one_player_per_owner"] = first.GetChildCount() == 1 && second.GetChildCount() == 1
                && a != null && b != null && a != b;
            checks["audio.native_playback_uses_sfx"] = a?.Playing == true && b?.Playing == true
                && a.Bus == "SFX" && b.Bus == "SFX" && a.MaxPolyphony == 1;
            KnightAudio.Stop(first);
            checks["audio.stop_is_owner_scoped"] = a?.Playing == false && b?.Playing == true;
            first.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            checks["audio.owner_frees_player"] = a != null && !GodotObject.IsInstanceValid(a)
                && b != null && GodotObject.IsInstanceValid(b);
        }
        finally
        {
            harmony.Unpatch(gain, HarmonyPatchType.All, harmony.Id);
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (createdBus) AudioServer.RemoveBus(bus);
            else AudioServer.SetBusMute(bus, wasMuted);
        }
    }

    private static bool OpenAudioGate(ref float __result) { __result = 1f; return false; }

    private static void Label(Node parent, string text, Vector2 position, int size)
    {
        var label = new Label { Text = text, Position = position };
        label.AddThemeFontSizeOverride("font_size", size); parent.AddChild(label);
    }
}
