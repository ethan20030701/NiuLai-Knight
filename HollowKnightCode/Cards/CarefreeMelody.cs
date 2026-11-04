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
/// Carefree Melody. A charm woven from a departed soul: grants 2 layers of
/// Melody — on fatal damage, consume one to survive at 1 HP, gain 22 Block
/// and draw a card. The injured line's safety net.
/// </summary>
public sealed class CarefreeMelody : HollowKnightCard
{
    private const string PowerVarName = "CarefreeMelody";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<CarefreeMelodyPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CarefreeMelodyPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public CarefreeMelody() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CarefreeMelodyPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
