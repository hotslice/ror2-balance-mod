using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Harmony;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.Projectile;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using MonoMod.Cil;

namespace BalanceMod
{
    public static class Hooks
	{
		public static void Hook()
        {
            PatchArtificerBaseMoveSpeed();
            PatchArtificerFireboltCoefficient();
            PatchBlazingDoTDamage();
            PatchWakeOfVultures();
            PatchItemProcDamageScaling();
            PatchGestureOfTheDrowned();
            PatchLateGameMonsterSpawns();
        }

        #region Patch late game monster spawns
        public class MobItemInfo
        {
            public ItemIndex itemIndex;
            public int itemCount;
            public MobItemInfo(ItemIndex index, int count)
            {
                itemIndex = index;
                itemCount = count;
            }
        }

        public static WeightedSelection<MobItemInfo> mobItemSelection;
        public static void InitializeMobItemSelection()
        {
            mobItemSelection = new WeightedSelection<MobItemInfo>();
            //White
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Syringe, 5), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Bear, 3), 1f);
            //Tooth - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.CritGlasses, 2), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Hoof, 7), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Crowbar, 1), 1f);
            //Mushroom - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.BleedOnHit, 2), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.WardOnLevel, 10), 1f); // buffs the mobs only :D
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.HealWhileSafe, 1), 1f);
            //PersonalShield - not sure how to get an appropriate number of these
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Medkit, 1000), 1f);
            //IgniteOnKill - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.StunChanceOnHit, 1), 1f); // this is gonna make people mad
            //Firework - does nothing
            //SprintBonus - does nothing
            //SecondarySkillMagazine - probably does nothing
            //StickyBomb - too scary
            //TreasureCache - does nothing
            //BossDamageBonus - does nothing

            //Green
            //Missile - too scary
            //ExplodeOnDeath - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Feather, 2), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.ChainLightning, 1), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Seed, 10000), 1f); // maybe not enough
            //AttackSpeedOnCrit - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.SprintOutOfCombat, 3), 1f);
            //FallBoots - does nothing
            //CooldownOnCrit - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Phasing, 1), 1f);
            //HealOnCrit - does nothing
            //TempestOnKill - does nothing
            //EquipmentMagazine - does nothing
            //Infusion - does nothing
            //Bandolier - does nothing
            //WarCryOnMultiKill - does nothing
            //SprintArmor - does nothing
            //IceRing - too scary
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.FireRing, 1), 1f); //should be ok as players can run out of the aoe
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.SlowOnHit, 1), 1f);
            //JumpBoost - does nothing

            //Red
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Behemoth, 1), 1f);
            //Dagger - does nothing
            //Icicle - does nothing
            //GhostOnKill - does nothing
            //NovaOnHeal - N'kuhana's Opinion, either does nothing or too scary
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.ShockNearby, 1), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Clover, 1), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.BounceNearby, 5), 1f);
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.AlienHead, 1), 1f);
            //Talisman - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.ExtraLife, 1), 1f); //lul
            //ExtraLifeConsumed - does nothing
            //UtilitySkillMagazine - does nothing
            //HeadHunter - does nothing
            //KillEliteFrenzy - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.IncreaseHealing, 1), 1f);

            //Blue
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.LunarDagger, 1), 1f); //shaped glass
            //GoldOnHit - does nothing
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.ShieldOnly, 1), 1f);
            //new MobItemInfo(ItemIndex.CritHeal, 1), 1); //not actually corpsebloom
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.RepeatHeal, 1), 1f); //corpsebloom
            //AutoCastEquipment - hmm, might add equipment later

            //Other
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.BoostHp, 10), 1f); //boring
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.BoostDamage, 10), 1f); //zzz
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.Knurl, 10), 1f); //might not be enough
            //BeetleGland - meh
            mobItemSelection.AddChoice(new MobItemInfo(ItemIndex.BurnNearby, 1), 1f); //RIGHTEOUS FIYAH
            //HealthDecay - no
            //DrizzlePlayerHelper - idk

            //Not implemented
            //AACannon, PlasmaCore, LevelBonus, MageAttunement, WarCryOnCombat, CrippleWardOnLevel, Ghost
        }

        public static bool PatchLateGameMonsterSpawns_Activated = false;
        public static float PatchLateGameMonsterSpawns_BeginCreditThreshold = 15000f; //set to 0f to begin at start of game
        public static bool PatchLateGameMonsterSpawns_EnemyItemsInChat = false;
        public static void PatchLateGameMonsterSpawns()
        {
            if(!BalanceMod.LateGameEnemyItemsEnabled.Value)
            {
                return;
            }
            //PatchLateGameMonsterSpawns_BeginCreditThreshold = BalanceMod.LateGameEnemyItemsActivationThreshold.Value;
            PatchLateGameMonsterSpawns_EnemyItemsInChat = BalanceMod.EnemyItemsInChatEnabled.Value;
            InitializeMobItemSelection();
            On.RoR2.Run.Start += (orig, self) =>
            {
                PatchLateGameMonsterSpawns_Activated = false;
                orig(self);
            };

            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += (il) =>
            {
                var c = new ILCursor(il).GotoNext(x => x.MatchStloc(0));
                //c.GotoNext();
                //c.Emit(OpCodes.Ldc_I4_1);
                //c.Emit(OpCodes.Stloc_0); //set canBeElite to true - uncomment these to force allow elite worms
                c.GotoNext(x => x.MatchStloc(1));
                c.GotoNext();

                c.Emit(OpCodes.Ldarg_0); //load self onto the stack
                c.EmitDelegate<Func<CombatDirector, bool>>((self) =>
                {
                    if (PatchLateGameMonsterSpawns_Activated)
                        return true;
                    if(self.monsterCredit >= PatchLateGameMonsterSpawns_BeginCreditThreshold)
                    {
                        // Debug.Log("Monsters can now start spawning with items.");
                        Chat.AddMessage("Monsters can now start spawning with items.");
                        PatchLateGameMonsterSpawns_Activated = true;
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brfalse_S, c.Next); //jump to normal behavior

                c.Emit(OpCodes.Ldarg_0); //load self onto the stack
                c.Emit(OpCodes.Ldarg_1); //load spawnTarget onto the stack
                c.Emit(OpCodes.Ldloc_0); //load canBeElite onto the stack
                c.Emit(OpCodes.Call, il.Method.Module.ImportReference(typeof(CombatDirector).GetMethod("AlternateSpawnBehavior")));
                c.Emit(OpCodes.Ret); //return result of delegate
            };
            
            BalanceMod.Logger.LogInfo("Patched: Enemies can spawn with items in long games.");
            if(PatchLateGameMonsterSpawns_EnemyItemsInChat)
            {
                BalanceMod.Logger.LogInfo("         Enemy items will appear in chat.");
            }
        }
        #endregion

        #region Gesture of the Drowned infinite spam bug fix
        public static void PatchGestureOfTheDrowned()
        {
            if(!BalanceMod.GestureOfTheDrownedFixEnabled.Value)
            {
                return;
            }
            IL.RoR2.Inventory.SetEquipmentIndex += (il) =>
            {
                // DevUtilsMonoMod.GenerateToLogInstructionFilterCodeFromIndex(il.Body.Instructions.ToList(), 19);
                var setNewEquipmentStateBlock = new List<InstructionFilter>()
                {
                    //new InstructionFilter(OpCodes.Bne_Un_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"), //this branches to 22
                    //new InstructionFilter(OpCodes.Ldc_I4_1, "null"),
                    //new InstructionFilter(OpCodes.Stloc_1, "null"),
                    new InstructionFilter(OpCodes.Ldloca_S, "Mono.Cecil.Cil.VariableDefinition", "V_2"),
                    new InstructionFilter(OpCodes.Ldarg_1, "null"),
                    new InstructionFilter(OpCodes.Ldloc_0, "null"),
                    new InstructionFilter(OpCodes.Ldfld, "Mono.Cecil.FieldReference", "RoR2.Run/FixedTimeStamp RoR2.EquipmentState::chargeFinishTime"),
                    new InstructionFilter(OpCodes.Ldloc_1, "null"),
                    new InstructionFilter(OpCodes.Call, "Mono.Cecil.MethodReference", "System.Void RoR2.EquipmentState::.ctor(RoR2.EquipmentIndex,RoR2.Run/FixedTimeStamp,System.Byte)"),
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Ldloc_2, "null"),
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Byte RoR2.Inventory::get_activeEquipmentSlot()"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Void RoR2.Inventory::SetEquipment(RoR2.EquipmentState,System.UInt32)"),
                    new InstructionFilter(OpCodes.Ret, "null"),
                };
                var matchingLocations = DevUtilsMonoMod.FindCodeBlockIndexes(il.Body.Instructions.ToList(), setNewEquipmentStateBlock);

                if (matchingLocations.Count != 1)
                {
                    if (matchingLocations.Count == 0)
                    {
                        BalanceMod.Logger.LogError($"GestureOfTheDrownedFix not loaded - found no matches.");
                    }
                    if (matchingLocations.Count > 1)
                    {
                        BalanceMod.Logger.LogError($"GestureOfTheDrownedFix not loaded - found multiple matches. Line numbers follow:");
                        BalanceMod.Logger.LogError(string.Join(", ", matchingLocations));
                    }
                }
                else
                {
                    var chargeFinishTimeField = AccessTools.Field(typeof(RoR2.EquipmentState), "chargeFinishTime");
                    var equipmentStateCtor = AccessTools.Constructor(typeof(RoR2.EquipmentState), new Type[]
                    {
                        typeof(EquipmentIndex),
                        typeof(Run.FixedTimeStamp),
                        typeof(byte)
                    });
                    var getActiveEquipmentSlotMethod = AccessTools.Method(typeof(RoR2.Inventory), "get_activeEquipmentSlot");
                    var setEquipmentMethod = AccessTools.Method(typeof(RoR2.Inventory), "SetEquipment");

                    var gestureStartIdx = matchingLocations[0]; // == 22
                    var c = new ILCursor(il).Goto(gestureStartIdx);
                    /*22*/c.Emit(OpCodes.Ldloc_0);
                    c.GotoNext();
                    c.IncomingLabels.First().Target = c.Previous;
                    c.RemoveRange(8);
                    /*23*/c.Emit(OpCodes.Ldfld, chargeFinishTimeField);
                    /*24*/c.Emit(OpCodes.Stloc_2);
                    /*25*/c.Emit(OpCodes.Ldloca_S, (byte)2);
                    /*26*/c.Emit(OpCodes.Call, AccessTools.Method(typeof(RoR2.Run.FixedTimeStamp), "get_isNegativeInfinity"));
                    /*27*///c.Emit(OpCodes.Brfalse_S); //come back later and set this jump label
                    /*28*/c.Emit(OpCodes.Ldarg_0);
                    /*29*/c.Emit(OpCodes.Ldarg_1);
                    /*30*/c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(RoR2.Run.FixedTimeStamp), "positiveInfinity"));
                    /*31*/c.Emit(OpCodes.Ldloc_1);
                    /*32*/c.Emit(OpCodes.Newobj, equipmentStateCtor);
                    /*33*/c.Emit(OpCodes.Ldarg_0);
                    /*34*/c.Emit(OpCodes.Call, getActiveEquipmentSlotMethod);
                    /*35*/c.Emit(OpCodes.Call, setEquipmentMethod);
                    /*36*/c.Emit(OpCodes.Ret);
                    /*37*/c.Emit(OpCodes.Ldarg_0); var brkInst = c.Previous;
                    /*38*/c.Emit(OpCodes.Ldarg_1);
                    /*39*/c.Emit(OpCodes.Ldloc_0);
                    /*40*/c.Emit(OpCodes.Ldfld, chargeFinishTimeField);
                    /*41*/c.Emit(OpCodes.Ldloc_1);
                    /*42*/c.Emit(OpCodes.Newobj, equipmentStateCtor);
                    // c.Emit(OpCodes.Ldarg_0);
                    // call Inventory.get_ActiveEquipmentSlot()
                    // call Inventory.SetEquipment()
                    // ret

                    il.Body.Instructions.Insert(27, Instruction.Create(OpCodes.Brfalse_S, brkInst));
                    il.Body.Variables[2].VariableType = il.Import(typeof(RoR2.Run.FixedTimeStamp));
                    BalanceMod.Logger.LogInfo($"Patched: GestureOfTheDrownedFix loaded @ line {gestureStartIdx}.");
                }
            };

            /* Following is debug output used to diagnose where the bug was coming from */
            //bool ranOnce = false;
            //bool ranTwice = false;
            //On.RoR2.Inventory.UpdateEquipment += (orig, self) =>
            //{
            //    if(self.GetEquipmentSlotCount() > 0 && !ranTwice)
            //    {
            //        if(ranOnce)
            //        {
            //            ranTwice = true;
            //        }
            //        ranOnce = true;
            //        var equip = self.GetEquipment(0);
            //        //0 charges, isNegativeInfinity
            //        Debug.Log($"{equip.charges} {equip.chargeFinishTime.isNegativeInfinity} {equip.chargeFinishTime.isPositiveInfinity}");

            //        //when walking over
            //        //first run, 1 isNegativeInfinity
            //        //second run, 0 isPositiveInfinity

            //        //when picking up with E
            //        //first run, 0 isNegativeInfinity
            //        //second run, 0 isNegativeInfinity
            //    }
            //    orig(self);
            //};

            //On.RoR2.Inventory.SetEquipmentIndex += (orig, self, index) =>
            //{
            //    var oldEquip = self.GetEquipment(0u);
            //    //old charges 0
            //    //chargeFinishTime isNegativeInfinity
            //    Debug.Log($"{oldEquip.chargeFinishTime.isInfinity} {oldEquip.chargeFinishTime.isNegativeInfinity} {oldEquip.chargeFinishTime.isPositiveInfinity}");
            //    orig(self, index);
            //    var newEquip = self.GetEquipment(0u);
            //    //new charges 1
            //    //chargeFinishTime isNegativeInfinity
            //    Debug.Log($"{newEquip.chargeFinishTime.isInfinity} {newEquip.chargeFinishTime.isNegativeInfinity} {newEquip.chargeFinishTime.isPositiveInfinity}");
            //};

            //stock was 1, this was called repeatedly
            //On.RoR2.EquipmentSlot.Execute += (orig, self) =>
            //{
            //    Debug.Log($"this.stock: {self.stock}");
            //    orig(self);
            //};

            //On.RoR2.Inventory.FixedUpdate += (orig, self) =>
            //{
            //    if (self.GetEquipmentSlotCount() > 0)
            //    {
            //        EquipmentState[] equipmentStates = ((EquipmentState[])AccessTools.Field(AccessTools.TypeByName("RoR2.Inventory"), "equipmentStateSlots").GetValue(self));
            //        if (equipmentStates.Length > 0)
            //        {
            //            var equipmentState = equipmentStates[0];
            //            Debug.Log($"charges {equipmentState.charges} finish {equipmentState.chargeFinishTime.ToString()} pos {equipmentState.chargeFinishTime.isPositiveInfinity} neg {equipmentState.chargeFinishTime.isNegativeInfinity}");
            //        }
            //    }
            //    orig(self);
            //};
            //when you go from empty slot -> first equipment, charges = 1 and ispositiveinfinity = false
            //On.RoR2.EquipmentSlot.FixedUpdate += (orig, self) =>
            //{
            //    Debug.Log("FixedUpdate");
            //    orig(self);
            //};
        }
        #endregion

        #region Artificer base move speed improved to 9
        public static void PatchArtificerBaseMoveSpeed()
        {
            if(!BalanceMod.ArtificerMoveSpeedBuffEnabled.Value)
            {
                return;
            }
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self.isPlayerControlled && self.baseNameToken == "MAGE_BODY_NAME")
                {
                    self.baseMoveSpeed = 9f;
                }
                orig(self);
            };
            BalanceMod.Logger.LogInfo("Patched: Artificer move speed set to 9");
        }
        #endregion

        #region Artificer FireBolt (M1) damageCoefficient 2.2 -> 1.0, procCoefficient 0.2 -> 1.0
        public static Lazy<GameObject> LazyFireBoltProjectilePrefab { get; } = new Lazy<GameObject>(() => (GameObject)(AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "projectilePrefab").GetValue(null)));
        public static Action<ProjectileController, FireProjectileInfo> orig_ProjectileManager_InitializeProjectile;

        public static void PatchArtificerFireboltCoefficient()
        {
            if (!BalanceMod.ArtificerM1CoeffBuffEnabled.Value)
            {
                return;
            }
            On.RoR2.Run.Start += (orig, self) =>
            {
                var damageCoeff = (float)AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "damageCoefficient").GetValue(null);
                //Debug.Log($"Artificer M1 damage was {damageCoeff}, setting to 100%");
                AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "damageCoefficient").SetValue(null, 1.0f);
                orig(self);
            };
            // Threw an exception with HookGen hook, Detour, and Hook. Only NativeDetour worked
            // This is fixed in future versions of MonoMod and will be updated soon to use a Hook
            IDetour h = new NativeDetour(typeof(ProjectileManager).GetMethod("InitializeProjectile", BindingFlags.Static | BindingFlags.NonPublic).GetNativeStart(),
                typeof(Hooks).GetMethod("ProjectileManager_InitializeProjectilePrefix", BindingFlags.Static | BindingFlags.Public));
            orig_ProjectileManager_InitializeProjectile = h.GenerateTrampoline<Action<ProjectileController, FireProjectileInfo>>();
            BalanceMod.Logger.LogInfo("Patched: Artificer M1 damageCoeff set to 1.0, procCoeff set to 1.0");
        }
        
        public static void ProjectileManager_InitializeProjectilePrefix(ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            if (fireProjectileInfo.projectilePrefab == LazyFireBoltProjectilePrefab.Value)
            {
                projectileController.procCoefficient = 1.0f;
            }
            orig_ProjectileManager_InitializeProjectile(projectileController, fireProjectileInfo);
        }
        #endregion

        #region Nerf Blazing elite damage over time
        public static void PatchBlazingDoTDamage()
        {
            if(!BalanceMod.BlazingDoTFixEnabled.Value)
            {
                return;
            }
            IL.RoR2.GlobalEventManager.OnHitEnemy += (il) =>
            {
                // DevUtilsMonoMod.GenerateToLogInstructionFilterCodeFromIndex(il.Body.Instructions.ToList(), 184, 23);
                var igniteCodeBlock = new List<InstructionFilter>
                {
                    new InstructionFilter(OpCodes.Ldc_I4_S, "System.SByte", "27"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Boolean RoR2.CharacterBody::HasBuff(RoR2.BuffIndex)"),
                    new InstructionFilter(OpCodes.Brtrue_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldc_I4_0, "null"),
                    new InstructionFilter(OpCodes.Br_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldc_I4_1, "null"),
                    new InstructionFilter(OpCodes.Ldc_I4_0, "null"),
                    new InstructionFilter(OpCodes.Bgt_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldarg_1, "null"),
                    new InstructionFilter(OpCodes.Ldfld, "Mono.Cecil.FieldReference", "RoR2.DamageType RoR2.DamageInfo::damageType"),
                    new InstructionFilter(OpCodes.Ldc_I4, "System.Int32", "128"),
                    new InstructionFilter(OpCodes.And, "null"),
                    new InstructionFilter(OpCodes.Brfalse_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldarg_2, "null"),
                    new InstructionFilter(OpCodes.Ldarg_1, "null"),
                    new InstructionFilter(OpCodes.Ldfld, "Mono.Cecil.FieldReference", "UnityEngine.GameObject RoR2.DamageInfo::attacker"),
                    new InstructionFilter(OpCodes.Ldc_I4_1, "null"),
                    new InstructionFilter(OpCodes.Ldc_R4, "System.Single", "4"),
                    new InstructionFilter(OpCodes.Ldarg_1, "null"),
                    new InstructionFilter(OpCodes.Ldfld, "Mono.Cecil.FieldReference", "System.Single RoR2.DamageInfo::procCoefficient"),
                    new InstructionFilter(OpCodes.Mul, "null"),
                    new InstructionFilter(OpCodes.Ldc_R4, "System.Single", "1"),
                    new InstructionFilter(OpCodes.Call, "Mono.Cecil.MethodReference", "System.Void RoR2.DotController::InflictDot(UnityEngine.GameObject,UnityEngine.GameObject,RoR2.DotController/DotIndex,System.Single,System.Single)"),
                };
                var matchingLocations = DevUtilsMonoMod.FindCodeBlockIndexes(il.Body.Instructions.ToList(), igniteCodeBlock); // 184

                if (matchingLocations.Count != 1)
                {
                    if (matchingLocations.Count == 0)
                    {
                        BalanceMod.Logger.LogError($"BlazingDoTFix not loaded - found no matches.");
                    }
                    if (matchingLocations.Count > 1)
                    {
                        BalanceMod.Logger.LogError($"BlazingDoTFix not loaded - found multiple matches. Line numbers follow:");
                        BalanceMod.Logger.LogError(string.Join(", ", matchingLocations));
                    }
                }
                else
                {
                    //This is the location of the DotController::InflictDot call
                    //We will change this to the patched InflictDotModBurnDamageFix
                    var burnDmgFixStartIdx = matchingLocations[0];
                    var burnDmgFixInsertIdx = burnDmgFixStartIdx + igniteCodeBlock.Count - 1;

                    //This method takes an additional parameter of the damage dealt by the hit
                    il.Body.Instructions.Insert(burnDmgFixInsertIdx, Instruction.Create(OpCodes.Ldarg_1));
                    il.Body.Instructions.Insert(burnDmgFixInsertIdx + 1, Instruction.Create(OpCodes.Ldfld,
                        il.Method.Module.ImportReference(typeof(DamageInfo).GetField("damage"))));

                    //Now overwrite the call
                    il.Body.Instructions[burnDmgFixInsertIdx + 2] = Instruction.Create(OpCodes.Call,
                        il.Method.Module.ImportReference(typeof(DotController).GetMethod("InflictDotModBurnDamageFix")));
                    BalanceMod.Logger.Log(LogLevel.Info, $"Patched: BlazingDoTFix loaded @ line {burnDmgFixStartIdx}.");
                }
            };
        }
        #endregion

        #region Wake of Vultures Fix
        public static void PatchWakeOfVultures()
        {
            if (!BalanceMod.WakeOfVulturesFixEnabled.Value)
            {
                return;
            }
            IL.RoR2.CharacterBody.RecalculateStats += (il) =>
            {
                // DevUtilsMonoMod.GenerateToLogInstructionFilterCodeFromIndex(il.Body.Instructions.ToList(), 256, 15);
                // DevUtilsMonoMod.GenerateToLogInstructionFilterCodeFromIndex(il.Body.Instructions.ToList(), 358, 9);
                var IfBlueAffix_HalveMaxHealth_CodeBlock = new List<InstructionFilter>
                {
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Ldc_I4_S, "System.SByte", "28"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Boolean RoR2.CharacterBody::HasBuff(RoR2.BuffIndex)"),
                    new InstructionFilter(OpCodes.Brfalse_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_27"),
                    new InstructionFilter(OpCodes.Ldc_R4, "System.Single", "0.5"),
                    new InstructionFilter(OpCodes.Mul, "null"),
                    new InstructionFilter(OpCodes.Stloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_27"),
                    new InstructionFilter(OpCodes.Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_27"),
                    new InstructionFilter(OpCodes.Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_23"),
                    new InstructionFilter(OpCodes.Div, "null"),
                    new InstructionFilter(OpCodes.Stloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_27"),
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_27"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Void RoR2.CharacterBody::set_maxHealth(System.Single)"),
                };
                var matchingLocations = DevUtilsMonoMod.FindCodeBlockIndexes(il.Body.Instructions.ToList(), IfBlueAffix_HalveMaxHealth_CodeBlock);
                if (matchingLocations.Count != 1)
                {
                    if (matchingLocations.Count == 0)
                    {
                        BalanceMod.Logger.LogError($"WakeofVultures1 not loaded - found no matches.");
                    }
                    if (matchingLocations.Count > 1)
                    {
                        BalanceMod.Logger.LogError($"WakeofVultures1 not loaded - found multiple matches. Line numbers follow:");
                        BalanceMod.Logger.LogError(string.Join(", ", matchingLocations));
                    }
                    return;
                }
                var wakeOfVultures_halfHealthIndex = matchingLocations[0];
                il.Body.Instructions.RemoveAt(wakeOfVultures_halfHealthIndex + 1); // remove the unnecessary BuffIndex parameter
                il.Body.Instructions[wakeOfVultures_halfHealthIndex + 1] = Instruction.Create(OpCodes.Call, //call to custom func IsBlueElite() instead
                    il.Method.Module.ImportReference(typeof(Hooks).GetMethod("IsBlueElite")));

                var IfBlueAffix_GainShield_CodeBlock = new List<InstructionFilter>
                {
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Ldc_I4_S, "System.SByte", "28"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Boolean RoR2.CharacterBody::HasBuff(RoR2.BuffIndex)"),
                    new InstructionFilter(OpCodes.Brfalse_S, "MonoMod.Cil.ILLabel", "MonoMod.Cil.ILLabel"),
                    new InstructionFilter(OpCodes.Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_30"),
                    new InstructionFilter(OpCodes.Ldarg_0, "null"),
                    new InstructionFilter(OpCodes.Callvirt, "Mono.Cecil.MethodReference", "System.Single RoR2.CharacterBody::get_maxHealth()"),
                    new InstructionFilter(OpCodes.Add, "null"),
                    new InstructionFilter(OpCodes.Stloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_30"),
                };
                matchingLocations = DevUtilsMonoMod.FindCodeBlockIndexes(il.Body.Instructions.ToList(), IfBlueAffix_GainShield_CodeBlock, wakeOfVultures_halfHealthIndex);
                if (matchingLocations.Count != 1)
                {
                    if (matchingLocations.Count == 0)
                    {
                        BalanceMod.Logger.LogError($"WakeofVultures2 not loaded - found no matches.");
                    }
                    if (matchingLocations.Count > 1)
                    {
                        BalanceMod.Logger.LogError($"WakeofVultures2 not loaded - found multiple matches. Line numbers follow:");
                        BalanceMod.Logger.LogError(string.Join(", ", matchingLocations));
                    }
                    return;
                }

                //The next fix is injected in the middle of the found block. Commented lines indicate existing unmodified instructions
                var wakeOfVultures_gainShieldIndex = matchingLocations[0];
                var idx = wakeOfVultures_gainShieldIndex + 7;
                var skipMulLabel = il.Body.Instructions[idx];
                // Ldarg_0, "null"
                // Ldc_I4_S, "System.SByte", "28"
                // Callvirt, "Mono.Cecil.MethodReference", "System.Boolean RoR2.CharacterBody::HasBuff(RoR2.BuffIndex)"
                // Brfalse_S
                // Ldloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_30"
                // Ldarg_0
                // Callvirt, "Mono.Cecil.MethodReference", "System.Single RoR2.CharacterBody::get_maxHealth()"

                var c = new ILCursor(il).Goto(idx);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Call, il.Import(typeof(CharacterBody).GetMethod("get_inventory")));
                c.Emit(OpCodes.Ldc_I4_S, (System.SByte)ItemIndex.HeadHunter);
                c.Emit(OpCodes.Callvirt, il.Import(typeof(Inventory).GetMethod("GetItemCount")));
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Ble_S, skipMulLabel);
                c.Emit(OpCodes.Ldc_R4, 0.5f);
                c.Emit(OpCodes.Mul);

                // Add (label: skipMulLabel)
                // Stloc_S, "Mono.Cecil.Cil.VariableDefinition", "V_30"),

                BalanceMod.Logger.LogInfo($"Patched: Wake of Vultures @ lines {wakeOfVultures_halfHealthIndex}, {wakeOfVultures_gainShieldIndex}");
            };
        }

        public static bool IsBlueElite(CharacterBody self)
        {
            return self.HasBuff(BuffIndex.AffixBlue) && self.inventory.GetItemCount(ItemIndex.HeadHunter) == 0;
        }
        #endregion

        #region Linear item damage scaling
        public static void PatchItemProcDamageScaling()
        {
            if (!BalanceMod.ItemBalanceEnabled.Value)
            {
                return;
            }
            On.RoR2.Util.OnHitProcDamage += (orig, damageThatProccedIt, baseDamage, damageCoefficient) =>
            {

                return Mathf.Max(1f, damageThatProccedIt * damageCoefficient);
            };
            BalanceMod.Logger.LogInfo("Patched: Item damage scaling to 100%");
        }
        #endregion

        #region Utility functions
        public static Color32 GetItemColor(ItemIndex index)
        {
            if (IsWhiteItem(index))
                return ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier1Item);
            if (IsGreenItem(index))
                return ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier2Item);
            if (IsRedItem(index))
                return ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier3Item);
            if (IsLunarItem(index))
                return ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem);
            if (IsBossItem(index))
                return ColorCatalog.GetColor(ColorCatalog.ColorIndex.BossItem);
            return Color.white;
        }

        public static bool IsWhiteItem(ItemIndex index)
        {
            return ItemCatalog.tier1ItemList.Contains(index);
        }

        public static bool IsGreenItem(ItemIndex index)
        {
            return ItemCatalog.tier2ItemList.Contains(index);
        }

        public static bool IsRedItem(ItemIndex index)
        {
            return ItemCatalog.tier3ItemList.Contains(index);
        }

        public static bool IsBossItem(ItemIndex index)
        {
            return index == ItemIndex.Knurl || index == ItemIndex.BeetleGland;
        }

        public static bool IsLunarItem(ItemIndex index)
        {
            return ItemCatalog.lunarItemList.Contains(index);
        }
        #endregion
    }
}