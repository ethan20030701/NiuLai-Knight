using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The Spell twin of the Skill-charge engine (ChargedArts): every Skill the
/// Knight plays adds Amount (3, or 5 when the card is upgraded) to the
/// accumulator; the next Spell played consumes ALL stacks as bonus spell
/// damage. The pending payout mirrors ChargedArtsPower's multi-hit pattern
/// but is always reset (turn start, and whenever a Spell resolves), which
/// fixes the leaked-payout bug class at the source.
/// </summary>
public sealed class SpellchargePower : HollowKnightPower
{
    private static readonly SpireField<SpellchargePower, int> ChargeStacks = new(() => 0);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => ChargeStacks[this];

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            ChargeStacks[this] = 0;
            _pendingPayout = 0m;
            _chargedPlay = null;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player)
        {
            return Task.CompletedTask;
        }
        if (NailRules.IsAttackSpell(cardPlay.Card))
        {
            // the next Spell consumes the charge (even a no-damage one like Focus)
            ChargeStacks[this] = 0;
            _pendingPayout = 0m;
            _chargedPlay = null;
            return Task.CompletedTask;
        }
        if (cardPlay.Card.Type == CardType.Skill)
        {
            ChargeStacks[this] += 1;
            InvokeDisplayAmountChanged();
            Flash();
        }
        return Task.CompletedTask;
    }

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack() || dealer != Owner || !NailRules.IsAttackSpell(cardSource))
        {
            return 0m;
        }
        return cardPlay != null
            ? ReferenceEquals(cardPlay, _chargedPlay) ? _pendingPayout : 0m
            : ChargeStacks[this] * Amount;
    }

    private decimal _pendingPayout;
    private CardPlay? _chargedPlay;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player && NailRules.IsAttackSpell(cardPlay.Card))
        {
            _chargedPlay = cardPlay;
            _pendingPayout = ChargeStacks[this] * Amount;
            ChargeStacks[this] = 0;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
}
