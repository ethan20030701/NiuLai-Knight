using System.Collections.Generic;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A pale charm of the King's own essence. Rewards banking SOUL:
/// the more SOUL held, the harder Nail strikes land. Spending SOUL on
/// spells softens the blow — the banking build's tension and payoff.
/// </summary>
public sealed class Kingsoul : HollowKnightCard
{
    private const string PowerVarName = "Kingsoul";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<KingsoulPower>(PowerVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<KingsoulPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public Kingsoul() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<KingsoulPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
