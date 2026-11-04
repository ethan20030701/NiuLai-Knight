using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// Cloak of the sprintmaster. The first Skill the vessel plays each turn
/// lets it dart again: draw 1 card. Sharp shadow stride, quick hands —
/// pure tempo, no SOUL involved. SpireField turn flag = SharpenedNail's
/// pattern; ownership checked via cardPlay.Card.Owner (multiplayer-safe).
/// </summary>
public sealed class SprintmastersGift : HollowKnightRelic
{
    /// <summary>Cards drawn when the first Skill of the turn is played.</summary>
    public const int CardsOnFirstSkill = 1;

    private static readonly SpireField<Player, bool> UsedThisTurn = new(() => false);

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner)
        {
            UsedThisTurn[player] = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (UsedThisTurn[base.Owner]
            || cardPlay.Card.Owner != base.Owner
            || cardPlay.Card.Type != CardType.Skill)
        {
            return;
        }
        UsedThisTurn[base.Owner] = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, CardsOnFirstSkill, base.Owner);
    }
}
