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
/// The spell-side twin of Kingsoul: while the vessel holds at least 66 SOUL,
/// every Spell deals Amount extra damage (7, or 10 upgraded). Spending SOUL
/// dims the blessing — the banking spell-build's tension and payoff.
/// </summary>
public sealed class FullSoul : HollowKnightCard
{
    private const string PowerVarName = "FullSoul";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FullSoulPower>(PowerVarName, 7m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FullSoulPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public FullSoul() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<FullSoulPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(3m); // 7 -> 10
    }
}
