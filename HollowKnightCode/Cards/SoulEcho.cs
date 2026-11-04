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
/// Every outflow of SOUL rings the echo: whenever the Knight spends SOUL
/// (casting a spell, firing the totem), draw a card (Dream Totem's
/// WasSpent hook, uncapped — SOUL itself is the natural limiter).
/// </summary>
public sealed class SoulEcho : HollowKnightCard
{
    private const string PowerVarName = "SoulEcho";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SoulEchoPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SoulEchoPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public SoulEcho() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SoulEchoPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // draw 2 per spend
    }
}
