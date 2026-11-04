using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// An ancient totem that hums when SOUL passes through it. The first time
/// each combat the vessel spends SOUL, the dream opens — draw cards.
/// </summary>
public sealed class DreamTotem : HollowKnightRelic
{
    /// <summary>Cards drawn when SOUL is first spent each combat.</summary>
    public const int CardsOnFirstSpend = 2;

    private static readonly SpireField<Player, bool> SpentThisCombat = new(() => false);

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task BeforeCombatStart()
    {
        if (base.Owner != null)
        {
            SpentThisCombat[base.Owner] = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || SpentThisCombat[base.Owner]
            || SoulSpendTracker.AmountSpent(cardPlay) <= 0)
        {
            return;
        }
        SpentThisCombat[base.Owner] = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, CardsOnFirstSpend, base.Owner);
    }
}
