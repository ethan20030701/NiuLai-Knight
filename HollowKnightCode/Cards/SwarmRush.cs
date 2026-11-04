using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>Each living companion strikes the chosen enemy and builds the command combo.</summary>
public sealed class SwarmRush : SwarmCommandCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CommandDamage", 4m)];

    public SwarmRush() : base(1, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        foreach (var pet in KnightSwarm.Living(Owner))
            await KnightSwarm.Strike(choiceContext, Owner, pet, cardPlay.Target,
                DynamicVars["CommandDamage"].BaseValue, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["CommandDamage"].UpgradeValueBy(2m);
}
