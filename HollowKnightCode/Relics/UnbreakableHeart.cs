using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A heart charm refined by the Nailsmith until it cannot crack: carrying
/// it permanently hardens the vessel (BigMushroom's pickup hook,
/// Unbreakable Heart charm's meaning).
/// </summary>
public sealed class UnbreakableHeart : HollowKnightRelic
{
    /// <summary>Max HP gained when this charm is picked up.</summary>
    public const int BonusMaxHp = 10;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(base.Owner.Creature, BonusMaxHp);
    }
}
