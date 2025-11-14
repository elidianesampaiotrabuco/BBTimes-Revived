using System.Collections.Generic;
using System.Reflection.Emit;
using BBTimes.Plugin;
using HarmonyLib;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch]
static class TimescalePatches
{
    const float timeScaleConstant = 0.65f, reverseTimeScaleConstant = 1f / timeScaleConstant;

    [HarmonyPatch(typeof(PropagatedAudioManager), "VirtualUpdate")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MakeTimescaleRight(IEnumerable<CodeInstruction> i) =>
        new CodeMatcher(i)
        .MatchForward(true, // num = EnvironmentController.PlayerTimeScale
            new(OpCodes.Ldarg_0),
            new(CodeInstruction.LoadField(typeof(PropagatedAudioManager), nameof(PropagatedAudioManager.environment))),
            new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(EnvironmentController), nameof(EnvironmentController.PlayerTimeScale))),
            new(OpCodes.Stloc_0)
        )
        .Advance(2) // Goes to IL_00AA: ldloc.0
        .InsertAndAdvance(
            // Reuses ldloc.0 instruction first
            Transpilers.EmitDelegate<System.Func<float, float>>((num) => Storage.IsBaldiFirstReleaseDate ? num * reverseTimeScaleConstant : num), // Normalizes num if march 31 is enabled
            new(OpCodes.Stloc_0),
            new(OpCodes.Ldloc_0) // Loads back num to maintain the if check that comes afterwards
        )
        .InstructionEnumeration();

    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.EnvironmentTimeScale), MethodType.Getter)]
    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.NpcTimeScale), MethodType.Getter)]
    [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.PlayerTimeScale), MethodType.Getter)]
    [HarmonyPostfix]
    static void ChangeTimeScale(ref float __result)
    {
        if (!Storage.IsBaldiFirstReleaseDate) return;
        __result *= timeScaleConstant;
    }
}