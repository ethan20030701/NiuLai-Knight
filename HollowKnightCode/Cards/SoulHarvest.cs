using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: reap every foe on the field and drink what falls. Kill tally
/// comes from the attack's own results (Feed's WasTargetKilled pattern).
/// </summary>
public sealed class SoulHarvest : HollowKnightCard
{
    /// <summary>HP restored per enemy killed by the reaping.</summary>
    public const int HealPerKill = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16m, ValueProp.Move)];

    public SoulHarvest() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        AttackCommand attack = await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        int killed = attack.Results.SelectMany((List<DamageResult> r) => r).Count((DamageResult r) => r.WasTargetKilled);
        if (killed > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, HealPerKill * killed);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
