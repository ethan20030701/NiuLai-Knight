using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Soul Tyrant. The spell-line build-defining engine: when the Knight plays
/// a Spell that KILLS an enemy, gain 1 Energy and draw 1 card
/// (max 2 triggers per turn). Inspect the killing DamageResult directly;
/// an earlier spell this turn must not credit later unrelated deaths.
/// </summary>
public sealed class SoulTyrantPower : HollowKnightPower
{
    private static readonly SpireField<SoulTyrantPower, int> TriggersThisTurn = new(() => 0);

    public const int MaxPerTurn = 2;
    public const decimal EnergyOnKill = 1m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner.Player)
        {
            TriggersThisTurn[this] = 0;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner
            || target.Side == base.Owner.Side
            || !result.WasTargetKilled
            || !NailRules.IsSpellCard(cardSource)
            || TriggersThisTurn[this] >= MaxPerTurn
            || base.Owner.Player is not { } player)
        {
            return;
        }
        TriggersThisTurn[this] += 1;
        Flash();
        await PlayerCmd.GainEnergy(EnergyOnKill, player);
        await CardPileCmd.Draw(choiceContext, 1, player);
    }
}
