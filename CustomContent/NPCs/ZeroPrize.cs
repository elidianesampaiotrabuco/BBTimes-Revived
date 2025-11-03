using System.Collections.Generic;
using BBTimes.CustomComponents;
using BBTimes.Extensions;
using UnityEngine;

namespace BBTimes.CustomContent.NPCs
{
	public class ZeroPrize : NPC, INPCPrefab
	{

		public void SetupPrefab()
		{
			audStartSweep = this.GetSound("0thprize_timetosweep.wav", "Vfx_0TH_WannaSweep", SoundType.Voice, new(0.99609375f, 0.99609375f, 0.796875f));
			audSweep = this.GetSound("0thprize_mustsweep.wav", "Vfx_0TH_Sweep", SoundType.Voice, new(0.99609375f, 0.99609375f, 0.796875f));
			blinkingSprites = this.GetSpriteSheet(2, 1, 45f, "0thprize.png");
			sweepingSprites = this.GetSpriteSheet(2, 1, 45f, "0thprize_brushing.png");
			spriteRenderer[0].sprite = blinkingSprites[0];

			audMan = GetComponent<PropagatedAudioManager>();

			((CapsuleCollider)baseTrigger[0]).radius = 4f;

			animComp = gameObject.AddComponent<AnimationComponent>();
			animComp.renderers = spriteRenderer;
			animComp.autoStart = false;
			animComp.speed = 6f;
			animComp.animation = blinkingSprites;
		}
		public void SetupPrefabPost() { }
		public string Name { get; set; }
		public string Category => "npcs";

		public NPC Npc { get; set; }
		[SerializeField] Character[] replacementNPCs; public Character[] GetReplacementNPCs() => replacementNPCs; public void SetReplacementNPCs(params Character[] chars) => replacementNPCs = chars;
		public int ReplacementWeight { get; set; }
		// --------------------------------------------------

		void Start()
		{
			home = ec.CellFromPosition(transform.position);
		}

		public override void Initialize()
		{
			base.Initialize();
			animComp.Initialize(ec);
			behaviorStateMachine.ChangeState(new ZeroPrize_Wait(this, SleepingCooldown, false));
		}

		internal void StartSweeping()
		{
			moveMod.forceTrigger = true;
			if (!isActiveAndSweeping)
				audMan.PlaySingle(audStartSweep);

			navigator.maxSpeed = speed;
			navigator.SetSpeed(speed);

			isActiveAndSweeping = true;
		}

		internal void StopSweeping()
		{
			moveMod.forceTrigger = false;
			navigator.SetSpeed(0f);
			navigator.maxSpeed = 0f;

			ClearActs();
			isActiveAndSweeping = false;
		}

		public override void VirtualOnTriggerEnter(Collider other) // copypaste from gotta sweep's code
		{
			if (IsSleeping) return;

			if (other.isTrigger && (other.CompareTag("Player") || other.CompareTag("NPC")))
			{
				Entity component = other.GetComponent<Entity>();
				if (component != null)
				{
					audMan.PlaySingle(audSweep);
					ActivityModifier externalActivity = component.ExternalActivity;
					if (!externalActivity.moveMods.Contains(moveMod))
					{
						externalActivity.moveMods.Add(moveMod);
						actMods.Add(externalActivity);
					}
				}
			}
		}
		public override void VirtualOnTriggerExit(Collider other) // copypaste from gotta sweep's code
		{
			if (other.isTrigger && (other.CompareTag("Player") || other.CompareTag("NPC")))
			{
				Entity component = other.GetComponent<Entity>();
				if (component != null)
				{
					ActivityModifier externalActivity = component.ExternalActivity;
					externalActivity.moveMods.Remove(moveMod);
					actMods.Remove(externalActivity);
				}
			}
		}

		public override void Despawn()
		{
			base.Despawn();
			ClearActs();
		}

		void ClearActs()
		{
			while (actMods.Count > 0)
			{
				actMods[0].moveMods.Remove(moveMod);
				actMods.RemoveAt(0);
			}
		}

		bool isActiveAndSweeping = false;

		internal bool IsHome => home == ec.CellFromPosition(transform.position);
		internal bool IsSleeping => navigationStateMachine.currentState is NavigationState_DoNothing;
		internal float ActiveCooldown => Random.Range(minActive, maxActive);
		internal float SleepingCooldown => Random.Range(minWait, maxWait);
		internal Cell home;


		readonly internal MovementModifier moveMod = new(Vector3.zero, 0f);
		readonly List<ActivityModifier> actMods = [];
		internal List<ActivityModifier> ActMods => actMods;
		public float SpotSweepCooldown => Random.Range(minSpotSweepTime, maxSpotSweepTime);
		public float AwakeningDelay => Random.Range(minAwakeningDelay, maxAwakeningDelay);

		[SerializeField]
		internal AnimationComponent animComp;

		[SerializeField]
		internal float minSpotSweepTime = 8f, maxSpotSweepTime = 14f;
		[SerializeField]
		internal float spotSweepBaseChance = 0.05f;
		[SerializeField]
		internal float spotSweepChancePerEntity = 0.1f;

		[SerializeField]
		internal SoundObject audSweep, audStartSweep;

