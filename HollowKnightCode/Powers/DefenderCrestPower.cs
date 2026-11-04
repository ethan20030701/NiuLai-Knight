using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
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
/// The Defender's Crest miasma: at the end of its owner's turn, every living
/// enemy is seeped with Poison. Grimmchild's turn-end hook pattern, spitting
/// venom instead of embers.
/// </summary>
public sealed class DefenderCrestPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner))
        {
            return;
        }
        var enemies = base.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
        {
            return;
        }
        Flash();
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, base.Amount, base.Owner, null);
        }
    }
}
