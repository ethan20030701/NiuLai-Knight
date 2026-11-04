using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Extensions;
using HollowKnight.HollowKnightCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HollowKnight.HollowKnightCode.Character;

public class HollowKnight : PlaceholderCharacterModel
{
    public const string CharacterId = "HollowKnight";

    public static readonly Color Color = new("d6e9ff"); //pale shell blue

    public override Color NameColor => Color;
    // vanilla characters use dark outlines behind the cream energy digits
    public override Color EnergyLabelOutlineColor => new(0.13f, 0.18f, 0.30f);
    public override Color MapDrawingColor => new(0.60f, 0.78f, 0.92f); // soul-soft path on the map
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    public override float AttackAnimDelay => KnightCombatRig.ContactTime;
    public override float CastAnimDelay => 0.22f;

    public override string CustomAttackSfx => PatchInstaller.IsInstalled(typeof(Patches.KnightInheritedAudioPatch))
        ? KnightAudio.ProceduralCue : base.CustomAttackSfx;
    public override string CustomCastSfx => PatchInstaller.IsInstalled(typeof(Patches.KnightInheritedAudioPatch))
        ? KnightAudio.ProceduralCue : base.CustomCastSfx;
    public override string CustomDeathSfx => PatchInstaller.IsInstalled(typeof(Patches.KnightInheritedAudioPatch))
        ? KnightAudio.ProceduralCue : base.CustomDeathSfx;


    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<OldNail>(),
        ModelDb.Card<OldNail>(),
        ModelDb.Card<OldNail>(),
        ModelDb.Card<OldNail>(),
        ModelDb.Card<Shell>(),
        ModelDb.Card<Shell>(),
        ModelDb.Card<Shell>(),
        ModelDb.Card<WeaverNest>(),
        ModelDb.Card<Focus>(),
        ModelDb.Card<VengefulSpirit>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<SoulCatcher>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<HollowKnightCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<HollowKnightRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<HollowKnightPotionPool>();

    /*  BaseLib registers these paths for automatic scene conversion
        (SceneConversionPatch: Sprite2D root -> NRestSiteCharacter /
        NMerchantCharacter built from the texture), so shipping a plain
        sprite scene is enough — the ironclad placeholder scenes are gone.
        Each path is pre-flighted like the select bg: if the scene ever
        fails to load we fall back to the ironclad scene instead of
        breaking the room. */
    private const string RestSiteScenePath = "res://HollowKnight/scenes/knight_rest_site.tres";
    private const string MerchantScenePath = "res://HollowKnight/scenes/knight_merchant.tres";
    private const string TrailScenePath = "res://HollowKnight/scenes/knight_trail.tres";

    private string? _restSitePath;
    private string? _merchantPath;
    private string? _trailPath;

    /*  knight-styled soul orb for the energy counter: BaseLib intercepts
        NEnergyCounter.Create and builds it from these 5 layers
        (L1 backing, L2/L3 rotating rune rings, L4 core, L5 shine).
        OutlineColor drives the number's text outline — dark navy so the
        white energy figures stay readable on the pale orb */
    public override CustomEnergyCounter? CustomEnergyCounter => new CustomEnergyCounter(
        layer => $"res://HollowKnight/images/charui/energy_layer_{layer}.png",
        new Color(0.13f, 0.16f, 0.27f),
        new Color(0.55f, 0.7f, 0.95f));

    public override string CustomRestSiteAnimPath =>
        _restSitePath ??= PreflightScene(RestSiteScenePath, base.CustomRestSiteAnimPath);

    public override string CustomMerchantAnimPath =>
        _merchantPath ??= PreflightScene(MerchantScenePath, base.CustomMerchantAnimPath);

    /*  soul-wisp card trail: root NCardTrailVfx + CPUParticles2D wisps
        (pre-flighted — a broken trail scene must never break card plays) */
    public override string CustomTrailPath =>
        _trailPath ??= PreflightScene(TrailScenePath, base.CustomTrailPath!);

    private static string PreflightScene(string path, string fallback)
    {
        try
        {
            return Godot.FileAccess.FileExists(path)
                   && ResourceLoader.Load(path, null, ResourceLoader.CacheMode.Reuse) is PackedScene
                ? path
                : fallback;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Scene {path} failed to load, using fallback: {e.Message}");
            return fallback;
        }
    }

    /*  matches the pre-rendered death animation (79 frames @ 30fps);
        BaseLib caps the post-death wait at 5s */
    public override float DeathAnimTime => 2.7f;

