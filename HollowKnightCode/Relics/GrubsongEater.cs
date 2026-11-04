using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// A charm fused from the Grub song and the Soul Catcher's hunger: whenever
/// one of the Knight's cards is Exhausted, its fading echo gathers 6 SOUL.
/// The banking build's bridge into the consume line — Weaver's Bites, Deep
/// Focus and Soul Totem each deepen the well instead of emptying it.
/// </summary>
public sealed class GrubsongEater : HollowKnightRelic
{
    /// <summary>SOUL gathered whenever one of the Knight's cards is exhausted.</summary>
    public const int SoulPerExhaust = 6;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == base.Owner)
        {
            Flash();
            Soul.Gain(base.Owner, SoulPerExhaust);
        }
        return Task.CompletedTask;
    }
}
