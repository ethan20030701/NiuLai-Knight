using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The nailsmith's finest honing. The first Nail attack the vessel swings
/// each turn cuts deeper than it should — one additional strike.
/// </summary>
public sealed class SharpenedNail : HollowKnightRelic
{
    private static readonly SpireField<Player, bool> BonusUsedThisTurn = new(() => false);

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner)
        {
            BonusUsedThisTurn[player] = false;
        }
        return Task.CompletedTask;
    }

    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
    {
        if (hitCount <= 0 || BonusUsedThisTurn[base.Owner] || attack.Attacker != base.Owner.Creature
            || !NailRules.IsNailCard(attack.ModelSource as CardModel))
        {
            return hitCount;
        }
        BonusUsedThisTurn[base.Owner] = true;
        Flash();
        return hitCount + 1;
    }
}
