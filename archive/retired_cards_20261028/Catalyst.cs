using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Distill what already seeps: double the target's Poison, then discard the
/// vial (Catalyst verbatim — the poison line's finisher multiplier).
/// </summary>
public sealed class Catalyst() : HollowKnightCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string MultiplierVarName = "Catalyst";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(MultiplierVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>(0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var poison = cardPlay.Target.GetPower<PoisonPower>();
        if (poison == null || poison.Amount <= 0)
        {
            return;
        }
        // doubling means adding the current amount once more (x3 -> twice more)
        var extra = poison.Amount * (base.DynamicVars[MultiplierVarName].BaseValue - 1m);
        await PowerCmd.Apply<PoisonPower>(choiceContext, cardPlay.Target, extra, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[MultiplierVarName].UpgradeValueBy(1m); // x2 -> x3
    }
}
