using HarmonyLib;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using System.Runtime.CompilerServices;
using KnightCharacter = HollowKnight.HollowKnightCode.Character.HollowKnight;

namespace HollowKnight.HollowKnightCode.Patches;

/// <summary>
/// FromCard defaults every attack to the Attack trigger, including spells.
/// Change non-nail damage to Cast and respond to the game's animation events
/// once per actual hit sequence, rather than once per card in the deck.
/// </summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.FromCard))]
[OptionalPatch("Knight attack animation selection")]
internal static class KnightAttackKindPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel card, AttackCommand __result)
    {
        if (card?.Owner?.Character is not KnightCharacter || __result == null)
        {
            return;
        }
        try
        {
            bool nail = (NailRules.IsNailCard(card)
                         && !card.Tags.Contains(CardTag.Shiv)
                         && card is not SoulUnbound)
                        || card is DreamNail;
            if (!nail)
            {
                __result.WithAttackerAnim("Cast", card.Owner.Character.CastAnimDelay);
            }
        }
        catch (System.Exception e)
        {
            PatchInstaller.WarnOptional(typeof(KnightAttackKindPatch), e);
        }
    }
}

// Reuse the attack builder's real per-target, per-hit timing. Effects are
// cosmetic and own no combat state, resource bookkeeping or gameplay waits.
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
[OptionalPatch("Knight impact visuals")]
internal static class KnightSpellImpactPatch
{
    private static readonly ConditionalWeakTable<AttackCommand, object> Configured = new();

    [HarmonyPrefix]
    private static void Prefix(AttackCommand __instance)
    {
        if (__instance?.ModelSource is not HollowKnightCard card
            || card.Owner?.Character is not KnightCharacter)
        {
            return;
        }
        try
        {
            if (Configured.TryGetValue(__instance, out _)) return;
            var style = card switch
            {
                DescendingDark or SoulUnbound => KnightImpact.Style.Void,
                DesolateDive => KnightImpact.Style.Dive,
                _ when NailRules.IsSpellCard(card) || card is SoulSiphon => KnightImpact.Style.Soul,
                _ => KnightImpact.Style.Nail
            };
            __instance.WithHitVfxNode(target => CreateImpact(target, style));
            if (__instance.HitVfx is null or "vfx/vfx_attack_slash" or "vfx/vfx_starry_impact")
                __instance.WithHitFx(null, __instance.HitSfx, __instance.TmpHitSfx);
            Configured.Add(__instance, new object());
        }
        catch (System.Exception e)
        {
            PatchInstaller.WarnOptional(typeof(KnightSpellImpactPatch), e);
        }
    }

    private static KnightImpact? CreateImpact(Creature target, KnightImpact.Style style)
    {
        // AttackCommand invokes this callback later, outside the setup prefix.
        try { return KnightImpact.AtTarget(target, style); }
        catch (Exception error)
        {
            PatchInstaller.WarnOptional(typeof(KnightSpellImpactPatch), error);
            return null;
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
[OptionalPatch("Knight combat animation feedback")]
internal static class KnightAnimationTriggerPatch
{
    [HarmonyPrefix]
    private static void Prefix(Creature creature, string triggerName)
    {
        if (creature == null || !creature.IsPlayer || creature.Player?.Character is not KnightCharacter)
        {
            return;
        }
        try
        {
            switch (triggerName)
            {
                case "Attack": KnightFeedback.Nail(creature); break;
                case "Cast": KnightFeedback.Soul(creature); break;
                case "Hit": KnightFeedback.Hurt(creature); break;
            }
        }
        catch (System.Exception e)
        {
            PatchInstaller.WarnOptional(typeof(KnightAnimationTriggerPatch), e);
        }
    }
}
