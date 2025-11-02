using System.Collections;
using System.Collections.Generic;
using BBTimes.CustomComponents;
using BBTimes.CustomComponents.NpcSpecificComponents;
using BBTimes.Extensions;
using BBTimes.Plugin;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using PixelInternalAPI.Components;
using PixelInternalAPI.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.NPCs
{
	public class Watcher : NPC, INPCPrefab
	{
		public void SetupPrefab()
		{
			Color
			normalColor = new(0.6f, 0.6f, 0.6f),
			shadowColor = new(0.45f, 0.45f, 0.45f);

			SoundObject[] soundObjects = [this.GetSound("WCH_ambience.wav", "Vfx_Wch_Idle", SoundType.Effect, normalColor),
		this.GetSoundNoSub("WCH_see.wav", SoundType.Effect),
		this.GetSound("WCH_teleport.wav", "Vfx_Wch_Teleport", SoundType.Effect, normalColor),
		this.GetSound("SHDWCH_spawn.wav", "Vfx_Wch_Spawn", SoundType.Effect, shadowColor),
		this.GetSound("SHDWCH_ambience.wav", "Vfx_Wch_Idle", SoundType.Effect, shadowColor)
			];
			var storedSprites = this.GetSpriteSheet(3, 1, 35f, "watcher.png");
			spriteRenderer[0].sprite = storedSprites[0];
			normalSprite = storedSprites[0];
			angrySprite = storedSprites[1];

			audMan = GetComponent<PropagatedAudioManager>();

			audAmbience = soundObjects[0];
			audSpot = soundObjects[1];
			audTeleport = soundObjects[2];

			spriteToHide = spriteRenderer[0];
			screenAudMan = gameObject.CreateAudioManager(45f, 75f).MakeAudioManagerNonPositional();

			// Hallucination Setup
			var hallRender = ObjectCreationExtensions.CreateSpriteBillboard(storedSprites[2]);
			hallRender.gameObject.layer = LayerMask.NameToLayer("Overlay");
			hallRender.name = "WatcherHallucination";
			hallRender.gameObject.ConvertToPrefab(true);

			var col = hallRender.gameObject.AddComponent<CapsuleCollider>();
			col.isTrigger = true;
			col.radius = 2f;
			col.height = 10f;
			col.center = new Vector3(0f, 5f, 0f);

			var hall = hallRender.gameObject.AddComponent<Hallucinations>();
			hall.audMan = hall.gameObject.CreateAudioManager(15f, 25f);
			hall.audSpawn = soundObjects[3];
			hall.audLoop = soundObjects[4];
			hall.renderer = hallRender;
			hall.nav = hallRender.gameObject.AddComponent<MomentumNavigator>();

			hallPre = hall;

			gaugeSprite = this.GetSprite(Storage.GaugeSprite_PixelsPerUnit, "gaugeIcon.png");
		}
		public void SetupPrefabPost() { }
		public string Name { get; set; }
		public string Category => "npcs";

		public NPC Npc { get; set; }
		[SerializeField] Character[] replacementNPCs; public Character[] GetReplacementNPCs() => replacementNPCs; public void SetReplacementNPCs(params Character[] chars) => replacementNPCs = chars;
		public int ReplacementWeight { get; set; }
		// --------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();
			audMan.maintainLoop = true;
			screenAudMan.maintainLoop = true;

			behaviorStateMachine.ChangeState(new Watcher_WaitBelow(this, false));
		}

		public void DespawnHallucinations(bool instaDespawn)
		{
			while (hallucinations.Count != 0)
			{
				if (instaDespawn)
					hallucinations[0]?.Despawn();
				else
					hallucinations[0]?.SetToDespawn();

				hallucinations.RemoveAt(0);
			}
		}

		public override void VirtualUpdate()
		{
			base.VirtualUpdate();
			for (int i = 0; i < hallucinations.Count; i++)
			{
				if (!hallucinations[i]) // remove any destroyed hallucination
					hallucinations.RemoveAt(i--);
			}
		}

		public override void Despawn()
		{
			base.Despawn();
			DespawnHallucinations(true);
			ClearDebuffs();
		}

		public void AngryHide()
		{
			spriteToHide.sprite = angrySprite;
			StartCoroutine(GradualHide());
		}

		public void Hide(bool hide)
		{
			if (!hide)
				spriteToHide.sprite = normalSprite;

			spriteToHide.enabled = !hide;
			for (int i = 0; i < baseTrigger.Length; i++)
				baseTrigger[i].enabled = !hide;

			if (hide)
			{
				audMan.FlushQueue(true);
				return;
			}
			audMan.SetLoop(true);
			audMan.QueueAudio(audAmbience);
		}

		public void SetFrozen(bool freeze)
		{
			if (freeze)
			{
				if (!navigator.Entity.ExternalActivity.moveMods.Contains(moveMod))
					navigator.Entity.ExternalActivity.moveMods.Add(moveMod);
			}
			else
				navigator.Entity.ExternalActivity.moveMods.Remove(moveMod);

		}

		public void GoToRandomSpot()
		{
			_randomSpots.Clear();
			for (int i = 0; i < ec.levelSize.x; i++)
			{
				for (int j = 0; j < ec.levelSize.z; j++)
				{
					if (!ec.cells[i, j].Null && ec.cells[i, j].room.type == RoomType.Hall && (ec.cells[i, j].shape == TileShapeMask.Corner || ec.cells[i, j].shape == TileShapeMask.End || ec.cells[i, j].shape == TileShapeMask.Single) && !ec.cells[i, j].HardCoverageFits(CellCoverage.Down | CellCoverage.Center))
						_randomSpots.Add(ec.cells[i, j]);
				}
			}

			navigator.Entity.Teleport(_randomSpots[Random.Range(0, _randomSpots.Count)].CenterWorldPosition);
			StartCoroutine(SpawnDelay());
		}


		// Add these methods to the Watcher class
		public void ApplyPlayerDebuff(PlayerManager player)
		{
			int debuffType = Random.Range(0, 3);
			switch (debuffType)
			{
				case 0: // Slow
					player.Am.moveMods.Add(slowMod);
					slowDebuffs.Add(slowMod);
					break;
				case 1: // Fog
					if (obscurityDebuff == null)
					{
						obscurityDebuff = new Fog
						{
							color = Color.black,
							startDist = 0f,
							maxDist = 20f,
							strength = 0f,
							priority = 70 // High priority
						};
						ec.AddFog(obscurityDebuff);
					}
					obscurityDebuff.strength = Mathf.Min(obscurityDebuff.strength + 0.15f, 0.8f);
					ec.UpdateFog();
					break;
				case 2: // Displacement
					float distance = Random.Range(10f, 20f) * (Random.value > 0.5f ? 1f : -1f);
					Vector3 newPos = player.transform.position + player.transform.forward * distance;
					// Basic check to avoid teleporting into walls
					if (ec.CellFromPosition(newPos).ConstBin == 0)
						player.Teleport(newPos);
					break;
			}
		}

		public void StartDebuffTimer(PlayerManager pm)
		{
			if (debuffRoutine != null)
				StopCoroutine(debuffRoutine);
			debuffRoutine = StartCoroutine(DebuffCleanupCoroutine(pm));
		}

		IEnumerator DebuffCleanupCoroutine(PlayerManager pm)
		{
			yield return new WaitForSecondsNPCTimescale(this, 45f);

			ClearDebuffs(pm);
			debuffRoutine = null;
		}
		public void ClearDebuffs() => ClearDebuffs(null);
		public void ClearDebuffs(PlayerManager targetedPlayer)
		{
			if (targetedPlayer)
			{
				while (slowDebuffs.Count != 0)
				{
					targetedPlayer.Am.moveMods.Remove(slowDebuffs[0]);
					slowDebuffs.RemoveAt(0);
				}
			}
			if (obscurityDebuff != null)
			{
				ec.RemoveFog(obscurityDebuff);
				obscurityDebuff = null;
				ec.UpdateFog();
			}
		}

		public void SpawnHallucinations(PlayerManager pm) =>
			StartCoroutine(HallucinationSpawner(pm));

		IEnumerator HallucinationSpawner(PlayerManager pm)
		{
			int halls = Random.Range(minHallucinations, maxHallucinations);
			for (int i = 0; i < halls; i++)
			{
				var hal = Instantiate(hallPre);
				hal.AttachToPlayer(pm, this);
				hallucinations.Add(hal);
				yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));
			}
		}

		IEnumerator SpawnDelay()
		{
			EntityOverrider overrider = new();
			navigator.Entity.Override(overrider);
			float normHeight = navigator.Entity.InternalHeight;
			overrider.SetHeight(0f);
			float curHeight = 0f;
			float tar = normHeight - 0.05f;

			while (true)
			{
				curHeight += (normHeight - curHeight) / 3f * TimeScale * Time.deltaTime * 15f;
				if (curHeight >= tar)
					break;

				overrider.SetHeight(curHeight);
				yield return null;
			}
			overrider.SetHeight(normHeight);
			overrider.Release();

			yield break;
		}

		IEnumerator GradualHide()
		{
			Color alpha = spriteToHide.color;
			while (alpha.a > 0f)
			{
				alpha.a -= ec.EnvironmentTimeScale * Time.deltaTime * 3f;
				spriteToHide.color = alpha;
				yield return null;
			}
			Hide(true);
			spriteToHide.color = Color.white;
		}
		readonly List<MovementModifier> slowDebuffs = [];
		Fog obscurityDebuff;
		Coroutine debuffRoutine;

		[SerializeField]
		internal SpriteRenderer spriteToHide;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal AudioManager screenAudMan;

		[SerializeField]
		internal SoundObject audAmbience, audSpot, audTeleport;

		[SerializeField]
		internal Hallucinations hallPre;

		[SerializeField]
		internal Sprite gaugeSprite, angrySprite, normalSprite;

		[SerializeField]
		internal int minHallucinations = 7, maxHallucinations = 9;

		[SerializeField]
		internal float minWaitCooldown = 20f, maxWaitCooldown = 40f;

		public HudGauge Gauge { get; private set; }

		public bool HasActiveHallucinations => hallucinations.Count != 0;

		readonly List<Hallucinations> hallucinations = [];

		readonly MovementModifier moveMod = new(Vector3.zero, 0f);
		readonly MovementModifier slowMod = new(Vector3.zero, 0.65f);
		readonly List<Cell> _randomSpots = [];
	}

	internal class Watcher_StateBase(Watcher w) : NpcState(w)
	{
		protected Watcher w = w;
	}

	internal class Watcher_WaitBelow(Watcher w, bool activeHallucinations) : Watcher_StateBase(w)
	{
		public float Cooldown { get; private set; }
		public float OriginalCooldown { get; private set; }
		bool hasActiveHallucinations = activeHallucinations;
		public override void Enter()
		{
			base.Enter();
			Cooldown = Random.Range(w.minWaitCooldown, w.maxWaitCooldown);
			OriginalCooldown = Cooldown;
			ChangeNavigationState(new NavigationState_DoNothing(w, 0));
			w.Hide(true);
		}

		public override void Update()
		{
			base.Update();
			Cooldown -= w.TimeScale * Time.deltaTime;
			if (hasActiveHallucinations)
			{
				w.Gauge?.SetValue(OriginalCooldown, Cooldown);
				if (!w.HasActiveHallucinations)
				{
					hasActiveHallucinations = false;
					w.Gauge?.Deactivate();
				}
			}
			if (Cooldown <= 0f)
			{
				w.behaviorStateMachine.ChangeState(new Watcher_Active(w));
				if (hasActiveHallucinations)
					w.Gauge?.Deactivate();
			}
		}
	}

	internal class Watcher_Active(Watcher w) : Watcher_StateBase(w)
	{
		public override void Initialize()
		{
			base.Initialize();
			w.DespawnHallucinations(false);
			w.GoToRandomSpot();
			w.SetFrozen(true);
			w.Hide(false);
			w.screenAudMan.FlushQueue(true);
			w.screenAudMan.Pause(true);
		}

		public override void Sighted()
		{
			base.Sighted();
			if (!hasPlayed)
			{
				hasPlayed = true;

				w.screenAudMan.SetLoop(true);
				w.screenAudMan.QueueAudio(w.audSpot);
			}
			w.screenAudMan.Pause(false);
			stillInSight = true;
		}

		public override void Unsighted()
		{
			base.Unsighted();
			w.screenAudMan.Pause(true);
			mod.addend = 0;
			stillInSight = false;
			if (lastSawPlayer && moveMods.TryGetValue(lastSawPlayer, out var mmod))
				mmod.movementAddend = Vector3.zero;
		}

		public override void InPlayerSight(PlayerManager player)
		{
			base.InPlayerSight(player);

			if (!moveMods.TryGetValue(player, out var moveMod))
				return;

			lastSawPlayer = player;
			spotStrength += w.TimeScale * Time.deltaTime * 6.5f;
			if (Time.timeScale > 0)
				mod.addend = spotStrength * (-1f + Random.value * 2f) * 2f;
			Vector3 distance = w.transform.position - player.transform.position;
			moveMod.movementAddend = distance.normalized * Mathf.Min(15f, distance.magnitude * 0.6f);

			player.transform.RotateSmoothlyToNextPoint(w.transform.position, 0.35f);
			if (spotStrength > strengthLimit)
			{
				player.GetCustomCam().RemoveModifier(mod);
				w.behaviorStateMachine.ChangeState(new Watcher_Attack(w, player));
			}
		}

		public override void PlayerSighted(PlayerManager player)
		{
			base.PlayerSighted(player);
			if (moveMods.ContainsKey(player))
				return;

			var moveMod = new MovementModifier(Vector3.zero, 1f);
			player.Am.moveMods.Add(moveMod);
			moveMods.Add(player, moveMod);

			mod.addend = 0;
			player.GetCustomCam().AddModifier(mod);
		}

		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			player.GetCustomCam().RemoveModifier(mod);
			if (moveMods.TryGetValue(player, out var modifier))
			{
				player.Am.moveMods.Remove(modifier);
				moveMods.Remove(player);
			}
		}

		public override void Update()
		{
			base.Update();
			if (stillInSight) return;

			leaveCooldown -= w.TimeScale * Time.deltaTime;
			if (leaveCooldown <= 0f)
				w.behaviorStateMachine.ChangeState(new Watcher_WaitBelow(w, false));
		}

		public override void Exit()
		{
			base.Exit();
			foreach (var move in moveMods)
				move.Key?.Am.moveMods.Remove(move.Value);
		}

		bool hasPlayed = false, stillInSight = false;


		float spotStrength = 0f, leaveCooldown = Random.Range(30f, 60f);
		const float strengthLimit = 12f;
		readonly ValueModifier mod = new();

		readonly Dictionary<PlayerManager, MovementModifier> moveMods = [];
		PlayerManager lastSawPlayer;

	}

	internal class Watcher_Attack(Watcher w, PlayerManager pm) : Watcher_StateBase(w)
	{
		readonly PlayerManager target = pm;
		CustomPlayerCameraComponent comp;

		public override void Initialize()
		{
			base.Initialize();
			comp = target.GetCustomCam();
			comp.RemoveModifier(mod);
			w.screenAudMan.FlushQueue(true);
			w.screenAudMan.PlaySingle(w.audTeleport);
			w.Hide(true); // Watcher disappears

			w.StartDebuffTimer(target);
			w.SpawnHallucinations(target); // Starts spawning hallucinations

			// After starting the attack, go back to waiting state
			var waitState = new Watcher_WaitBelow(w, true);
			w.behaviorStateMachine.ChangeState(waitState);
			w.Gauge.Activate(w.gaugeSprite, waitState.OriginalCooldown);
		}

		public override void Exit()
		{
			base.Exit();
			comp?.RemoveModifier(mod);
		}

		readonly ValueModifier mod = new();
	}
}
