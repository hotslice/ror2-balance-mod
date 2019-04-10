using MonoMod;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2
{
	public class patch_DotController : DotController
	{
        [MonoModAdded]
		[Server]
		private void AddDotModBurnDamageFix(GameObject attackerObject, float duration, DotIndex dotIndex, float damageMultiplier, float damageThatProccedIt)
		{
            if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::AddDot(UnityEngine.GameObject,System.Single,RoR2.DotController/DotIndex,System.Single)' called on client");
				return;
			}

			if (dotIndex < DotIndex.Bleed || dotIndex >= DotIndex.Count)
			{
				return;
			}

			TeamIndex teamIndex = TeamIndex.Neutral;
			float single = 0f;
			TeamComponent component = attackerObject.GetComponent<TeamComponent>();
			if (component)
			{
				teamIndex = component.teamIndex;
			}

			CharacterBody characterBody = attackerObject.GetComponent<CharacterBody>();
			if (characterBody)
			{
				single = characterBody.damage;
			}

			DotDef dotDef = dotDefs[(int)dotIndex];
			DotStack dotStack = new DotStack
			{
				dotIndex = dotIndex,
				dotDef = dotDef,
				attackerObject = attackerObject,
				attackerTeam = teamIndex,
				timer = duration,
				damageType = DamageType.Generic
			};
			if (teamIndex != TeamIndex.Monster || damageThatProccedIt == 0f)
			{
				dotStack.damage = dotDef.damageCoefficient * single * damageMultiplier;
			}
			else
			{
                dotStack.damage = dotDef.damageCoefficient * damageThatProccedIt * damageMultiplier * 0.4f;
			}

			if (dotIndex == DotIndex.Helfire)
			{
				if (!characterBody)
				{
					return;
				}

				HealthComponent healthComponent = characterBody.healthComponent;
				if (!healthComponent)
				{
					return;
				}

				dotStack.damage = healthComponent.fullHealth * 0.01f * damageMultiplier;
				if (victimObject == attackerObject)
				{
					DotStack dotStack1 = dotStack;
					dotStack1.damageType = dotStack1.damageType | DamageType.NonLethal | DamageType.Silent;
				}
				else if (victimTeam != teamIndex)
				{
					dotStack.damage *= 24f;
				}
				else
				{
					dotStack.damage *= 0.5f;
				}

				int num = 0;
				int count = dotStackList.Count;
				while (num < count)
				{
					if (dotStackList[num].dotIndex == DotIndex.Helfire && dotStackList[num].attackerObject == attackerObject)
					{
						dotStackList[num].timer = Mathf.Max(dotStackList[num].timer, duration);
						dotStackList[num].damage = dotStack.damage;
						return;
					}

					num++;
				}

				if (victimBody)
				{
					EffectManager.instance.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/HelfireIgniteEffect"), new EffectData
					{
						origin = victimBody.corePosition
					}, true);
				}
			}

			dotStackList.Add(dotStack);
		}

		[MonoModAdded]
		[Server]
		public static void InflictDotModBurnDamageFix(GameObject victimObject, GameObject attackerObject, DotIndex dotIndex, float duration = 8f, float damageMultiplier = 1f, float damageThatProccedIt = 0f)
		{
            if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::InflictDot(UnityEngine.GameObject,UnityEngine.GameObject,RoR2.DotController/DotIndex,System.Single,System.Single)' called on client");
				return;
			}

			if (victimObject && attackerObject)
			{
				if (!dotControllerLocator.TryGetValue(victimObject.GetInstanceID(), out var component))
				{
					GameObject gameObject = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkedObjects/DotController"));
					component = gameObject.GetComponent<DotController>();
					component.victimObject = victimObject;
					component.recordedVictimInstanceId = victimObject.GetInstanceID();
					dotControllerLocator.Add(component.recordedVictimInstanceId, component);
					NetworkServer.Spawn(gameObject);
				}

				
				((patch_DotController)component).AddDotModBurnDamageFix(attackerObject, duration, dotIndex, damageMultiplier, damageThatProccedIt);
			}
		}
	}
}