using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using HollowKnight.HollowKnightCode.Summons;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>Shared rules tooltip for the pet archetype.</summary>
public abstract class SwarmCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : HollowKnightCard(cost, type, rarity, target)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [new HoverTip(new LocString("static_hover_tips", "HOLLOWKNIGHT-SWARM.title"),
        new LocString("static_hover_tips", "HOLLOWKNIGHT-SWARM.description"), null)];
}

/// <summary>Command attacks come from the bugs, so they cannot gather nail SOUL.</summary>
public abstract class SwarmCommandCard(int cost, CardRarity rarity, TargetType target)
    : SwarmCard(cost, CardType.Attack, rarity, target)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [..base.ExtraHoverTips,
        new HoverTip(new LocString("static_hover_tips", "HOLLOWKNIGHT-SWARM_COMMAND.title"),
            new LocString("static_hover_tips", "HOLLOWKNIGHT-SWARM_COMMAND.description"), null)];
    public override bool GathersSoul => false;
    protected virtual bool HasRequiredPet => KnightSwarm.LivingCount(Owner) > 0;
    protected override bool IsPlayable => HasRequiredPet;
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && !HasRequiredPet;
}
