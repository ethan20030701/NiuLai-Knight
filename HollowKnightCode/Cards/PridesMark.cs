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
/// Mark of Pride. Upgrades: whenever the Knight plays a Sword Art, the draw
/// becomes 2 cards (base 1). The double-rate charge (+6) is the UNUPGRADED
/// effect and is always on — the upgrade extends the card-draw payoff.
/// Implemented via PowerVar 1m -> 2m; PridesMarkPower.AfterCardPlayed reads
/// its own Amount to size the draw.
/// </summary>
public sealed class PridesMark : HollowKnightCard
{
    private const string PowerVarName = "PridesMark";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PridesMarkPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PridesMarkPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public PridesMark() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<PridesMarkPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
