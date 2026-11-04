using Godot;

namespace HollowKnight.HollowKnightCode.Character;

/// <summary>
/// A cutout rig built from the existing original Knight art. The feet are the
/// origin; the head, cloak and arm move independently, and the Nail's pivot is
/// always the gripping hand. No whole-body vertical bob or weapon overlay.
/// </summary>
public partial class KnightCombatRig : Node2D
{
    public const float AttackDuration = 0.22f;
    public const float ContactTime = 0.065f;
    private static readonly Color Ink = new("080a10");
    private Node2D _head = null!, _cloak = null!, _weapon = null!, _rearLeg = null!, _frontLeg = null!, _stowed = null!;
    private Line2D _arm = null!;
    private Polygon2D _hand = null!;
    private Vector2 _frontFoot, _shoulder;
    private float _clock, _attackTime = -1f, _slashAlpha;
    private bool _preview;

    public Vector2 GripPosition => _weapon.Position;
    public Vector2 RearFootPosition => new(-9f, 0f);
    public Vector2 FrontFootPosition => _frontFoot;
    public bool IsAttacking => _attackTime >= 0f;

    private readonly record struct Pose(float Time, float Lean, float Crouch,
        float CloakAngle, float HeadAngle, Vector2 Hand, float NailAngle);

    // Actual recovery is longer than the contact delay. Damage lands at the
    // horizontal strike, then the cloak and arm settle while the feet stay down.
    private static readonly Pose[] AttackPoses =
    [
        new(0f, 0f, 0f, 0f, 0f, new(22f, -67f), 24f),
        new(0.025f, -3f, 2f, -3f, -4f, new(-4f, -88f), -170f),
        new(0.05f, 1f, 2f, -5f, -2f, new(15f, -95f), -70f),
        new(ContactTime, 7f, 2f, 7f, 5f, new(35f, -77f), -8f),
        new(0.105f, 6f, 2f, 9f, 2f, new(30f, -68f), 23f),
        new(0.15f, 2f, 1f, -2f, -1f, new(24f, -66f), 27f),
        new(AttackDuration, 0f, 0f, 0f, 0f, new(22f, -67f), 24f)
    ];

    public static KnightCombatRig Create(Sprite2D body)
    {
        var art = ResourceLoader.Load<Texture2D>("res://HollowKnight/images/charui/knight_combat_attack.png")
            ?? throw new InvalidOperationException("Knight cutout art is missing");
        var nail = ResourceLoader.Load<Texture2D>("res://HollowKnight/images/charui/rig/old_nail.png")
            ?? throw new InvalidOperationException("Knight Nail art is missing");
        var rig = new KnightCombatRig { Name = "KnightCombatRig", Position = new Vector2(0f, 238f) };
        rig.Build(art, nail);
        body.AddChild(rig);
        // Hide just the original static bitmap, not this rig or the spell VFX.
        body.SelfModulate = Colors.Transparent;
        return rig;
    }

    private void Build(Texture2D art, Texture2D nail)
    {
        _cloak = Part(art, "Cloak", new Rect2(204, 386, 83, 71), new Vector2(242, 392), 2);
        _head = Part(art, "Head", new Rect2(190, 232, 115, 154), new Vector2(242, 392), 3);
        // Keep the original short, rounded feet. Drawing new straight legs
        // changed the Knight's silhouette and made the first rig look wrong.
        _rearLeg = Part(art, "RearLeg", new Rect2(216, 450, 27, 24), new Vector2(242, 474), 0);
        _frontLeg = Part(art, "FrontLeg", new Rect2(243, 450, 27, 24), new Vector2(242, 474), 0);
        var idle = ResourceLoader.Load<Texture2D>("res://HollowKnight/images/charui/knight_combat.png");
        _stowed = Part(idle, "StowedGrip", new Rect2(186, 374, 40, 36), new Vector2(242, 478), 1);
        _weapon = new Node2D { Name = "NailGrip", ZIndex = 4 };
        AddChild(_weapon);
        // The original Old Nail's grip is at (44, 34); rotation is about that
        // point, rather than about the middle of the blade.
        var blade = new Sprite2D
        {
            Name = "Nail", Texture = nail, Centered = false,
            Offset = new Vector2(-44f, -34f), Scale = Vector2.One * (115f / nail.GetHeight()),
            TextureFilter = TextureFilterEnum.Linear
        };
        _weapon.AddChild(blade);
        _arm = new Line2D
        {
            Name = "HoldingArm", Width = 5f, DefaultColor = Ink, Antialiased = true,
            BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round,
            JointMode = Line2D.LineJointMode.Round, ZIndex = 1
        };
        AddChild(_arm);
        _hand = new Polygon2D { Name = "GrippingHand", Polygon = Ellipse(3.5f, 3.2f), Color = Ink, ZIndex = 5 };
        AddChild(_hand);
        ApplyPose(AttackPoses[0]);
    }

