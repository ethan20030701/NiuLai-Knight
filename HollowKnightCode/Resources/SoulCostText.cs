using BaseLib.Abstracts;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Resources;

/// <summary>Spell Twister and temporary discounts must agree with the SOUL badge and payment.</summary>
[HarmonyPatch]
[OptionalPatch("SOUL description formatting")]
public static class SoulCostText
{
    private static IEnumerable<MethodBase> TargetMethods() =>
    // The public wrappers can be inlined into NCard by earlier UI patches.
    // Hook the shared formatting implementation so both rendered text and direct previews agree.
    [AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(method =>
        method.Name == nameof(CardModel.GetDescriptionForPile) && method.GetParameters().Length == 3)];

    [HarmonyPostfix]
    private static void DisplayActualCost(CardModel __instance, ref string __result)
    {
        try
        {
            if (CustomResources<Soul>.Cost(__instance) is not { CostsX: false } cost) return;
            // The localized descriptions identify this resource with its own image path.
            // Costs elsewhere in a card (thresholds, damage, etc.) must remain unchanged.
            var canonical = CustomResources<Soul>.CanonicalCost(__instance);
            if (canonical < 0) return;
            var marker = $"{canonical} [img]res://HollowKnight/images/charui/soul_icon_text.png[/img]";
            __result = __result.Replace(marker,
                $"{cost.GetWithModifiers(CostModifiers.All)} [img]res://HollowKnight/images/charui/soul_icon_text.png[/img]",
                StringComparison.Ordinal);
        }
        catch (Exception error)
        {
            PatchInstaller.WarnOptional(typeof(SoulCostText), error);
        }
    }
}
