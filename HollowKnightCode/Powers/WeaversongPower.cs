using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Weaversong: each turn the weavers spin a Weaver's Bite into the hand,
/// and their song sharpens every bite's fangs (Amount = bonus bite damage).
/// Modeled on InfiniteBlades (turn-start token) + Accuracy (Shiv bonus).
/// </summary>
public sealed class WeaversongPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<WeaverBite>()];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == base.Owner.Player)
        {
            Flash();
            await WeaverBite.CreateInHand(player, combatState);
        }
    }

    public override decimal KnightDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        return dealer == Owner && props.IsPoweredAttack() && cardSource is WeaverBite ? Amount : 0m;
    }
}
