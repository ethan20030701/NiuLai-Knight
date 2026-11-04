using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// Focus sharpens with practice: the Focus family (Focus, Deep Focus) spends
/// 22 SOUL instead of 33. Spell Twister deliberately skips the sustain
/// spells — this charm is their dedicated discount.
/// </summary>
public sealed class QuickFocus : HollowKnightRelic
{
    /// <summary>SOUL cost of the Focus family while this charm is equipped.</summary>
    public const int ReducedFocusCost = 22;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner || card is not (Focus or DeepFocus))
        {
            return Task.CompletedTask;
        }
        if (CustomResources<Soul>.Cost(card) is not { CostsX: false } cost)
        {
            return Task.CompletedTask;
        }
        cost.SetThisCombat(ReducedFocusCost, reduceOnly: true);
        return Task.CompletedTask;
    }
}
