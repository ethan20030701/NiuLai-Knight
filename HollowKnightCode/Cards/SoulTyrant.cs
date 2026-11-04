using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Soul Tyrant. Spell-line gold: when a spell kills, gain 1 Energy and draw
/// 1 (max 2/turn). See SoulTyrantPower.
/// </summary>
public sealed class SoulTyrant : HollowKnightCard
{
    private const string PowerVarName = "SoulTyrant";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<SoulTyrantPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SoulTyrantPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public SoulTyrant() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SoulTyrantPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 2 -> 1; previously had no upgrade
    }
}
