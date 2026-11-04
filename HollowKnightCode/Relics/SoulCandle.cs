using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A candle that calls wandering SOUL home before the fight even starts:
/// every combat opens with a free hit's worth of SOUL already banked.
/// </summary>
public sealed class SoulCandle : HollowKnightRelic
{
    /// <summary>SOUL granted at the start of each combat.</summary>
    public const int OpeningSoul = 11;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override Task BeforeCombatStart()
    {
        Flash();
        Soul.Gain(base.Owner, OpeningSoul);
        return Task.CompletedTask;
    }
}
