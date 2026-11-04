using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The vessel's innate ability to gather SOUL, manifested as a charm.
/// Every Nail strike (an Attack card that costs no SOUL) gathers SOUL per hit —
/// this is the single source of SOUL generation for the character.
/// SOUL-costing spells deal damage but never return SOUL.
/// </summary>
public sealed class SoulCatcher : HollowKnightRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeCombatStart()
    {
        // silent infra: powers the SharpAssault combo counter (always live)
        await PowerCmd.Apply<HitCounterPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1m, base.Owner.Creature, null, silent: true);
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (result.TotalDamage > 0 && NailRules.IsNailHit(dealer, base.Owner.Creature, props, cardSource))
        {
            Flash();
            Soul.Gain(base.Owner, Soul.PerNailHit);
        }
        return Task.CompletedTask;
    }
}
