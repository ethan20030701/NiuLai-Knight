using MegaCrit.Sts2.Core.Entities.Powers;

namespace HollowKnight.HollowKnightCode.Powers;

/// <summary>Only a living Grimmchild benefits. Its automatic flame is resolved by SwarmGuardPower.</summary>
public sealed class GrimmchildPower : HollowKnightPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
