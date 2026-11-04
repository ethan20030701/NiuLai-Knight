using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Draw the foe's essence out through the nail: a raw (unpowered) cut and a
/// measure of SOUL carried back — skill damage gathers nothing on its own,
/// the siphon is what brings essence home.
/// </summary>
public sealed class SoulSiphon : HollowKnightCard
{
    private const string SoulVarName = "Soul";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Unpowered), new DynamicVar(SoulVarName, 22m)];

    public SoulSiphon() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CombatApi.Damage(choiceContext, cardPlay.Target, base.DynamicVars.Damage.BaseValue,
            ValueProp.Unpowered, Owner.Creature, this, cardPlay);
        Soul.Gain(Owner, base.DynamicVars[SoulVarName].IntValue);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars[SoulVarName].UpgradeValueBy(11m); // 22 -> 33 SOUL
    }
}
