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
/// What breaks rings back: whenever one of the Knight's cards is Exhausted,
/// harden the shell for Amount Block (4, or 6 upgraded). The consume line's
/// defensive anchor — Weaver's Bites, Deep Focus and Soul Totem all echo.
/// </summary>
public sealed class BrokenShell : HollowKnightCard
{
    private const string PowerVarName = "BrokenShell";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ShellEchoPower>(PowerVarName, 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ShellEchoPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public BrokenShell() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ShellEchoPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m);
    }
}
