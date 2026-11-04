using BaseLib.Abstracts;
using BaseLib.Extensions;
using HollowKnight.HollowKnightCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>
/// This is the base class for your mod's powers, which is set up to load the power's images from your mod's resources.
/// When creating a power, right click the Powers folder and create a new file with the Custom Power template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
public abstract class HollowKnightPower : CustomPowerModel, IKnightDamageModifier
{
    public override decimal ModifyDamageAdditive(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        decimal amount, MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
        => KnightDamageAdditive(target, amount, props, dealer, cardSource, CombatApi.CurrentPlay(cardSource));

    public virtual decimal KnightDamageAdditive(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        decimal amount, MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource,
        MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay) => 0m;

    //Loads from HollowKnight/images/powers/your_power.png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    /// <summary>
    /// Whether this power is a buff or debuff.
    /// </summary>
    public abstract override PowerType Type { get; }
    
    /// <summary>
    /// How this power stacks if reapplied. Counter is the most common type, where applying the power again just
    /// adds to the amount. Single means the power does not stack, like Barricade. None functions identically to
    /// Single, but you're suggested to use Single as it is more explicit about how it will work.
    /// </summary>
    public abstract override PowerStackType StackType { get; }
}
