using System.Runtime.CompilerServices;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using HollowKnight.HollowKnightCode.Extensions;
using HollowKnight.HollowKnightCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HollowKnight.HollowKnightCode.Resources;

/// <summary>SOUL occupies the game's secondary-cost slot beside energy, like the Regent's Stars.</summary>
public partial class NSoulCostBadge : NAdditionalCostDisplay
{
    public NSoulCostBadge() : base(Soul.ResourceId, "charui/soul_meter.png".ImagePath(), Colors.White)
    {
        Name = "SoulCostBadge";
        Size = new Vector2(42f, 44f);
        PivotOffset = Vector2.Zero;
        // Keep the cost in its card's draw order. A raised ZIndex lets background
        // deck badges paint over the inspect screen and the enlarged description.
        ZIndex = 0;

        // Match the native energy number's light fill and substantial dark outline.
        _label.Reparent(this);
        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        _label.Position = new Vector2(1f, 1f);
        _label.Size = new Vector2(40f, 40f);
        _label.MinFontSize = 22;
        _label.MaxFontSize = 22;
        _label.AddThemeFontSizeOverride("font_size", 22);
        _label.AddThemeConstantOverride("outline_size", 8);
        _label.AddThemeConstantOverride("shadow_outline_size", 0);
        _label.AddThemeConstantOverride("shadow_offset_x", 0);
        _label.AddThemeConstantOverride("shadow_offset_y", 0);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        _icon.Position = new Vector2(1f, 1f);
        _icon.Size = new Vector2(40f, 40f);
        _icon.SelfModulate = new Color(1f, 1f, 1f, 0.95f);
    }

    public void Refresh(NCard card, ICustomResourceCost cost, PileType pileType)
    {
        UpdateCostVisual(card, cost, pileType);
        FinishLayout(card);
    }

    public void RefreshManual(NCard card, PileType pileType)
    {
        Visible = card.Model != null && card.Visibility == ModelVisibility.Visible;
        if (!Visible) { _label.SetTextAutoSize(string.Empty); return; }
        _label.SetTextAutoSize(card.Model is SoulTotem ? Soul.SpellCost.ToString() : "X");
        var color = StsColors.cream;
        int minimum = card.Model is SoulTotem or SoulUnbound ? Soul.SpellCost : 0;
        if (pileType == PileType.Hand
            && CustomResources<Soul>.TryGet(card.Model!.Owner?.PlayerCombatState, out var soul)
            && soul.Amount < minimum)
            color = StsColors.red;
        _label.AddThemeColorOverride("font_color", color);
        _unplayableIcon.Visible = false;
        FinishLayout(card);
    }

    private void FinishLayout(NCard card)
    {
        var color = _label.GetThemeColor("font_color");
        _label.AddThemeFontOverride("font", card.GetNode<Control>("%EnergyLabel").GetThemeFont("font"));
        var outline = color == StsColors.green ? StsColors.energyGreenOutline
            : color == StsColors.red ? StsColors.unplayableEnergyCostOutline
            : color == StsColors.energyBlue ? StsColors.energyBlueOutline
            : card.Model!.Pool.EnergyOutlineColor;
        _label.AddThemeColorOverride("font_outline_color", outline);
        // Follow the actual native StarIcon slot in both game versions and enlarged previews.
        var secondaryCost = card.GetNode<Control>("%StarIcon");
        Position = ((CanvasItem)GetParent()).GetGlobalTransform().AffineInverse()
            * secondaryCost.GlobalPosition + (secondaryCost.Size - Size) * 0.5f;
        if (secondaryCost.Visible)
        {
            // Some effects can add a real Star cost; keep both resource costs visible.
            Position += new Vector2(0f, secondaryCost.Size.Y + 2f);
        }
    }
}

