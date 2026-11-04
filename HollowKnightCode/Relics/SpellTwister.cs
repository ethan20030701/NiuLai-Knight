using BaseLib.Abstracts;
using BaseLib.Extensions;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// An etched charm that bends SOUL to the caster's will: offensive spells
/// cost 24 SOUL instead of 33. Focus is exempt - sustain stays full price.
/// Uses the cost's own per-combat override so the check, the spend and the
/// card UI all agree on the reduced number.
/// </summary>
public sealed class SpellTwister : HollowKnightRelic
{
    /// <summary>SOUL cost of spells while Spell Twister is equipped.</summary>
    public const int ReducedSpellCost = 24;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner || !NailRules.IsSpellCard(card) || card is HollowKnightCard { ExemptFromSpellCostReduction: true })
        {
            return Task.CompletedTask;
        }
        if (CustomResources<Soul>.Cost(card) is not { CostsX: false } cost)
        {
            return Task.CompletedTask;
        }
        cost.SetThisCombat(ReducedSpellCost, reduceOnly: true);
        return Task.CompletedTask;
    }
}
