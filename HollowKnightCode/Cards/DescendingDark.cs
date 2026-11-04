using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: fall as darkness and erupt. The shockwave tears through enemy
/// Block while the hardened shell finishes forming on the next turn
/// (BlockNextTurnPower, the native Dodge and Roll pattern).
/// The spell's attack is powered (Shaman Stone applies); the pierce portion
/// is raw void and never returns SOUL.
/// </summary>
public sealed class DescendingDark : HollowKnightCard
{
    /// <summary>Damage that ignores enemy Block, dealt to every enemy.</summary>
    public const int PierceDamage = 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new BlockVar(12m, ValueProp.Move)
    ];

    public override bool GainsBlock => true;

    public DescendingDark() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay).TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CombatApi.Damage(choiceContext, base.CombatState!.HittableEnemies, PierceDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, this, cardPlay);
        await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, base.Owner.Creature,
            base.DynamicVars.Block.BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
        base.DynamicVars.Block.UpgradeValueBy(4m);
    }
}
