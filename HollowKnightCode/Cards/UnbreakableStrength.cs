using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class UnbreakableStrength : HollowKnightCard
{
    private const string PowerVarName = "UnbreakableStrength";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<UnbreakableStrengthPower>(PowerVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<UnbreakableStrengthPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public UnbreakableStrength() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<UnbreakableStrengthPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}
