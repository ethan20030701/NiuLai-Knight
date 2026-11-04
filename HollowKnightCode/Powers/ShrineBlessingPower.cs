using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Shrine blessing: the next Attack Spell played this turn deals bonus damage
/// (Shaman Stone's hook family — spells, not nails). Expires at turn end.
/// </summary>
public sealed class ShrineBlessingPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != base.Owner || !props.IsPoweredAttack() || cardSource?.Owner != base.Owner.Player)
        {
            return 0m;
        }
        return NailRules.IsAttackSpell(cardSource) ? base.Amount : 0m;
    }

    /*  the card text promises "the NEXT Spell" — the blessing rides the first
        spell that resolves, then goes away (BeforeSideTurnEnd stays as the
        expiry backstop for a turn where no spell is played) */
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == base.Owner.Player && NailRules.IsAttackSpell(cardPlay.Card))
        {
            Flash();
            await PowerCmd.Remove(this);
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
