using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A vessel distilled to hold far more than its shell suggests. Hoard SOUL
/// through your turn and the vessel hardens around you.
/// </summary>
public sealed class SoulVessel : HollowKnightRelic
{
    /// <summary>SOUL threshold that hardens the vessel (two spells' worth).</summary>
    public const int Threshold = Soul.SpellCost * 2;

    /// <summary>Block gained at end of turn while above the threshold.</summary>
    public const int BlockOnHold = 8;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner.Creature)
            || !CustomResources<Soul>.TryGet(base.Owner.PlayerCombatState, out var soul)
            || soul.Amount < Threshold)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(base.Owner.Creature, BlockOnHold, ValueProp.Unpowered, null);
    }
}
