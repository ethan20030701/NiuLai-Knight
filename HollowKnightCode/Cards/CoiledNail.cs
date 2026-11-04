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
/// The nail, wound tight like a spring: every Nail attack strikes one more time.
/// The heart of the multi-hit build.
/// </summary>
public sealed class CoiledNail : HollowKnightCard
{
    private const string PowerVarName = "CoiledNail";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<CoiledNailPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CoiledNailPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public CoiledNail() : base(3, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CoiledNailPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // One extra hit on every Nail attack already scales both damage and
        // SOUL; upgrading to two extra hits was too large a multiplier.
        base.EnergyCost.UpgradeBy(-1); // 3 -> 2 energy
    }
}
