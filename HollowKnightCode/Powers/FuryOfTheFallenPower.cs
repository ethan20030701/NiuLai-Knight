using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Fury of the Fallen: while the vessel is at 30% HP or less, Nail strikes
/// deal bonus damage. Spells are unaffected — this is the nail line's
/// low-HP payoff (integer-exact check, no float drift).
/// </summary>
public sealed class FuryOfTheFallenPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        var owner = base.Owner;
        return NailRules.GainsNailStrength(dealer, owner, props, cardSource)
               && owner.CurrentHp * 10 <= owner.MaxHp * 3
            ? base.Amount
            : 0m;
    }
}
