using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// Venom rides the nail's edge: every nail strike that draws blood applies
/// Poison to the struck foe (EnvenomPower's hook, gated to real nail hits
/// so spells never carry the toxin).
/// </summary>
public sealed class NailEnvenomPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (result.UnblockedDamage <= 0 || !NailRules.IsNailHit(dealer, base.Owner, props, cardSource))
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, base.Amount, base.Owner, null);
    }
}
