using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>SOUL gives birth to a fragile companion that bursts when sacrificed.</summary>
public sealed class GlowingWomb : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SummonHp", 6m)];

    public GlowingWomb() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        CustomResources<Soul>.SetCanonicalCost(this, 8);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await KnightSwarm.Summon<HatchlingPet>(choiceContext, Owner, DynamicVars["SummonHp"].IntValue);

    protected override void OnUpgrade() => DynamicVars["SummonHp"].UpgradeValueBy(3m);
}
