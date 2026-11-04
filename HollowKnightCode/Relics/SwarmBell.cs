using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>Rewards maintaining more than one kind of companion.</summary>
public sealed class SwarmBell : HollowKnightRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && KnightSwarm.LivingCount(player) >= 2)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, 1, player);
        }
    }
}
