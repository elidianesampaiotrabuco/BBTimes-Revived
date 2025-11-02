using HarmonyLib;

namespace BBTimes.ModPatches.GeneratorPatches
{
	[HarmonyPatch(typeof(LevelBuilder), nameof(LevelBuilder.Start))]
	public static class LevelBuilderInstanceGrabber
	{
		private static void Prefix(LevelBuilder __instance) =>
			i = __instance;

		internal static LevelBuilder i;
	}
}
