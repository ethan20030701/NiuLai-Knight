using System.Linq;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: a scream from the Abyss that strikes every enemy, echoing
/// harder the more foes still stand (+5 per other surviving enemy).
/// Costs 33 SOUL in addition to its energy cost. Being a spell, its damage never returns SOUL.
/// </summary>
public sealed class AbyssShriek : HollowKnightCard
{
    /// <summary>Bonus damage per other surviving enemy at cast time.</summary>
    public const decimal PerOtherEnemyDamage = 5m;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m, ValueProp.Move)];

    public AbyssShriek() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // The shriek echoes off every living thing: +5 per OTHER surviving
        // enemy at cast time. Single boss = base 20; 3 enemies = 30; 5 = 40.
        int others = Math.Max(0, base.CombatState!.HittableEnemies.Count - 1);
        decimal total = base.DynamicVars.Damage.BaseValue + PerOtherEnemyDamage * others;
        await DamageCmd.Attack(total).FromKnightCard(this, cardPlay).TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
