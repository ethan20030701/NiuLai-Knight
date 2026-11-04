using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class BroodInstinct : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BroodInstinctPower>("Bonus", 1m)];
    public BroodInstinct() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<BroodInstinctPower>(choiceContext, Owner.Creature,
            DynamicVars["Bonus"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}
