using System.Collections;
using BBTimes.CustomComponents;
using BBTimes.CustomComponents.NpcSpecificComponents;
using BBTimes.Extensions;
using BBTimes.Manager;
using BBTimes.Plugin;
using MTM101BaldAPI;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Components;
using PixelInternalAPI.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.NPCs
{
	public class Pix : NPC, INPCPrefab
	{
		public void SetupPrefab()
		{
			SoundObject[] soundObjects = [
				this.GetSound("Pix_Detected.wav", "Vfx_Pix_TarDet", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Prepare.wav", "Vfx_Pix_Prepare", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Stop.wav", "Vfx_Pix_Stop1", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Easy.wav", "Vfx_Pix_Ez", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Successful.wav", "Vfx_Pix_MisSuc", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Failed.wav", "Vfx_Pix_MisFail", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Grrr.wav", "Vfx_Pix_Grr", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_NextTime.wav", "Vfx_Pix_GetYou", SoundType.Voice, new(0.6f, 0f, 0f)),
				this.GetSound("Pix_Shoot.wav", "Vfx_Pix_Shoot", SoundType.Effect, new(0.6f, 0f, 0f)),
				this.GetSoundNoSub("shock.wav", SoundType.Effect)
				];

			soundObjects[2].additionalKeys = [new() { key = "Vfx_Pix_Stop2", time = 1.3f }, new() { key = "Vfx_Pix_Stop3", time = 1.724f }];

			// Setup audio
			audMan = GetComponent<PropagatedAudioManager>();
			audReady = [soundObjects[0], soundObjects[1], soundObjects[2]];
			audHappy = [soundObjects[3], soundObjects[4]];
			audAngry = [soundObjects[5], soundObjects[6], soundObjects[7]];
			audShoot = soundObjects[8];

			Sprite[] storedSprites = [.. this.GetSpriteSheet(22, 1, 12f, "pix.png"), .. this.GetSpriteSheet(2, 1, 25f, "laserBeam.png"), .. this.GetSpriteSheet(4, 1, 15f, "shock.png")];
			// setup animated sprites
			rotator = spriteRenderer[0].CreateAnimatedSpriteRotator(
				GenericExtensions.CreateRotationMap(4, storedSprites[0], storedSprites[2], storedSprites[4], storedSprites[6]), // Normal first frame of rotation map
				GenericExtensions.CreateRotationMap(4, storedSprites[1], storedSprites[3], storedSprites[5], storedSprites[7]), // Normal second frame of rotation map
				GenericExtensions.CreateRotationMap(4, storedSprites[8], storedSprites[10], storedSprites[4], storedSprites[12]), // Angry first frame of rotation map
				GenericExtensions.CreateRotationMap(4, storedSprites[9], storedSprites[11], storedSprites[5], storedSprites[13]), // Angry second frame of rotation map
				GenericExtensions.CreateRotationMap(4, storedSprites[14], storedSprites[16], storedSprites[4], storedSprites[18]), // Happy first frame of rotation map
				GenericExtensions.CreateRotationMap(4, storedSprites[15], storedSprites[17], storedSprites[5], storedSprites[19]) // Happy second frame of rotation map
				);
			normalSprites = [storedSprites[0], storedSprites[1]];
			spriteRenderer[0].sprite = normalSprites[0];
			angrySprites = [storedSprites[8], storedSprites[9]];
			happySprites = [storedSprites[14], storedSprites[15]];
			idleShootingSprites = [storedSprites[20], storedSprites[21]];

			explodingSprites = this.GetSpriteSheet(2, 1, 12f, "pix_explosion.png");
			explosionPrefab = BBTimesManager.man.Get<QuickExplosion>("TestExplosion");

			// laser (16, 17)
			var laserRend = ObjectCreationExtensions.CreateSpriteBillboard(storedSprites[23]).AddSpriteHolder(out var laserRenderer, 0f, LayerStorage.standardEntities);
			laserRend.gameObject.ConvertToPrefab(true);
			laserRenderer.name = "PixLaserBeamRenderer";
			laserRend.name = "PixLaserBeam";

			var laser = laserRend.gameObject.AddComponent<PixLaserBeam>();
			laser.flyingSprites = [storedSprites[22], storedSprites[23]];
			laser.shockSprites = [storedSprites[24], storedSprites[25], storedSprites[26], storedSprites[27]];
			laser.renderer = laserRenderer;

			laser.entity = laserRend.gameObject.CreateEntity(2f, 2f, laserRenderer.transform).SetEntityCollisionLayerMask(LayerStorage.gumCollisionMask);
			laser.entity.SetGrounded(false);
			laser.audMan = laserRend.gameObject.CreatePropagatedAudioManager(15, 65);
			laser.audShock = soundObjects[9];

			laser.gaugeSprite = this.GetSprite(Storage.GaugeSprite_PixelsPerUnit, "gaugeIcon.png");

			laserPre = laser;

			// Target indicator
			var targetIndcRenderer = ObjectCreationExtensions.CreateSpriteBillboard(this.GetSprite(27.5f, "pixIndicator.png"));
			targetIndicator = targetIndcRenderer.gameObject.AddComponent<VisualAttacher>();
			targetIndicator.gameObject.AddComponent<BillboardRotator>();
			targetIndicator.name = "TargetIndicatorVisual";
			targetIndicator.gameObject.ConvertToPrefab(true);
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
			navigator.maxSpeed = walkingSpeed;
			navigator.SetSpeed(walkingSpeed);
			behaviorStateMachine.ChangeState(new Pix_Wandering(this, 0f));
		}

		public void SetReadyToShoot()
		{
			currentState = 3;
			rotator.targetSprite = idleShootingSprites[0];
			rotator.BypassRotation(true);
			audMan.PlayRandomAudio(audReady);
			navigator.Am.moveMods.Add(moveMod);
		}

		public void DoneShootingState(bool failed, bool saySomething = true)
		{
			if (rageStreak + 1 == maxRageStreak)
			{
				currentState = 3;
				StartCoroutine(ExplosionAnimation());
				return;
			}
			if (saySomething)
				audMan.PlayRandomAudio(failed ? audAngry : audHappy);
			currentState = failed ? 1u : 2u;

			if (failed)
				rageStreak = Mathf.Min(rageStreak + 1, maxRageStreak);
			else
				rageStreak = 0;
			navigator.Am.moveMods.Remove(moveMod);
			rotator.BypassRotation(false);
		}

		public void SetToNormalState() =>
			currentState = 0;


		public override void VirtualUpdate()
		{
			base.VirtualUpdate();

			switch (currentState)
			{
				case 3: // do nothing lol
					return;
				case 2:
					frame += walkingAnimSpeed * TimeScale * Time.deltaTime;
					frame %= happySprites.Length;
					rotator.targetSprite = happySprites[Mathf.FloorToInt(frame)];
					return;
				case 1:
					frame += walkingAnimSpeed * TimeScale * Time.deltaTime;
					frame %= angrySprites.Length;
					rotator.targetSprite = angrySprites[Mathf.FloorToInt(frame)];
					return;
				default:
					frame += walkingAnimSpeed * TimeScale * Time.deltaTime;
					frame %= normalSprites.Length;
					rotator.targetSprite = normalSprites[Mathf.FloorToInt(frame)];
					return;
			}
		}

		public void InitiateShooting(Entity target)
		{
			StartCoroutine(PreparationAndShooting(target));
		}

		private IEnumerator PreparationAndShooting(Entity target)
		{
			SetReadyToShoot();

			var indicator = Instantiate(targetIndicator);
			indicator.AttachTo(target.transform, Vector3.up * targetOffset, true);
			indicator.SetOwnerRefToSelfDestruct(gameObject);

			while (audMan.AnyAudioIsPlaying)
				yield return null;

			yield return new WaitForSecondsNPCTimescale(this, delayBeforeShooting); // Preparation time

			if (target && target.gameObject.activeInHierarchy)
			{
				yield return StartCoroutine(Shooting(target));
			}
			else
			{
				DoneShootingState(true); // Target lost
				behaviorStateMachine.ChangeState(new Pix_Wandering(this, postShootingCooldown));
			}

			if (indicator)
				Destroy(indicator.gameObject);
		}

		void Shoot(Entity target)
		{
			beams++;
			var l = Instantiate(laserPre);
			l.InitBeam(this, target);
			StartCoroutine(ShootingAnimation(l));


			audMan.PlaySingle(audShoot);
		}

		IEnumerator Shooting(Entity target)
		{
			hasFailed = true;
			beams = 0;
			int max = 3 + (rageStreak * 3);

			for (int i = 0; i < max; i++)
			{
				float cooldown = Mathf.Max(0.25f, 2f - (max / 3f));

				while (audMan.AnyAudioIsPlaying)
					yield return null; // Wait til it is done

				while (cooldown > 0f)
				{
					cooldown -= TimeScale * Time.deltaTime;
					yield return null;
				}

				Shoot(target);
				yield return null;
			}

			while (beams > 0)
				yield return null;

			DoneShootingState(hasFailed);
			behaviorStateMachine.ChangeState(new Pix_Wandering(this, postShootingCooldown));
		}

		IEnumerator ShootingAnimation(PixLaserBeam beam)
		{
			rotator.targetSprite = idleShootingSprites[1];
			yield return null;
			if (beam)
				beam.transform.position = transform.position; // workaround for the stupid entity thing from the game

			int frame = 0;
			while (frame++ < idleAnimSpeed)
				yield return null;

			rotator.targetSprite = idleShootingSprites[0];
		}

		IEnumerator ExplosionAnimation()
		{
			rotator.targetSprite = explodingSprites[0];
			yield return new WaitForSecondsNPCTimescale(this, explosionDelay);
			transform.Explode(explosionRadius, looker.layerMask, explosionForce, -explosionForce);
			Instantiate(explosionPrefab, transform.position, Quaternion.identity);
			rotator.targetSprite = explodingSprites[1];
			yield return new WaitForSecondsNPCTimescale(this, postExplosionWait);
			rageStreak = 0;
			DoneShootingState(false, false);
		}

		internal void SetAsSuccess() => hasFailed = false;

		internal void DecrementBeamCount()
		{
			beams = Mathf.Max(0, beams - 1);
			SetGuilt(5f, "Bullying");
		}

		bool hasFailed = true;
		int beams = 0, rageStreak = 0;

		[SerializeField]
		internal AnimatedSpriteRotator rotator;

		[SerializeField]
		internal Sprite[] idleShootingSprites, normalSprites, angrySprites, happySprites, explodingSprites;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject[] audReady, audAngry, audHappy;

		[SerializeField]
		internal SoundObject audShoot;

		[SerializeField]
		internal PixLaserBeam laserPre;

		[SerializeField]
		internal QuickExplosion explosionPrefab;
		[SerializeField]
		internal float explosionRadius = 40f, explosionForce = 50f, explosionDelay = 5f, postExplosionWait = 15f,
			postShootingCooldown = 20f, delayBeforeShooting = 1f, prepShootSpeed = 26f, walkingSpeed = 14f,
			idleAnimSpeed = 10f, walkingAnimSpeed = 5f, targetOffset = 3f;
		[SerializeField]
		internal VisualAttacher targetIndicator;
		[SerializeField]
		internal int maxRageStreak = 3;

		uint currentState = 0; // 0 = normal, 1 = angry, 2 = happy, 3 = idle

		float frame = 0f;

		readonly MovementModifier moveMod = new(Vector3.zero, 0f);
	}

	internal class Pix_StateBase(Pix pix) : NpcState(pix)
	{
		protected Pix pix = pix;
	}

	internal class Pix_Wandering(Pix pix, float cooldown) : Pix_StateBase(pix)
	{
		public override void Enter()
		{
			base.Enter();
			ChangeNavigationState(new NavigationState_WanderRandom(pix, 0));
		}

		public override void Update()
		{
			base.Update();
			if (cooldown > 0f)
			{
				cooldown -= pix.TimeScale * Time.deltaTime;
				if (cooldown <= 0f)
				{
					pix.SetToNormalState();
				}
				return;
			}


			Entity target = FindTarget();
			if (target != null)
			{
				pix.behaviorStateMachine.ChangeState(new Pix_PrepShoot(pix, target));
			}
		}

		private Entity FindTarget()
		{
			if (pix.Blinded) return null;

			Entity closestTarget = null;
			float closestDist = float.MaxValue;

			// Find players first
			for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
			{
				PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(i);
				if (player.Tagged) continue;

				if (pix.looker.PlayerInSight(player))
				{
					float dist = Vector3.Distance(pix.transform.position, player.transform.position);
					if (dist < closestDist)
					{
						closestDist = dist;
						closestTarget = player.plm.Entity;
					}
				}
			}

			// Find NPCs
			foreach (NPC npc in pix.ec.Npcs)
			{
				if (npc == pix) continue;

				if (pix.looker.RaycastNPC(npc))
				{
					float dist = Vector3.Distance(pix.transform.position, npc.transform.position);
					if (dist < closestDist)
					{
						closestDist = dist;
						closestTarget = npc.Navigator.Entity;
					}
				}
			}
			return closestTarget;
		}

		float cooldown = cooldown;
	}

	internal class Pix_PrepShoot(Pix pix, Entity target) : Pix_StateBase(pix)
	{
		public override void Enter()
		{
			base.Enter();
			pix.Navigator.maxSpeed = pix.prepShootSpeed;
			pix.Navigator.SetSpeed(pix.prepShootSpeed);
			pix.Navigator.FindPath(pix.transform.position, target.transform.position);
			ChangeNavigationState(new NavigationState_TargetPosition(pix, 63, pix.Navigator.NextPoint));
		}
		public override void DestinationEmpty()
		{
			if (target != null)
			{
				if ((target.CompareTag("Player") && target.TryGetComponent<PlayerManager>(out var pm) && pix.looker.PlayerInSight(pm)) ||
				(target.CompareTag("NPC") && target.TryGetComponent<NPC>(out var npc) && pix.looker.RaycastNPC(npc)))
				{
					pix.InitiateShooting(target);
					pix.behaviorStateMachine.ChangeState(new Pix_StateBase(pix)); // Who will change state now is Pix himself
					return;
				}
			}

			pix.behaviorStateMachine.ChangeState(new Pix_Wandering(pix, 0f));
		}

		public override void Exit()
		{
			base.Exit();
			pix.Navigator.maxSpeed = pix.walkingSpeed;
			pix.Navigator.SetSpeed(pix.walkingSpeed);
		}

		readonly protected Entity target = target;
	}
}