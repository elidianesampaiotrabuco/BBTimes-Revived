using BBTimes.Plugin;
using HarmonyLib;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch(typeof(Principal), nameof(Principal.Initialize))]
static class PrincipalAllKnowing
{
    static void Prefix(Principal __instance) => // Principal always knows where you are
        __instance.allKnowing = Storage.IsBaldiFirstReleaseDate;
}