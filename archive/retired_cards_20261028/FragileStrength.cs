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
/// A cracked charm, still brimming with fury. Heavy Nail damage that chips away as you take hits.
/// </summary>
public sealed class FragileStrength : HollowKnightCard
{
    private const string PowerVarName = "FragileStrength";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FragileStrengthPower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FragileStrengthPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public FragileStrength() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<FragileStrengthPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
