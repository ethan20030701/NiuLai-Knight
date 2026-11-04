using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A dream the vessel finally learned to wake from. Focus mends more shell,
/// and the first cast each combat stirs a memory worth a card.
/// </summary>
public sealed class AwakenedDream : HollowKnightRelic
{
    /// <summary>Extra HP healed by Focus while this charm is worn.</summary>
    public const int BonusFocusHeal = 6;

    private static readonly SpireField<Player, bool> DrawnThisCombat = new(() => false);

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task BeforeCombatStart()
    {
        if (base.Owner != null)
        {
            DrawnThisCombat[base.Owner] = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || DrawnThisCombat[base.Owner] || cardPlay.Card is not Focus)
        {
            return;
        }
        DrawnThisCombat[base.Owner] = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, 1, base.Owner);
    }
}
