using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HollowKnight.HollowKnightCode.Character;

/// <summary>
/// Short, code-drawn impacts. They follow the actual target marker and the
/// attack command's hit timing; no scene, texture or gameplay RNG is needed.
/// The combat container owns them and every effect frees itself after 0.45s.
/// </summary>
public partial class KnightImpact : Node2D
{
    public enum Style { Nail, Soul, Void, Dive, Focus, Guard }

    private float _elapsed;
    private Style _style;
    private Vector2? _worldPosition;
    public const float Lifetime = 0.45f;

    public static KnightImpact Create(Style style) => new()
        { Name = $"Knight{style}Impact", _style = style, ZIndex = 2 };

    public static KnightImpact? AtTarget(Creature target, Style style)
    {
        if (target.GetCreatureNode()?.Visuals?.VfxSpawnPosition is not { } marker
            || !GodotObject.IsInstanceValid(marker) || !marker.IsInsideTree()) return null;
        var effect = Create(style);
        effect._worldPosition = marker.GlobalPosition;
        return effect;
    }

    public static void Pulse(Sprite2D body, Style style)
    {
        if (!body.IsInsideTree()) return;
        body.AddChild(new KnightImpact
        {
            Name = $"Knight{style}Pulse", _style = style,
            Position = new Vector2(0f, 70f), ZIndex = 6
        });
    }

    public override void _Ready()
    {
        if (_worldPosition is { } position) GlobalPosition = position;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Lifetime) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = Mathf.Clamp(_elapsed / Lifetime, 0f, 1f);
        var fade = (1f - t) * (1f - t);
        var pale = new Color(0.78f, 0.91f, 1f, fade);
        var white = new Color(0.97f, 0.99f, 1f, fade);
        var violet = new Color(0.59f, 0.48f, 0.88f, fade);
        switch (_style)
        {
            case Style.Nail:
                // A narrow slash and six bright fragments, rather than the
                // spell's circular burst. Readable on light or dark enemies.
                var slash = new Vector2(43f, -26f) * (0.65f + t * 0.55f);
                DrawLine(-slash, slash, new Color(0.1f, 0.16f, 0.24f, fade), 8f, true);
                DrawLine(-slash, slash, white, 3f * (1f - t) + 1f, true);
                Rays(6, t, pale, 14f, 42f);
                break;
            case Style.Dive:
                DrawLine(new Vector2(0f, -90f * (1f - t)), new Vector2(0f, 27f), pale, 8f * (1f - t) + 1f, true);
                DrawEllipse(18f + t * 70f, 0.3f, white);
                Rays(9, t, pale, 20f, 58f);
                break;
            case Style.Void:
                DrawCircle(Vector2.Zero, (1f - t) * 24f, new Color(0.035f, 0.025f, 0.075f, fade));
                DrawArc(Vector2.Zero, 18f + t * 48f, 0f, Mathf.Tau, 48, violet, 3f, true);
                Rays(10, t, pale, 16f, 55f);
                break;
            case Style.Focus:
                DrawArc(Vector2.Zero, 54f - t * 28f, 0f, Mathf.Tau, 64, pale, 2f, true);
                for (var i = 0; i < 7; i++)
                {
                    var angle = i * Mathf.Tau / 7f;
                    var p = Vector2.FromAngle(angle) * (55f * (1f - t));
                    DrawCircle(p, 2.8f, white);
                }
                break;
            case Style.Guard:
                var r = 45f + t * 14f;
                Vector2[] shield = [new(-r * 0.7f, -r * 0.7f), new(0f, -r), new(r * 0.7f, -r * 0.7f),
                    new(r * 0.6f, r * 0.3f), new(0f, r), new(-r * 0.6f, r * 0.3f), new(-r * 0.7f, -r * 0.7f)];
                DrawPolyline(shield, pale, 2.5f, true);
                break;
            default:
                DrawCircle(Vector2.Zero, 12f * (1f - t), white);
                DrawArc(Vector2.Zero, 12f + t * 44f, 0f, Mathf.Tau, 48, pale, 2f, true);
                Rays(8, t, white, 12f, 45f);
                break;
        }
    }

    private void Rays(int count, float t, Color color, float inner, float outer)
    {
        for (var i = 0; i < count; i++)
        {
            var direction = Vector2.FromAngle(i * Mathf.Tau / count + 0.21f);
            var end = inner + (outer - inner) * Mathf.Sqrt(t);
            DrawLine(direction * (end - 9f * (1f - t)), direction * end, color, 1.6f, true);
        }
    }

    private void DrawEllipse(float radius, float squash, Color color)
    {
        var points = new Vector2[49];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = i * Mathf.Tau / (points.Length - 1);
            points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * squash + 25f);
        }
        DrawPolyline(points, color, 3f, true);
    }
}
