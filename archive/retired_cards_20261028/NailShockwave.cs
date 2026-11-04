using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A charged swing releases a cutting wave of air. It gains Nail strength,
/// but the distant wave does not gather SOUL. Higher damage distinguishes
/// it from Cyclone Slash, which does gather SOUL on every enemy hit.
/// </summary>
public sealed class NailShockwave() : HollowKnightCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override bool IsNailArt => true;
    public override bool GathersSoul => false;


    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromKnightCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
