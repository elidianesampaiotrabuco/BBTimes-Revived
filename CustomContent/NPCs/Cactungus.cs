using System.Collections.Generic;
using System.Linq;
using BBTimes.CustomComponents;
using BBTimes.Extensions;
using BBTimes.Plugin;
using MTM101BaldAPI;
using PixelInternalAPI.Extensions;
using UnityEngine;


namespace BBTimes.CustomContent.NPCs
{
	public class Cactungus : NPC, IItemAcceptor, INPCPrefab
	{
		public void SetupPrefab()
		{
			Color subColor = new Color32(0, 145, 32, 255);
			SoundObject[] soundObjects = [
			this.GetSound("Cactungus_LetsHug.wav", "Vfx_Cactungus_Hug1", SoundType.Voice, subColor),
			this.GetSound("Cactungus_LetsBeFriends.wav", "Vfx_Cactungus_Hug2", SoundType.Voice, subColor),
			this.GetSound("Cactungus_LoveYou.wav", "Vfx_Cactungus_Hug3", SoundType.Voice, subColor),
			this.GetSound("Cactungus_WhyAreYouRunning.wav", "Vfx_Cactungus_Left1", SoundType.Voice, subColor),
			this.GetSound("Cactungus_DontLeaveMe.wav", "Vfx_Cactungus_Left2", SoundType.Voice, subColor),
			this.GetSound("Cactungus_noises.wav", "Vfx_Cactungus_Noise", SoundType.Effect, subColor)
			];

			audMan = GetComponent<PropagatedAudioManager>();
			walkAudMan = gameObject.CreatePropagatedAudioManager(30f, 100f);
			renderer = spriteRenderer[0];
			var storedSprites = this.GetSpriteSheet(2, 1, 55f, "cactungus.png");
			normSprite = storedSprites[0];
			sadSprite = storedSprites[1];
			renderer.sprite = storedSprites[0];

			audFindPlayer = [.. soundObjects.Take(3)];
			audLostPlayer = [.. soundObjects.Skip(3).Take(2)];
			audWalk = soundObjects[5];

			var can = ObjectCreationExtensions.CreateCanvas();
			can.gameObject.ConvertToPrefab(false);
			can.gameObject.SetActive(false);
			can.transform.SetParent(transform);

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
			behaviorStateMachine.ChangeState(new Cactungus_Wandering(this));
			navigator.Am.moveMods.Add(walkMod);
		}

		public override void VirtualUpdate()
		{
			base.VirtualUpdate();
			float spd = Mathf.Abs(Mathf.Sin(Time.fixedTime * navigator.speed * TimeScale * slownessWalkFactor));
			if (spd > 0.5f)
			{
				if (!isWalking)
				{
					isWalking = true;
					walkAudMan.PlaySingle(audWalk);
				}
			}
			else if (spd < 0.5f && isWalking)
				isWalking = false;

			walkMod.movementMultiplier = spd;
		}

		public override void Despawn()
		{
			base.Despawn();
			gauge?.Deactivate();
		}

		public void SeeYouNoise()
		{
			audMan.FlushQueue(true);
			audMan.PlayRandomAudio(audFindPlayer);
		}

		public void SadState()
		{
			audMan.FlushQueue(true);
			audMan.PlayRandomAudio(audLostPlayer);
			renderer.sprite = sadSprite;
		}


		public void NormalState() =>
			renderer.sprite = normSprite;

		public bool ItemFits(Items itm) =>
			hittableItms.Contains(itm) && behaviorStateMachine.CurrentState is Cactungus_HugPlayer;
		public void InsertItem(PlayerManager pm, EnvironmentController ec)
		{
			pm.RuleBreak("Bullying", 3f);
			if (behaviorStateMachine.CurrentState is Cactungus_HugPlayer)
			{
				EndHug(false);
			}
		}

		public void HugPlayer(PlayerManager pm)
		{
			gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(gaugeSprite, hugCooldown);
		}

