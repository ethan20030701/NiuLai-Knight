using HarmonyLib;
using HollowKnight.HollowKnightCode.Compatibility;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HollowKnight.HollowKnightCode.Patches;

// Core patch: failure must reject loading rather than silently replacing
// old player cards with the game's DeprecatedCard fallback.
[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.CardOrDeprecated), [typeof(ModelId)])]
internal static class RetiredCardSavePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref ModelId id) => id = RetiredCards.Resolve(id);
}

// The engine can inline the tiny SaveUtil lookup into existing callers.
// Normalize the complete native input as well, preserving its standard
// upgrade, saved-property and enchantment reconstruction.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable), [typeof(SerializableCard)])]
internal static class RetiredSerializedCardPatch
{
    [HarmonyPrefix]
    private static void Prefix(SerializableCard save)
    {
        if (save.Id is { } id) save.Id = RetiredCards.Resolve(id);
    }
}
