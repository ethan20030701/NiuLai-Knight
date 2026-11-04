using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The Spell mirror of Charged Arts: every Skill played this turn charges
/// the next Spell with +Amount damage (stacking, consumed by the next
/// Spell). Upgrade raises the per-Skill rate 3 to 5 — an engine upgrade,
/// not dead text (the payout reads the power's Amount).
/// </summary>
public sealed class Spellcharge : HollowKnightCard
{
    private const string PowerVarName = "Spellcharge";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SpellchargePower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SpellchargePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public Spellcharge() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SpellchargePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m); // +3 -> +5 per Skill
    }
}
