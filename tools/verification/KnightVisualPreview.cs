using Godot;
using HollowKnight.HollowKnightCode.Character;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

public static class KnightVisualPreview
{
    public static async Task Run()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.ContentScaleSize = new Vector2I(720, 420);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.ContentScaleFactor = 1f;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var root = new Node2D { Name = "KnightPreview" };
        tree.Root.AddChild(root);
        if (!root.IsInsideTree()) throw new Exception("Preview was not attached to the renderer");
        RenderingServer.SetDefaultClearColor(new Color("202634"));
        root.AddChild(new Polygon2D { Polygon = [new(0, 338), new(720, 338), new(720, 420), new(0, 420)], Color = new Color("171d28") });
        root.AddChild(new Line2D { Points = [new(20, 338), new(700, 338)], Width = 1.5f, DefaultColor = new Color("64748b") });
        Label(root, "BEFORE: BODY + NAIL OVERLAY", new Vector2(25, 15));
        Label(root, "AFTER: QUICK SLASH, ORIGINAL SHAPE", new Vector2(370, 15));
        var caption = Label(root, "READY", new Vector2(300, 378));

        var original = ResourceLoader.Load<Texture2D>("res://HollowKnight/images/charui/knight_combat.png");
        var attackBody = ResourceLoader.Load<Texture2D>("res://HollowKnight/images/charui/knight_combat_attack.png");
        var old = new Sprite2D { Texture = original, Position = new Vector2(145, 100) };
        var oldNail = new Sprite2D(); old.AddChild(oldNail); root.AddChild(old);
        var visuals = new Knight().CreateCustomVisuals();
        visuals.Position = new Vector2(470, 338); root.AddChild(visuals);
        var body = visuals.GetNode<Sprite2D>("%Visuals");
        var rig = body.GetNode<KnightCombatRig>("KnightCombatRig");
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "art_review", "combat_rig_frames"));
        Directory.CreateDirectory(folder);
        var frames = new List<object>();
        for (int i = 0; i < 31; i++)
        {
            float t = Mathf.Clamp((i - 3) / 60f, 0f, KnightCombatRig.AttackDuration);
            bool active = i >= 3 && t < 0.27f;
            old.Texture = active ? attackBody : original;
            old.Position = new Vector2(145f + (active ? t < 0.1f ? 46f * t / 0.1f : 46f * (1f - (t - 0.1f) / 0.17f) : 0f),
                100f - Mathf.Sin(i * 0.12f) * 6f);
            oldNail.Visible = active;
            if (active) oldNail.Texture = ResourceLoader.Load<Texture2D>($"res://HollowKnight/images/vfx/knight_nail_{Math.Min(5, (int)(t * 24f)):00}.png");
            rig.SampleAttack(t);
            caption.Text = t < 0.018f ? "READY" : t < KnightCombatRig.ContactTime - 0.015f ? "WIND UP" :
                t < KnightCombatRig.ContactTime + 0.04f ? "STRIKE" : t < KnightCombatRig.AttackDuration - 0.05f ? "FOLLOW THROUGH" : "RECOVER";
            if (Mathf.Abs(rig.RearFootPosition.Y) > 0.01f || Mathf.Abs(rig.FrontFootPosition.Y) > 0.01f)
                throw new Exception("Feet left the ground");
            var hand = rig.GetNode<Polygon2D>("GrippingHand");
            var grip = rig.GetNode<Node2D>("NailGrip");
            var separation = hand.GlobalPosition.DistanceTo(grip.GlobalPosition);
            if (separation > 0.01f) throw new Exception("Nail separated from the hand");
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var screenshot = tree.Root.GetTexture().GetImage();
            if (screenshot == null || screenshot.IsEmpty()) throw new Exception("Renderer did not produce an image");
            var path = Path.Combine(folder, $"frame-{i:00}.png");
            if (screenshot.SavePng(path) != Error.Ok) throw new Exception("Could not save rendered frame");
            frames.Add(new { frame = i, time = t, grip_separation = separation, feet_y = rig.FrontFootPosition.Y });
        }
        rig.Interrupt();
        if (rig.IsAttacking) throw new Exception("Interrupted attack kept playing");
        rig.Attack(); rig.Attack(); rig.Interrupt();
        // Explicit sampling alone cannot prove that Godot invokes _Process.
        // Let the real scene clock finish a swing, then interrupt/restart it.
        rig.Attack();
        await tree.ToSignal(tree.CreateTimer(0.48), SceneTreeTimer.SignalName.Timeout);
        if (rig.IsAttacking) throw new Exception("Live animation did not finish on the scene clock");
        rig.Attack();
        await tree.ToSignal(tree.CreateTimer(0.06), SceneTreeTimer.SignalName.Timeout);
        rig.Interrupt();
        if (rig.IsAttacking) throw new Exception("Live attack did not stop after interruption");
        File.WriteAllText(Path.Combine(folder, "verification.json"), System.Text.Json.JsonSerializer.Serialize(
            new { rendered_frames = frames.Count, feet_grounded = true, nail_attached_to_hand = true,
                interrupts_and_restarts = true, live_scene_clock_completed = true,
                attack_seconds = KnightCombatRig.AttackDuration, contact_seconds = KnightCombatRig.ContactTime, frames },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"KNIGHT VISUAL PREVIEW: rendered {frames.Count} frames to {folder}");
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Quit();
    }

    private static Label Label(Node2D parent, string text, Vector2 position)
    {
        var label = new Label { Text = text, Position = position };
        label.AddThemeFontSizeOverride("font_size", 14); parent.AddChild(label); return label;
    }
}
