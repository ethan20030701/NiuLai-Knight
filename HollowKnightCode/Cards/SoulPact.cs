using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Offer something up, take insight back: exhaust a card from your hand,
/// draw two (Burning Pact verbatim — the consume line's card-quality
/// filter, feeding Broken Shell and Grubsong Eater as it goes).
/// </summary>
public sealed class SoulPact : HollowKnightCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Draw", 2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public SoulPact() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            context: choiceContext, player: base.Owner, filter: null, source: this)).FirstOrDefault();
        if (chosen != null)
        {
            await CombatApi.Exhaust(choiceContext, chosen);
        }
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Draw"].UpgradeValueBy(1m); // draw 3
    }
}
