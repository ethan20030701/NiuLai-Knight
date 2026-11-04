using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: let a measure of stored SOUL bleed into the vessel's limbs.
/// 33 SOUL becomes one net energy and a card. Exhausts after use; unlike
/// the manual-payment Soul Totem this is a spell and needs no energy to start.
/// </summary>
public sealed class SoulFlow : HollowKnightCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new CardsVar("Draw", 1)];

    public SoulFlow() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Draw"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Draw"].UpgradeValueBy(1m); // draw 2
    }
}
