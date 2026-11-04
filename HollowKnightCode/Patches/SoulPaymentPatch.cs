using System.Threading.Tasks;
using HarmonyLib;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class SoulPaymentPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardPlay cardPlay)
    {
        SoulSpendTracker.Bind(cardPlay);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class SoulPaymentCleanupPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel __instance, out SoulSpendTracker.PlayScope __state)
    {
        // Reserve payment before pile/play-count hooks can auto-play the same card recursively.
        __state = SoulSpendTracker.BeginScope(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(SoulSpendTracker.PlayScope __state, ref Task __result)
    {
        SoulSpendTracker.RestoreCallerScope(__state);
        __result = Finish(__result, __state);
    }

    [HarmonyFinalizer]
    private static void Finalizer(SoulSpendTracker.PlayScope __state)
    {
        if (__state != null)
        {
            SoulSpendTracker.RestoreCallerScope(__state);
        }
    }

    private static async Task Finish(Task play, SoulSpendTracker.PlayScope scope)
    {
        try
        {
            await play;
        }
        finally
        {
            // Covers zero-play series, combat ending, and exceptions before the first CardPlay.
            SoulSpendTracker.FinishScope(scope);
        }
    }
}
