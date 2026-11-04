using System.Collections.Generic;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The banking spell-build's finisher: pour X energy worth of stored SOUL
/// into one torrent — X waves of {Damage} to every enemy. A real spell:
/// 33 SOUL + X energy, amplified by Shaman Stone / Full Soul, feeds Soul
/// Tyrant. Upgrade +2 per wave.
/// </summary>
public sealed class SoulTorrent : HollowKnightCard
{
    public SoulTorrent() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        CustomResources<Soul>.SetCanonicalCost(this, Soul.SpellCost);
    }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).WithHitCount(ResolveEnergyXValue())
            .FromKnightCard(this, cardPlay).TargetingAllOpponents(base.CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
