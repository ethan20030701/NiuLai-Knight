using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// An etched stone of the Shaman tribe: SOUL spells strike with far greater force.
/// Cards that cost SOUL are spells — their damage is amplified by this power.
/// </summary>
public sealed class ShamanStonePower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != base.Owner)
            return 0m;
        if (!props.IsPoweredAttack())
            return 0m;
        return NailRules.IsSpellCard(cardSource) ? base.Amount : 0m;
    }
}
