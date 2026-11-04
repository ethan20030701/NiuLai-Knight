using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Whenever the vessel actually loses HP, gain Amount SOUL.
/// </summary>
public sealed class GrubsongsMelodyPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner && delta < 0 && base.Owner.Player is { } player)
        {
            Flash();
            Soul.Gain(player, (int)base.Amount);
        }
        return Task.CompletedTask;
    }
}
