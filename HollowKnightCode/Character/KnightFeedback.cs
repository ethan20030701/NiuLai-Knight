using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HollowKnight.HollowKnightCode.Character;

/// <summary>
/// Combat reactions for the Knight's cutout rig and short spell effects.
/// Each creature resolves to its own visual node, so co-op Knights never
/// animate each other's bodies. Attack motion is owned by the rig's joints.
/// </summary>
public static class KnightFeedback
{
    private const int FrameCount = 6;
    private static readonly ConditionalWeakTable<Sprite2D, AnimState> States = new();
    private static readonly Dictionary<string, SpriteFrames> Frames = new();

    private sealed class AnimState(Vector2 basePosition)
    {
        public Vector2 BasePosition { get; } = basePosition;
        public KnightCombatRig? Rig { get; set; }
        public Tween? Hurt { get; set; }
    }

    public static void Register(Sprite2D sprite)
    {
        States.Remove(sprite);
        var state = new AnimState(sprite.Position);
        States.Add(sprite, state);
        try { state.Rig = KnightCombatRig.Create(sprite); }
        catch (Exception e)
        {
            sprite.SelfModulate = Colors.White;
            MainFile.Logger.Warn($"Knight joint rig unavailable, keeping original sprite: {e.Message}");
        }
    }

    private static Sprite2D? Body(Creature creature)
    {
        return creature.GetCreatureNode()?.Visuals?.GetNodeOrNull<Sprite2D>("%Visuals") is { } sprite
               && GodotObject.IsInstanceValid(sprite) ? sprite : null;
    }

    private static AnimState State(Sprite2D sprite) =>
        States.GetValue(sprite, s => new AnimState(s.Position));

    public static void Stop(Sprite2D sprite)
    {
        var state = State(sprite);
        state.Hurt?.Kill();
        state.Rig?.Interrupt();
        state.Rig?.SetProcess(false);
        sprite.Position = state.BasePosition;
        KnightAudio.Stop(sprite);
    }

    public static void Nail(Creature creature)
    {
        if (Body(creature) is not { } sprite)
        {
            return;
        }
        var state = State(sprite);
        state.Hurt?.Kill();
        sprite.Position = state.BasePosition;
        sprite.Modulate = Colors.White;
        state.Rig?.Attack();
        KnightAudio.Play(sprite, "nail");
    }

    public static void Soul(Creature creature) => PlayOnBody(creature, "soul");
    public static void Power(Creature creature) => PlayOnBody(creature, "power");

    public static void Skill(Creature creature, string kind = "guard")
    {
        if (Body(creature) is not { } sprite) return;
        Play(sprite, "skill");
        KnightAudio.Play(sprite, kind);
        if (kind is "focus" or "guard" or "void")
            KnightImpact.Pulse(sprite, kind switch
            {
                "focus" => KnightImpact.Style.Focus,
                "void" => KnightImpact.Style.Void,
                _ => KnightImpact.Style.Guard
            });
    }

    private static void PlayOnBody(Creature creature, string kind)
    {
        if (Body(creature) is { } sprite)
        {
            Play(sprite, kind);
            KnightAudio.Play(sprite, kind);
        }
    }

    public static void Hurt(Creature creature)
    {
        if (Body(creature) is not { } sprite)
        {
            return;
        }
        var state = State(sprite);
        KnightAudio.Play(sprite, "hurt");
        state.Rig?.Interrupt();
        state.Hurt?.Kill();
        sprite.Position = state.BasePosition;
        sprite.Modulate = Colors.White;
        var tween = sprite.CreateTween();
        for (var i = 0; i < 3; i++)
        {
            tween.TweenProperty(sprite, "position:x", state.BasePosition.X + (i % 2 == 0 ? -5f : 5f), 0.045f);
        }
        tween.TweenProperty(sprite, "position:x", state.BasePosition.X, 0.05f);
        tween.Parallel().TweenProperty(sprite, "modulate:a", 0.35f, 0.08f);
        tween.TweenProperty(sprite, "modulate:a", 1.0f, 0.12f);
        state.Hurt = tween;
    }

    private static AnimatedSprite2D? Play(Sprite2D body, string kind)
    {
        var frames = LoadFrames(kind);
        if (frames == null)
        {
            return null;
        }
        var effect = new AnimatedSprite2D
        {
            Name = $"Knight{kind}Fx",
            SpriteFrames = frames,
            Animation = "play",
            Position = Vector2.Zero,
            ZIndex = 5,
        };
        body.AddChild(effect);
        effect.AnimationFinished += effect.QueueFree;
        effect.Play("play");
        return effect;
    }

    private static SpriteFrames? LoadFrames(string kind)
    {
        if (Frames.TryGetValue(kind, out var cached))
        {
            return cached;
        }
        var frames = new SpriteFrames();
        frames.AddAnimation("play");
        frames.SetAnimationSpeed("play", 24f);
        frames.SetAnimationLoop("play", false);
        for (var i = 0; i < FrameCount; i++)
        {
            var path = $"res://HollowKnight/images/vfx/knight_{kind}_{i:00}.png";
            if (!ResourceLoader.Exists(path))
            {
                MainFile.Logger.Warn($"Knight {kind} effect missing {path}");
                return null;
            }
            frames.AddFrame("play", ResourceLoader.Load<Texture2D>(path));
        }
        Frames.Add(kind, frames);
        return frames;
    }
}
