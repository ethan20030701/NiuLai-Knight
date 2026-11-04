using MegaCrit.Sts2.Core.Commands;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Harden the shell temporarily. Costs no SOUL.
/// </summary>
public sealed class DarkShrine() : HollowKnightCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string BlessingVarName = "Blessing";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("SoulGain", 11m),
        new RepeatVar(BlessingVarName, 3) // blessing: next Spell this turn +N damage
    ];

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        Soul.Gain(Owner, DynamicVars["SoulGain"].IntValue);
        await PowerCmd.Apply<ShrineBlessingPower>(choiceContext, Owner.Creature,
            base.DynamicVars[BlessingVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
        base.DynamicVars[BlessingVarName].UpgradeValueBy(1m); // blessing 3 -> 4
    }
}
