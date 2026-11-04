using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Lifeblood Cocoon: whenever damage is absorbed by the vessel's Block,
/// wring SOUL from the cocoon (per damage event, like Grubsong is per
/// HP-loss event). Expires at the end of the turn (Baldur Shell pattern).
/// </summary>
public sealed class LifebloodCocoonPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && result.BlockedDamage > 0 && base.Owner.Player is { } player)
        {
            Flash();
            Soul.Gain(player, (int)base.Amount);
        }
        return Task.CompletedTask;
    }

    /*  Removed when the player's NEXT turn starts (ReflectPower's pattern):
        the whole point is to soak enemy attacks, so the buff must live
        through the enemy turn — removing at BeforeSideTurnEnd killed it
        before the enemy ever swung. */
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
