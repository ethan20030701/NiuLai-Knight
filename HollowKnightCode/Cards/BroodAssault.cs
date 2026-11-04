using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class BroodAssault : SwarmCommandCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BaseDamage", 10m)];
    public BroodAssault() : base(2, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (KnightSwarm.Strongest(Owner) is { } pet)
            await KnightSwarm.Strike(choiceContext, Owner, pet, cardPlay.Target,
                DynamicVars["BaseDamage"].BaseValue + pet.CurrentHp, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["BaseDamage"].UpgradeValueBy(4m);
}
