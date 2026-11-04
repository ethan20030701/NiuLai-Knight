using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// The cloak's namesake dash: harden the shell mid-flight and keep a card in
/// hand (Backflip's slot, tuned to common). Cheap defense that cycles.
/// </summary>
public sealed class MothwingDash() : HollowKnightCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new CardsVar(1),
        new DynamicVar("SoulThreshold", 66m),
        new CardsVar("BonusCards", 1)
    ];

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Snapshot before block/other listeners run. Banking SOUL adds one
        // card but spends no SOUL; the threshold never scales with upgrades.
        var banked = CustomResources<Soul>.TryGet(Owner.PlayerCombatState, out var soul)
                     && soul.Amount >= DynamicVars["SoulThreshold"].IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext,
            DynamicVars.Cards.IntValue + (banked ? DynamicVars["BonusCards"].IntValue : 0), Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
