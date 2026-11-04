using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Relics;
using HollowKnight.HollowKnightCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Summons;

/// <summary>
/// One living pet of each species per player. Repeated summons add both current
/// and maximum HP, in the same manner as Osty's Summon command.
/// </summary>
public static class KnightSwarm
{
    public const int MaxCompanionHp = 24;

    public enum Species { Weaverling, Grimmchild, Hatchling }

    public static Species KindOf(Creature pet) => pet.Monster switch
    {
        WeaverlingPet => Species.Weaverling,
        GrimmkinPet => Species.Grimmchild,
        HatchlingPet => Species.Hatchling,
        _ => throw new ArgumentException("Creature is not a Knight companion", nameof(pet))
    };

    public static Creature? Find<T>(Player? player) where T : KnightPet =>
        player?.Creature.Pets.FirstOrDefault(pet => pet.IsAlive && pet.Monster is T);

    public static Creature[] Living(Player? player) => player?.Creature.Pets.Where(pet =>
        pet.IsAlive && pet.Monster is KnightPet).ToArray() ?? [];

    public static int LivingCount(Player? player) => Living(player).Length;
    public static Creature? Strongest(Player player) => Living(player)
        .OrderByDescending(pet => pet.CurrentHp).FirstOrDefault();
    public static Creature? Weakest(Player player) => Living(player)
        .OrderBy(pet => pet.CurrentHp).FirstOrDefault();
    public static SwarmGuardPower? State(Player? player) => player?.Creature.Powers
        .OfType<SwarmGuardPower>().FirstOrDefault();

    // Maturity belongs to Grimm's automatic flame. Applying it to every command
    // hit made the highest-HP Grimm strictly better than the weaver at combos.
    public static decimal DamageBonus(Player player, Creature pet) =>
        player.Creature.Powers.OfType<BroodInstinctPower>().FirstOrDefault()?.Amount ?? 0;

    public static decimal AutomaticDamageBonus(Player player, Creature pet) =>
        DamageBonus(player, pet)
        + 3m * (pet.Powers.OfType<GrimmMaturityPower>().FirstOrDefault()?.Amount ?? 0)
        + (pet.Monster is GrimmkinPet
            ? player.Creature.Powers.OfType<GrimmchildPower>().FirstOrDefault()?.Amount ?? 0
            : 0);

    public static (int CurrentHp, int MaxHp) CalculateGrowth(int currentHp, int maxHp, int amount)
    {
        if (amount <= 0) return (currentHp, maxHp);
        int nextMaxHp = (int)Math.Min(MaxCompanionHp, (long)maxHp + amount);
        int nextCurrentHp = (int)Math.Min(nextMaxHp, (long)currentHp + amount);
        return (nextCurrentHp, nextMaxHp);
    }

    /// <summary>Growth is separate from summoning: it does not mature Grimm or trigger summon rewards.</summary>
    public static async Task Grow(Creature pet, int amount)
    {
        if (!pet.IsAlive || amount <= 0) return;
        var growth = CalculateGrowth(pet.CurrentHp, pet.MaxHp, amount);
        await CreatureCmd.SetMaxHp(pet, growth.MaxHp);
        await CreatureCmd.SetCurrentHp(pet, growth.CurrentHp);
    }

    /// <summary>One successful directed strike counts once for the turn's command combo.</summary>
    public static async Task Strike(PlayerChoiceContext choiceContext, Player player, Creature pet,
        Creature target, decimal damage, CardPlay? cardPlay = null, bool command = true)
    {
        if (!pet.IsAlive || !target.IsAlive) return;
        if (command && State(player) is { } state) state.CommandsThisTurn++;
        var bonus = command ? DamageBonus(player, pet) : AutomaticDamageBonus(player, pet);
        await CombatApi.Damage(choiceContext, target, damage + bonus,
            ValueProp.Move, pet, cardPlay?.Card, cardPlay);
    }

    private static void RefreshGrimmArt(Creature pet)
    {
        var phase = pet.Powers.OfType<GrimmMaturityPower>().FirstOrDefault()?.Amount ?? 0;
        var portrait = phase switch
        {
            1 => "pets/grimmkin_phase3.png",
            >= 2 => "pets/grimmkin_phase4.png",
            _ => "pets/grimmkin.png"
        };
        if (pet.GetCreatureNode()?.Visuals?.GetNodeOrNull<Sprite2D>("%Visuals") is { } body)
            body.Texture = PreloadManager.Cache.GetTexture2D(portrait.ImagePath());
    }

    public static Task<Creature> SummonKind(PlayerChoiceContext choiceContext, Player player,
        Species species, int health) => species switch
    {
        Species.Weaverling => Summon<WeaverlingPet>(choiceContext, player, health),
        Species.Grimmchild => Summon<GrimmkinPet>(choiceContext, player, health),
        Species.Hatchling => Summon<HatchlingPet>(choiceContext, player, health),
        _ => throw new ArgumentOutOfRangeException(nameof(species))
    };

    public static async Task<Creature> Summon<T>(PlayerChoiceContext choiceContext, Player player, int health)
        where T : KnightPet
    {
        var pet = Find<T>(player);
        var bonus = player.Relics.Any(relic => relic is BroodCharm) ? BroodCharm.BonusHp : 0;
        var amount = health + bonus;
        if (pet == null)
        {
            pet = await PlayerCmd.AddPet<T>(player);
            await CreatureCmd.SetMaxAndCurrentHp(pet, Math.Clamp(amount, 1, MaxCompanionHp));
        }
        else
        {
            await Grow(pet, amount);
            if (pet.Monster is GrimmkinPet
                && (pet.Powers.OfType<GrimmMaturityPower>().FirstOrDefault()?.Amount ?? 0) < 2)
                await PowerCmd.Apply<GrimmMaturityPower>(choiceContext, pet, 1m, player.Creature, null);
        }

        if (!player.Creature.Powers.Any(power => power is SwarmGuardPower))
        {
            await PowerCmd.Apply<SwarmGuardPower>(choiceContext, player.Creature,
                1m, player.Creature, null);
        }
        if (State(player) is { } state && state.LastFallen == KindOf(pet)) state.LastFallen = null;
        if (pet.Monster is GrimmkinPet) RefreshGrimmArt(pet);
        if (player.Creature.Powers.OfType<BroodInstinctPower>().FirstOrDefault() is { } instinct)
            await instinct.AfterSwarmSummoned(choiceContext);
        return pet;
    }
}
