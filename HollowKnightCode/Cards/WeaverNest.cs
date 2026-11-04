using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>A durable weaverling rather than a disposable attack token.</summary>
public sealed class WeaverNest : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SummonHp", 6m)];

    public WeaverNest() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await KnightSwarm.Summon<WeaverlingPet>(choiceContext, Owner, DynamicVars["SummonHp"].IntValue);

    protected override void OnUpgrade() => DynamicVars["SummonHp"].UpgradeValueBy(3m);
}
