using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Establishes a real Grimmchild and strengthens its automatic flame.
/// The persistent blessing survives the pet; damage requires a living pet.
/// </summary>
public sealed class Grimmchild : SwarmCard
{
    private const string PowerVarName = "Grimmchild";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<GrimmchildPower>(PowerVarName, 2m), new DynamicVar("SummonHp", 6m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips,
            HoverTipFactory.FromPower<GrimmchildPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public Grimmchild() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<GrimmchildPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
        await KnightSwarm.Summon<GrimmkinPet>(choiceContext, Owner, DynamicVars["SummonHp"].IntValue);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
        DynamicVars["SummonHp"].UpgradeValueBy(3m);
    }
}