		public void UpdateHugStatus(float time) =>
			gauge?.SetValue(hugCooldown, time);

		public void EndHug(bool success)
		{
			gauge?.Deactivate();
			behaviorStateMachine.ChangeState(new Cactungus_Wandering(this, success ? normalPostHugCooldown : sadPostHugCooldown, !success));
		}

		HudGauge gauge;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal Sprite normSprite, sadSprite;

		[SerializeField]
		internal AudioManager audMan, walkAudMan;

		[SerializeField]
		internal SoundObject[] audFindPlayer;

		[SerializeField]
		internal SoundObject[] audLostPlayer;

		[SerializeField]
		internal SoundObject audWalk;

		[SerializeField]
		[Range(0.0f, 1.0f)]
		internal float slownessWalkFactor = 0.1f;

		[SerializeField]
		internal float hugCooldown = 20f, hugDistanceTolerance = 14f, normalPostHugCooldown = 5f, sadPostHugCooldown = 15f, walkSpeed = 12f, maxPullForce = 17f;

		[SerializeField]
		internal Sprite gaugeSprite;

		readonly MovementModifier walkMod = new(Vector3.zero, 1f);

		bool isWalking = true;

		readonly static HashSet<Items> hittableItms = [Items.Scissors];

		public static void AddHittableItem(Items itm) =>
			hittableItms.Add(itm);
	}

	internal class Cactungus_StateBase(Cactungus mu) : NpcState(mu)
	{
		protected Cactungus mu = mu;
	}

	internal class Cactungus_Wandering(Cactungus mu, float cooldown = 0f, bool sad = false) : Cactungus_StateBase(mu)
	{
		float cooldown = cooldown;
		readonly bool sad = sad;
		public override void Enter()
		{
			base.Enter();
			mu.Navigator.maxSpeed = mu.walkSpeed;
			mu.Navigator.SetSpeed(mu.walkSpeed);
			ChangeNavigationState(new NavigationState_WanderRandom(mu, 0));
			if (sad)
				mu.SadState();
		}

		public override void Update()
		{
			base.Update();
			cooldown -= mu.TimeScale * Time.deltaTime;
			if (cooldown <= 0f)
				mu.NormalState();
		}

		public override void OnStateTriggerEnter(Collider other, bool validCollision)
		{
			base.OnStateTriggerEnter(other, validCollision);
			if (cooldown <= 0f && other.isTrigger && (other.CompareTag("NPC") || other.CompareTag("Player")))
			{
				var e = other.GetComponent<Entity>();
				if (e)
					mu.behaviorStateMachine.ChangeState(new Cactungus_HugPlayer(mu, e));
			}
		}
	}

	internal class Cactungus_HugPlayer : Cactungus_StateBase
	{
		readonly List<HuggedEntity> huggedEntities = [];
		float hugCooldown;

		public Cactungus_HugPlayer(Cactungus mu, Entity initialEntity) : base(mu) =>
			AddEntityToHug(initialEntity);


		void AddEntityToHug(Entity entity)
		{
			if (huggedEntities.Exists(x => x.entity == entity) || entity.Frozen)
				return; // Already hugging this one or entity is frozen

			var hugData = new HuggedEntity(mu, entity, mu.hugDistanceTolerance);
			huggedEntities.Add(hugData);
			entity.ExternalActivity.moveMods.Add(hugData.hugMod);
			hugData.SetInteractionState(false); // Disable interaction

			var pm = entity.GetComponent<PlayerManager>();
			if (pm)
				mu.HugPlayer(pm);
		}

		public override void OnStateTriggerEnter(Collider other, bool validCollision)
		{
			base.OnStateTriggerEnter(other, validCollision);
			if (validCollision && other.isTrigger && (other.CompareTag("NPC") || other.CompareTag("Player")))
			{
				var e = other.GetComponent<Entity>();
				if (e)
					AddEntityToHug(e);
			}
		}

