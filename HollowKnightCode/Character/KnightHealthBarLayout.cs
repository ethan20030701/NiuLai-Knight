using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

namespace HollowKnight.HollowKnightCode.Character;

/// <summary>Keep the compact Knight's selection bounds independent of player health-bar width.</summary>
[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.UpdateLayoutForCreatureBounds))]
[OptionalPatch("Knight health bar layout")]
public static class KnightHealthBarLayout
{
    public const float PlayerBarWidth = 240f;
    private static readonly ConditionalWeakTable<NHealthBar, Control> LayoutBounds = new();

    [HarmonyPrefix]
    private static void MatchPlayerWidth(NHealthBar __instance, Creature ____creature, ref Control bounds)
    {
        try
        {
            if (____creature?.Player?.Character is not Knight || !GodotObject.IsInstanceValid(bounds)
                || !bounds.IsInsideTree() || !GodotObject.IsInstanceValid(__instance)) return;
            var source = bounds;
            var parent = source.GetParent();
            if (parent == null || parent.IsQueuedForDeletion()) return;
            if (LayoutBounds.TryGetValue(__instance, out var old) && !GodotObject.IsInstanceValid(old))
                LayoutBounds.Remove(__instance);
            var layout = LayoutBounds.GetValue(__instance, _ =>
            {
                var created = new Control { Name = "KnightHealthBarLayoutBounds", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
                try { parent.AddChild(created); }
                catch { created.Free(); throw; }
                __instance.TreeExiting += () =>
                {
                    if (GodotObject.IsInstanceValid(created)) created.QueueFree();
                };
                return created;
            });
            if (layout.GetParent() != parent) layout.Reparent(parent);
            layout.Size = new Vector2(PlayerBarWidth, source.Size.Y);
            layout.GlobalPosition = source.GlobalPosition + new Vector2((source.Size.X - PlayerBarWidth) * 0.5f, 0f);
            bounds = layout; // Leave the original argument unchanged until layout succeeds.
        }
        catch (Exception error)
        {
            PatchInstaller.WarnOptional(typeof(KnightHealthBarLayout), error);
        }
    }
}
