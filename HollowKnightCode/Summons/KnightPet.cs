using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using HollowKnight.HollowKnightCode.Extensions;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HollowKnight.HollowKnightCode.Summons;

/// <summary>A visible, damageable ally using the original game's companion sprite.</summary>
public abstract class KnightPet(int health, string portrait) : CustomPetModel(true)
{
    public override int MinInitialHp => health;
    public override int MaxInitialHp => health;
    public override bool ShouldShowInCompendium => false;

    public override NCreatureVisuals CreateCustomVisuals()
    {
        var texture = PreloadManager.Cache.GetTexture2D(portrait.ImagePath());
        return NodeFactory<NCreatureVisuals>.CreateFromResource(texture);
    }
}

/// <summary>Weaversong's spiderling: modest health, reliable chip damage.</summary>
public sealed class WeaverlingPet() : KnightPet(6, "pets/weaverling.png");

/// <summary>The Grimmchild, whose flame grows through repeated summons.</summary>
public sealed class GrimmkinPet() : KnightPet(6, "pets/grimmkin.png");

/// <summary>A Glowing Womb hatchling that explodes when it dies.</summary>
public sealed class HatchlingPet() : KnightPet(6, "pets/hatchling.png");
