using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(HollowKnightCardPool))]
public abstract class HollowKnightCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    /// <summary>
    /// Nail Arts are the charged nail techniques (Great Slash, Dash Slash,
    /// Cyclone Slash). Relics like Nailmaster's Glory key off this - the game's
    /// CardTag enum is closed, so this virtual property is our tag mechanism.
    /// </summary>
    public virtual bool IsNailArt => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsNailArt
        ? [new HoverTip(new LocString("static_hover_tips", "HOLLOWKNIGHT-NAIL_ART.title"),
            new LocString("static_hover_tips", "HOLLOWKNIGHT-NAIL_ART.description"), null)]
        : base.ExtraHoverTips;

    /// <summary>
    /// Spells exempt from SOUL-cost reductions (Spell Twister). Focus stays
    /// full price so sustain cannot be discounted into spam.
    /// </summary>
    public virtual bool ExemptFromSpellCostReduction => false;

    /// <summary>
    /// Non-nail attacks (companion commands and SOUL eruptions) opt out of
    /// gathering SOUL. NailRules.IsNailHit checks this gate independently
    /// of the card's cost and damage bonuses.
    /// </summary>
    public virtual bool GathersSoul => true;

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /* Each card receives BeforeCardPlayed through the deck listener list.
       Identity-checking the played instance gives utility skills one pulse
       without repeating it for every deck card. Attacks and hits animate
       from the game's actual animation triggers. */
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, this) || Owner?.Creature is not { } creature)
        {
            return Task.CompletedTask;
        }
        CombatApi.Begin(cardPlay);
        try
        {
            if (Type == CardType.Skill
                && this is not DreamNail)
            {
                KnightFeedback.Skill(creature, this switch
                {
                    Focus or DeepFocus => "focus",
                    ShadeCloak => "void",
                    SoulSiphon => "soul",
                    _ when GainsBlock => "guard",
                    _ when NailRules.IsSpellCard(this) => "soul",
                    _ => "power"
                });
            }
            else if (Type == CardType.Power)
            {
                KnightFeedback.Power(creature);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Knight skill animation skipped: {e.Message}");
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (ReferenceEquals(cardPlay.Card, this)) CombatApi.End(cardPlay);
        return Task.CompletedTask;
    }
}
