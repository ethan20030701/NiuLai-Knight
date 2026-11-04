using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HollowKnight.HollowKnightCode.Compatibility;

/// <summary>Runs real Harmony processors against isolated, non-inlined managed methods.</summary>
public static class PatchInstallerChecks
{
    public static IReadOnlyList<string> Run()
    {
        var passed = new List<string>();
        OptionalFailureRollsBackOnlyItsOwner();
        passed.Add("optional class failure rolls back all its targets and preserves required/foreign patches");
        RequiredFailureRejectsAndRollsBack();
        passed.Add("required class failure rejects loading and rolls back earlier classes plus compatibility bridge");
        BridgeFailureRejectsAndRollsBack();
        passed.Add("required bridge failure rejects loading and rolls back partial bridge patches");
        UnmarkedMissingTargetIsRequired();
        passed.Add("an unclassified missing-target patch fails closed by default");
        return passed;
    }

    private static void OptionalFailureRollsBackOnlyItsOwner()
    {
        string id = $"HK.patch-check.optional.{Guid.NewGuid():N}";
        var harmony = new Harmony(id);
        var foreign = new Harmony(id + ".foreign");
        var warnings = new List<string>();
        OptionalPartialObserved = false;
        try
        {
            // Deliberately share the *same patch method* across owners. Cleanup
            // must filter by owner instead of unpatching that method globally.
            foreign.Patch(Method(nameof(Targets.OptionalA)), prefix:
                new HarmonyMethod(typeof(OptionalPartial), nameof(OptionalPartial.Prefix)));
            var report = PatchInstaller.Install(harmony,
                [typeof(RequiredGood), typeof(OptionalPartial), typeof(OptionalMissing)], _ => { }, warnings.Add);
            Require(OptionalPartialObserved, "The injected failure did not happen after real patch installation.");
            Require(report.Installed.SequenceEqual([typeof(RequiredGood)]), "Required registration was lost.");
            Require(report.Skipped.Count == 2 && warnings.Count == 2, "Optional failures were not reported.");
            Require(Targets.Required() == 10, "A failed optional class removed a required class.");
            Require(Targets.OptionalA() == 100 && Targets.OptionalB() == 2,
                "Partial optional hooks leaked, or another owner's identical prefix was removed.");
            Require(!Harmony.HasAnyPatches(PatchInstaller.ClassOwner(id, typeof(OptionalPartial))),
                "Failed optional class still owns Harmony patches.");
            Require(PatchInstaller.IsInstalled(id, typeof(RequiredGood))
                && !PatchInstaller.IsInstalled(id, typeof(OptionalPartial)), "Installed status is incorrect.");
        }
        finally { Cleanup(id); }
    }

    private static void RequiredFailureRejectsAndRollsBack()
    {
        string id = $"HK.patch-check.required.{Guid.NewGuid():N}";
        try
        {
            bool rejected = false;
            try
            {
                PatchInstaller.Install(new Harmony(id), [typeof(RequiredGood), typeof(ZRequiredFailure)],
                    PatchBridge, _ => throw new Exception("A required failure must not be downgraded to a warning."));
            }
            catch (InvalidOperationException error)
            {
                rejected = error.Message.Contains(nameof(ZRequiredFailure), StringComparison.Ordinal);
            }
            Require(rejected, "The required failure was hidden or omitted its failing class.");
            Require(Targets.Required() == 1 && Targets.RequiredFailure() == 4 && Targets.Bridge() == 3,
                "Failed required registration left gameplay/compatibility patches active.");
            Require(!PatchInstaller.IsInstalled(id, typeof(RequiredGood)), "Failed initialization reports itself installed.");
        }
        finally { Cleanup(id); }
    }

