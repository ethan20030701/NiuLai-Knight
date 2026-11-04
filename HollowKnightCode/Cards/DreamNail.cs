using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Strike an enemy to pluck insight from its dream. Deals no-soul damage
/// (a Skill, so NailRules never mistakes it for a nail hit) and draws a card
/// — Elites and Bosses have deeper dreams and give more.
/// </summary>
public sealed class DreamNail() : HollowKnightCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string DreamsVarName = "Dreams";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new CardsVar(DreamsVarName, 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        var dreams = base.CombatState!.Encounter?.RoomType is RoomType.Elite or RoomType.Boss
            ? base.DynamicVars[DreamsVarName].BaseValue
            : 1m;
        await CardPileCmd.Draw(choiceContext, dreams, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars[DreamsVarName].UpgradeValueBy(1m);
    }
}
