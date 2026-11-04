using System.Collections.Generic;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Wrap agony around the shell: attackers are pierced by bursting thorns.
/// </summary>
public sealed class ThornsOfAgony : HollowKnightCard
{
    private const string PowerVarName = "ThornsOfAgony";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsOfAgonyPower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ThornsOfAgonyPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public ThornsOfAgony() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ThornsOfAgonyPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m);
    }
}
