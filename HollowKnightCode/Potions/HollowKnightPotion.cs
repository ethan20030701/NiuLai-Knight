using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HollowKnight.HollowKnightCode.Character;
using HollowKnight.HollowKnightCode.Extensions;

namespace HollowKnight.HollowKnightCode.Potions;

[Pool(typeof(HollowKnightPotionPool))]
public abstract class HollowKnightPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}