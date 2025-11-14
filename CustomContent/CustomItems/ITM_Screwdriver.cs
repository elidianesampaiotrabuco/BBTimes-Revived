using BBTimes.CustomComponents;
using BBTimes.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.CustomItems
{
	public class ITM_Screwdriver : Item, IItemPrefab
	{
		public void SetupPrefab()
		{
			audScrew = this.GetSound("sd_screw.wav", "Vfx_SD_screw", SoundType.Effect, Color.white);
			item = ItmObj.itemType;
		}
		public void SetupPrefabPost() { }

		public string Name { get; set; }
		public string Category => "items";

		public ItemObject ItmObj { get; set; }


		public override bool Use(PlayerManager pm)
		{
			Destroy(gameObject);
			bool success = false;

			if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out var hit, pm.pc.reach))
			{
				var math = hit.transform.GetComponentInParent<MathMachine>();
				if (math && !math.IsCompleted)
				{
					math.Completed(pm.playerNumber, true);
					math.NumberDropped(pm.playerNumber);
					success = true;
				}
				else
				{
					var matchMachine = hit.transform.GetComponentInParent<MatchActivity>();
					if (matchMachine && !matchMachine.IsCompleted)
					{
						matchMachine.Completed(0, true);
						success = true;
					}
					else
					{
						var balloonPopper = hit.transform.GetComponentInParent<BalloonBuster>();
						if (balloonPopper && !balloonPopper.IsCompleted)
						{
							balloonPopper.unpoppedBalloons.Clear();
							for (int i = 0; i < balloonPopper.startingTotal; i++)
							{
								if (!balloonPopper.balloon[i].popped)
								{
									balloonPopper.balloon[i].Pop(false);
									balloonPopper.unpoppedBalloons.Add(balloonPopper.balloon[i]);
								}
							}
							balloonPopper.Completed(0, true);
							balloonPopper.audMan.PlaySingle(balloonPopper.audWin);
							Singleton<BaseGameManager>.Instance.PleaseBaldi(balloonPopper.baldiPause + balloonPopper.poppedBallonBaldiPauseRate * balloonPopper.poppedBalloons, false);
							Singleton<CoreGameManager>.Instance.AddPoints(balloonPopper.bonusMode ? balloonPopper.bonusPoints : balloonPopper.normalPoints, 0, true);
							Singleton<CoreGameManager>.Instance.GetPlayer(0).plm.AddStamina(Singleton<CoreGameManager>.Instance.GetPlayer(0).plm.staminaMax, true);
							success = true;
						}
						else
						{
							var lockdownDoor = hit.transform.GetComponentInParent<LockdownDoor>();
							if (lockdownDoor && !lockdownDoor.IsOpen && !lockdownDoor.moving)
							{
								lockdownDoor.Open(true, false);
								success = true;
							}
							else
							{
								var facultyOnlyDoor = hit.transform.GetComponentInParent<FacultyOnlyDoor>();
								if (facultyOnlyDoor && !facultyOnlyDoor.IsOpen)
								{
									facultyOnlyDoor.gameObject.AddComponent<FacultyDoorOpener>(); // Faculty Only Door killer lol
									success = true;
								}
								else
								{
									var machine = hit.transform.GetComponent<IItemAcceptor>();
									if (machine != null && machine.ItemFits(item))
									{
										machine.InsertItem(pm, pm.ec);
										success = true;
									}
								}
							}
						}
					}
				}

			}

			if (success)
			{
				Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audScrew);
			}

			return success;
		}

		[SerializeField]
		internal SoundObject audScrew;

		[SerializeField]
		internal Items item;
	}
}
