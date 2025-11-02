using BBTimes.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch]
static class PlaytimeAndJumpropePatches // Basically Playtime won't say the last jump line and will instantly end the minigame after it is over
{
    // [HarmonyPatch(typeof(Playtime), nameof(Playtime.Count))]
    // [HarmonyPrefix]
    // static bool ShouldCountFurther(Playtime __instance)
    // {
    //     if (!Storage.IsBaldiFirstReleaseDate) return true;
    //     return __instance.currentJumprope.jumps < __instance.currentJumprope.maxJumps; // Do not count if max is reached
    // }

    [HarmonyPatch(typeof(Jumprope), nameof(Jumprope.RopeDown))]
    [HarmonyPrefix]
    static bool AntecipateHeightChange(Jumprope __instance)
    {
        if (!Storage.IsBaldiFirstReleaseDate) return true;
        if (__instance.height > __instance.jumpBuffer && __instance.jumps + 1 >= __instance.maxJumps)
        {
            __instance.jumps++;
            __instance.End(true);
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Playtime_Playing), nameof(Playtime_Playing.PlayerLost))]
    [HarmonyPrefix]
    static bool IgnorePlayerLostTrigger() => !Storage.IsBaldiFirstReleaseDate;

    [HarmonyPatch(typeof(Playtime), nameof(Playtime.VirtualUpdate))]
    [HarmonyPostfix]
    static void MakePlaytimeSadByAnyTrigger(Playtime __instance)
    {
        if (!Storage.IsBaldiFirstReleaseDate) return;

        var jprope = __instance.currentJumprope;
        if (!jprope) return;

        if (jprope.player.plm.Entity.InteractionDisabled || jprope.player.Am.moveMods.Exists(mod => mod.forceTrigger && mod.movementAddend != Vector3.zero))
            jprope.End(false);
    }
}
