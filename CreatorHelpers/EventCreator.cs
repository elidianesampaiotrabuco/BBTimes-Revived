using BBTimes.CustomComponents;
using BBTimes.Manager;
using MTM101BaldAPI.ObjectCreation;
using PlusStudioLevelLoader;

namespace BBTimes.Helpers
{
	public static partial class CreatorExtensions
	{

		public static RandomEvent SetupEvent(this RandomEvent ev)
		{
			var data = ev.gameObject.GetComponent<IObjectPrefab>();
			if (data != null)
			{
				data.Name = ev.name;
				data.SetupPrefab();
				BasePlugin._cstData.Add(data);
			}

			LevelLoaderPlugin.Instance.randomEventAliases.Add("times_" + ev.name, ev);
			BBTimesManager.man.Add($"Event_{ev.name}", ev);
			return ev;
		}

		public static RandomEventBuilder<T> AddRequiredCharacters<T>(this RandomEventBuilder<T> r, params Character[] chars) where T : RandomEvent
		{
			for (int i = 0; i < chars.Length; i++)
				r.AddRequiredCharacter(chars[i]);
			return r;
		}


	}
}
