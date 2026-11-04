using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace HollowKnight.HollowKnightCode.Resources;

/// <summary>
/// Custom SOUL counter for the combat HUD: the soul-orb icon with the "n/99"
/// label BESIDE it. BaseLib's built-in NAdditionalResourceDisplay stamps the
/// label on top of the icon on a fixed 60px orbit around the energy orb,
/// where the two overlap; this one sits to the right of the energy orb, slides in
/// at combat start, ticks up with a pulse + soul wisps when SOUL is gathered,
/// and shows the SOUL hover tip.
/// </summary>
public sealed partial class NSoulCounter : Control
{
    /// <summary>Pale soul blue shared with Soul.MainColor (gen_art.py SOUL_SOFT).</summary>
    public static readonly Color SoulBlue = new(0.60f, 0.78f, 0.92f);

    private const float IconSide = 44f;
    private const string FontPath = "res://themes/kreon_bold_glyph_space_two.tres";

    private readonly Soul _soul;
    private readonly TextureRect _icon;
    private readonly Label _label;
    private Tween? _countTween;
    private Tween? _pulseTween;
    private bool _listening;
    private bool _disabled;
    private bool _mouseEnteredConnected;
    private bool _mouseExitedConnected;
    private Control? _energyContainer;
    private bool _resizeSubscribed;

