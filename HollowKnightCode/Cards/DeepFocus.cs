using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Gather every stray wisp and pour it into the shell. A one-shot burst of
/// healing — the deep-end of the Focus line. Costs 33 SOUL and exhausts.
/// </summary>
public sealed class DeepFocus : HollowKnightCard
{
    // Focus-family sustain is never discounted by Spell Twister.
    public override bool ExemptFromSpellCostReduction => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(16m)];

    public DeepFocus() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, base.DynamicVars.Heal.BaseValue);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Heal.UpgradeValueBy(6m);
    }
}
