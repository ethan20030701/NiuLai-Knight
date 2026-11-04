using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Spores burst from the shell as it hardens, seeping venom into the target
/// (Spore Shroom charm, on-demand). Small block plus a poison seed.
/// </summary>
public sealed class SporeBreath() : HollowKnightCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string PoisonVarName = "Poison";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4m, ValueProp.Move),
        new DynamicVar(PoisonVarName, 2m)
    ];

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>((int)base.DynamicVars[PoisonVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PoisonPower>(choiceContext, cardPlay.Target, base.DynamicVars[PoisonVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        base.DynamicVars[PoisonVarName].UpgradeValueBy(1m); // 2 -> 3 poison
    }
}
