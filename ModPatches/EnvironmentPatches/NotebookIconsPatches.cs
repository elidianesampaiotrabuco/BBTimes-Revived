using HarmonyLib;
using UnityEngine;

namespace BBTimes.ModPatches.EnvironmentPatches;

[HarmonyPatch]
internal static class NotebookIconsPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MathMachine), nameof(MathMachine.Start))]
    private static void MathMachineRightIcon(Notebook ___notebook) => ___notebook.icon.spriteRenderer.sprite = NotebookIcons[0];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MatchActivity), nameof(MatchActivity.Start))]
    static void MatchActivityRightIcon(Notebook ___notebook) => ___notebook.icon.spriteRenderer.sprite = NotebookIcons[1];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BalloonBuster), nameof(BalloonBuster.Start))]
    static void BalloonBusterRightIcon(Notebook ___notebook) => ___notebook.icon.spriteRenderer.sprite = NotebookIcons[2];
    public static Sprite[] NotebookIcons;
}