    private Node2D Part(Texture2D source, string name, Rect2 region, Vector2 pivot, int layer)
    {
        var joint = new Node2D { Name = name + "Joint", ZIndex = layer };
        var cutout = new Sprite2D
        {
            Name = name, Texture = new AtlasTexture { Atlas = source, Region = region, FilterClip = true },
            Position = region.Position + region.Size * 0.5f - pivot,
            TextureFilter = TextureFilterEnum.Linear
        };
        joint.AddChild(cutout); AddChild(joint); return joint;
    }

    public void Attack()
    {
        SetProcess(true);
        _preview = false; _attackTime = 0f;
        ApplyPose(AttackPoses[0]);
    }

    public void Interrupt()
    {
        _attackTime = -1f; _slashAlpha = 0f; ApplyPose(AttackPoses[0]);
    }

    /// <summary>Sample the same poses used in combat for a renderer preview.</summary>
    public void SampleAttack(float seconds)
    {
        _preview = true; _attackTime = Mathf.Clamp(seconds, 0f, AttackDuration);
        AnimateAttack();
    }

    public override void _Process(double delta)
    {
        if (_preview) return;
        _clock += (float)delta;
        if (_attackTime >= 0f)
        {
            _attackTime += (float)delta;
            AnimateAttack();
            if (_attackTime >= AttackDuration) _attackTime = -1f;
        }
        else
        {
            // Breathing is confined to the head/cloth. The two feet and the
            // ground shadow never follow a vertical idle tween.
            var breath = Mathf.Sin(_clock * 2.1f);
            ApplyPose(AttackPoses[0] with { Crouch = 0.7f * breath, HeadAngle = 0.45f * breath });
        }
    }

    private void AnimateAttack()
    {
        var pose = AttackPoses[^1];
        for (int i = 1; i < AttackPoses.Length; i++)
        {
            if (_attackTime > AttackPoses[i].Time) continue;
            var a = AttackPoses[i - 1]; var b = AttackPoses[i];
            var t = Mathf.SmoothStep(0f, 1f, (_attackTime - a.Time) / (b.Time - a.Time));
            pose = new Pose(_attackTime, Mathf.Lerp(a.Lean, b.Lean, t), Mathf.Lerp(a.Crouch, b.Crouch, t),
                Mathf.Lerp(a.CloakAngle, b.CloakAngle, t), Mathf.Lerp(a.HeadAngle, b.HeadAngle, t),
                a.Hand.Lerp(b.Hand, t), Mathf.Lerp(a.NailAngle, b.NailAngle, t));
            break;
        }
        // A short motion trail at the strike, never a looping sword animation.
        _slashAlpha = _attackTime >= 0.038f && _attackTime <= 0.125f
            ? Mathf.Sin((_attackTime - 0.038f) / 0.087f * Mathf.Pi) : 0f;
        ApplyPose(pose);
    }

