using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Melody layers: on fatal damage consume one layer — survive at 1 HP,
/// gain 22 Block. Contract mirrors LizardTail:
/// ShouldDieLate + AfterPreventingDeath. Last layer spent removes the power.
/// </summary>
public sealed class CarefreeMelodyPower : HollowKnightPower
{
    public const decimal TriggerBlock = 22m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner)
        {
            return true;
        }
        return base.Amount <= 0;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        Flash();
        await CreatureCmd.Heal(creature, 1m);
        var ctx = new ThrowingPlayerChoiceContext();
        if (base.Amount > 1)
        {
            await PowerCmd.Apply(ctx, this, creature, -1m, creature, null);
        }
        else
        {
            await PowerCmd.Remove(this);
        }
        await CreatureCmd.GainBlock(creature, TriggerBlock, ValueProp.Unpowered, null);
    }
}
