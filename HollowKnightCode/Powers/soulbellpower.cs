using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// The Soulbell tolls for the first Spell the Knight casts each turn,
/// pulling that many extra cards from the pile (1, or 2 when the card is
/// upgraded). The Spell-draw twin of Sprintmaster's Gift (first Skill).
/// </summary>
public sealed class SoulbellPower : HollowKnightPower
{
    private static readonly SpireField<SoulbellPower, bool> TolledThisTurn = new(() => false);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            TolledThisTurn[this] = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (TolledThisTurn[this] || cardPlay.Card.Owner != base.Owner.Player
            || !NailRules.IsSpellCard(cardPlay.Card))
        {
            return Task.CompletedTask;
        }
        TolledThisTurn[this] = true;
        Flash();
        return CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
    }
}
