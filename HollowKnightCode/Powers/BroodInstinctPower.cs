using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>Shared pet damage bonus and a single summon draw each turn.</summary>
public sealed class BroodInstinctPower : HollowKnightPower
{
    private bool _drewThisTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) _drewThisTurn = false;
        return Task.CompletedTask;
    }

    public async Task AfterSwarmSummoned(PlayerChoiceContext choiceContext)
    {
        if (_drewThisTurn || Owner.Player is not { } player) return;
        _drewThisTurn = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, 1, player);
    }
}
