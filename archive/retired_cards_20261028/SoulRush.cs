using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The vessel floods with borrowed speed: gain energy and draw, then it is
/// spent (Adrenaline verbatim — the Knight's one-shot tempo spike).
/// </summary>
public sealed class SoulRush : HollowKnightCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new CardsVar("Draw", 2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [base.EnergyHoverTip];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public SoulRush() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(1m); // 1 -> 2 energy
    }
}
