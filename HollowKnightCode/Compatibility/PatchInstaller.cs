using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HollowKnight.HollowKnightCode.Compatibility;

/// <summary>Only presentation patches may opt out of a failed installation.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OptionalPatchAttribute(string feature) : Attribute
{
    public string Feature { get; } = feature;
}

public sealed record PatchInstallReport(IReadOnlyList<Type> Installed, IReadOnlyList<Type> Skipped);

/// <summary>
/// Install required hooks before cosmetic hooks. A separate Harmony owner per
/// class makes a failed multi-target class reversible without touching another
/// class or another mod. Unknown patch classes are required by default.
/// </summary>
public static class PatchInstaller
{
    private static readonly ConcurrentDictionary<string, IReadOnlySet<Type>> Installed = new();
    private static readonly ConcurrentDictionary<Type, byte> RuntimeWarnings = new();

    public static PatchInstallReport Install(Harmony harmony) => Install(harmony,
        typeof(MainFile).Assembly.GetTypes().Where(type => type.IsDefined(typeof(HarmonyPatch), false)),
        InstallCombatBridge, message => MainFile.Logger.Warn(message));

    public static bool IsInstalled(Type patchClass) => IsInstalled(MainFile.ModId, patchClass);

    public static bool IsInstalled(string owner, Type patchClass) =>
        Installed.TryGetValue(owner, out var classes) && classes.Contains(patchClass);

    public static string ClassOwner(string rootOwner, Type patchClass) => $"{rootOwner}.{patchClass.FullName}";

    /// <summary>
    /// The explicit overload is also used by the native Harmony regression
    /// checks: it exercises real class processors and real rollback, not mocks.
    /// A required compatibility bridge is part of the same installation.
    /// </summary>
    public static PatchInstallReport Install(Harmony harmony, IEnumerable<Type> patchClasses,
        Action<Harmony> requiredBridge, Action<string> warning)
    {
        var classes = patchClasses.Distinct().OrderBy(IsOptional)
            .ThenBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        var owners = new List<Harmony>();
        var installed = new List<Type>();
        var skipped = new List<Type>();
        string feature = "combat compatibility bridge";
        Installed.TryRemove(harmony.Id, out _);
        try
        {
            var bridge = new Harmony($"{harmony.Id}.compatibility");
            owners.Add(bridge);
            requiredBridge(bridge); // A failure here must reject initialization.
            foreach (var patchClass in classes)
            {
                feature = patchClass.FullName ?? patchClass.Name;
                var owner = new Harmony(ClassOwner(harmony.Id, patchClass));
                owners.Add(owner); // Include partially installed classes in rollback.
                try
                {
                    var methods = owner.CreateClassProcessor(patchClass).Patch();
                    if (methods.Count == 0)
                        throw new MissingMethodException($"{feature} resolved no patch targets.");
                    installed.Add(patchClass);
                }
                catch (Exception error) when (IsOptional(patchClass))
                {
                    // Cleanup is mandatory even for optional hooks. If it fails,
                    // the outer handler rejects initialization instead of claiming
                    // the optional feature was safely disabled.
                    RemoveOwner(owner);
                    owners.Remove(owner);
                    skipped.Add(patchClass);
                    var label = patchClass.GetCustomAttribute<OptionalPatchAttribute>()!.Feature;
                    warning($"Optional {label} disabled ({feature}): {error.GetBaseException().Message}");
                }
            }
            Installed[harmony.Id] = installed.ToHashSet();
            return new PatchInstallReport(installed.ToArray(), skipped.ToArray());
        }
        catch (Exception error)
        {
            Installed.TryRemove(harmony.Id, out _);
            var errors = new List<Exception> { error };
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                try { RemoveOwner(owners[i]); }
                catch (Exception rollback) { errors.Add(rollback); }
            }
            throw new InvalidOperationException(
                $"HollowKnight cannot initialize: required patch/compatibility step '{feature}' failed. "
                + "Initialization was rejected; the mod must not be used with incomplete gameplay hooks.",
                errors.Count == 1 ? error : new AggregateException(errors));
        }
    }

    private static bool IsOptional(Type type) => type.IsDefined(typeof(OptionalPatchAttribute), false);

    private static void RemoveOwner(Harmony owner)
    {
        // The typed owner filter preserves patches installed by other mods,
        // including another owner which deliberately uses the same patch method.
        foreach (var method in owner.GetPatchedMethods().ToArray())
            owner.Unpatch(method, HarmonyPatchType.All, owner.Id);
        if (Harmony.HasAnyPatches(owner.Id))
            throw new InvalidOperationException($"Failed to roll back Harmony owner {owner.Id}.");
    }

    private static void InstallCombatBridge(Harmony harmony)
    {
        Type[] stable = [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)];
        var betaHook = typeof(AbstractModel).GetMethod("ModifyDamageAdditive", [..stable, typeof(CardPlay)]);
        var stableHook = typeof(AbstractModel).GetMethod("ModifyDamageAdditive", stable);
        if ((betaHook ?? stableHook)?.ReturnType != typeof(decimal))
            throw new MissingMethodException("No supported ModifyDamageAdditive combat hook exists.");
        CombatApi.Install(harmony); // Also initializes the required damage/exhaust delegates.
        if (betaHook != null && !harmony.GetPatchedMethods().Contains(betaHook))
            throw new InvalidOperationException("The beta damage compatibility hook was not installed.");
    }

    /// <summary>Presentation failures are reported once and never break a card play.</summary>
    public static void WarnOptional(Type patchClass, Exception error)
    {
        if (!RuntimeWarnings.TryAdd(patchClass, 0)) return;
        try
        {
            MainFile.Logger.Warn($"Optional {patchClass.Name} skipped a presentation update: {error.GetBaseException().Message}");
        }
        catch
        {
            // A logger failure must not turn an already-contained UI failure
            // into a combat exception. No gameplay work occurs in this handler.
        }
    }
}
