using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The charm that completes the vessel. Hoard two spells' worth of SOUL and
/// the Void pays interest each turn: 1 Energy for 33 SOUL. The gate on the
/// hoard archetype — spending SOUL turns it off, hoarding turns it on.
/// Fires on AfterPlayerTurnStart, which the game calls after energy reset.
/// </summary>
public sealed class VoidHeart : HollowKnightRelic
{
    /// <summary>SOUL the vessel must hoard for the Void to answer.</summary>
    public const int Threshold = Soul.SpellCost * 2;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner
            || !CustomResources<Soul>.TryGet(player.PlayerCombatState, out var soul)
            || soul.Amount < Threshold)
        {
            return;
        }
        Flash();
        await PlayerCmd.GainEnergy(1m, player);
        Soul.Gain(player, -Soul.SpellCost);
    }
}
