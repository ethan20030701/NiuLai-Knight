using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A spell: scream wraiths into the sky, three times over.
/// The multi-wave little brother of Abyss Shriek - three Shaman Stone
/// applications per cast, and each wave re-targets whoever is still alive.
/// </summary>
public sealed class HowlingWraiths : HollowKnightCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public HowlingWraiths() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).WithHitCount(3).FromKnightCard(this, cardPlay).TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
