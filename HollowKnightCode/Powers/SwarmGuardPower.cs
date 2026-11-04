using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>Routes unblocked enemy attacks into a living companion and runs their turn effects.</summary>
public sealed class SwarmGuardPower : HollowKnightPower
{
    private int _commandsThisTurn;
    public int CommandsThisTurn
    {
        get => _commandsThisTurn;
        set
        {
            if (_commandsThisTurn == value) return;
            _commandsThisTurn = value;
            DynamicVars["Commands"].BaseValue = value;
            InvokeDisplayAmountChanged();
        }
    }
    public KnightSwarm.Species? LastFallen { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => CommandsThisTurn;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Commands", 0m)];

    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount,
        ValueProp props, Creature? dealer)
    {
        if (target != Owner || amount <= 0 || dealer == null
            || dealer.Side == Owner.Side || !props.IsPoweredAttack())
        {
            return target;
        }

        var player = Owner.Player;
        if (player == null)
        {
            return target;
        }

        // The expendable hatchling guards first. This never redirects pet damage
        // back into another pet or costs a companion HP for non-attack effects.
        return KnightSwarm.Find<HatchlingPet>(player)
            ?? KnightSwarm.Find<WeaverlingPet>(player)
            ?? KnightSwarm.Find<GrimmkinPet>(player)
            ?? target;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) CommandsThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature.Monster is not KnightPet
            || creature.PetOwner != Owner.Player || Owner.Player is not { } player || !Owner.IsAlive)
            return;
        LastFallen = KnightSwarm.KindOf(creature);
        if (creature.Monster is HatchlingPet && Owner.CombatState is { } combat
            && combat.HittableEnemies.Count > 0)
        {
            var target = player.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies);
            if (target != null)
                await CombatApi.Damage(choiceContext, target,
                    9m + KnightSwarm.DamageBonus(player, creature), ValueProp.Unpowered, Owner);
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player
            || Owner.CombatState is not { } combat || combat.HittableEnemies.Count == 0)
        {
            return;
        }

        if (KnightSwarm.Find<WeaverlingPet>(player) is { } weaver)
        {
            var target = player.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies);
            if (target != null)
            {
                await KnightSwarm.Strike(choiceContext, player, weaver, target, 3m, command: false);
            }
        }

        if (KnightSwarm.Find<GrimmkinPet>(player) is { } grimm
            && combat.HittableEnemies.Count > 0)
        {
            var target = player.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies);
            if (target != null)
            {
                await KnightSwarm.Strike(choiceContext, player, grimm, target, 5m, command: false);
            }
        }
    }
}
