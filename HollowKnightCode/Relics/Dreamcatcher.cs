using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// Dreams caught in the woven net mend the sleeper: resting at a bench
/// heals 15 extra HP (Regal Pillow's hook, Dreamcatcher charm's meaning).
/// </summary>
public sealed class Dreamcatcher : HollowKnightRelic
{
    /// <summary>Extra HP restored when resting at a bench.</summary>
    public const int ExtraRestHeal = 15;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player != base.Owner)
        {
            return amount;
        }
        return amount + ExtraRestHeal;
    }
}
