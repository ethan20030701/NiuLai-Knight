using System;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A well-honed edge rewards a landed combo: 5 damage + draw 1 — or 10
/// damage if the Knight has already landed 4+ hits this turn. First
/// "hit count as resource" card. Also silently ensures the Knight carries
/// the hidden hit-counter (applied once per combat on first draw).
/// </summary>
public sealed class SharpAssault : HollowKnightCard
{
    /// <summary>Hits needed this turn to unlock the bonus damage.</summary>
    public const int HitsThreshold = 4;

    /// <summary>Base damage (no combo).</summary>
    public const decimal BaseDamage = 5m;

    /// <summary>Bonus damage while the combo (4+ hits this turn) is live.</summary>
    public const decimal ComboBonus = 5m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CalculationBaseVar(BaseDamage),
            new ExtraDamageVar(ComboBonus),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
                HitCounterPower.HitsThisTurnFor(card.Owner) >= HitsThreshold ? 1m : 0m)
        ];

    public SharpAssault() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        decimal dmg = base.DynamicVars.CalculatedDamage.Calculate(cardPlay.Target);
        await DamageCmd.Attack(dmg).FromKnightCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.CalculationBase.UpgradeValueBy(2m);
    }
}
