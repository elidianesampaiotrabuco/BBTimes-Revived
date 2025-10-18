
using System.Collections.Generic;
using BBTimes.CustomComponents;
using BBTimes.Extensions;
using HarmonyLib;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Registers;
using PixelInternalAPI.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.NPCs
{
	public class Faker : NPC, INPCPrefab
	{
		public void SetupPrefab()
		{
			forms = this.GetSpriteSheet(3, 1, 31f, "faker.png");
			spriteRenderer[0].sprite = forms[0];
			audMan = GetComponent<PropagatedAudioManager>();
			renderer = spriteRenderer[0];
		}
		public void SetupPrefabPost()
		{
			List<SoundObject> sds = [];
			foreach (var npc in NPCMetaStorage.Instance.All()) // get literally every sound from npcs registered in the meta storage
			{
				foreach (var pre in npc.prefabs)
				{
					if (pre.Value is Faker)
						continue;


					foreach (var field in AccessTools.GetDeclaredFields(pre.Value.GetType()))
					{
						if (field.FieldType == typeof(SoundObject))
						{
							var obj = (SoundObject)field.GetValue(pre.Value);
							if (obj != null && !sds.Contains(obj))
								sds.Add(obj);
						}
						else if (field.FieldType == typeof(SoundObject[]))
						{
							var obj = (SoundObject[])field.GetValue(pre.Value);
							if (obj != null)
							{
								for (int i = 0; i < obj.Length; i++)
									if (obj[i] != null && !sds.Contains(obj[i]))
										sds.Add(obj[i]);
							}
						}
					}
				}

			}
			soundsToEmit = [.. sds];
		}
		public string Name { get; set; }
		public string Category => "npcs";

		public NPC Npc { get; set; }
		[SerializeField] Character[] replacementNPCs; public Character[] GetReplacementNPCs() => replacementNPCs; public void SetReplacementNPCs(params Character[] chars) => replacementNPCs = chars;
		public int ReplacementWeight { get; set; }

		// ---------------------------------------------------------------

		public override void Initialize()
		{
			base.Initialize();
			ChangeRandomState();
		}

		public override void VirtualUpdate()
		{
			base.VirtualUpdate();
			if (!audMan.QueuedAudioIsPlaying && IsActive)
				PlayRandomAudio();

		}

		public void PlayRandomAudio()
		{
			audMan.FlushQueue(true);
			audMan.pitchModifier = Random.Range(minPitchChange, maxPitchChange);
			audMan.QueueRandomAudio(soundsToEmit);
		}

		public void ChangeRandomState()
		{
			int rng = Random.Range(0, variantTypes.Length);
			behaviorStateMachine.ChangeState(System.Activator.CreateInstance(variantTypes[rng], [this, rng]) as Faker_ActiveState);
		}

		public void ApplyScale(bool add)
		{
			if (add)
				ec.AddTimeScale(mod);
			else
				ec.RemoveTimeScale(mod);
		}


		internal bool IsActive { get; set; } = false;

		[SerializeField]
		internal SoundObject[] soundsToEmit;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal Sprite[] forms;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal float minPitchChange = 0.35f, maxPitchChange = 1.25f, despawnCooldown = 60f, spawnCooldown = 5f, walkSpeed = 25f, despawnHeight = -15f,
		blueVariant_FovModifier = -25f, redVariant_rotationSmoothness = 0.45f;

		[SerializeField]
		internal System.Type[] variantTypes = [typeof(Faker_GreenVariant_Idle), typeof(Faker_RedVariant), typeof(Faker_BlueVariant)];

		readonly TimeScaleModifier mod = new(0.25f, 1f, 1f);
	}

	internal class Faker_StateBase(Faker f) : NpcState(f)
	{
		protected Faker f = f;
	}

	internal class Faker_ActiveState(Faker f, int form) : Faker_StateBase(f)
	{
		protected int formIdx = form;
		float despawnCooldown = f.despawnCooldown;

		protected bool CanDespawn
		{
			get => _canDespawn;
			set
			{
				if (value)
				{
					_despawns++;
					_canDespawn = true;
					return;
				}
				_despawns--;
				if (_despawns <= 0)
				{
					_despawns = 0;
					_canDespawn = false;
				}
			}
		}
		bool _canDespawn = true;
		int _despawns = 0;

		public override void Enter()
		{
			base.Enter();
			f.renderer.sprite = f.forms[formIdx];
		}

		public override void Update()
		{
			base.Update();
			if (!_canDespawn) return;

			despawnCooldown -= f.TimeScale * Time.deltaTime;
			if (despawnCooldown <= 0f)
				f.ChangeRandomState();
		}
	}

	internal class Faker_Spawn(Faker f, Faker_StateBase stateToChange) : Faker_StateBase(f)
	{
		float prevHeight;
		float spawnCooldown = f.spawnCooldown;
		public override void Enter()
		{
			base.Enter();
			f.IsActive = false;
			f.audMan.FlushQueue(true);
			f.Entity.Enable(false);
			f.Navigator.speed = 0;
			f.Navigator.SetSpeed(0);
			ChangeNavigationState(new NavigationState_DoNothing(f, 0));
			prevHeight = f.Entity.InternalHeight;
			f.Entity.SetHeight(f.despawnHeight);
			var cells = f.ec.mainHall.AllTilesNoGarbage(false, false);
			f.Entity.Teleport(cells[Random.Range(0, cells.Count)].CenterWorldPosition);
		}
		public override void Update()
		{
			base.Update();
			spawnCooldown -= f.TimeScale * Time.deltaTime;
			if (spawnCooldown <= 0f)
			{
				f.IsActive = true;
				f.behaviorStateMachine.ChangeState(stateToChange);
			}
		}

		public override void Exit()
		{
			base.Exit();
			f.Entity.Enable(true);
			f.Navigator.speed = f.walkSpeed;
			f.Navigator.SetSpeed(f.walkSpeed);
			f.Entity.SetHeight(prevHeight);
		}

		public override void InPlayerSight(PlayerManager player)
		{
			base.InPlayerSight(player);
			spawnCooldown = f.spawnCooldown;
		}

		public override void PlayerInSight(PlayerManager player)
		{
			base.PlayerInSight(player);
			spawnCooldown = f.spawnCooldown;
		}
	}

	internal class Faker_BlueVariant(Faker f, int form) : Faker_ActiveState(f, form)
	{
		public override void Enter()
		{
			base.Enter();
			f.Navigator.maxSpeed = 0;
			f.Navigator.SetSpeed(0);
			ChangeNavigationState(new NavigationState_DoNothing(f, 0));
		}

		public override void InPlayerSight(PlayerManager player)
		{
			base.InPlayerSight(player);

			if (!players.ContainsKey(player))
			{
				var val = new ValueModifier();
				players.Add(player, new(val, player.GetCustomCam().SlideFOVAnimation(val, f.blueVariant_FovModifier)));
				player.Am.moveMods.Add(moveMod);
				CanDespawn = false;
				f.ApplyScale(true);
			}
		}

		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			if (players.ContainsKey(player))
				TakeFovOut(player);

			if (players.Count == 0)
			{
				CanDespawn = true;
				f.ApplyScale(false);
			}
		}

		void TakeFovOut(PlayerManager player, bool removeFromDic = true)
		{

			player.Am.moveMods.Remove(moveMod);
			var cam = player.GetCustomCam();
			var k = players[player];
			if (k.Value != null)
				cam.StopCoroutine(k.Value);
			cam.ResetSlideFOVAnimation(k.Key);
			if (removeFromDic)
				players.Remove(player);

		}

		public override void Exit()
		{
			base.Exit();
			f.ApplyScale(false);

			foreach (var player in players.Keys)
				TakeFovOut(player, false);
		}

		readonly Dictionary<PlayerManager, KeyValuePair<ValueModifier, Coroutine>> players = [];

		readonly MovementModifier moveMod = new(Vector3.zero, 0.5f);
	}

	internal class Faker_RedVariant(Faker f, int form) : Faker_ActiveState(f, form)
	{
		public override void Enter()
		{
			base.Enter();
			f.Navigator.maxSpeed = 0;
			f.Navigator.SetSpeed(0);
			ChangeNavigationState(new NavigationState_DoNothing(f, 0));
		}

		public override void PlayerInSight(PlayerManager player)
		{
			base.PlayerInSight(player);
			player.transform.RotateSmoothlyToNextPoint(f.transform.position, f.redVariant_rotationSmoothness);
			f.ApplyScale(true);
		}
		public override void PlayerSighted(PlayerManager player)
		{
			base.PlayerSighted(player);
			CanDespawn = false;
		}
		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			CanDespawn = true;
			f.ApplyScale(false);
		}
		public override void Exit()
		{
			base.Exit();
			f.ApplyScale(false);
		}
	}

	internal class Faker_GreenVariant_Idle(Faker f, int form) : Faker_ActiveState(f, form)
	{
		public override void Enter()
		{
			base.Enter();
			f.Navigator.maxSpeed = 0;
			f.Navigator.SetSpeed(0);
			ChangeNavigationState(new NavigationState_DoNothing(f, 0));
			CanDespawn = true;
		}
		public override void PlayerSighted(PlayerManager player)
		{
			base.PlayerSighted(player);
			f.behaviorStateMachine.ChangeState(new Faker_GreenVariant_Follow(f, player, this, formIdx));
		}
	}
	internal class Faker_GreenVariant_Follow(Faker f, PlayerManager pm, Faker_GreenVariant_Idle prev, int form) : Faker_ActiveState(f, form)
	{
		int sighted = 0;
		NavigationState_TargetPlayer target;
		public override void Enter()
		{
			CanDespawn = false;
			target = new(f, 64, pm.transform.position);
			ChangeNavigationState(target);
			f.ApplyScale(true);
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			ChangeNavigationState(target);
		}
		public override void PlayerInSight(PlayerManager player)
		{
			base.PlayerInSight(player);
			if (player == pm)
				target.UpdatePosition(player.transform.position);
		}
		public override void InPlayerSight(PlayerManager player)
		{
			base.InPlayerSight(player);
			f.Navigator.maxSpeed = 0;
			f.Navigator.SetSpeed(0);
		}
		public override void Sighted()
		{
			base.Sighted();
			sighted++;
		}
		public override void Unsighted()
		{
			base.Unsighted();
			f.Navigator.maxSpeed = f.walkSpeed;
			f.Navigator.SetSpeed(f.walkSpeed);
			sighted--;
			if (sighted <= 0)
				f.ApplyScale(false);
		}
		public override void OnStateTriggerEnter(Collider other, bool validCollision)
		{
			base.OnStateTriggerEnter(other, validCollision);
			if (sighted <= 0 && other.isTrigger && other.gameObject == pm.gameObject)
			{
				if (validCollision)
					pm.itm.RemoveRandomItem();
				f.ChangeRandomState();
			}
		}
		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			f.ApplyScale(false);
			f.behaviorStateMachine.ChangeState(prev);
		}
		public override void Exit()
		{
			base.Exit();
			f.ApplyScale(false);
		}
	}
}
