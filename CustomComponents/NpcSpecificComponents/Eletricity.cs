using UnityEngine;

namespace BBTimes.CustomComponents.NpcSpecificComponents
{
	public class Eletricity : GlueObject
	{
		protected override void Initialize()
		{
			base.Initialize();
			ani.Initialize(ec);
		}

		protected override void VirtualUpdate()
		{
			base.VirtualUpdate();
			if (++frameDelay >= 3)
			{
				moveMod.movementAddend.x = Random.Range(-eletricityForce, eletricityForce);
				moveMod.movementAddend.z = Random.Range(-eletricityForce, eletricityForce);
				frameDelay = 0;
			}

			speedDelay -= ec.EnvironmentTimeScale * Time.deltaTime;
			if (speedDelay <= 0f)
			{
				speedDelay += delayToChangeSpeed;
				moveMod.movementMultiplier = Random.Range(minSpeedFactor, maxSpeedFactor);
			}
		}

		[SerializeField]
		internal AnimationComponent ani;

		[SerializeField]
		internal float eletricityForce = 5f;

		[SerializeField]
		internal float delayToChangeSpeed = 2.5f, minSpeedFactor = 0.75f, maxSpeedFactor = 1.25f;

		int frameDelay = 0;
		float speedDelay = 0f;
	}
}
