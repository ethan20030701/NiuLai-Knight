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
/// The grubs' grace shines through an unblemished shell: while at FULL HP,
/// nail strikes fire beams of elegance (Grubberfly's Elegance charm — the
/// mirror of Fury of the Fallen's wounded line).
/// </summary>
public sealed class GrubberflyElegance : HollowKnightCard
{
    private const string PowerVarName = "GrubberflyElegance";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<GrubberflyElegancePower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<GrubberflyElegancePower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public GrubberflyElegance() : base(1, CardType.Power, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<GrubberflyElegancePower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m); // 3 -> 5
    }
}
