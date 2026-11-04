using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class SwarmEcho : SwarmCommandCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CommandDamage", 4m)];
    public SwarmEcho() : base(1, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (KnightSwarm.Strongest(Owner) is not { } pet) return;
        var hits = Math.Min(4, 1 + (KnightSwarm.State(Owner)?.CommandsThisTurn ?? 0));
        for (var hit = 0; hit < hits && pet.IsAlive && cardPlay.Target.IsAlive; hit++)
            await KnightSwarm.Strike(choiceContext, Owner, pet, cardPlay.Target,
                DynamicVars["CommandDamage"].BaseValue, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["CommandDamage"].UpgradeValueBy(1m);
}
