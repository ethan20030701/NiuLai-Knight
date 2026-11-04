using System.Collections.Generic;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: take the shape of a humble creature and harden the shell.
/// Spend 33 SOUL for Block and insight — the spell line's defensive option.
/// </summary>
public sealed class ShapeOfUnn : HollowKnightCard
{
    private const string DrawVarName = "Draw";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(18m, ValueProp.Move),
        new CardsVar(DrawVarName, 1)
    ];

    public ShapeOfUnn() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars[DrawVarName].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(6m);
        base.DynamicVars[DrawVarName].UpgradeValueBy(1m);
    }
}
