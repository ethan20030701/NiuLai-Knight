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
/// Brand every wound into the nail: whenever the vessel takes unblocked
/// damage, its strikes grow fiercer (Rupture's slot — Broken Vessel, Grubsong
/// and the injured line all feed it).
/// </summary>
public sealed class PainBrand : HollowKnightCard
{
    private const string PowerVarName = "PainBrand";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PainBrandPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PainBrandPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public PainBrand() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<PainBrandPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // +1 -> +2 Strength per wound
    }
}
