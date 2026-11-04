using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Cards;

/// <summary>
/// Draw a slow breath and let the excess go: exhaust every non-Attack card
/// in hand, each one hardening the shell (Second Wind verbatim). The
/// consume line's payoff — Deep Focus, Soul Totem and Weaver's Bites echo
/// into more block through Broken Shell and Grubsong Eater.
/// </summary>
public sealed class SecondWind : HollowKnightCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    public SecondWind() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var card in PileType.Hand.GetPile(base.Owner).Cards.Where(c => c.Type != CardType.Attack).ToList())
        {
            await CombatApi.Exhaust(choiceContext, card);
            await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
