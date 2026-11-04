using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The mantis lords' blessing upon a worthy nail. The first Nail Art the
/// vessel swings each turn lands with the weight of all three masters.
/// </summary>
public sealed class NailmastersGlory : HollowKnightRelic
{
    /// <summary>Bonus damage on the first Nail Art of each turn.</summary>
    public const int FirstArtBonusDamage = 8;

    private static readonly SpireField<Player, bool> GlorySpentThisTurn = new(() => false);
    private CardPlay? _bonusPlay;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner)
        {
            GlorySpentThisTurn[player] = false;
        }
        return Task.CompletedTask;
    }

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!NailRules.IsNailArt(cardSource)
            || !NailRules.GainsNailStrength(dealer, base.Owner.Creature, props, cardSource))
        {
            return 0m;
        }
        return cardPlay != null
            ? ReferenceEquals(cardPlay, _bonusPlay) ? FirstArtBonusDamage : 0m
            : !GlorySpentThisTurn[Owner] ? FirstArtBonusDamage : 0m;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && !GlorySpentThisTurn[Owner] && NailRules.IsNailArt(cardPlay.Card))
        {
            GlorySpentThisTurn[Owner] = true;
            _bonusPlay = cardPlay;
            Flash();
        }
        return Task.CompletedTask;
    }
}
