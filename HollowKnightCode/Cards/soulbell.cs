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
/// The spell school's draw engine: the first Spell cast each turn tolls the
/// bell and draws Amount cards (1 base, 2 upgraded). Sprintmaster's Gift's
/// first-Skill pattern, aimed at Spells — the missing half of the draw line.
/// </summary>
public sealed class Soulbell : HollowKnightCard
{
    private const string PowerVarName = "Soulbell";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SoulbellPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SoulbellPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public Soulbell() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SoulbellPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // 1 -> 2 cards
    }
}
