using MegaCrit.Sts2.Core.Entities.Relics;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A cloak dyed in the Abyss. When the vessel's Shade Cloak phases an attack,
/// the shadow left behind lashes out at the attacker. The damage itself is
/// dealt by ShadeCloakPower (it is the one that sees the dodge happen).
/// </summary>
public sealed class SharpShadow : HollowKnightRelic
{
    /// <summary>Damage dealt to the attacker when a Shade Cloak dodge triggers.</summary>
    public const int ShadowDamage = 9;

    public override RelicRarity Rarity => RelicRarity.Rare;
}
