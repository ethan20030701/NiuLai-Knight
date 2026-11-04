using HollowKnight.HollowKnightCode.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

public sealed class SilkenAegis : SwarmCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Multiplier", 2m), new DynamicVar("MaxBlock", 30m)];

    public decimal BlockFromCompanion(MegaCrit.Sts2.Core.Entities.Creatures.Creature pet) =>
        Math.Min(pet.CurrentHp * DynamicVars["Multiplier"].BaseValue, DynamicVars["MaxBlock"].BaseValue);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    public override bool GainsBlock => true;
    protected override bool IsPlayable => KnightSwarm.LivingCount(Owner) > 0;
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && !IsPlayable;
    public SilkenAegis() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (KnightSwarm.Strongest(Owner) is not { } pet) return;
        var block = BlockFromCompanion(pet);
        await CreatureCmd.Kill(pet);
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Multiplier"].UpgradeValueBy(1m);
        DynamicVars["MaxBlock"].UpgradeValueBy(10m);
    }
}