/// <summary>Uses the card's reload lifecycle without BaseLib's orbiting AroundCost placement.</summary>
[HarmonyPatch]
[OptionalPatch("SOUL card cost badges")]
public static class SoulCardUi
{
    private static readonly ConditionalWeakTable<NCard, NSoulCostBadge> Badges = new();
    private static readonly ConditionalWeakTable<NCard, object> FailedCards = new();

    private static IEnumerable<MethodBase> TargetMethods() =>
    [AccessTools.Method(typeof(NCard), "Reload", Type.EmptyTypes),
        AccessTools.Method(typeof(NCard), nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)])];

    public static bool HasManualCost(CardModel card) => card is SoulTotem or SoulUnbound or SoulAegis;

    private static NSoulCostBadge GetBadge(NCard card)
    {
        if (Badges.TryGetValue(card, out var old) && !GodotObject.IsInstanceValid(old))
            Badges.Remove(card);
        return Badges.GetValue(card, node =>
        {
            // Preflight the scene before allocating/attaching a custom node.
            var body = node.GetNodeOrNull<Control>("%CardContainer")
                ?? throw new InvalidOperationException("Card scene has no %CardContainer.");
            var secondaryCost = node.GetNodeOrNull<Control>("%StarIcon")
                ?? throw new InvalidOperationException("Card scene has no %StarIcon.");
            if (node.GetNodeOrNull<Control>("%EnergyLabel") == null)
                throw new InvalidOperationException("Card scene has no %EnergyLabel.");
            var created = new NSoulCostBadge();
            try
            {
                body.AddChild(created);
                body.MoveChild(created, Math.Min(secondaryCost.GetIndex() + 1, body.GetChildCount() - 1));
                // Reload precedes the native cost update; follow later changes
                // to the Star slot, and disconnect if the badge is removed.
                void OnVisibilityChanged() => Reload(node);
                secondaryCost.VisibilityChanged += OnVisibilityChanged;
                created.TreeExiting += () =>
                {
                    if (GodotObject.IsInstanceValid(secondaryCost))
                        secondaryCost.VisibilityChanged -= OnVisibilityChanged;
                };
                return created;
            }
            catch
            {
                created.Free();
                throw;
            }
        });
    }

    public static void Update(NCard card, ICustomResourceCost cost, PileType pileType)
    {
        try
        {
            if (PatchInstaller.IsInstalled(typeof(SoulCardUi)) && GodotObject.IsInstanceValid(card)
                && !FailedCards.TryGetValue(card, out _) && !card.IsQueuedForDeletion()
                && card.IsNodeReady() && card.Model != null)
                GetBadge(card).Refresh(card, cost, pileType);
        }
        catch (Exception error) { Downgrade(card, error); }
    }

    [HarmonyPostfix]
    private static void Reload(NCard __instance)
    {
        try
        {
            if (!PatchInstaller.IsInstalled(typeof(SoulCardUi)) || !GodotObject.IsInstanceValid(__instance)
                || FailedCards.TryGetValue(__instance, out _)
                || __instance.IsQueuedForDeletion() || !__instance.IsNodeReady()) return;
            var cost = __instance.Model == null ? null : CustomResources<Soul>.Cost(__instance.Model);
            if (cost != null) Update(__instance, cost, __instance.DisplayingPile);
            else if (__instance.Model != null && HasManualCost(__instance.Model))
                GetBadge(__instance).RefreshManual(__instance, __instance.DisplayingPile);
            else if (Badges.TryGetValue(__instance, out var badge) && GodotObject.IsInstanceValid(badge))
                badge.Visible = false;
        }
        catch (Exception error) { Downgrade(__instance, error); }
    }

    private static void Downgrade(NCard card, Exception error)
    {
        if (card != null)
        {
            FailedCards.GetValue(card, _ => new object());
            if (Badges.TryGetValue(card, out var badge) && GodotObject.IsInstanceValid(badge))
                badge.Visible = false;
        }
        PatchInstaller.WarnOptional(typeof(SoulCardUi), error);
    }
}
