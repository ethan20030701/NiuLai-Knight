using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Slip wholly into the shade: for a few turns the vessel is barely there —
/// all damage taken drops to 1. Match the native Wraith Form's 3-energy
/// commitment and its ongoing 1 Dexterity loss per turn.
/// </summary>
public sealed class WraithForm : HollowKnightCard
{
    private const string WraithVarName = "Wraith";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(WraithVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>((int)base.DynamicVars[WraithVarName].BaseValue),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public WraithForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature,
            base.DynamicVars[WraithVarName].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WraithFormPower>(choiceContext, Owner.Creature,
            1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[WraithVarName].UpgradeValueBy(1m); // 2 -> 3 turns
    }
}
