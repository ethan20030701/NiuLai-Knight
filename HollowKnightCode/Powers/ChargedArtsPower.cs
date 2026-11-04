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
/// The Skill-charge &gt; Sword-Art-release engine. Every Skill the Knight
/// plays adds ChargePerSkill (3, doubled to 6 while PridesMark is present)
/// to the accumulator; the next Sword Art played consumes ALL stacks,
/// adding the full charge to its damage (every segment of multi-hit arts).
/// </summary>
public sealed class ChargedArtsPower : HollowKnightPower
{
    private static readonly SpireField<ChargedArtsPower, int> ChargeStacks = new(() => 0);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => ChargeStacks[this];

    private decimal ChargePerSkill
    {
        get
        {
            // v0.3.9: the rate is this power's own Amount (3, or 5 when
            // Charged Arts is upgraded) — Pride's Mark doubles whatever it is.
            return base.Owner.HasPower<PridesMarkPower>() ? base.Amount * 2m : base.Amount;
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            ChargeStacks[this] = 0;
            _pendingPayout = 0m; // v0.3.9 fix: a settled payout must never leak into later turns
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
        if (NailRules.IsNailArt(cardPlay.Card))
        {
            if (ChargeStacks[this] > 0)
            {
                Flash();
            }
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
        if (!props.IsPoweredAttack() || dealer != Owner || !NailRules.IsNailArt(cardSource))
        {
            return 0m;
        }
        return cardPlay != null
            ? ReferenceEquals(cardPlay, _chargedPlay) ? _pendingPayout : 0m
            : ChargeStacks[this] * ChargePerSkill;
    }

    private decimal _pendingPayout;
    private CardPlay? _chargedPlay;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player && NailRules.IsNailArt(cardPlay.Card))
        {
            _chargedPlay = cardPlay;
            _pendingPayout = ChargeStacks[this] * ChargePerSkill;
            ChargeStacks[this] = 0;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
}
