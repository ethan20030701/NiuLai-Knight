using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode;

/// <summary>
/// Classifies the Knight's damage into the two halves of the SOUL loop:
/// Nail strikes (Attack cards that cost no SOUL) gather SOUL;
/// Spells (cards with a SOUL cost) spend it and never generate it.
/// A card's SOUL cost is the canonical discriminator, so no separate tag is needed
/// and future cards fall into the right category automatically.
/// </summary>
public static class NailRules
{
    /// <summary>
    /// True when the damage is a Nail hit: the owner's own Attack card, powered, with no SOUL cost.
    /// Shared by the SOUL-gain hooks (Soul Catcher, Soul Eater) and the nail-strength powers.
    /// Shiv-tagged cards (spawned token attacks, e.g. Weaversong's weaver bites) are excluded:
    /// SOUL only comes from real nail strikes, never from tokens.
    /// </summary>
    public static bool IsNailHit(Creature? dealer, Creature ownerCreature, ValueProp props, CardModel? cardSource)
    {
        return dealer == ownerCreature
            && cardSource != null
            && cardSource.Type == CardType.Attack
            && props.IsPoweredAttack()
            && !cardSource.Tags.Contains(CardTag.Shiv)
            && (cardSource is not Cards.HollowKnightCard { GathersSoul: false })
            && CustomResources<Soul>.CanonicalCost(cardSource) == -1;
    }

    /// <summary>True when the card consumes SOUL, i.e. it is one of the Knight's spells.</summary>
    public static bool IsSpellCard(CardModel? card)
    {
        return card != null && CustomResources<Soul>.CanonicalCost(card) != -1;
    }

    /// <summary>Attack spells consume attack-only buffs; sustain skills keep them.</summary>
    public static bool IsAttackSpell(CardModel? card) => card?.Type == CardType.Attack && IsSpellCard(card);

    /// <summary>
    /// True when the card is a Nail Art (Great Slash, Dash Slash, Cyclone Slash).
    /// Used by Nailmaster's Glory; the tag lives on HollowKnightCard.IsNailArt.
    /// </summary>
    public static bool IsNailArt(CardModel? card)
    {
        return card is Cards.HollowKnightCard { IsNailArt: true };
    }

    /// <summary>
    /// True when the card is a Nail attack: an Attack card with no SOUL cost,
    /// excluding generated Shiv tokens, companion commands and Soul Unbound's SOUL eruption.
    /// Used outside of damage resolution (e.g. hit-count modifiers) where the
    /// damage ValueProp is not yet known.
    /// </summary>
    public static bool IsNailCard(CardModel? card)
    {
        return card != null && card.Type == CardType.Attack
            && !card.Tags.Contains(CardTag.Shiv)
            && card is not Cards.SoulUnbound
            && card is not Cards.SwarmCommandCard
            && CustomResources<Soul>.CanonicalCost(card) == -1;
    }

    /// <summary>
    /// True when damage gains nail strength (Unbreakable Strength, Whetstone,
    /// Fury of the Fallen, Nailmaster's Glory): a regular nail hit or a
    /// powered Nail Art. Damage bonuses and SOUL generation remain separate.
    /// </summary>
    public static bool GainsNailStrength(Creature? dealer, Creature ownerCreature, ValueProp props, CardModel? cardSource)
    {
        return IsNailHit(dealer, ownerCreature, props, cardSource)
            || (dealer == ownerCreature && props.IsPoweredAttack() && IsNailArt(cardSource));
    }
}
