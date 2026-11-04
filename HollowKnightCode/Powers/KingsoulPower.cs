using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// A pale charm of the King's own essence. The fuller the vessel, the harder
/// it strikes — Nail damage scales with SOUL currently held, rewarding those
/// who bank their SOUL instead of spending it.
/// </summary>
public sealed class KingsoulPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!NailRules.IsNailHit(dealer, base.Owner, props, cardSource))
        {
            return 0m;
        }
        var player = base.Owner.Player;
        if (player?.PlayerCombatState == null || !CustomResources<Soul>.TryGet(player.PlayerCombatState, out var soul))
        {
            return 0m;
        }
        return base.Amount * (soul.Amount / Soul.SpellCost);
    }
}
