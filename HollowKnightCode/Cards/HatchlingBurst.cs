using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class HatchlingBurst : SwarmCommandCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BaseDamage", 10m)];
    protected override bool HasRequiredPet => KnightSwarm.Find<HatchlingPet>(Owner) != null;
    public HatchlingBurst() : base(2, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (KnightSwarm.Find<HatchlingPet>(Owner) is not { } pet || CombatState is not { } combat) return;
        var damage = DynamicVars["BaseDamage"].BaseValue + pet.CurrentHp;
        // A single area command counts once, regardless of the number of enemies.
        if (KnightSwarm.State(Owner) is { } state) state.CommandsThisTurn++;
        await CombatApi.Damage(choiceContext, combat.HittableEnemies.ToArray(),
            damage + KnightSwarm.DamageBonus(Owner, pet), ValueProp.Move, pet, this, cardPlay);
        if (pet.IsAlive) await CreatureCmd.Kill(pet);
    }

    protected override void OnUpgrade() => DynamicVars["BaseDamage"].UpgradeValueBy(4m);
}
