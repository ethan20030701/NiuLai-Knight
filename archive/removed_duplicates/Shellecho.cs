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
/// Every shatter rings: whenever one of the Knight's cards is Exhausted,
/// gain Amount Block (4 base, 6 upgraded). FeelNoPain for the consume line —
/// Weaver's Bites, Deep Focus and Soul Totem all harden the shell.
/// </summary>
public sealed class ShellEcho : HollowKnightCard
{
    private const string PowerVarName = "ShellEcho";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ShellEchoPower>(PowerVarName, 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ShellEchoPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public ShellEcho() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ShellEchoPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m); // 4 -> 6
    }
}
