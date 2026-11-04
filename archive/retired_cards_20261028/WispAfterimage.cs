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
/// Every motion leaves a shade behind it: gain Block each time a card is
/// played (After Image's slot, in Shade Cloak's imagery). The permanent
/// engine for decks that chain many cheap actions.
/// </summary>
public sealed class WispAfterimage : HollowKnightCard
{
    private const string PowerVarName = "WispAfterimage";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WispAfterimagePower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WispAfterimagePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public WispAfterimage() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<WispAfterimagePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // 1 -> 2 Block per card
    }
}
