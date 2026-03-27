using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BBTimes.ModPatches // sorry if i stole code from bbpoop. denys, give credit to thumbly for the main menu patch
{
    [HarmonyPatch(typeof(MainMenu))]
    [HarmonyPatch("Start")]
    internal class MenuPatch
    {
        static void Postfix(MainMenu __instance)
        {
            __instance.gameObject.transform.Find("Copyright").GetComponent<TextMeshProUGUI>().text = "2023-2026 EkremTheStickman\nDeveloped by DenysCrasav4ik";
            __instance.gameObject.transform.Find("Version").GetComponent<TextMeshProUGUI>().text = "V1.4";

        }
    }
}