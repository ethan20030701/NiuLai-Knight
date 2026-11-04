using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A weaver's spectral bite, spun into the hand by Weaversong. A Shiv-style
/// token: free, exhausted when played, tagged Shiv so NailRules.IsNailHit
/// never mistakes it for a nail — spawned attacks gather no SOUL.
/// Deliberately hidden from the library and locked to Token rarity: reward
/// and shop rolls only ever draw Common/Uncommon/Rare, so the pool attribute
/// (required by BaseLib's content registration) never surfaces it there.
/// Its displayed damage includes the WeaversongPower bonus via
/// ModifyDamageAdditive (the Accuracy pattern), so the base var is 0 and the
/// song's Amount carries the real damage.
/// </summary>
[Pool(typeof(HollowKnightCardPool))]
public sealed class WeaverBite : CustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Shiv];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(0m, ValueProp.Move)];

    public WeaverBite() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, showInCardLibrary: false) { }

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public static async Task CreateInHand(Player owner, ICombatState combatState, int count = 1)
    {
        var bites = new List<CardModel>();
        for (var i = 0; i < count; i++)
        {
            bites.Add(combatState.CreateCard<WeaverBite>(owner));
        }
        await CardPileCmd.AddGeneratedCardsToCombat(bites, PileType.Hand, owner);
    }
}
