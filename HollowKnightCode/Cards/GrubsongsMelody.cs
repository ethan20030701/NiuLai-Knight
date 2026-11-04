using System.Collections.Generic;
using System.Threading.Tasks;
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
/// Grubsong's Melody. The injured line's card-side engine: whenever the
/// vessel actually loses HP, gather one Nail hit of SOUL. Spending a card
/// and an energy on this engine needs a full threshold-relevant payout.
/// </summary>
public sealed class GrubsongsMelody : HollowKnightCard
{
    private const string PowerVarName = "GrubsongsMelody";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<GrubsongsMelodyPower>(PowerVarName, 11m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<GrubsongsMelodyPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public GrubsongsMelody() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<GrubsongsMelodyPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(11m); // 11 -> 22 SOUL per HP-loss event
    }
}
