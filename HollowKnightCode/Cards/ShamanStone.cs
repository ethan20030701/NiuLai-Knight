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
/// An etched stone that resonates with SOUL: spells deal much heavier damage.
/// </summary>
public sealed class ShamanStone : HollowKnightCard
{
    private const string PowerVarName = "ShamanStone";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ShamanStonePower>(PowerVarName, 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ShamanStonePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public ShamanStone() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ShamanStonePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m);
    }
}
