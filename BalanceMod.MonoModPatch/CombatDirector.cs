using MonoMod;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
#pragma warning disable IDE1006 // Naming Styles

namespace RoR2
{

    class patch_CombatDirector : CombatDirector
    {
        [MonoModAdded]
        public bool AlternateSpawnBehavior(GameObject spawnTarget, bool canBeElite)
        {
            if (this.currentMonsterCard.CardIsValid() && this.monsterCredit >= (float)this.currentMonsterCard.cost)
            {
                SpawnCard spawnCard = this.currentMonsterCard.spawnCard;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    spawnOnTarget = spawnTarget.transform,
                    preventOverhead = this.currentMonsterCard.preventOverhead
                };
                DirectorCore.GetMonsterSpawnDistance(this.currentMonsterCard.spawnDistance, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                directorPlacementRule.minDistance *= this.spawnDistanceMultiplier;
                directorPlacementRule.maxDistance *= this.spawnDistanceMultiplier;
                GameObject gameObject = DirectorCore.instance.TrySpawnObject(spawnCard, directorPlacementRule, this.rng);
                if (gameObject)
                {
                    int cost = this.currentMonsterCard.cost;
                    float num3 = 1f;
                    float num4 = 1f;
                    CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
                    GameObject bodyObject = component.GetBodyObject();
                    CharacterBody characterBody = bodyObject.GetComponent<CharacterBody>();
                    if (this.isBoss)
                    {
                        if (!this.bossGroup)
                        {
                            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/BossGroup"));
                            NetworkServer.Spawn(gameObject2);
                            this.bossGroup = gameObject2.GetComponent<BossGroup>();
                            this.bossGroup.dropPosition = this.dropPosition;
                        }
                        this.bossGroup.AddMember(component);
                    }
                    // assumes CombatDirector.maximumNumberToSpawnBeforeSkipping == 4f; maybe add a check for this?
                    if (canBeElite) //try to make the mob elite before adding bonus items
                    {
                        if((float)cost * CombatDirector.eliteMultiplierCost <= this.monsterCredit)
                        {
                            num3 = 4.7f;
                            num4 = 2f;
                            component.inventory.SetEquipmentIndex(EliteCatalog.GetEliteDef(this.currentActiveEliteIndex).eliteEquipmentIndex);
                            cost = (int)((float)cost * CombatDirector.eliteMultiplierCost);

                            //This is where we add the bonus items
                            var minCost = ((int)this.monsterCredit) / 4;  //CombatDirector.maximumNumberToSpawnBeforeSkipping
                            while (cost < minCost)
                            {
                                var newItem = BalanceMod.Hooks.mobItemSelection.Evaluate(this.rng.nextNormalizedFloat);
                                var itemIndex = newItem.itemIndex;
                                var itemCount = newItem.itemCount;
                                // Debug.Log($"Giving {itemCount} of item {itemIndex}");
                                if (BalanceMod.Hooks.PatchLateGameMonsterSpawns_EnemyItemsInChat)
                                {
                                    Chat.AddPickupMessage(characterBody, ItemCatalog.GetItemDef(itemIndex).nameToken, BalanceMod.Hooks.GetItemColor(itemIndex), (uint)itemCount);
                                }
                                component.inventory.GiveItem(itemIndex, itemCount);
                                cost *= 2;
                            }
                        }
                        
                    }
                    else
                    {   //This is where we add the bonus items
                        var minCost = ((int)this.monsterCredit) / 4;  //CombatDirector.maximumNumberToSpawnBeforeSkipping
                        while (cost < minCost)
                        {
                            var newItem = BalanceMod.Hooks.mobItemSelection.Evaluate(this.rng.nextNormalizedFloat);
                            var itemIndex = newItem.itemIndex;
                            var itemCount = newItem.itemCount;
                            // Debug.Log($"Giving {itemCount} of item {itemIndex}");
                            if (BalanceMod.Hooks.PatchLateGameMonsterSpawns_EnemyItemsInChat)
                            {
                                Chat.AddPickupMessage(characterBody, ItemCatalog.GetItemDef(itemIndex).nameToken, BalanceMod.Hooks.GetItemColor(itemIndex), (uint)itemCount);
                            }
                            component.inventory.GiveItem(itemIndex, itemCount);
                            cost *= 2;
                        }
                    }
                    this.monsterCredit -= (float)cost;
                    if (this.isBoss)
                    {
                        int livingPlayerCount = Run.instance.livingPlayerCount;
                        num3 *= Mathf.Pow((float)livingPlayerCount, 1f);
                    }
                    //elites have +400% hp and +100% damage
                    component.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((num3 - 1f) * 10f));
                    component.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((num4 - 1f) * 10f));
                    DeathRewards component2 = bodyObject.GetComponent<DeathRewards>();
                    if (component2)
                    {
                        component2.expReward = (uint)((float)cost * this.expRewardCoefficient * Run.instance.compensatedDifficultyCoefficient);
                        component2.goldReward = (uint)((float)cost * this.expRewardCoefficient * 2f * Run.instance.compensatedDifficultyCoefficient);
                    }
                    if (this.spawnEffectPrefab && NetworkServer.active)
                    {
                        Vector3 origin = gameObject.transform.position;
                        if (characterBody)
                        {
                            origin = characterBody.corePosition;
                        }
                        EffectManager.instance.SpawnEffect(this.spawnEffectPrefab, new EffectData
                        {
                            origin = origin
                        }, true);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
