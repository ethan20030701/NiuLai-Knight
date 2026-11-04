using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Compatibility;

public interface IKnightDamageModifier
{
    decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay);
}

/// <summary>Bind the few combat calls whose signatures differ in 0.107.1 and 0.111.0.</summary>
public static class CombatApi
{
    private delegate Task<IEnumerable<DamageResult>> StableDamage(PlayerChoiceContext context,
        IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? source);
    private delegate Task<IEnumerable<DamageResult>> BetaDamage(PlayerChoiceContext context,
        IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? source, CardPlay? play);
    private static readonly Func<AttackCommand, CardModel, AttackCommand>? StableFromCard;
    private static readonly Func<AttackCommand, CardModel, CardPlay?, AttackCommand>? BetaFromCard;
    private static readonly StableDamage? Damage107;
    private static readonly BetaDamage? Damage111;
    private static readonly Func<PlayerChoiceContext, CardModel, bool, bool, Task> ExhaustCard;
    private sealed class ActivePlay { public CardPlay? Play; }
    private static readonly ConditionalWeakTable<CardModel, ActivePlay> Plays = new();

    static CombatApi()
    {
        var from = typeof(AttackCommand).GetMethod("FromCard", [typeof(CardModel), typeof(CardPlay)]);
        if (from != null) BetaFromCard = from.CreateDelegate<Func<AttackCommand, CardModel, CardPlay?, AttackCommand>>();
        else StableFromCard = typeof(AttackCommand).GetMethod("FromCard", [typeof(CardModel)])!
            .CreateDelegate<Func<AttackCommand, CardModel, AttackCommand>>();

        Type[] damageArgs = [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel)];
        var damage = typeof(CreatureCmd).GetMethod("Damage", [..damageArgs, typeof(CardPlay)]);
        if (damage != null) Damage111 = damage.CreateDelegate<BetaDamage>();
        else Damage107 = typeof(CreatureCmd).GetMethod("Damage", damageArgs)!.CreateDelegate<StableDamage>();

        // Stable returns Task; beta returns Task<CardPileAddResult?>. Task is
        // the common return type (the beta result is not used by our cards).
        ExhaustCard = typeof(CardCmd).GetMethod("Exhaust", [typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool)])!
            .CreateDelegate<Func<PlayerChoiceContext, CardModel, bool, bool, Task>>();
    }

    public static void Begin(CardPlay play) => Plays.GetOrCreateValue(play.Card).Play = play;
    public static void End(CardPlay play)
    {
        if (Plays.TryGetValue(play.Card, out var slot) && ReferenceEquals(slot.Play, play)) slot.Play = null;
    }
    public static CardPlay? CurrentPlay(CardModel? card) => card != null && Plays.TryGetValue(card, out var slot) ? slot.Play : null;

    public static AttackCommand FromKnightCard(this AttackCommand attack, CardModel card, CardPlay play)
        => BetaFromCard != null ? BetaFromCard(attack, card, play) : StableFromCard!(attack, card);

    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource = null, CardPlay? cardPlay = null)
        => Damage(context, [target], amount, props, dealer, cardSource, cardPlay);

    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, Creature target,
        decimal amount, ValueProp props, CardModel cardSource, CardPlay? cardPlay)
        => Damage(context, [target], amount, props, cardSource.Owner.Creature, cardSource, cardPlay);

    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, IEnumerable<Creature> targets,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource = null, CardPlay? cardPlay = null)
        => Damage111 != null ? Damage111(context, targets, amount, props, dealer, cardSource, cardPlay)
            : Damage107!(context, targets, amount, props, dealer, cardSource);

    public static Task Exhaust(PlayerChoiceContext context, CardModel card)
        => ExhaustCard(context, card, false, false);

    public static void Install(Harmony harmony)
    {
        // The stable virtual hook has five arguments; beta added CardPlay.
        // On beta, the base implementation dispatches into our shared modifier.
        var hook = typeof(AbstractModel).GetMethod("ModifyDamageAdditive",
            [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)]);
        if (hook != null) harmony.Patch(hook, prefix: new HarmonyMethod(typeof(CombatApi), nameof(BetaDamageHook)));
    }

    private static bool BetaDamageHook(AbstractModel __instance, Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, ref decimal __result)
    {
        if (__instance is not IKnightDamageModifier modifier) return true;
        __result = modifier.KnightDamageAdditive(target, amount, props, dealer, cardSource, cardPlay);
        return false;
    }
}