    public NSoulCounter(Soul soul)
    {
        _soul = soul;

        MouseFilter = MouseFilterEnum.Pass;
        Name = "KnightSoulCounter";
        Size = new Vector2(180f, 48f);
        PivotOffset = Size / 2f;

        _icon = new TextureRect
        {
            Texture = PreloadManager.Cache.GetTexture2D(soul.TexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(IconSide, Size.Y),
        };
        AddChild(_icon);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = new Vector2(IconSide + 8f, 0f),
            Size = new Vector2(Size.X - IconSide - 8f, Size.Y),
        };
        var font = PreloadManager.Cache.GetAsset<Font>(FontPath);
        _label.AddThemeFontOverride("font", font);
        _label.AddThemeFontSizeOverride("font_size", 28);
        _label.AddThemeColorOverride("font_color", StsColors.cream);
        _label.AddThemeColorOverride("font_outline_color", new Color("172735"));
        _label.AddThemeConstantOverride("outline_size", 4);
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.18f));
        _label.AddThemeConstantOverride("shadow_offset_x", 1);
        _label.AddThemeConstantOverride("shadow_offset_y", 1);
        AddChild(_label);

        SetTextCore(soul.Amount);
    }

    public override void _Ready()
    {
        Guard(() =>
        {
            if (!_listening)
            {
                _soul.AmountChanged += OnAmountChanged;
                _listening = true;
            }
            if (!_mouseEnteredConnected)
            {
                MouseEntered += OnHovered;
                _mouseEnteredConnected = true;
            }
            if (!_mouseExitedConnected)
            {
                MouseExited += OnUnhovered;
                _mouseExitedConnected = true;
            }
        });
    }

    public override void _ExitTree()
    {
        Cleanup();
    }

    public void AttachBesideEnergy(Control energyContainer)
    {
        Guard(() =>
        {
            DetachLayout();
            if (!GodotObject.IsInstanceValid(energyContainer) || energyContainer.IsQueuedForDeletion())
                throw new ObjectDisposedException(nameof(energyContainer));
            energyContainer.AddChild(this);
            if (_disabled) return;
            _energyContainer = energyContainer;
            PositionCounter();
            if (_disabled) return;
            energyContainer.Resized += PositionCounter;
            _resizeSubscribed = true;
        });
    }

    private void PositionCounter() => Guard(() =>
    {
        if (!GodotObject.IsInstanceValid(_energyContainer) || _energyContainer!.IsQueuedForDeletion())
            throw new ObjectDisposedException("Energy counter container");
        var energySize = _energyContainer.Size;
        if (energySize.X < 1f || energySize.Y < 1f) energySize = new Vector2(128f, 128f);
        Position = new Vector2(energySize.X + 24f, (energySize.Y - Size.Y) * 0.5f);
    });

    private void OnAmountChanged(int oldAmount, int newAmount) => Guard(() =>
    {
        // This event runs synchronously inside real resource Gain/Spend calls.
        // Presentation failures must never escape back into their payment flow.
        StopTween(ref _countTween);
        if (newAmount > oldAmount)
        {
            SpawnWisps();
            _countTween = CreateTween();
            _countTween.TweenMethod(Callable.From<float>(v => SetText((int)MathF.Round(v))),
                (float)oldAmount, (float)newAmount, 0.25f);
            StopTween(ref _pulseTween);
            Scale = Vector2.One;
            _pulseTween = CreateTween();
            _pulseTween.TweenProperty(this, "scale", new Vector2(1.15f, 1.15f), 0.09f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            _pulseTween.TweenProperty(this, "scale", Vector2.One, 0.18f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        }
        else
        {
            SetTextCore(newAmount);
        }
    });

    // Tween callbacks execute after OnAmountChanged returns, so they need
    // their own guard rather than relying on the event handler's try/catch.
    private void SetText(int amount) => Guard(() => SetTextCore(amount));

    private void SetTextCore(int amount)
    {
        _label.Text = _soul.GetTextForAmount(amount);
        _label.AddThemeColorOverride("font_color", amount == 0 ? StsColors.red : StsColors.cream);
        _icon.Modulate = amount == 0 ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
    }

    private void SpawnWisps()
    {
        // Resolve the optional asset now, inside the guarded callback. The
        // particle factory can run later, after the original event returns.
        var texture = PreloadManager.Cache.GetTexture2D(_soul.TexturePath);
        var ps = new ParticleSystemBuilder()
            .AddParticle(() => texture, 3, 0.9f)
            .GrowFade()
            .Build();
        try
        {
            AddChild(ps);
            if (ps is Node2D n2) n2.Position = Size / 2f;
            MoveChild(ps, 0);
        }
        catch
        {
            if (GodotObject.IsInstanceValid(ps)) ps.Free();
            throw;
        }
    }

    private void OnHovered() => Guard(() =>
    {
        var tip = _soul.MakeTip();
        if (tip.HasValue)
        {
            var shown = NHoverTipSet.CreateAndShow(this, tip.Value, (HoverTipAlignment)0);
            shown?.SetGlobalPosition(GlobalPosition + new Vector2(-30f, -300f));
        }
    });

    private void OnUnhovered() => Guard(() => NHoverTipSet.Remove(this));

    private void Guard(Action update)
    {
        if (_disabled) return;
        try
        {
            if (!GodotObject.IsInstanceValid(this) || IsQueuedForDeletion())
            {
                Disable();
                return;
            }
            update();
        }
        catch (Exception error) { Disable(error); }
    }

    private void Disable(Exception? error = null)
    {
        _disabled = true;
        Cleanup();
        TryCleanup(() =>
        {
            if (!GodotObject.IsInstanceValid(this)) return;
            Visible = false;
            MouseFilter = MouseFilterEnum.Ignore;
            SetProcess(false);
        });
        if (error != null) PatchInstaller.WarnOptional(typeof(NSoulCounter), error);
    }

    private void Cleanup()
    {
        // Clear managed subscriptions even if a native node or tween has
        // already been freed. Each cleanup step is independent and contained.
        if (_listening)
        {
            _listening = false;
            TryCleanup(() => _soul.AmountChanged -= OnAmountChanged);
        }
        TryCleanup(() => StopTween(ref _countTween));
        TryCleanup(() => StopTween(ref _pulseTween));
        DetachLayout();
        if (_mouseEnteredConnected)
        {
            _mouseEnteredConnected = false;
            TryCleanup(() => { if (GodotObject.IsInstanceValid(this)) MouseEntered -= OnHovered; });
        }
        if (_mouseExitedConnected)
        {
            _mouseExitedConnected = false;
            TryCleanup(() => { if (GodotObject.IsInstanceValid(this)) MouseExited -= OnUnhovered; });
        }
        TryCleanup(() => { if (GodotObject.IsInstanceValid(this)) NHoverTipSet.Remove(this); });
    }

    private void DetachLayout()
    {
        var container = _energyContainer;
        bool subscribed = _resizeSubscribed;
        _energyContainer = null;
        _resizeSubscribed = false;
        TryCleanup(() =>
        {
            if (subscribed && GodotObject.IsInstanceValid(container))
                container!.Resized -= PositionCounter;
        });
    }

    private static void StopTween(ref Tween? slot)
    {
        var tween = slot;
        slot = null;
        if (GodotObject.IsInstanceValid(tween)) tween!.Kill();
    }

    private static void TryCleanup(Action cleanup)
    {
        try { cleanup(); }
        catch (Exception error) { PatchInstaller.WarnOptional(typeof(NSoulCounter), error); }
    }
}
