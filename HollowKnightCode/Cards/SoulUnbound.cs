using System;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The banking line's harvest. Releases every drop of stored SOUL in one
/// eruption: 15 damage + 15 per full 33 SOUL held (33=30, 66=45, 99=60).
/// Deliberately NOT a nail hit: it consumes SOUL, so nail-strength bonuses
/// (Unbreakable/Fragile/Fury/Whetstone/Kingsoul) never apply — the eruption
/// is SOUL itself. Upgrade raises the base by 5.
/// </summary>
public sealed class SoulUnbound : HollowKnightCard
{
    public override bool GathersSoul => false;

    /// <summary>Base damage of the eruption.</summary>
    public const decimal BaseDamage = 15m;

    /// <summary>Extra damage per full 33 SOUL held.</summary>
    public const decimal DamagePerFullCharge = 15m;

    public SoulUnbound() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override bool IsPlayable => Owner?.PlayerCombatState is not { } combat
        || (CustomResources<Soul>.TryGet(combat, out var soul) && soul.Amount >= Soul.SpellCost);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.PlayerCombatState == null
            || !CustomResources<Soul>.TryGet(player.PlayerCombatState, out var soul)
            || soul.Amount < Soul.SpellCost)
        {
            return;
        }
        decimal total = base.DynamicVars.CalculatedDamage.Calculate(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(total).FromKnightCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
        Soul.SpendAll(player, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.CalculationBase.UpgradeValueBy(5m);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CalculationBaseVar(BaseDamage),
            new ExtraDamageVar(DamagePerFullCharge),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => FullCharges(card.Owner))
        ];

    public static int FullCharges(Player? player) =>
        CustomResources<Soul>.TryGet(player?.PlayerCombatState, out Soul? soul)
            ? soul.Amount / Soul.SpellCost : 0;
}
