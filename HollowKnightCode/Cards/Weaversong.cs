using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Summons;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Summons a real weaver on play and preserves the existing spectral-bite
/// engine as a bridge into exhaust decks. The token is not a pet command.
/// </summary>
public sealed class Weaversong() : SwarmCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const string PowerVarName = "Weaversong";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeaversongPower>(PowerVarName, 4m), new DynamicVar("SummonHp", 6m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips,
            HoverTipFactory.FromPower<WeaversongPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeaversongPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
        await KnightSwarm.Summon<WeaverlingPet>(choiceContext, Owner, DynamicVars["SummonHp"].IntValue);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m);
        DynamicVars["SummonHp"].UpgradeValueBy(3m);
    }
}
