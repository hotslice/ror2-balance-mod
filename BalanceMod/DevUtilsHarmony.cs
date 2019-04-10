using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using Harmony;

namespace BalanceMod
{
    class DevUtilsHarmony
    {
        public static string GetType(object o)
        {
            if (o == null)
            {
                return "null";
            }
            else
            {
                return o.GetType().ToString();
            }
        }

        //Finds all contiguous blocks of IL that match the list of instruction filters added
        public static List<int> FindCodeBlockIndexes(List<CodeInstruction> input, List<InstructionFilterHarmony> search, int startIndex = 0, int count = 0)
        {
            var result = new List<int>();

            search.ForEach(x => { if (x.operandType == null) x.operandType = "null"; });

            if (startIndex < 0) startIndex = 0;
            int endIndex;
            if (count == 0)
            {
                endIndex = input.Count;
            }
            else
            {
                endIndex = startIndex + count > input.Count ? input.Count : startIndex + count;
            }

            for (int inputIndex = startIndex; inputIndex < endIndex; inputIndex++)
            {
                if (input[inputIndex].opcode == search[0].opcode)
                {
                    bool match = true;
                    for (int searchIndex = 0; searchIndex < search.Count; searchIndex++)
                    {
                        if (inputIndex + searchIndex > input.Count)
                        {
                            match = false;
                            break;
                        }
                        var inputVal = input[inputIndex + searchIndex];
                        var searchVal = search[searchIndex];
                        if (searchVal.opcode != inputVal.opcode)
                        {
                            match = false;
                            break;
                        }
                        if (searchVal.operandType != String.Empty)
                        {
                            if (searchVal.operandType == "null")
                            {
                                if (inputVal.operand != null)
                                {
                                    //BalanceMod.Logger.LogError($"Operand not null {searchVal.operandType}");
                                    match = false;
                                    break;
                                }
                                else continue;

                            }
                            if (searchVal.operandType != GetType(inputVal.operand))
                            {
                                //BalanceMod.Logger.LogError($"Operand type did not match {searchVal.operandType}");
                                match = false;
                                break;
                            }
                            if (searchVal.value == null)
                            {
                                //BalanceMod.Logger.LogError($"Operand null {searchVal.operandType}");
                                continue;
                            }
                            if (!TypeEqualFuncDict.TryGetValue(searchVal.operandType, out var equalityComparisonFunc))
                            {
                                BalanceMod.Logger.LogError($"Did not find equality comparison func for type {searchVal.operandType}");
                                match = false;
                                break;
                            }
                            bool operandEquals = false;
                            try
                            {
                                operandEquals = equalityComparisonFunc(searchVal.value, inputVal.operand);
                            }
                            catch
                            {
                                BalanceMod.Logger.LogError($"Equality comparer for type {searchVal.operandType} threw exception");
                            }
                            if (!operandEquals)
                            {
                                //BalanceMod.Logger.LogError($"Did not match {searchVal.value} == {inputVal.operand} at line {inputIndex} | {searchIndex}");
                                match = false;
                                break;
                            }
                            else
                            {
                                //BalanceMod.Logger.LogError($"Matched {searchVal.value} == {inputVal.operand}");
                            }
                        }

                    }
                    if (match)
                    {
                        result.Add(inputIndex);
                    }
                }
            }
            return result;
        }

        public static List<string> GenerateToLogInstructionFilterCodeFromIndex(List<CodeInstruction> input, int startIndex = 0, int count = 0)
        {
            var result = new List<string>();

            if (startIndex < 0) startIndex = 0;
            int endIndex;
            if (count == 0)
            {
                endIndex = input.Count;
            }
            else
            {
                endIndex = startIndex + count > input.Count ? input.Count : startIndex + count;
            }
            var opcodeDict = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
              .Where(f => f.FieldType == typeof(OpCode))
              .ToDictionary(f => ((OpCode)f.GetValue(null)).Name, f => f.Name);
            for (int index = startIndex; index < endIndex; index++)
            {
                string value;
                if (!opcodeDict.TryGetValue(input[index].opcode.Name, out var opcodeString))
                {
                    value = $"Failed to find opcode field for ${input[index].opcode.Name}";
                    result.Add(value);
                    BalanceMod.Logger.Log(LogLevel.Error, value);
                    continue;
                }
                string operandString = GetType(input[index].operand);
                if (operandString == "null" || operandString == String.Empty)
                {

                    value = $"new InstructionFilter(OpCodes.{opcodeString}, \"{operandString}\"),";
                }
                else
                {
                    value = $"new InstructionFilter(OpCodes.{opcodeString}, \"{operandString}\", {input[index].operand.ToString()}),";
                }
                BalanceMod.Logger.Log(LogLevel.Error, value);
                result.Add(value);
            }
            return result;
        }

        private static Dictionary<string, Func<object, object, bool>> TypeEqualFuncDict = new Dictionary<string, Func<object, object, bool>>()
        {
            {"System.Boolean", (a, b) => Convert.ToBoolean(a) == Convert.ToBoolean(b) },
            {"System.Byte", (a, b) => Convert.ToByte(a) == Convert.ToByte(b) },
            {"System.SByte", (a, b) => Convert.ToSByte(a) == Convert.ToSByte(b) },
            {"System.Char", (a, b) => Convert.ToChar(a) == Convert.ToChar(b) },
            {"System.Decimal", (a, b) => Convert.ToDecimal(a) == Convert.ToDecimal(b) },
            {"System.Double", (a, b) => Convert.ToDouble(a) == Convert.ToDouble(b) },
            {"System.Single", (a, b) => Convert.ToSingle(a) == Convert.ToSingle(b) },
            {"System.Int32", (a, b) => Convert.ToInt32(a) == Convert.ToInt32(b) },
            {"System.UInt32", (a, b) => Convert.ToUInt32(a) == Convert.ToUInt32(b) },
            {"System.Int64", (a, b) => Convert.ToInt64(a) == Convert.ToInt64(b) },
            {"System.UInt64", (a, b) => Convert.ToUInt64(a) == Convert.ToUInt64(b) },
            {"System.Int16", (a, b) => Convert.ToInt16(a) == Convert.ToInt16(b) },
            {"System.UInt16", (a, b) => Convert.ToUInt16(a) == Convert.ToUInt16(b) },
            {"System.String", (a, b) => Convert.ToString(a) == Convert.ToString(b) }, //string comparison might not work idk
        };
    }
    public class InstructionFilterHarmony
    {
        //this field is not optional
        public OpCode opcode;

        //if you want to match null operand, set type to null or "null"
        //if you don't care about operand type, set type to empty string
        public string operandType;

        //if you don't care what the value is, set value to null
        public object value = null;

        public InstructionFilterHarmony(OpCode opc, string oprType)
        {
            opcode = opc;
            operandType = oprType;
        }
        public InstructionFilterHarmony(OpCode opc, string oprType, object val)
        {
            opcode = opc;
            operandType = oprType;
            value = val;
        }
    }
}
