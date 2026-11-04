using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Combat-wide hit counter (silent infra): increments on every attack
/// segment landed on enemies; resets at the owner's turn start. Applied at
/// combat start by the Soul Catcher relic. Invisible.
/// </summary>
public sealed class HitCounterPower : HollowKnightPower
{
    private static readonly SpireField<HitCounterPower, int> Hits = new(() => 0);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;

    /// <summary>Pure internal infra — never shown on the power bar.</summary>
    protected override bool IsVisibleInternal => false;

    public static int HitsThisTurnFor(Player? player)
    {
        if (player?.PlayerCombatState == null)
        {
            return 0;
        }
        var power = player.Creature.GetPower<HitCounterPower>();
        return power == null ? 0 : Hits[power];
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            Hits[this] = 0;
        }
        return Task.CompletedTask;
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != base.Owner || command.TargetSide == base.Owner.Side)
        {
            return Task.CompletedTask;
        }
        foreach (var results in command.Results)
        {
            foreach (var r in results)
            {
                if (r.UnblockedDamage > 0 || r.BlockedDamage > 0)
                {
                    Hits[this] += 1;
                }
            }
        }
        return Task.CompletedTask;
    }
}
