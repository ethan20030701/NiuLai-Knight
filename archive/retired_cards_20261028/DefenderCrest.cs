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
/// The crest's miasma hangs over the whole field: at the end of your turn,
/// every enemy seeps with venom (Defender's Crest charm, Noxious Fumes'
/// slot for the Knight).
/// </summary>
public sealed class DefenderCrest : HollowKnightCard
{
    private const string PowerVarName = "DefenderCrest";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DefenderCrestPower>(PowerVarName, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DefenderCrestPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public DefenderCrest() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<DefenderCrestPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(1m); // 2 -> 3 poison per turn
    }
}
