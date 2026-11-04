using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Wrapped in shadow: the next enemy attack this turn deals 0 damage
/// (fully blocked hits do not consume it). Each consumed charge phases the
/// attacker with raw void when Sharp Shadow is equipped, then expires.
/// </summary>
public sealed class ShadeCloakPower : HollowKnightPower
{
    private bool _dodgePending;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (base.Amount > 0 && amount > 0 && target == base.Owner && dealer != null
            && dealer.Side != Owner.Side && props.IsPoweredAttack())
        {
            return 0m;
        }
        return amount;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        // Preview calls ModifyHpLost too; only real damage reaches this notification.
        _dodgePending = true;
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_dodgePending || target != base.Owner || dealer == null
            || dealer.Side == base.Owner.Side || !props.IsPoweredAttack())
        {
            return;
        }
        _dodgePending = false;
        await PowerCmd.Decrement(this);
        var player = base.Owner.Player;
        if (player != null && player.Relics.Any(relic => relic is Relics.SharpShadow))
        {
            await CombatApi.Damage(choiceContext, dealer, Relics.SharpShadow.ShadowDamage,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim, base.Owner, null, null);
        }
    }

    /*  Lives through the enemy turn (ReflectPower's pattern): the dodge is
        meant for the next enemy attack, which never lands during our own turn. */
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
