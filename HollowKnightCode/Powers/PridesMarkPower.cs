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
/// Mark of Pride aura. While present, the ChargedArts accumulator pays out
/// at double rate (6 per Skill instead of 3). Whenever the Knight plays a
/// Sword Art, draw `Amount` cards (1 base, 2 upgraded — Amount is the
/// PowerVar applied by the card).
/// </summary>
public sealed class PridesMarkPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player || !NailRules.IsNailArt(cardPlay.Card))
        {
            return;
        }
        Flash();
        await CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
    }
}
