using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HollowKnight.HollowKnightCode.Resources;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// Salubra's whisper blesses the unwounded: at each turn start while the
/// vessel stands at FULL HP, gather SOUL (Salubra's Blessing charm — the
/// flawless-defense banking engine).
/// </summary>
public sealed class SalubrasBlessing : HollowKnightRelic
{
    /// <summary>SOUL gathered per turn while at full HP.</summary>
    public const int SoulPerFullTurn = 22;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner || base.Owner.Creature.CurrentHp < base.Owner.Creature.MaxHp)
        {
            return Task.CompletedTask;
        }
        Flash();
        Soul.Gain(base.Owner, SoulPerFullTurn);
        return Task.CompletedTask;
    }
}
