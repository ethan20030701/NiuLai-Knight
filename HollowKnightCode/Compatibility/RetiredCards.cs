using HollowKnight.HollowKnightCode.Cards;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Compatibility;

/// <summary>
/// Save-only aliases for retired cards. No legacy models are registered in
/// the library, card pools, rewards or network model table. Native card load
/// still restores upgrades, saved properties, enchantments and deck history;
/// the next save naturally records the replacement's canonical ID.
/// </summary>
public static class RetiredCards
{
    public static ModelId Resolve(ModelId id)
    {
        // Scope to our card category AND prefixed entries. Other model kinds
        // or another mod's similarly named cards must never be rewritten.
        if (id.Category != ModelDb.GetId<VengefulSpirit>().Category) return id;
        return id.Entry switch
        {
            "HOLLOWKNIGHT-SOUL_BOLT" => ModelDb.GetId<VengefulSpirit>(),
            "HOLLOWKNIGHT-HUM" => ModelDb.GetId<QuickSlash>(),
            "HOLLOWKNIGHT-HALLOWNESTS_ECHO" => ModelDb.GetId<AbyssShriek>(),
            "HOLLOWKNIGHT-BONE_BREAK" => ModelDb.GetId<HeavyBlow>(),
            "HOLLOWKNIGHT-FRAGILE_STRENGTH" => ModelDb.GetId<UnbreakableStrength>(),
            "HOLLOWKNIGHT-NAIL_FLURRY" => ModelDb.GetId<NailStorm>(),
            "HOLLOWKNIGHT-SPORE_BREATH" => ModelDb.GetId<DarkShrine>(),
            "HOLLOWKNIGHT-CATALYST" => ModelDb.GetId<Whetstone>(),
            "HOLLOWKNIGHT-DEFENDER_CREST" => ModelDb.GetId<ThornsOfAgony>(),
            "HOLLOWKNIGHT-VENOM_BLADE" => ModelDb.GetId<WebSpin>(),
            "HOLLOWKNIGHT-NAIL_ENVENOM" => ModelDb.GetId<CoiledNail>(),
            "HOLLOWKNIGHT-SHELL_SLAM" => ModelDb.GetId<Dash>(),
            "HOLLOWKNIGHT-IMPREGNABLE" => ModelDb.GetId<Dreamshield>(),
            "HOLLOWKNIGHT-WISP_AFTERIMAGE" => ModelDb.GetId<BrokenShell>(),
            "HOLLOWKNIGHT-WRAITH_FORM" => ModelDb.GetId<ShadeCloak>(),
            "HOLLOWKNIGHT-VOID_SURGE" => ModelDb.GetId<Kingsoul>(),
            "HOLLOWKNIGHT-REFINEMENT" => ModelDb.GetId<SoulPact>(),
            "HOLLOWKNIGHT-VOID_FEAST" => ModelDb.GetId<SoulHarvest>(),
            "HOLLOWKNIGHT-RAGING_NAIL" => ModelDb.GetId<QuickSlash>(),
            "HOLLOWKNIGHT-SOUL_RUSH" => ModelDb.GetId<SoulFlow>(),
            "HOLLOWKNIGHT-NAIL_SHOCKWAVE" => ModelDb.GetId<CycloneSlash>(),
            _ => id
        };
    }
}
