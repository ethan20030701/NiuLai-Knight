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
/// A quiet pool that refills itself: at each turn start, gather a little
/// SOUL (the banking line's steady trickle — no conditions, no fights).
/// </summary>
public sealed class SoulBattery : HollowKnightCard
{
    private const string PowerVarName = "SoulBattery";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SoulBatteryPower>(PowerVarName, 11m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SoulBatteryPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public SoulBattery() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SoulBatteryPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(11m); // 11 -> 22: one or two nail hits per turn
    }
}
