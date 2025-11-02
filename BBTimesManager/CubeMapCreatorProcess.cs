using System.IO;
using MTM101BaldAPI.AssetTools;
using PixelInternalAPI.Extensions;
using PlusStudioLevelLoader;
using UnityEngine;

namespace BBTimes.Manager
{
	internal static partial class BBTimesManager
	{
		static void CreateCubeMaps()
		{
			var F3Map = AssetLoader.CubemapFromFile(Path.Combine(MiscPath, TextureFolder, GetAssetName("cubemap_night.png")));
			LevelLoaderPlugin.Instance.skyboxAliases.Add("TimesNightSky", F3Map);

			var twilight = GenericExtensions.FindResourceObjectByName<Cubemap>("Cubemap_Twilight");

			// Add lightings outside for GameManagers
			foreach (var man in GenericExtensions.FindResourceObjects<SceneObject>())
			{
				//if (man.levelTitle == "F1") By default, it's the *default* cube map
				//{
				//	comp.mapForToday = ObjectCreationExtension.defaultCubemap;
				//	continue;
				//}
				if (man.levelTitle == F2 || man.levelTitle == F5)
				{
					man.skybox = twilight;
					continue;
				}
				if (man.levelTitle == F3 || man.levelTitle == F4)
				{
					man.skybox = F3Map;
					continue;
				}
			}
		}
	}
}