		public override void Enter()
		{
			base.Enter();
			mu.SeeYouNoise();
			hugCooldown = mu.hugCooldown;
		}

		public override void Update()
		{
			base.Update();
			hugCooldown -= mu.TimeScale * Time.deltaTime;
			mu.UpdateHugStatus(hugCooldown);

			if (hugCooldown < 0f)
			{
				mu.EndHug(true); // Success, everyone was hugged for the full duration
				return;
			}

			// Iterate backwards to remove during iteration
			for (int i = huggedEntities.Count - 1; i >= 0; i--)
			{
				var hugged = huggedEntities[i];
				hugged.timer += mu.TimeScale * Time.deltaTime;

				if (!hugged.entity) // If entity is destroyed or null
				{
					huggedEntities.RemoveAt(i);
					continue;
				}

				var dist = mu.transform.position - hugged.entity.transform.position;

				// Escape check
				if (dist.magnitude >= hugged.hugTolerance || hugged.entity.Hidden || !hugged.entity.InBounds || !hugged.entity.Visible || mu.Blinded)
				{
					mu.EndHug(false); // Someone escaped, fail state
					return;
				}

				// "Glued" effect
				if (hugged.timer < 3f)
					hugged.hugMod.movementMultiplier = 0f; // Completely stuck for 3 seconds
				else
					hugged.hugMod.movementMultiplier = 0.5f; // Can move slowly after 3 seconds

				hugged.hugMod.movementAddend = dist.normalized * Mathf.Min(dist.magnitude, mu.maxPullForce); // Pull velocity towards Cactungus

				// Tighten the hug tolerance over time
				hugged.hugTolerance -= mu.TimeScale * Time.deltaTime * dist.magnitude * 0.5f;
				if (hugged.hugTolerance < 3f)
					hugged.hugTolerance = 3f;
			}
		}

		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			if (huggedEntities.Exists(x => x.entity == player.plm.Entity))
				mu.EndHug(false);
		}

		public override void Exit()
		{
			base.Exit();
			foreach (var hugged in huggedEntities)
			{
				if (hugged.entity)
				{
					hugged.entity.ExternalActivity.moveMods.Remove(hugged.hugMod);
					hugged.SetInteractionState(true); // Re-enable interaction
				}
			}
			huggedEntities.Clear();
			mu.NormalState();
		}

		// Helper class to store state for each hugged entity
		class HuggedEntity(Cactungus owner, Entity e, float initialTolerance)
		{
			public readonly Entity entity = e;
			public readonly MovementModifier hugMod = new MovementModifier(Vector3.zero, 0f);
			public float hugTolerance = initialTolerance;
			public float timer = 0f;
			public bool InteractionDisabled => interactionDisables != 0;
			readonly Cactungus cactus = owner;

			public void SetInteractionState(bool state)
			{
				interactionDisables += state ? -1 : 1;
				if (interactionDisables < 0)
					interactionDisables = 0;
				bool interactionDisabled = InteractionDisabled;
				if (interactionCurrentlyDisabled == interactionDisabled) return; // Shouldn't be run twice

				// Ignore npcs
				for (int i = 0; i < entity.Ec.Npcs.Count; i++)
				{
					var npc = entity.Ec.Npcs[i];
					if (npc.Navigator.Entity == entity || cactus == npc) continue; // Ignore itself and Cactungus collision
					entity.IgnoreEntity(npc.Navigator.Entity, interactionDisabled);
				}

				// Ignore players
				for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
				{
					var pm = Singleton<CoreGameManager>.Instance.GetPlayer(i);
					if (pm.plm.Entity == entity) continue;
					entity.IgnoreEntity(pm.plm.Entity, interactionDisabled);
				}

				interactionCurrentlyDisabled = interactionDisabled;
			}

			int interactionDisables = 0;
			bool interactionCurrentlyDisabled = false;
		}
	}
}
