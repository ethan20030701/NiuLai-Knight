using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// An edge wet with old venom: a heavy dose of Poison on one target
/// (Deadly Poison's slot — the poison line's single-target payload).
/// </summary>
public sealed class VenomBlade() : HollowKnightCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string PoisonVarName = "Poison";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(PoisonVarName, 6m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>((int)base.DynamicVars[PoisonVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<PoisonPower>(choiceContext, cardPlay.Target, base.DynamicVars[PoisonVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PoisonVarName].UpgradeValueBy(3m); // 6 -> 9 poison
    }
}
