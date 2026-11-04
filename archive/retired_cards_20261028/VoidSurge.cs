using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Open the vessel to the Void, and it answers a little more each turn
/// (Demon Form's slot): every turn start grants Strength. The long-fight
/// payoff the Knight's other scaling engines orbit.
/// </summary>
public sealed class VoidSurge : HollowKnightCard
{
    private const string PowerVarName = "VoidSurge";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VoidSurgePower>(PowerVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VoidSurgePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public VoidSurge() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<VoidSurgePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // 2 -> 3 Strength per turn
    }
}
