using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A shield of dreams, spun from the wishes of Hallownest: for the next
/// three turns the vessel wakes guarded, gaining Block at each turn start.
/// Rare, no-SOUL Skill — fills the rarity x type reward-pool matrix's empty
/// Rare-Skill cell. Sustained payoff for one energy, at the cost of exhaust
/// and patience.
/// </summary>
public sealed class Dreamshield : HollowKnightCard
{
    private const string PowerVarName = "Dreamshield";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DreamshieldPower>(PowerVarName, 10m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DreamshieldPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Dreamshield() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<DreamshieldPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
        power?.Begin(DreamshieldPower.TurnsLasting);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(3m); // 10 -> 13 per turn
    }
}
