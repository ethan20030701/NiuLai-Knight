using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace HollowKnight.HollowKnightCode.Patches;

/*  The game-over screen moves the creature visuals onto itself but never
    fires the death animation trigger (the Knight just stands there at 0 HP).
    Hornet's mod works around the same gap by re-triggering death on the
    screen; ours replays our AnimatedSprite2D ("Dead" frames, built in
    HollowKnight.AddDeathAnimation) — its FrameChanged handler hides the
    living visuals underneath. Installed as an optional presentation hook. */
[HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
[OptionalPatch("Knight game-over death animation")]
internal static class GameOverDeathPatch
{
    [HarmonyPostfix]
    private static void Postfix(NGameOverScreen __instance)
    {
        try
        {
            PlayDeathAnim(__instance);
        }
        catch (System.Exception e)
        {
            PatchInstaller.WarnOptional(typeof(GameOverDeathPatch), e);
        }
    }

    private static void PlayDeathAnim(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion()) return;
        if (node is AnimatedSprite2D { SpriteFrames: not null } anim
            && anim.Name == "KnightDeathAnim"
            && anim.SpriteFrames.HasAnimation("Dead"))
        {
            anim.Play("Dead");
            return;
        }
        foreach (var child in node.GetChildren())
        {
            PlayDeathAnim(child);
        }
    }
}
