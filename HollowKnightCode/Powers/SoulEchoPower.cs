using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Whenever its owner plays a card that actually spent SOUL, draw Amount
/// cards. Compare amounts: BaseLib 3.4.7 WasSpent is true even at zero.
/// </summary>
public sealed class SoulEchoPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player || SoulSpendTracker.AmountSpent(cardPlay) <= 0)
        {
            return;
        }
        Flash();
        await CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
    }
}
