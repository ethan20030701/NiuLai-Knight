using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// A cocoon of living lifeblood wraps the vessel: Block now, and this turn
/// every blow the shell soaks seeps SOUL back in. Defense becomes fuel —
/// without ever needing to lose HP like Grubsong does.
/// </summary>
public sealed class LifebloodCocoon : HollowKnightCard
{
    private const string PowerVarName = "SoulPerAbsorb";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(9m, ValueProp.Move),
        new PowerVar<LifebloodCocoonPower>(PowerVarName, 11m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<LifebloodCocoonPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public LifebloodCocoon() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<LifebloodCocoonPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
        base.DynamicVars[PowerVarName].UpgradeValueBy(7m); // 11 -> 18; two blocked hits fund a spell
    }
}
