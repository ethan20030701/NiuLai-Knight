using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Extensions;
using Godot;

namespace HollowKnight.HollowKnightCode.Character;

public class HollowKnightRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => HollowKnight.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}