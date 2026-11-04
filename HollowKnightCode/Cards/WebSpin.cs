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
/// The weavers' thread binds every foe in place: apply Weak to ALL enemies
/// (the weaver line's team support, alongside Weaversong's song).
/// </summary>
public sealed class WebSpin() : HollowKnightCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string WeakVarName = "Weak";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(WeakVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>((int)base.DynamicVars[WeakVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in base.CombatState!.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, base.DynamicVars[WeakVarName].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[WeakVarName].UpgradeValueBy(1m); // 2 -> 3 Weak
    }
}
