using System.Reflection;
using HarmonyLib;
using HollowKnight.HollowKnightCode.Character;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace HollowKnight.HollowKnightCode.Patches;

/// <summary>
/// The placeholder character inherits Ironclad attack/cast/death sounds.
/// Only consume our explicit marker, including direct death playback and
/// the parameterized overload. Unrelated character/audio events pass through.
/// </summary>
[HarmonyPatch]
[OptionalPatch("Knight inherited audio replacement")]
internal static class KnightInheritedAudioPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() => typeof(NAudioManager)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Where(method => method.Name == nameof(NAudioManager.PlayOneShot)
                         && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(string));

    [HarmonyPrefix]
    private static bool Prefix(string path) => path != KnightAudio.ProceduralCue;
}
