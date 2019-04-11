using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Harmony;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.Projectile;
using System.Reflection;
using UnityEngine;
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
            // PatchGestureOfTheDrowned();
        }

        //public static void PatchGestureOfTheDrowned()
        //{
        //    On.RoR2.Inventory.FixedUpdate += (orig, self) =>
        //    {
        //        if (self.GetEquipmentSlotCount() > 0)
        //        {
        //            EquipmentState[] equipmentStates = ((EquipmentState[])AccessTools.Field(AccessTools.TypeByName("RoR2.Inventory"), "equipmentStateSlots").GetValue(self));
        //            if (equipmentStates.Length > 0)
        //            {
        //                var equipmentState = equipmentStates[0];
        //                Debug.Log($"charges {equipmentState.charges} finish {equipmentState.chargeFinishTime.isPositiveInfinity}");
        //            }
        //        }
        //    };
        //    //when you go from empty slot -> first equipment, charges = 1 and ispositiveinfinity = false
        //    //On.RoR2.EquipmentSlot.FixedUpdate += (orig, self) =>
        //    //{
        //    //    Debug.Log("FixedUpdate");
        //    //    orig(self);
        //    //};
        //}

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
        public static bool didSetArtificerFireBoltDamageCoefficient = false;
        public static Lazy<GameObject> LazyFireBoltProjectilePrefab { get; } = new Lazy<GameObject>(() => (GameObject)(AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "projectilePrefab").GetValue(null)));
        //public static Lazy<GameObject> lazyFireBoltProjectilePrefab { get; } = new Lazy<GameObject>(() => (GameObject)(Type.GetType("EntityStates.Mage.Weapon.FireBolt").GetField("projectilePrefab").GetValue(null)));
        public static Action<ProjectileController, FireProjectileInfo> orig_ProjectileManager_InitializeProjectile;

        public static void PatchArtificerFireboltCoefficient()
        {
            if (!BalanceMod.ArtificerM1CoeffBuffEnabled.Value)
            {
                return;
            }
            On.EntityStates.Mage.Weapon.FireBolt.FireGauntlet += (orig, self) =>
            {
                if(!didSetArtificerFireBoltDamageCoefficient)
                {
                    didSetArtificerFireBoltDamageCoefficient = true;
                    var damageCoeff = (float)AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "damageCoefficient").GetValue(null);
                    Debug.Log($"Artificer M1 damage was {damageCoeff}, setting to 100%");
                    AccessTools.Field(AccessTools.TypeByName("EntityStates.Mage.Weapon.FireBolt"), "damageCoefficient").SetValue(null, 1.0f);
                }
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
                var matchingLocations = DevUtilsMonoMod.FindCodeBlockIndexes(il.Body.Instructions.ToList(), igniteCodeBlock, 184);

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
    }
}