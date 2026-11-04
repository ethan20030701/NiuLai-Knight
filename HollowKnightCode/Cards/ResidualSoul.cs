using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A stray wisp, caught and cupped: a small free dose of SOUL, spent and
/// gone. Replaces itself so this small dose of SOUL doesn't clog the hand.
/// </summary>
public sealed class ResidualSoul : HollowKnightCard
{
    private const string SoulVarName = "Soul";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(SoulVarName, 11m)];

    public ResidualSoul() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Soul.Gain(Owner, (int)base.DynamicVars[SoulVarName].BaseValue);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[SoulVarName].UpgradeValueBy(11m); // 11 -> 22 SOUL
    }
}
