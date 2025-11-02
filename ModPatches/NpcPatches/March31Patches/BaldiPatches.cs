using BBTimes.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch(typeof(Baldi), nameof(Baldi.UpdateSoundTarget))]
static class BaldiPatches
{
    static void Postfix(Baldi __instance)
    { // Baldi will forget any other sound and only stay up to the current sound index
        if (!Storage.IsBaldiFirstReleaseDate) return;

        for (int i = 0; i < __instance.currentSoundVal; i++) // Resets all the other sounds below the current one
        {
            __instance.soundLocations[i] = Vector3.zero;
            __instance.baldiInteractions[i] = null;
        }
    }
}