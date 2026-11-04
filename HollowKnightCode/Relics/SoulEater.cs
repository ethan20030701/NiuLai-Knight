using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A darker charm carved from a discarded shell. The vessel draws in SOUL
/// with far greater efficiency — every Nail strike gathers bonus SOUL
/// on top of what Soul Catcher provides.
/// </summary>
public sealed class SoulEater : HollowKnightRelic
{
    /// <summary>Extra SOUL per Nail hit, on top of Soul Catcher's gain.</summary>
    public const int BonusSoulPerHit = 6; // 11 + 6 = 17; two hits now fund a spell

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (result.TotalDamage > 0 && NailRules.IsNailHit(dealer, base.Owner.Creature, props, cardSource))
        {
            Flash();
            Soul.Gain(base.Owner, BonusSoulPerHit);
        }
        return Task.CompletedTask;
    }
}
