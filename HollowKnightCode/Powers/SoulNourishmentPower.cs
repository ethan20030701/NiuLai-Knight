using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using HollowKnight.HollowKnightCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>Actual SOUL spending feeds all living pets; a partial payment carries over.</summary>
public sealed class SoulNourishmentPower : HollowKnightPower
{
    private int _spent;
    private int _weaverSoulHits;
    public const int WeaverSoulPerHit = 3;
    public const int WeaverSoulHitsPerTurn = 3;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) _weaverSoulHits = 0;
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // An explicit, capped charm synergy; ordinary companion hits still do
        // not trigger Soul Catcher, Soul Eater, or any nail-strength effects.
        if (Owner.Player is { } player && dealer?.PetOwner == player
            && dealer.Monster is WeaverlingPet && target.Side != Owner.Side
            && cardSource is SwarmCommandCard && cardSource.Owner == player
            && props.IsPoweredAttack() && result.TotalDamage > 0
            && _weaverSoulHits < WeaverSoulHitsPerTurn)
        {
            _weaverSoulHits++;
            Flash();
            Soul.Gain(player, WeaverSoulPerHit);
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || Owner.Player is not { } player) return;
        _spent += SoulSpendTracker.AmountSpent(cardPlay);
        var payments = _spent / Soul.SpellCost;
        _spent %= Soul.SpellCost;
        if (payments == 0) return;
        Flash();
        foreach (var pet in KnightSwarm.Living(player))
            await KnightSwarm.Grow(pet, payments * Amount);
    }
}
