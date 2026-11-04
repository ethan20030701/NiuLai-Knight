using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Resources;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Pour every stored drop of SOUL back into the shell: one Block per two
/// SOUL currently held, then the vessel is empty (the banking line's
/// defensive eruption — Soul Unbound's shield-twin). One-shot: exhausts.
/// </summary>
public sealed class SoulAegis : HollowKnightCard
{
    public override bool GainsBlock => true;
    public static int BlockFromSoul(int soul) => Math.Max(soul, 0) / 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public SoulAegis() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!CustomResources<Soul>.TryGet(Owner.PlayerCombatState!, out var soul))
        {
            return;
        }
        var held = soul.Amount;
        if (held <= 0)
        {
            return;
        }
        await CreatureCmd.GainBlock(Owner.Creature, BlockFromSoul(held), ValueProp.Unpowered, cardPlay);
        Soul.SpendAll(Owner, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
