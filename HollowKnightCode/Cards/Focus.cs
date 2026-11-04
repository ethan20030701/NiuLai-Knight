using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Gather the SOUL within and mend the vessel's shell.
/// Costs 33 SOUL in addition to its energy cost.
/// </summary>
public sealed class Focus : HollowKnightCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // Spell Twister never discounts the sustain spell.
    public override bool ExemptFromSpellCostReduction => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(6m)];

    public Focus() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var heal = base.DynamicVars.Heal.BaseValue;
        if (Owner.Relics.Any(relic => relic is Relics.AwakenedDream))
        {
            heal += Relics.AwakenedDream.BonusFocusHeal;
        }
        await CreatureCmd.Heal(Owner.Creature, heal);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Heal.UpgradeValueBy(3m);
    }
}
