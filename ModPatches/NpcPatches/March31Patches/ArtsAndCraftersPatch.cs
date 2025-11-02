using BBTimes.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch(typeof(ArtsAndCrafters_Stalking))]
static class ArtsAndCraftersHideNoShy
{
    [HarmonyPatch(nameof(NpcState.Update))]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    static void BaseUpdate(this NpcState instance) => throw new System.NotImplementedException("Stub");

    [HarmonyPatch(nameof(ArtsAndCrafters_Stalking.Update))]
    static bool Prefix(ArtsAndCrafters_Stalking __instance) // Arts and Crafters hides without hesitating
    {
        if (!Storage.IsBaldiFirstReleaseDate) return true;

        __instance.BaseUpdate();
        __instance.timeLeftToRespawn -= Time.deltaTime * __instance.npc.TimeScale;
        if (__instance.timeLeftToRespawn <= 0f)
        {
            __instance.crafters.state = new ArtsAndCrafters_Waiting(__instance.crafters);
            __instance.npc.behaviorStateMachine.ChangeState(__instance.crafters.state);
        }

        return false;
    }
}