    /*  BaseLib builds the creature wrapper from the existing combat texture.
        A grounded joint rig animates its head, cloak, arm and held Nail.
        Failure anywhere falls back to the placeholder without crashing. */
    public override NCreatureVisuals CreateCustomVisuals()
    {
        try
        {
            var visuals = NodeFactory<NCreatureVisuals>.CreateFromResource(
                PreloadManager.Cache.GetTexture2D("charui/knight_combat.png".ImagePath()));
            if (visuals.GetNodeOrNull<Sprite2D>("%Visuals") is { } sprite)
            {
                // The rig uses pixel 478 of the 480px canvas as its foot
                // origin. Keep that origin at the creature's ground (y=0).
                sprite.Position = new Vector2(0f, -238f);
                KnightFeedback.Register(sprite);
                // Factory bounds use the entire transparent canvas. Align
                // selection and hit effects with the actual visible body.
                if (visuals.GetNodeOrNull<Control>("Bounds") is { } bounds)
                {
                    bounds.Position = new Vector2(-70f, -248f);
                    bounds.Size = new Vector2(140f, 250f);
                    if (visuals.GetNodeOrNull<Control>("%FormVfx") is { } form)
                    {
                        form.Position = bounds.Position;
                        form.Size = bounds.Size;
                    }
                }
                if (visuals.GetNodeOrNull<Marker2D>("%CenterPos") is { } center) center.Position = new Vector2(0f, -130f);
                if (visuals.GetNodeOrNull<Marker2D>("IntentPos") is { } intent) intent.Position = new Vector2(0f, -285f);
                AddDeathAnimation(visuals, sprite);
            }
            else
            {
                MainFile.Logger.Warn("Custom knight visuals missing %Visuals sprite; joint rig skipped.");
            }
            return visuals;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Custom knight visuals failed to build, falling back to placeholder: {e.Message}");
            return null!; // BaseLib falls back to the placeholder VisualsPath scene
        }
    }

    /*  BaseLib's CustomAnimationPatch plays "Dead"/"Die"/"die" on any
        AnimatedSprite2D under the creature when the death trigger fires and
        no Spine skeleton exists. Frames are pre-rendered from the wiki's
        The_Knight_Death.mp4 with the background matted out; frame 0 is fully
        transparent so the sprite renders nothing until the death plays, and
        the first real frame change hides the living visuals underneath —
        without that, the death frames would overlap the standing figure. */
    private static void AddDeathAnimation(NCreatureVisuals visuals, Sprite2D sprite)
    {
        try
        {
            var frames = new SpriteFrames();
            frames.AddAnimation("Dead");
            frames.SetAnimationSpeed("Dead", 30f);
            frames.SetAnimationLoop("Dead", false);

            var count = 0;
            for (var i = 0; i < 128; i++)
            {
                var path = $"res://HollowKnight/images/charui/death/death_f{i:000}.png";
                if (!ResourceLoader.Exists(path))
                {
                    break;
                }
                frames.AddFrame("Dead", ResourceLoader.Load<Texture2D>(path));
                count++;
            }
            if (count < 2)
            {
                MainFile.Logger.Warn($"Knight death anim: only {count} frames found, skipped.");
                return;
            }
            // sibling of %Visuals (not a child): hiding the living visuals at
            // death must not hide this sprite with them
            var death = new AnimatedSprite2D
            {
                Name = "KnightDeathAnim",
                SpriteFrames = frames,
                Position = sprite.Position,
            };
            var deathStarted = false;
            death.AnimationFinished += () => deathStarted = false;
            death.FrameChanged += () =>
            {
                if (!deathStarted && death.IsPlaying() && death.Animation == "Dead")
                {
                    deathStarted = true;
                    sprite.Visible = false;
                    KnightFeedback.Stop(sprite);
                    KnightAudio.Play(sprite, "void");
                }
            };
            visuals.AddChild(death);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Knight death animation skipped: {e.Message}");
        }
    }

    /*  The select-screen backdrop is a scene shipped inside the pck. The quick
        pck packer refuses the ".tscn" extension, but ".tres" passes it and the
        text resource loader recognizes the gd_scene content, so the file is a
        .tres that is actually a PackedScene. Falls back to the placeholder
        scene if it is ever missing. */
    private string? _selectBgPath;

    public override string CustomCharacterSelectBg
    {
        get
        {
            if (_selectBgPath == null)
            {
                const string path = "res://HollowKnight/scenes/knight_select_bg.tres";
                // Pre-flight: only use our scene if it exists AND loads as a
                // PackedScene; otherwise fall back to the placeholder backdrop.
                _selectBgPath = Godot.FileAccess.FileExists(path)
                                && ResourceLoader.Load(path, null, ResourceLoader.CacheMode.Reuse) is PackedScene
                    ? path
                    : base.CustomCharacterSelectBg;
            }
            return _selectBgPath;
        }
    }

    public override string? CustomIconOutlineTexturePath => "character_icon_char_name_outline.png".CharacterUiPath();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
