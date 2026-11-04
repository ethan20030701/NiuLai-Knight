using MegaCrit.Sts2.Core.Entities.Powers;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>Each phase adds 3 automatic flame damage, never command damage; capped at two.</summary>
public sealed class GrimmMaturityPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
