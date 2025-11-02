using System.Collections;
using BBTimes.Plugin;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBTimes.ModPatches.NpcPatches.March31Patches;

[HarmonyPatch(typeof(GottaSweep), nameof(GottaSweep.VirtualOnTriggerEnter))]
static class GottaSweepPatch
{
    static void Postfix(GottaSweep __instance, Collider other)
    { // Baldi will forget any other sound and only stay up to the current sound index
        if (!Storage.IsBaldiFirstReleaseDate) return; // Surprisingly, the forceTrigger is the only way to tell whether sweep is active or not

        if (other.isTrigger && other.GetComponent<Entity>())
            __instance.StartCoroutine(DelayedPlay(__instance));
    }

    static IEnumerator DelayedPlay(GottaSweep sweep)
    {
        yield return new WaitForSecondsNPCTimescale(sweep, Random.Range(0.05f, 0.25f));
        sweep.audMan.PlaySingle(sweep.audSweep);
    }
}