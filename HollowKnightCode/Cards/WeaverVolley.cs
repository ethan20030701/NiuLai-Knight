using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class WeaverVolley : SwarmCommandCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("CommandDamage", 3m), new CardsVar(1)];
    protected override bool HasRequiredPet => KnightSwarm.Find<WeaverlingPet>(Owner) != null;
    public WeaverVolley() : base(1, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (KnightSwarm.Find<WeaverlingPet>(Owner) is not { } weaver) return;
        for (var hit = 0; hit < 3 && cardPlay.Target.IsAlive && weaver.IsAlive; hit++)
            await KnightSwarm.Strike(choiceContext, Owner, weaver, cardPlay.Target,
                DynamicVars["CommandDamage"].BaseValue, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["CommandDamage"].UpgradeValueBy(1m);
}
