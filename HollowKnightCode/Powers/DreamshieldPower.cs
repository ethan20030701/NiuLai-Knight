using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The dreamshield circles the vessel: at the start of each of the next few
/// turns, gain Block. Amount is the per-turn Block (10, or 13 upgraded); the
/// turn countdown lives in a SpireField the card seeds via Begin() when it
/// applies this power (re-playing the card refreshes the countdown). The
/// power bar displays the remaining turns, not the Block value.
/// </summary>
public sealed class DreamshieldPower : HollowKnightPower
{
    /// <summary>How many turn-starts a freshly spun dreamshield lasts.</summary>
    public const int TurnsLasting = 3;

    private static readonly SpireField<DreamshieldPower, int> TurnsLeft = new(() => 0);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => TurnsLeft[this];

    /// <summary>Start (or refresh) the countdown. Called by the card on play.</summary>
    public void Begin(int turns)
    {
        TurnsLeft[this] = turns;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player || TurnsLeft[this] <= 0)
        {
            return;
        }
        TurnsLeft[this] -= 1;
        Flash();
        await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
        if (TurnsLeft[this] <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
