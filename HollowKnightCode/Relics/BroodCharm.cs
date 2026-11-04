using MegaCrit.Sts2.Core.Entities.Relics;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>Each summon adds a little more lasting HP to its species.</summary>
public sealed class BroodCharm : HollowKnightRelic
{
    public const int BonusHp = 3;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
