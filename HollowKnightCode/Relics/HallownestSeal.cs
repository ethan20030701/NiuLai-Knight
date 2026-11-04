using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The seal of the old kingdom, worn with pride: every nail strike lands
/// harder (Nailmaster's Glory's relic-side ModifyDamageAdditive hook, this
/// one unconditional).
/// </summary>
public sealed class HallownestSeal : HollowKnightRelic
{
    /// <summary>Bonus damage on every nail strike.</summary>
    public const int NailBonusDamage = 3;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!NailRules.IsNailHit(dealer, base.Owner.Creature, props, cardSource))
        {
            return 0m;
        }
        return NailBonusDamage;
    }
}
