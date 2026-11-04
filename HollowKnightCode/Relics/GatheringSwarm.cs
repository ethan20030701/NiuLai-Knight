using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A swarm of tiny companions gathers every last geo left behind: all gold
/// the Knight earns is increased by 25% (BowlerHat's hook, Gathering Swarm
/// charm's meaning).
/// </summary>
public sealed class GatheringSwarm : HollowKnightRelic
{
    /// <summary>Multiplier applied to all gold gained.</summary>
    public const decimal GoldMultiplier = 1.25m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override decimal ModifyGoldGained(Player player, decimal amount)
    {
        if (player != base.Owner)
        {
            return amount;
        }
        return amount * GoldMultiplier;
    }

    public override Task AfterModifyingGoldGained(Player player, decimal amount)
    {
        Flash();
        return Task.CompletedTask;
    }
}
