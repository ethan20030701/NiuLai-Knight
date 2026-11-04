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
/// Old nail-poison, refined: every nail strike that lands seeps venom into
/// the wound (Envenom's slot, narrowed to nail hits — spells stay pure
/// SOUL). The nail line's scaling finisher against long fights.
/// </summary>
public sealed class NailEnvenom : HollowKnightCard
{
    private const string PowerVarName = "NailEnvenom";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<NailEnvenomPower>(PowerVarName, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<NailEnvenomPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public NailEnvenom() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<NailEnvenomPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // 1 -> 2 poison per nail hit
    }
}
