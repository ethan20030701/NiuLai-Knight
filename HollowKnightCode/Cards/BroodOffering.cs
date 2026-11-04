using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class BroodOffering : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Draw", 1m), new DynamicVar("SoulGain", 22m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override bool IsPlayable => KnightSwarm.LivingCount(Owner) > 0;
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && !IsPlayable;
    public BroodOffering() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (KnightSwarm.Weakest(Owner) is not { } pet) return;
        await CreatureCmd.Kill(pet);
        Soul.Gain(Owner, DynamicVars["SoulGain"].IntValue);
        await PlayerCmd.GainEnergy(1m, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Draw"].UpgradeValueBy(1m);
}
