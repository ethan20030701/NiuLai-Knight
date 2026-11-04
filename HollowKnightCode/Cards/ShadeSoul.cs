using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: launch a shade of pure void. It slams the target and the
/// shockwave tears into everything else on the field.
/// </summary>
public sealed class ShadeSoul : HollowKnightCard
{
    /// <summary>Damage dealt to every enemy other than the main target.</summary>
    public const int SplashDamage = 12;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(32m, ValueProp.Move)];

    public ShadeSoul() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 2m, Owner.Creature, this);
        var others = base.CombatState!.HittableEnemies.Where(creature => creature != cardPlay.Target).ToList();
        if (others.Count > 0)
        {
            await CombatApi.Damage(choiceContext, others, SplashDamage, ValueProp.Unpowered, Owner.Creature, this, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(8m);
    }
}
