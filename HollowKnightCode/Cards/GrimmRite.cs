using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>Gives the Grimm Troupe's child a body and its own health bar.</summary>
public sealed class GrimmRite : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SummonHp", 6m)];

    public GrimmRite() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, 22);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await KnightSwarm.Summon<GrimmkinPet>(choiceContext, Owner, DynamicVars["SummonHp"].IntValue);

    protected override void OnUpgrade() => DynamicVars["SummonHp"].UpgradeValueBy(3m);
}
