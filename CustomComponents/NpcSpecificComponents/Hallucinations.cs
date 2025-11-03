using System.Collections;
using System.Collections.Generic;
using BBTimes.CustomContent.NPCs;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using PixelInternalAPI.Components;
using PixelInternalAPI.Extensions;
using UnityEngine;

namespace BBTimes.CustomComponents.NpcSpecificComponents
{
	public class Hallucinations : MonoBehaviour
	{
		public MomentumNavigator nav; // Add navigator field
		Watcher watcher;
		DijkstraMap map;
		public int minDistanceFromPlayer = 4, maxDistanceFromPlayer = 7;
		public void AttachToPlayer(PlayerManager pm, Watcher watcher)
		{
			if (initialized) return;
			this.watcher = watcher;
			timeAlive = lifeTime;
			target = pm;
			ec = pm.ec;
			initialized = true;
			map = pm.DijkstraMap;

			// Initialize the navigator
			nav.Initialize(ec);

			activeHallucinations.Add(new(this, pm));
			StartCoroutine(Hallucinating());
		}

		IEnumerator Hallucinating()
		{
			map.StoreFoundCells = true;
			map.Calculate();
			map.StoreFoundCells = false;

			int distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer + 1);
			List<Cell> candidateCells = map.FoundCells();
			for (int i = 0; i < candidateCells.Count; i++)
			{
				int valDist = map.Value(candidateCells[i].position);
				if (valDist < minDistanceFromPlayer || valDist > distance)
					candidateCells.RemoveAt(i--);
			}

			// Spawn at a random position around the player
			transform.position = candidateCells.Count != 0 ? candidateCells[Random.Range(0, candidateCells.Count)].CenterWorldPosition :
				target.transform.position;

			audMan.QueueAudio(audLoop);
			audMan.SetLoop(true);
			audMan.maintainLoop = true;
			audMan.PlaySingle(audSpawn);

			// Fade in
			Color alpha = renderer.color;
			alpha.a = 0f;
			renderer.color = alpha;
			while (alpha.a < 1f)
			{
				alpha.a += ec.EnvironmentTimeScale * Time.deltaTime * 2f; // Faster fade in
				renderer.color = alpha;
				yield return null;
			}
			alpha.a = 1f;
			renderer.color = alpha;

			// Chase the player until lifetime expires
			while (timeAlive > 0f)
			{
				if (!target)
				{
					Despawn();
					yield break;
				}
				nav.FindPath(target.transform.position);
				yield return null;
			}

			// Fade out
			while (alpha.a > 0f)
			{
				alpha.a -= ec.EnvironmentTimeScale * Time.deltaTime * 2f; // Faster fade out
				renderer.color = alpha;
				yield return null;
			}

			Despawn(true);
		}

		void Update()
		{
			if (!initialized) return;

			timeAlive -= ec.EnvironmentTimeScale * Time.deltaTime;
		}

		void OnTriggerEnter(Collider other)
		{
			if (!initialized) return;

			if (other.isTrigger && other.CompareTag("Player") && other.gameObject == target.gameObject)
			{
				// Camera Flicker
				StartCoroutine(FlickerFOV(target.GetCustomCam()));

				// Cumulative Fog
				if (watcher.obscurityDebuff == null)
				{
					watcher.obscurityDebuff = new Fog
					{
						color = Color.black,
						startDist = 5f,
						maxDist = 20f,
						strength = 0f,
						priority = 1
					};
					ec.AddFog(watcher.obscurityDebuff);
				}
				watcher.obscurityDebuff.strength = Mathf.Min(watcher.obscurityDebuff.strength + 0.15f, 0.8f);
				ec.UpdateFog();


				Despawn();
			}
		}

		IEnumerator FlickerFOV(CustomPlayerCameraComponent cam)
		{
			var mod = new ValueModifier() { addend = 20f };
			cam.AddModifier(mod);
			yield return new WaitForSecondsEnvironmentTimescale(ec, 0.1f);
			cam.RemoveModifier(mod);
		}


		public void SetToDespawn() =>
			timeAlive = 0f;


		public void Despawn() => Despawn(true); // Public overload for simplicity
		public void Despawn(bool destroy)
		{
			if (isDespawning) return;
			isDespawning = true;
			StopAllCoroutines();
			StartCoroutine(FadeOutAndDestroy(destroy));
		}
		IEnumerator FadeOutAndDestroy(bool destroy)
		{
			activeHallucinations.RemoveAll(x => x.Key == this);
			if (nav) nav.ClearDestination();
			GetComponent<Collider>().enabled = false;

			Color alpha = renderer.color;
			while (alpha.a > 0f)
			{
				alpha.a -= ec.EnvironmentTimeScale * Time.deltaTime * 3f;
				renderer.color = alpha;
				yield return null;
			}

			if (destroy)
				Destroy(gameObject);
		}


		EnvironmentController ec;
		PlayerManager target;
		bool initialized = false, isDespawning = false;
		float timeAlive;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal float lifeTime = 45f, delayAroundThePlayer = 3f;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject audSpawn, audLoop;

		readonly public static List<KeyValuePair<Hallucinations, PlayerManager>> activeHallucinations = [];
	}
}