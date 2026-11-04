using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A wistful grub rides into battle. Every point of HP the vessel actually
/// loses (block fully absorbed hits do not count) wrings a little SOUL from
/// the shell - pain becomes fuel for the next spell.
/// </summary>
public sealed class Grubsong : HollowKnightRelic
{
    /// <summary>SOUL gained per HP-loss event.</summary>
    public const int SoulPerHpLoss = 11;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner.Creature && delta < 0)
        {
            Flash();
            Soul.Gain(base.Owner, SoulPerHpLoss);
        }
        return Task.CompletedTask;
    }
}
