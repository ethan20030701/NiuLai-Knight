using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The spell-side twin of Kingsoul: a Spell cast while holding 66+ SOUL
/// strikes harder. The cost has been paid by damage time, so add back that
/// CardPlay's true payment snapshot to compare the pre-cast balance. Only powered
/// spell damage is amplified; raw void splashes stay raw.
/// </summary>
public sealed class FullSoulPower : HollowKnightPower
{
    private CardPlay? _spellPlay;
    private bool _wasFull;
    /// <summary>SOUL held required for the bonus to apply (2 full charges).</summary>
    public const int SoulThreshold = 66;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != base.Owner || !props.IsPoweredAttack()
            || !NailRules.IsSpellCard(cardSource))
        {
            return 0m;
        }
        var player = base.Owner.Player;
        if (player?.PlayerCombatState == null || !CustomResources<Soul>.TryGet(player.PlayerCombatState, out var soul))
        {
            return 0m;
        }
        return (cardPlay == null ? soul.Amount >= SoulThreshold
            : ReferenceEquals(cardPlay, _spellPlay) && _wasFull) ? Amount : 0m;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player && NailRules.IsSpellCard(cardPlay.Card))
        {
            _spellPlay = cardPlay;
            var soul = CustomResources<Soul>.Get(Owner.Player!.PlayerCombatState!);
            _wasFull = (SoulSpendTracker.AmountBeforePayment(cardPlay)
                ?? soul.Amount + SoulSpendTracker.AutomaticAmountSpent(cardPlay)) >= SoulThreshold;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (ReferenceEquals(cardPlay, _spellPlay)) _spellPlay = null;
        return Task.CompletedTask;
    }
}
