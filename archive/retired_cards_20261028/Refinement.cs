using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// One hundred days at the forge, distilled into a single moment: every
/// card in the deck becomes its perfected self (Apotheosis verbatim —
/// innate so it always opens in hand, exhausts once it has done its work).
/// </summary>
public sealed class Refinement : HollowKnightCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Innate];

    // The native Apotheosis is Ancient rarity at 2/1 energy. This copy is
    // obtainable from the Knight's ordinary rare pool, so it costs one more.
    public Refinement() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var card in base.Owner.PlayerCombatState!.AllCards)
        {
            if (card != this && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 3 -> 2
    }
}
