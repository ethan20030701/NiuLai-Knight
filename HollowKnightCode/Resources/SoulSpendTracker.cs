using System.Threading;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Resources;

public static class SoulSpendTracker
{
    internal sealed record Payment(PlayerCombatState? Combat, int Amount, int? BeforeAmount);

    public sealed class PlayScope
    {
        internal CardModel Card { get; }
        internal Payment? Payment { get; set; }
        internal PlayScope? Parent { get; }

        internal PlayScope(CardModel card, Payment? payment, PlayScope? parent)
        {
            Card = card;
            Payment = payment;
            Parent = parent;
        }
    }

    private static readonly AsyncLocal<PlayScope?> CurrentScope = new();

    private static readonly SpireField<CardModel, Payment?> PendingPayment = new(() => null);

    private static readonly SpireField<CardPlay, bool> Bound = new(() => false);

    private static readonly SpireField<CardPlay, int> AutomaticSpend = new(() => 0);

    private static readonly SpireField<CardPlay, int?> BeforePayment = new(() => null);

    private static readonly SpireField<CardPlay, int> ManualSpend = new SpireField<CardPlay, int>(() => 0);

    public static void QueuePayment(CardModel card, int amount, int? beforeAmount = null)
    {
        PendingPayment[card] = new Payment(card.Owner?.PlayerCombatState, amount, beforeAmount);
    }

    public static PlayScope BeginScope(CardModel card)
    {
        PlayScope scope = new(card, PendingPayment[card], CurrentScope.Value);
        PendingPayment[card] = null;
        CurrentScope.Value = scope;
        return scope;
    }

    public static void RestoreCallerScope(PlayScope scope)
    {
        if (CurrentScope.Value == scope)
        {
            CurrentScope.Value = scope.Parent;
        }
    }

    public static void FinishScope(PlayScope scope)
    {
        scope.Payment = null;
    }

    public static void Bind(CardPlay cardPlay)
    {
        if (Bound[cardPlay])
        {
            return;
        }
        Bound[cardPlay] = true;
        // A series pays once. Replays and free auto-plays must not reuse BaseLib's LastSpend.
        if (!cardPlay.IsFirstInSeries)
        {
            return;
        }
        Payment? payment;
        PlayScope? scope = CurrentScope.Value;
        if (scope?.Card == cardPlay.Card)
        {
            payment = scope.Payment;
            scope.Payment = null;
        }
        else
        {
            payment = PendingPayment[cardPlay.Card];
            PendingPayment[cardPlay.Card] = null;
        }
        if (payment != null && payment.Combat == cardPlay.Card.Owner?.PlayerCombatState)
        {
            AutomaticSpend[cardPlay] = payment.Amount;
            BeforePayment[cardPlay] = payment.BeforeAmount;
        }
    }

    public static void ClearPending(CardModel card)
    {
        PendingPayment[card] = null;
    }

    public static int AutomaticAmountSpent(CardPlay cardPlay)
    {
        Bind(cardPlay);
        return AutomaticSpend[cardPlay];
    }

    public static int? AmountBeforePayment(CardPlay cardPlay)
    {
        Bind(cardPlay);
        return BeforePayment[cardPlay];
    }

    public static void RecordManual(CardPlay cardPlay, int amount)
    {
        if (amount > 0)
        {
            ManualSpend[cardPlay] += amount;
        }
    }

    public static int AmountSpent(CardPlay cardPlay)
    {
        return AutomaticAmountSpent(cardPlay) + ManualSpend[cardPlay];
    }
}