		[SerializeField]
		internal Sprite[] blinkingSprites, sweepingSprites;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal float moveModMultiplier = 0.97f, minActive = 30f, maxActive = 50f, minWait = 40f, maxWait = 60f, speed = 80f, sweepingSpeed = 25f, minAwakeningDelay = 4f, maxAwakeningDelay = 6f;
	}
	internal class ZeroPrize_StateBase(ZeroPrize prize) : NpcState(prize)
	{
		protected ZeroPrize prize = prize;
	}

	internal class ZeroPrize_Wait(ZeroPrize prize, float cooldown, bool isActive) : ZeroPrize_StateBase(prize)
	{
		float waitTime = cooldown;
		readonly bool active = isActive;
		float spotSweepCheck = 1f;

		public override void Enter()
		{
			base.Enter();
			prize.animComp.animation = prize.blinkingSprites;
			if (!active) // he is not active
			{
				prize.StopSweeping();
				ChangeNavigationState(new NavigationState_DoNothing(prize, 0));
				prize.animComp.ResetFrame(true, 1);
				prize.animComp.Pause(true);
				return;
			}

			prize.StartSweeping(); // he is active
			ChangeNavigationState(new NavigationState_WanderRandom(prize, 0));

			prize.animComp.ResetFrame(true);
			prize.animComp.Pause(true);
		}

		public override void Update()
		{
			base.Update();
			waitTime -= Time.deltaTime * prize.TimeScale;

			if (active)
			{
				spotSweepCheck -= Time.deltaTime * prize.TimeScale;
				if (spotSweepCheck <= 0f)
				{
					spotSweepCheck = 1f; // Check every second
					float chance = prize.spotSweepBaseChance + (prize.ActMods.Count * prize.spotSweepChancePerEntity);
					if (Random.value < chance)
					{
						prize.behaviorStateMachine.ChangeState(new ZeroPrize_SweepingSpot(prize, prize.SpotSweepCooldown, waitTime));
						return;
					}
				}

				prize.moveMod.movementAddend = prize.Navigator.Velocity.normalized * prize.Navigator.speed * prize.moveModMultiplier * prize.Navigator.Am.Multiplier;

				if (waitTime <= 0f)
				{
					prize.behaviorStateMachine.ChangeState(new ZeroPrize_WaitForSpawnBack(prize));
				}
			}
			else
			{
				if (waitTime <= 0f)
				{
					prize.behaviorStateMachine.ChangeState(prize.IsSleeping
						? new ZeroPrize_Awakening(prize) : new ZeroPrize_Wait(prize, prize.ActiveCooldown, true));
				}
			}
		}

		public override void Exit()
		{
			base.Exit();
			if (active)
				prize.moveMod.movementAddend = Vector3.zero;
		}
	}

	internal class ZeroPrize_Awakening(ZeroPrize prize) : ZeroPrize_StateBase(prize)
	{
		public override void Enter()
		{
			base.Enter();
			prize.animComp.animation = prize.blinkingSprites;
			prize.animComp.ResetFrame(true);
		}
		public override void Update()
		{
			base.Update();
			blinkTime -= prize.TimeScale * Time.deltaTime;
			if (blinkTime <= 0f)
				prize.behaviorStateMachine.ChangeState(new ZeroPrize_Wait(prize, prize.ActiveCooldown, true));
		}

		float blinkTime = prize.AwakeningDelay;

	}

	internal class ZeroPrize_WaitForSpawnBack(ZeroPrize prize) : ZeroPrize_StateBase(prize)
	{
		public override void Enter()
		{
			base.Enter();
			ChangeNavigationState(new NavigationState_TargetPosition(prize, 63, prize.home.FloorWorldPosition));
		}

		public override void Update()
		{
			base.Update();
			prize.moveMod.movementAddend = prize.Navigator.Velocity.normalized * prize.Navigator.speed * prize.moveModMultiplier * prize.Navigator.Am.Multiplier;
		}

		public override void Exit()
		{
			base.Exit();
			prize.moveMod.movementAddend = Vector3.zero;
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!prize.IsHome)
			{
				prize.behaviorStateMachine.CurrentNavigationState.UpdatePosition(prize.home.FloorWorldPosition);
				return;
			}
			prize.behaviorStateMachine.ChangeState(new ZeroPrize_Wait(prize, prize.SleepingCooldown, false));
		}
	}

	internal class ZeroPrize_SweepingSpot(ZeroPrize prize, float cooldown, float lastingActiveCooldown) : ZeroPrize_StateBase(prize)
	{
		float sweepTime = cooldown;
		readonly float activeCooldownLeft = lastingActiveCooldown;

		public override void Enter()
		{
			base.Enter();
			prize.navigator.maxSpeed = prize.sweepingSpeed;
			prize.navigator.SetSpeed(prize.sweepingSpeed);
			prize.moveMod.movementAddend = Vector3.zero;
			prize.moveMod.movementMultiplier = 1f;
			prize.animComp.animation = prize.sweepingSprites;
			prize.animComp.ResetFrame(true); // Resets frame and unpauses
		}
		public override void Update()
		{
			base.Update();
			sweepTime -= Time.deltaTime * prize.TimeScale;
			if (sweepTime <= 0f)
			{
				prize.behaviorStateMachine.ChangeState(new ZeroPrize_Wait(prize, activeCooldownLeft, true));
			}
		}
		public override void Exit()
		{
			base.Exit();
			prize.moveMod.movementMultiplier = 0f;
		}
	}
}
