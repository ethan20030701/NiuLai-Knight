using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A vessel fused with lifeblood. Combat opens with a shell of blue blood:
/// at the start of each combat gain 8 Block. Whenever lifeblood would be the
/// difference — i.e. whenever HP is actually lost — wring 6 SOUL from it
/// (Lifeblood Cocoon's per-event yield, weaker than Grubsong since it stacks
/// with it). The opening shell rewards aggressive openers.
/// </summary>
public sealed class LifebloodCore : HollowKnightRelic
{
    /// <summary>Block gained at combat start.</summary>
    public const int OpeningBlock = 8;

    /// <summary>SOUL gained per HP-loss event (stacks with Grubsong).</summary>
    public const int SoulPerHpLoss = 6;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        if (base.Owner?.Creature is { } me)
        {
            Flash();
            await CreatureCmd.GainBlock(me, OpeningBlock, ValueProp.Unpowered, null);
        }
    }

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