    private void ApplyPose(Pose pose)
    {
        var step = Mathf.Max(0f, pose.Lean) * 0.3f;
        _frontFoot = new Vector2(10f + step, 0f);
        _rearLeg.Position = Vector2.Zero;
        _frontLeg.Position = new Vector2(step, 0f);
        bool holding = _attackTime > 0.022f && _attackTime < 0.086f;
        _weapon.Visible = holding; _arm.Visible = holding; _hand.Visible = holding;
        _stowed.Visible = !holding;
        var neck = new Vector2(pose.Lean, -86f + pose.Crouch);
        _cloak.Position = neck; _cloak.Rotation = Mathf.DegToRad(pose.CloakAngle);
        _head.Position = neck + new Vector2(-pose.Lean * 0.1f, -0.6f);
        _head.Rotation = Mathf.DegToRad(pose.HeadAngle);
        _shoulder = neck + new Vector2(7f, 7f);
        var grip = pose.Hand;
        var direction = grip - _shoulder;
        var length = direction.Length();
        var normal = length > 0.01f ? new Vector2(-direction.Y, direction.X) / length : Vector2.Down;
        // A short forearm emerges from behind the cloak. No visible full
        // shoulder/elbow loop drawn across the front of the torso.
        var elbow = _shoulder.Lerp(grip, 0.6f) + normal * 3f;
        _arm.Points = [_shoulder, elbow, grip];
        _hand.Position = grip; _hand.Rotation = Mathf.DegToRad(pose.NailAngle);
        _weapon.Position = grip; _weapon.Rotation = Mathf.DegToRad(pose.NailAngle - 90f);
        _weapon.ZIndex = pose.NailAngle < -70f ? 1 : 4;
        _hand.ZIndex = pose.NailAngle < -70f ? 1 : 5;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Contact shadow is anchored to the ground, independent of the torso.
        DrawSetTransform(new Vector2(2f, 3f), 0f, new Vector2(1f, 0.19f));
        DrawCircle(Vector2.Zero, 39f, new Color(0f, 0f, 0f, 0.25f));
        DrawSetTransform(Vector2.Zero);
        if (_slashAlpha > 0f)
        {
            // The supplied game frames show a broad, brief crescent. The
            // blade is barely visible; the white sweep carries the motion.
            var center = new Vector2(12f, -122f);
            var progress = Mathf.Clamp((_attackTime - 0.038f) / 0.087f, 0f, 1f);
            var tail = Mathf.SmoothStep(0.4f, 1f, progress);
            var start = Mathf.Lerp(-130f, 42f, tail);
            DrawCrescent(center, 205f, 129f, 180f, 111f, start, 137f,
                new Color(0.96f, 0.96f, 0.87f, _slashAlpha * 0.09f));
            DrawCrescent(center, 196f, 122f, 177f, 111f, start, 137f,
                new Color(1f, 0.99f, 0.94f, _slashAlpha * 0.86f));
            DrawCrescent(center, 192f, 119f, 180f, 112f, start + 2f, 135f,
                new Color(1f, 1f, 0.98f, _slashAlpha * 0.52f));
        }
    }

    private void DrawCrescent(Vector2 center, float rx, float ry, float innerX, float innerY, float start, float end, Color color)
    {
        const int segments = 36;
        var points = new Vector2[(segments + 1) * 2];
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.DegToRad(Mathf.Lerp(start, end, (float)i / segments));
            points[i] = center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
            float taper = Mathf.Sin(Mathf.Pi * i / segments);
            points[points.Length - 1 - i] = center + new Vector2(
                Mathf.Cos(angle) * Mathf.Lerp(rx, innerX, taper) - 21f * taper,
                Mathf.Sin(angle) * Mathf.Lerp(ry, innerY, taper));
        }
        // The tapered ends coincide. Explicit strip triangles avoid sending
        // duplicate/near-zero edges to the concave polygon triangulator.
        for (int i = 0; i < segments; i++)
        {
            var a = points[i]; var b = points[i + 1];
            var c = points[points.Length - 2 - i]; var d = points[points.Length - 1 - i];
            if (Mathf.Abs((b - a).Cross(c - a)) > 0.01f) DrawColoredPolygon([a, b, c], color);
            if (Mathf.Abs((c - a).Cross(d - a)) > 0.01f) DrawColoredPolygon([a, c, d], color);
        }
    }

    private static Vector2[] Ellipse(float rx, float ry) => Enumerable.Range(0, 20)
        .Select(i => new Vector2(Mathf.Cos(i * Mathf.Tau / 20f) * rx, Mathf.Sin(i * Mathf.Tau / 20f) * ry)).ToArray();
}
