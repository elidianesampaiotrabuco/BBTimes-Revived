using System.Collections.Generic;
using BBTimes.Plugin;
using MTM101BaldAPI.Registers;
using UnityEngine;

namespace BBTimes.CustomContent.Misc
{
	public class JoeChef : EnvironmentObject, IClickable<int>
	{
		public override void LoadingFinished()
		{
			base.LoadingFinished();
			positions = [transform.position - (transform.forward * 2f),
				transform.position + ((transform.right * 16f) - (transform.forward * 2f)),
			transform.position + ((-transform.right * 16f) - (transform.forward * 2f))];
			ogPosition = transform.position;
			target = ogPosition;

			Vector2 itemPosition = new(target.x, target.z);
			itemPosition.x += transform.forward.x * 10f;
			itemPosition.y += transform.forward.z * 10f;
			backupPickup = ec.CreateItem(ec.CellFromPosition(transform.position).room, Singleton<CoreGameManager>.Instance.NoneItem, itemPosition);
			ec.items.Remove(backupPickup); // Don't globalize this pickup
			backupPickup.gameObject.SetActive(false);
		}

		public void Clicked(int player)
		{
			if (workingOn) return;
			if (Singleton<CoreGameManager>.Instance.GetPoints(player) < price)
			{
				audMan.PlaySingle(audScream);
				return;
			}
			workingOn = true;
			Singleton<CoreGameManager>.Instance.AddPoints(-price, player, true);
			audMan.PlaySingle(audWelcome);
			kitchenAudMan.QueueAudio(audKitchen);
			kitchenAudMan.SetLoop(true);
			cooldown = Random.Range(10f, 25f);
		}

		public void ClickableUnsighted(int player) { }
		public void ClickableSighted(int player) { }
		public bool ClickableRequiresNormalHeight() => true;
		public bool ClickableHidden() => workingOn || (transform.position - target).magnitude > 3f;

		void Update()
		{
			if (workingOn)
			{
				cooldown -= ec.EnvironmentTimeScale * Time.deltaTime;
				if (cooldown <= 0f)
				{
					kitchenAudMan.FlushQueue(true);
					audMan.PlaySingle(audScream);
					workingOn = false;
					target = ogPosition;
					foodToGive = WeightedItemObject.RandomSelection([.. foods]);
					backupPickup.gameObject.SetActive(true);
					backupPickup.AssignItem(foodToGive);
				}
				else if ((transform.position - target).magnitude <= 3f)
					target = positions[Random.Range(0, positions.Length)];
			}

			transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, 0.45f);
		}

		Vector3[] positions;
		Vector3 ogPosition;
		Vector3 target;
		Vector3 _velocity;
		ItemObject foodToGive = null;
		bool workingOn = false;
		float cooldown = 0f;

		[SerializeField]
		internal PropagatedAudioManager audMan, kitchenAudMan;

		[SerializeField]
		internal SoundObject audWelcome, audScream, audKitchen;

		[SerializeField]
		internal int price = 25;

		readonly static List<WeightedItemObject> foods = [];
		Pickup backupPickup;

		public static void SearchForFoodToAdd()
		{
			// Get normal food
			foods.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, weight = 25 });
			foods.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(Items.DietBsoda).value, weight = 45 });
			foods.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(Items.Apple).value, weight = 1 });
			foods.Add(new() { selection = ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, weight = 75 });

			// Get modded food
			foreach (var itemMeta in ItemMetaStorage.Instance.All())
			{
				if (!itemMeta.tags.Contains(Storage.TAG_CHEFJOE_SELECTFOOD)) continue;
				foods.Add(new() { selection = itemMeta.value, weight = itemMeta.value.value });
			}
		}
	}
}
