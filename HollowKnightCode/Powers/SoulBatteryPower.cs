using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The pool refills quietly: at each of its owner's turn starts, gather
/// Amount SOUL unconditionally.
/// </summary>
public sealed class SoulBatteryPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player)
        {
            return Task.CompletedTask;
        }
        Flash();
        Soul.Gain(base.Owner.Player, (int)base.Amount);
        return Task.CompletedTask;
    }
}
