using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// Charm of the hive. Whenever the vessel actually loses HP (Grubsong's
/// trigger), the next Spell played that turn costs 1 less energy — pain
/// accelerates the casting. Armed once per turn, consumed once per turn
/// (Fury of the Fallen's restraint). Cost preview stays consistent because
/// the discount runs through TryModifyEnergyCostInCombat (SpikedGauntlets
/// pattern), not an energy refund.
/// </summary>
public sealed class Hiveblood : HollowKnightRelic
{
    private static readonly SpireField<Player, bool> DiscountArmed = new(() => false);

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner)
        {
            DiscountArmed[player] = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner.Creature && delta < 0)
        {
            DiscountArmed[base.Owner] = true;
            Flash();
        }
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!DiscountArmed[base.Owner]
            || card.Owner != base.Owner
            || !NailRules.IsSpellCard(card))
        {
            return false;
        }
        if (originalCost <= 0m)
        {
            return false;
        }
        modifiedCost = originalCost - 1m;
        return true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // the discount is consumed by the first spell actually cast while armed
        if (DiscountArmed[base.Owner] && cardPlay.Card.Owner == base.Owner && NailRules.IsSpellCard(cardPlay.Card))
        {
            DiscountArmed[base.Owner] = false;
        }
        return Task.CompletedTask;
    }
}
