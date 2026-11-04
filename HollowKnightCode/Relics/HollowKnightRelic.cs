using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Extensions;

namespace HollowKnight.HollowKnightCode.Relics;

/// <summary>
/// This is the base class for your mod's relics, which is set up to load the relic's images from your mod's resources.
/// When creating a relic, right click the Relics folder and create a new file with the Custom Relic template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
///
/// The [Pool] annotation marks this relic as being tied to your specific character. Inheriting from this class means
/// that your relics don't need to invidually say which pool they should be in.
/// </summary>
[Pool(typeof(HollowKnightRelicPool))]
public abstract class HollowKnightRelic : CustomRelicModel, IKnightDamageModifier
{
    public override decimal ModifyDamageAdditive(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        decimal amount, MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
        => KnightDamageAdditive(target, amount, props, dealer, cardSource, CombatApi.CurrentPlay(cardSource));

    public virtual decimal KnightDamageAdditive(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        decimal amount, MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource,
        MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay) => 0m;

    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
}
