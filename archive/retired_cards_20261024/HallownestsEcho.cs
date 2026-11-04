using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: the old kingdom answers one last time. A flat, reliable wave
/// across the whole field — Abyss Shriek's echo scales with survivors, this
/// simply always lands at full weight (the boss-fight AOE).
/// </summary>
public sealed class HallownestsEcho : HollowKnightCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(26m, ValueProp.Move)];

    public HallownestsEcho() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay).TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
