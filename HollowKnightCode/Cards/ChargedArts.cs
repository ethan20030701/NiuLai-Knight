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
/// The Skill-charge &gt; Sword-Art-release engine: every Skill played this
/// turn adds Amount (3, or 5 upgraded) to the next Sword Art's damage
/// (stacks; a played Sword Art consumes the charge). v0.3.9: the per-Skill
/// rate is the power's Amount — the upgrade is a visible 3 to 5 engine
/// bump, and Pride's Mark doubles whatever the rate is.
/// </summary>
public sealed class ChargedArts : HollowKnightCard
{
    private const string PowerVarName = "ChargedArts";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ChargedArtsPower>(PowerVarName, 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ChargedArtsPower>((int)base.DynamicVars[PowerVarName].BaseValue)];

    public ChargedArts() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ChargedArtsPower>(choiceContext, Owner.Creature,
            base.DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars[PowerVarName].UpgradeValueBy(2m); // +3 -> +5 per Skill
    }
}
