using System.Collections.Generic;
using BBTimes.CustomContent.RoomFunctions;
using BBTimes.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.Misc
{
	public class ChristmasBaldi : TileBasedObject, IClickable<int>
	{
		void Start()
		{
			var room = ec.CellFromPosition(position).room;
			Vector3 rightDirOffset = direction.PerpendicularList()[0].ToVector3();
			int rightPresentsOffset = Mathf.FloorToInt(presents / 2);

			Vector2 pos = new(transform.position.x, transform.position.z),
				frontOffset = new Vector2(direction.ToVector3().x, direction.ToVector3().z) * 3.5f,
				rightOffset = new Vector2(rightDirOffset.x * rightPresentsOffset, rightDirOffset.z * rightPresentsOffset) * 5f;

			for (int i = 0; i < presents; i++)
			{
				var pickup = ec.CreateItem(room, present, pos + frontOffset + (rightOffset * (rightPresentsOffset - i)));
				pickup.showDescription = true;
				pickup.free = false;

				pickup.price = price;

				generatedPickups.Add(pickup);
				pickup.OnItemPurchased += BuyPresent;
				pickup.OnItemDenied += DenyPresent;
				pickup.OnItemCollected += CollectPresent;
			}

			func = room.functionObject.GetComponent<SimulatedStoreRoomFunction>();
			if (!func)
			{
				func = room.functionObject.AddComponent<SimulatedStoreRoomFunction>();
				room.functions.AddFunction(func);
				func.Initialize(room);
			}

			func.OnPlayerExitStore += SayMerryChristmas;
		}

		public void SayMerryChristmas()
		{
			if (!merryChristmased)
			{
				merryChristmased = true;

				audMan.FlushQueue(true);
				audMan.QueueAudio(audBuyItem);
			}
		}

		void CollectPresent(Pickup p, int player)
		{
			p.free = true;
			p.price = 0;
			p.showDescription = false;
		}

		void BuyPresent(Pickup p, int player)
		{
			Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audBell);

			if (!audMan.QueuedAudioIsPlaying || audMan.IsPlayingClip(audIntro))
			{
				audMan.FlushQueue(true);
				audMan.QueueRandomAudio(audCollectingPresent);
			}
		}

		void DenyPresent(Pickup p, int player)
		{
			audMan.FlushQueue(true);

			if (!Singleton<CoreGameManager>.Instance.johnnyHelped && feelingGenerous && price - Singleton<CoreGameManager>.Instance.GetPoints(player) <= generousOffset)
			{
				feelingGenerous = false;
				audMan.QueueAudio(audGenerous);
				p.Collect(player);
				Singleton<CoreGameManager>.Instance.AddPoints(-Singleton<CoreGameManager>.Instance.GetPoints(player), player, true);
				Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audBell);
				Singleton<CoreGameManager>.Instance.johnnyHelped = true;
				return;
			}

			audMan.QueueAudio(audNoYtps);
		}

		public void Clicked(int player)
		{
			if (interactedWith) return;

			interactedWith = true;
			audMan.FlushQueue(true);
			audMan.QueueAudio(audIntro);
		}

		public bool ClickableRequiresNormalHeight() => false;
		public bool ClickableHidden() => interactedWith;
		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }


		[SerializeField]
		internal int presents = 3, price = 100, generousOffset = 50;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject audIntro, audNoYtps, audBuyItem, audGenerous, audBell;

		[SerializeField]
		internal SoundObject[] audCollectingPresent;

		[SerializeField]
		internal ItemObject present;

		SimulatedStoreRoomFunction func;
		readonly List<Pickup> generatedPickups = [];
		bool interactedWith = false, merryChristmased = false, feelingGenerous = true; // yes, I made this word up lol
	}
}
