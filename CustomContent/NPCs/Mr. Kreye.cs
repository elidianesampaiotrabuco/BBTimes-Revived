using System.Collections.Generic;
using BBTimes.CustomComponents;
using BBTimes.CustomComponents.NpcSpecificComponents;
using BBTimes.Extensions;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.NPCs
{
	public class MrKreye : NPC, INPCPrefab
	{
		public void SetupPrefab()
		{

			audMan = GetComponent<PropagatedAudioManager>();
			audStatic = this.GetSound("active.wav", "Sfx_Crafters_Intro", SoundType.Effect, audMan.subtitleColor);
			audOpenChest = this.GetSound("open.wav", "Sfx_Effects_Pop", SoundType.Effect, audMan.subtitleColor);

			var sprs = this.GetSpriteSheet(3, 3, 36f, "kreye.png");

			spriteRenderer[0].sprite = sprs[0];

			sprWalk = sprs.ExcludeNumOfSpritesFromSheet(4).MirrorSprites();
			sprOpenEye = sprs.ExcludeNumOfSpritesFromSheet(4, false).ExcludeNumOfSpritesFromSheet(1); // Makes two new arrays, but since GC exists, this can be ignored lol
			sprChestOpen = [sprs[sprs.Length - 1]];


			hookPre = ObjectCreationExtensions.CreateSpriteBillboard(this.GetSprite(25f, "hook.png"))
				.AddSpriteHolder(out var hookRenderer, 0f, LayerStorage.standardEntities).gameObject.SetAsPrefab(true)
				.AddComponent<KreyeHook>();

			hookPre.name = "KreyeHook";
			hookRenderer.name = "KreyeHook_Renderer";

			var grapInstance = GenericExtensions.FindResourceObject<ITM_GrapplingHook>();

			hookPre.lineRenderer = hookRenderer.gameObject.AddComponent<LineRenderer>();
			hookPre.lineRenderer.material = new(grapInstance.lineRenderer.material); // Clone material

			hookPre.lineRenderer.material.SetColor("_TextureColor", new(0.44140625f, 0.078125f, 0.0234375f));
			hookPre.lineRenderer.widthMultiplier = 0.75f;

			hookPre.entity = hookPre.gameObject.CreateEntity(2.5f, 3f, hookRenderer.transform);
			hookPre.entity.SetGrounded(false);

			hookPre.audMan = hookPre.gameObject.CreatePropagatedAudioManager(30f, 100f);
			hookPre.audGrab = this.GetSound("hookGrab.wav", "Vfx_Kreye_Grab", SoundType.Effect, audMan.subtitleColor);

			animComp = gameObject.AddComponent<AnimationComponent>();
			animComp.speed = 9f;
			animComp.renderers = spriteRenderer;
			animComp.animation = sprWalk;
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

			hook = Instantiate(hookPre);
			hook.Initialize(ec, this);
			animComp.Initialize(ec);
			WanderAgain();
		}

		public void Walk(bool walk) // If walk = false, implies he's active
		{
			float speed = walk ? this.speed : 0f;
			navigator.maxSpeed = speed;
			navigator.SetSpeed(speed);

			animComp.ResetFrame(true);
			animComp.animation = walk ? sprWalk : sprOpenEye;
			audMan.FlushQueue(true);
			if (!walk)
			{
				animComp.StopLastFrameMode();
				audMan.maintainLoop = true;
				audMan.SetLoop(true);
				audMan.QueueAudio(audStatic);
			}
		}

		public void ThrowHook(Entity target)
		{
			navigator.maxSpeed = 0;
			navigator.SetSpeed(0);

			hook.Throw(target, hookSpeed);
			audMan.FlushQueue(true);
			audMan.QueueAudio(audOpenChest);

			animComp.ResetFrame(true);
			animComp.animation = sprChestOpen;
			animComp.StopLastFrameMode();

			throwHookState = true;
		}

		public void SendToDetention(Entity entity)
		{
			if (entity.CompareTag("Player"))
			{
				var pm = entity.GetComponent<PlayerManager>();
				pm.ClearGuilt();
				pm.SendToDetention(detentionTime);

				WanderAgain();
				return;
			}

			// Should be a NPC then
			var n = entity.GetComponent<NPC>();

			int num = Random.Range(0, ec.offices.Count);
			entity.Teleport(ec.offices[num].RandomEntitySafeCellNoGarbage().CenterWorldPosition);
			n.SentToDetention();

			WanderAgain();
		}

		public void WanderAgain()
		{
			behaviorStateMachine.ChangeState(throwHookState ? new MrKreye_Watch(this) : new MrKreye_Wander(this));
			throwHookState = false;
		}

		public override void Despawn()
		{
			base.Despawn();
			Destroy(hook.gameObject);
		}


		KreyeHook hook;
		internal HashSet<Entity> watchingEntities = [];

		[SerializeField]
		internal SoundObject audStatic, audOpenChest;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal KreyeHook hookPre;

		[SerializeField]
		internal AnimationComponent animComp;

		[SerializeField]
		internal Sprite[] sprWalk, sprOpenEye, sprChestOpen;

		[SerializeField]
		internal float speed = 20f, hookSpeed = 50f, watchTime = 7f, detentionTime = 15f, noticeDelay = 0.75f;

		bool throwHookState = false;
	}

	internal class MrKreye_StateBase(MrKreye kre) : NpcState(kre)
	{
		protected MrKreye kre = kre;

		public override void DoorHit(StandardDoor door)
		{
			base.DoorHit(door);
			door.OpenTimedWithKey(door.DefaultTime, false);
		}

		public override void OnRoomExit(RoomController room)
		{
			base.OnRoomExit(room);
			if (room.ec.timeOut && room.type == RoomType.Room && !room.HasIncompleteActivity)
			{
				room.SetPower(false);
			}
		}
	}

	internal class MrKreye_Wander(MrKreye kre) : MrKreye_StateBase(kre)
	{
		public override void Enter()
		{
			base.Enter();
			kre.Walk(true);
			ChangeNavigationState(new NavigationState_WanderRounds(kre, 0));
		}

		public override void Update()
		{
			base.Update();
			if (kre.Blinded)
				return;

			for (int i = 0; i < kre.ec.Npcs.Count; i++)
			{
				if (kre != kre.ec.Npcs[i] && kre.ec.Npcs[i].Navigator.isActiveAndEnabled)
				{
					if (kre.looker.RaycastNPC(kre.ec.Npcs[i]) && kre.ec.Npcs[i].Disobeying)
					{
						kre.behaviorStateMachine.ChangeState(new MrKreye_Watch(kre));
						return;
					}
				}
			}
		}

		public override void PlayerInSight(PlayerManager player)
		{
			base.PlayerInSight(player);
			if (player.Disobeying && !player.Tagged)
				kre.behaviorStateMachine.ChangeState(new MrKreye_Watch(kre));
		}

	}

	internal class MrKreye_Watch(MrKreye kre) : MrKreye_StateBase(kre)
	{
		float watchTimer = kre.watchTime, catchAfterStartWatchDelay = kre.noticeDelay;
		public override void Enter()
		{
			base.Enter();
			kre.Walk(false); // Enters the "eye open" state
			ChangeNavigationState(new NavigationState_DoNothing(kre, 0));
		}

		public override void Update()
		{
			base.Update();
			if (catchAfterStartWatchDelay > 0f)
				catchAfterStartWatchDelay -= kre.TimeScale * Time.deltaTime;
			watchTimer -= kre.TimeScale * Time.deltaTime;
			if (watchTimer <= 0f)
			{
				kre.behaviorStateMachine.ChangeState(new MrKreye_Wander(kre)); // Revert to wandering after 7 seconds
				return;
			}

			if (kre.Blinded || catchAfterStartWatchDelay > 0f) return;

			// Check for any moving NPC in sight
			for (int i = 0; i < kre.ec.Npcs.Count; i++)
			{
				NPC potentialTarget = kre.ec.Npcs[i];
				if (kre != potentialTarget && potentialTarget.Navigator.isActiveAndEnabled && potentialTarget.Navigator.Velocity.magnitude > 0.1f)
				{
					kre.looker.Raycast(potentialTarget.transform, float.PositiveInfinity, out bool sighted);
					if (sighted)
					{
						kre.behaviorStateMachine.ChangeState(new MrKreye_ThrowHook(kre, potentialTarget.Entity));
						return;
					}
				}
			}
		}

		public override void PlayerInSight(PlayerManager player)
		{
			base.PlayerInSight(player);
			if (catchAfterStartWatchDelay <= 0f && !player.Tagged && player.plm.RealVelocity > 0.1f) // Check if Player is moving
			{
				kre.behaviorStateMachine.ChangeState(new MrKreye_ThrowHook(kre, player.plm.Entity));
			}
		}

		public override void Exit()
		{
			base.Exit();
			kre.watchingEntities.Clear();
		}
	}

	internal class MrKreye_ThrowHook(MrKreye kre, Entity target) : MrKreye_StateBase(kre)
	{
		readonly Entity target = target;
		public override void Enter()
		{
			ChangeNavigationState(new NavigationState_DoNothing(kre, 0));
			kre.ThrowHook(target);
		}
	}
}
