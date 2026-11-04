using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>A rare one-shot that assembles the full brood.</summary>
public sealed class BroodChorus : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SummonHp", 5m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BroodChorus() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var health = DynamicVars["SummonHp"].IntValue;
        await KnightSwarm.Summon<WeaverlingPet>(choiceContext, Owner, health);
        await KnightSwarm.Summon<GrimmkinPet>(choiceContext, Owner, health);
        await KnightSwarm.Summon<HatchlingPet>(choiceContext, Owner, health);
    }

    protected override void OnUpgrade() => DynamicVars["SummonHp"].UpgradeValueBy(2m);
}
