using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class SoulNourishment : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SoulNourishmentPower>("Growth", 2m)];
    public SoulNourishment() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<SoulNourishmentPower>(choiceContext, Owner.Creature,
            DynamicVars["Growth"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Growth"].UpgradeValueBy(1m);
}
