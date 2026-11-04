using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class BroodReturn : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SummonHp", 6m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
    protected override bool IsPlayable => KnightSwarm.State(Owner)?.LastFallen != null;
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && !IsPlayable;
    public BroodReturn() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (KnightSwarm.State(Owner)?.LastFallen is { } species)
            await KnightSwarm.SummonKind(choiceContext, Owner, species, DynamicVars["SummonHp"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["SummonHp"].UpgradeValueBy(3m);
}
