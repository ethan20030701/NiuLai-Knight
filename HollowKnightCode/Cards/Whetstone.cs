using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A rough stone that hones the nail's edge, if only for a moment.
/// The temporary nail-strength buff (the Flex pattern): expires at the end
/// of the turn, costs no SOUL.
/// </summary>
public sealed class Whetstone() : HollowKnightCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string PowerVarName = "Whetstone";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WhetstonePower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WhetstonePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WhetstonePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
