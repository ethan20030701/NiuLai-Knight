using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The vessel's fury made permanent: Nail strikes deal bonus damage.
/// Spells are unaffected — SOUL empowers them through other means.
/// </summary>
public sealed class UnbreakableStrengthPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        return NailRules.GainsNailStrength(dealer, base.Owner, props, cardSource) ? base.Amount : 0m;
    }
}