    private static void BridgeFailureRejectsAndRollsBack()
    {
        string id = $"HK.patch-check.bridge.{Guid.NewGuid():N}";
        try
        {
            bool rejected = false;
            try
            {
                PatchInstaller.Install(new Harmony(id), [typeof(RequiredGood)], owner =>
                {
                    PatchBridge(owner);
                    throw new MissingMethodException("Unsupported combat signature.");
                }, _ => { });
            }
            catch (InvalidOperationException error)
            {
                rejected = error.Message.Contains("combat compatibility bridge", StringComparison.Ordinal);
            }
            Require(rejected && Targets.Bridge() == 3 && Targets.Required() == 1,
                "Partial bridge installation was not rejected and rolled back.");
        }
        finally { Cleanup(id); }
    }

    private static void UnmarkedMissingTargetIsRequired()
    {
        string id = $"HK.patch-check.unknown.{Guid.NewGuid():N}";
        try
        {
            bool rejected = false;
            try { PatchInstaller.Install(new Harmony(id), [typeof(UnmarkedMissing)], _ => { }, _ => { }); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "A newly added core/save patch was treated as optional without an explicit annotation.");
        }
        finally { Cleanup(id); }
    }

    private static void PatchBridge(Harmony owner) => owner.Patch(Method(nameof(Targets.Bridge)),
        prefix: new HarmonyMethod(typeof(PatchInstallerChecks), nameof(BridgePrefix)));

    private static bool BridgePrefix(ref int __result) { __result = 30; return false; }
    private static MethodInfo Method(string name) => AccessTools.Method(typeof(Targets), name);
    private static void Require(bool condition, string error) { if (!condition) throw new Exception(error); }

    private static void Cleanup(string root)
    {
        var harmony = new Harmony(root);
        foreach (var method in Harmony.GetAllPatchedMethods().ToArray())
            foreach (var owner in Harmony.GetPatchInfo(method).Owners
                         .Where(owner => owner.StartsWith(root + ".", StringComparison.Ordinal)).ToArray())
                harmony.Unpatch(method, HarmonyPatchType.All, owner);
    }

    private static bool OptionalPartialObserved;

    private static class Targets
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int Required() => 1;
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int OptionalA() => 1;
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int OptionalB() => 2;
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int RequiredFailure() => 4;
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int Bridge() => 3;
    }

    [HarmonyPatch(typeof(Targets), nameof(Targets.Required))]
    private static class RequiredGood
    {
        [HarmonyPrefix]
        private static bool Prefix(ref int __result) { __result = 10; return false; }
    }

    [HarmonyPatch]
    [OptionalPatch("injected optional multi-target feature")]
    private static class OptionalPartial
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetsToPatch() =>
            [Method(nameof(Targets.OptionalA)), Method(nameof(Targets.OptionalB))];
        [HarmonyPrefix]
        public static bool Prefix(ref int __result) { __result = 100; return false; }
        [HarmonyCleanup]
        private static void Cleanup(MethodBase? original)
        {
            // Harmony invokes cleanup after *each* target, then once more for
            // the class. Fail only after the second target has been installed;
            // throwing after OptionalA would never exercise two-target rollback.
            if (original?.Name != nameof(Targets.OptionalB)) return;
            OptionalPartialObserved = Targets.OptionalA() == 100 && Targets.OptionalB() == 100;
            throw new InvalidOperationException("Injected failure after applying both class targets.");
        }
    }

    [HarmonyPatch(typeof(Targets), "MissingMethod")]
    [OptionalPatch("injected missing optional target")]
    private static class OptionalMissing
    {
        [HarmonyPrefix] private static void Prefix() { }
    }

    [HarmonyPatch(typeof(Targets), nameof(Targets.RequiredFailure))]
    private static class ZRequiredFailure
    {
        [HarmonyPrefix]
        private static bool Prefix(ref int __result) { __result = 40; return false; }
        [HarmonyCleanup]
        private static void Cleanup() => throw new InvalidOperationException("Injected required failure after installation.");
    }

    [HarmonyPatch(typeof(Targets), "MissingMethod")]
    private static class UnmarkedMissing
    {
        [HarmonyPrefix] private static void Prefix() { }
    }
}
