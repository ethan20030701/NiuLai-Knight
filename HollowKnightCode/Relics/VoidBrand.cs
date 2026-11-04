using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// The Void's mark, seared before battle: the vessel opens each combat
/// already stronger (Vajra's slot, in Void imagery).
/// </summary>
public sealed class VoidBrand : HollowKnightRelic
{
    /// <summary>Strength granted at combat start.</summary>
    public const int OpeningStrength = 2;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
            base.Owner.Creature, OpeningStrength, base.Owner.Creature, null);
    }
}
