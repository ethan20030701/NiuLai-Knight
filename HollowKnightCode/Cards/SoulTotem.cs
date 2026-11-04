using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// An old totem that converts stored SOUL into raw energy. Deliberately has
/// NO canonical SOUL cost — it is a converter, not a spell: it never counts
/// as a spell for Shaman Stone or Spell Twister, and it stays reward-legal.
/// Requires a full 33 SOUL charge to fire (IsPlayable gate). The deal must
/// be worth taking: one energy (the totem's own cost) buys three energy and
/// a card, paid in SOUL — the old 2-cost-for-2-energy version netted zero.
/// </summary>
public sealed class SoulTotem : HollowKnightCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(3), new CardsVar("Draw", 1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable => Owner?.PlayerCombatState is not { } combat
        || (CustomResources<Soul>.TryGet(combat, out var soul) && soul.Amount >= Soul.SpellCost);

    public SoulTotem() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Soul.TrySpendManual(Owner, cardPlay, Soul.SpellCost)) return;
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Draw"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Draw"].UpgradeValueBy(1m); // draw 2
    }
}
