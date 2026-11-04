using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using BaseLib.Patches.UI;
using Godot;
using HollowKnight.HollowKnightCode.Extensions;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HollowKnight.HollowKnightCode.Resources;

/// <summary>
/// SOUL, the Knight's combat resource. Nail strikes gather SOUL; spells and Focus spend it.
/// BaseLib 3.4.7 renders custom resources via BasicCustomResource, but its built-in
/// counter (label stamped on the icon, orbiting 60px from the energy orb) collides
/// with the orb itself and can never be repositioned, so Soul overrides
/// RegisterResourceVisuals: the combat counter is our own NSoulCounter beside the
/// energy orb, while card costs use the game's native secondary-cost position.
/// StartOfTurnReset is deliberately a no-op: BasicCustomResource would refill to
/// DefaultMax every turn, but SOUL never self-regenerates — it only comes from nail
/// strikes and HP loss.
/// </summary>
public class Soul : BasicCustomResource
{
    public const string ResourceId = "Soul";

    /// <summary>Soul gained per nail strike, matching Hollow Knight.</summary>
    public const int PerNailHit = 11;

    /// <summary>Soul required to cast a spell or Focus, matching Hollow Knight.</summary>
    public const int SpellCost = 33;

    /// <summary>Maximum soul the vessel can hold.</summary>
    public const int MaxSoul = 99;

    public Soul() : base(ResourceId)
    {
        DefaultMax = MaxSoul;
    }

    /// <summary>Glowing soul orb shown by the counter, on cards, and in the hover tip.</summary>
    public override string TexturePath => "charui/soul_orb.png".ImagePath();

    public override string? IconPath => TexturePath;

    /// <summary>Pale soul-blue outline for card SOUL costs.</summary>
    public override Color MainColor => NSoulCounter.SoulBlue;

    public override int Amount
    {
        get => base.Amount;
        set => base.Amount = Math.Clamp(value, 0, MaxSoul);
    }

    public override async Task<bool> Spend<T>(ICombatState combatState, AbstractModel? spender, int amount, bool optional)
    {
        int beforeAmount = Amount;
        int paid = Math.Clamp(amount, 0, beforeAmount);
        bool spent = await base.Spend<T>(combatState, spender, amount, optional);
        if (spender is CardModel card)
        {
            // Record after resource hooks finish so a nested free play cannot claim this payment.
            SoulSpendTracker.QueuePayment(card, spent ? paid : 0, spent && paid > 0 ? beforeAmount : null);
        }
        return spent;
    }

    public static bool TrySpendManual(Player player, CardPlay cardPlay, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }
        if (!CustomResources<Soul>.TryGet(player.PlayerCombatState, out Soul? soul) || soul.Amount < amount)
        {
            return false;
        }
        soul.ModifyAmount(-amount);
        SoulSpendTracker.RecordManual(cardPlay, amount);
        return true;
    }

    public static int SpendAll(Player player, CardPlay cardPlay)
    {
        if (!CustomResources<Soul>.TryGet(player.PlayerCombatState, out Soul? soul))
        {
            return 0;
        }
        int paid = soul.Amount;
        soul.Amount = 0;
        SoulSpendTracker.RecordManual(cardPlay, paid);
        return paid;
    }

    /// <summary>Hover tip with our own (prefixed) loc keys — the base impl looks up "Soul.title",
    /// which would collide with any other mod registering a resource named Soul.</summary>
    public override HoverTip? MakeTip()
    {
        LocString title = new("static_hover_tips", "HOLLOWKNIGHT-SOUL.title");
        LocString description = new("static_hover_tips", "HOLLOWKNIGHT-SOUL.description");
        title.Add("resourceIcon", $"[img]{TexturePath}[/img]");
        description.Add("resourceIcon", $"[img]{TexturePath}[/img]");
        return new HoverTip(title, description, null);
    }

    public override void StartOfTurnReset(PlayerCombatState playerCombatState, ICombatState combatState)
    {
    }

    /// <summary>
    /// Replaces BasicCustomResource.RegisterResourceVisuals. The combat counter is
    /// registered without ExtraCombatUi auto-positioning (None) and placed manually
    /// just above the energy orb — BaseLib's "AroundEnergy" orbit is where it used to
    /// overlap the orb. Only built for the Knight so co-op partners don't see a
    /// phantom 0/99 counter (the resource SpireField lazily creates instances for
    /// any player's combat state).
    /// </summary>
    public override void RegisterResourceVisuals<T>()
    {
        ValidateType<T>("RegisterResourceVisuals");

        ExtraCombatUi.RegisterCombatUiElement((ui, player, combatState) =>
        {
            NSoulCounter? counter = null;
            try
            {
                var pcs = player.PlayerCombatState;
                if (pcs == null || player.Character is not Knight
                    || CustomResources<T>.Get(pcs) is not Soul soul) return null;
                counter = new NSoulCounter(soul);
                var container = ui.GetNodeOrNull<Control>("%EnergyCounterContainer");
                if (container != null)
                {
                    counter.AttachBesideEnergy(container);
                }
                else
                {
                    ui.AddChild(counter);
                    counter.Position = new Vector2(260f, 846f);
                    MainFile.Logger.Warn("SOUL counter: %EnergyCounterContainer not found, using fallback position.");
                }
                return counter;
            }
            catch (Exception error)
            {
                try
                {
                    if (counter != null && GodotObject.IsInstanceValid(counter))
                    {
                        if (counter.IsInsideTree()) counter.QueueFree();
                        else counter.Free();
                    }
                }
                catch { /* A failed cosmetic cleanup must not escape into combat. */ }
                PatchInstaller.WarnOptional(typeof(NSoulCounter), error);
                return null;
            }
        }, ExtraCombatUi.CombatUiPositioning.None);

        CustomResources<T>.UpdateCostVisuals += SoulCardUi.Update;
    }

    public static void Gain(Player player, int amount)
    {
        var combatState = player.PlayerCombatState;
        if (combatState == null)
        {
            MainFile.Logger.Warn("Attempted to gain SOUL outside of combat.");
            return;
        }
        CustomResources<Soul>.Get(combatState).ModifyAmount(amount);
    }
}
