using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HollowKnight.HollowKnightCode.Powers;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The vessel's fury, stoked by its own broken shell. While at 30% HP or
/// less, Nail strikes hit far harder. Costs no SOUL.
/// </summary>
public sealed class FuryOfTheFallen() : HollowKnightCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string PowerVarName = "Fury";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FuryOfTheFallenPower>(PowerVarName, 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FuryOfTheFallenPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FuryOfTheFallenPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m);
    }
}
