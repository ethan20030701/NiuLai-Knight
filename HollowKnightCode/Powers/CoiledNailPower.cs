using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The nail, wound tight and released faster: every Nail attack the vessel
/// swings strikes additional times. Spells are unaffected.
/// </summary>
public sealed class CoiledNailPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
    {
        if (hitCount > 0 && base.Amount > 0 && attack.Attacker == base.Owner && NailRules.IsNailCard(attack.ModelSource as CardModel))
        {
            return hitCount + 1;
        }
        return hitCount;
    }
}
