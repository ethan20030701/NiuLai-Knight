using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Grubberfly's Elegance: while the vessel is at FULL HP, nail strikes deal
/// bonus damage (Fury of the Fallen's pristine mirror). The bonus is nail
/// strength, so it never touches spells.
/// </summary>
public sealed class GrubberflyElegancePower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!NailRules.IsNailHit(dealer, base.Owner, props, cardSource))
        {
            return 0m;
        }
        return base.Owner.CurrentHp >= base.Owner.MaxHp ? base.Amount : 0m;
    }
}
