using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Fury borrowed from a broken charm: Nail strikes deal heavy bonus damage,
/// but every point of HP lost chips the fury away.
/// </summary>
public sealed class FragileStrengthPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner && delta < 0 && base.Amount > 0)
        {
            Flash();
            return PowerCmd.Decrement(this);
        }
        return Task.CompletedTask;
    }

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        return NailRules.GainsNailStrength(dealer, base.Owner, props, cardSource) ? base.Amount : 0m;
    }
}
