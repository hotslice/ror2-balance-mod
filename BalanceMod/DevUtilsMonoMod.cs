using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil.Cil;

namespace BalanceMod
{
    class DevUtilsMonoMod
    {
        //Finds all contiguous blocks of IL that match the list of instruction filters added
        public static List<int> FindCodeBlockIndexes(List<Instruction> input, List<InstructionFilter> search, int startIndex = 0, int count = 0)
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
                if (input[inputIndex].OpCode == search[0].opcode)
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
                        if (searchVal.opcode != inputVal.OpCode)
                        {
                            match = false;
                            break;
                        }
                        if (searchVal.operandType != String.Empty)
                        {
                            if (searchVal.operandType == "null")
                            {
                                if (inputVal.Operand != null)
                                {
                                    //BalanceMod.Logger.LogError($"Operand not null {searchVal.operandType}");
                                    match = false;
                                    break;
                                }
                                else continue;

                            }
                            if (searchVal.operandType != GetType(inputVal.Operand))
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
                            //if (!TypeEqualFuncDict.TryGetValue(searchVal.operandType, out var equalityComparisonFunc))
                            //{
                            //    BalanceMod.Logger.LogError($"Did not find equality comparison func for type {searchVal.operandType}");
                            //    match = false;
                            //    break;
                            //}
                            bool operandEquals = false;
                            try
                            {
                                operandEquals = GetEqualityComparisonFunc(searchVal.operandType)(searchVal.value, inputVal.Operand);
                            }
                            catch
                            {
                                BalanceMod.Logger.LogError($"Equality comparer for type {searchVal.operandType} threw exception");
                            }
                            if (!operandEquals)
                            {
                                //BalanceMod.Logger.LogError($"Did not match {searchVal.value} == {inputVal.Operand} at line {inputIndex} | {searchIndex}");
                                match = false;
                                break;
                            }
                            else
                            {
                                //BalanceMod.Logger.LogError($"Matched {searchVal.value} == {inputVal.Operand}");
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

        public static List<string> GenerateToLogInstructionFilterCodeFromIndex(List<Instruction> input, int startIndex = 0, int count = 0)
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
                if (!opcodeDict.TryGetValue(input[index].OpCode.Name, out var opcodeString))
                {
                    value = $"Failed to find opcode field for ${input[index].OpCode.Name}";
                    result.Add(value);
                    BalanceMod.Logger.Log(LogLevel.Error, value);
                    continue;
                }
                string operandString = GetType(input[index].Operand);
                if (operandString == "null" || operandString == String.Empty)
                {

                    value = $"new InstructionFilter(Mono.Cecil.Cil.OpCodes.{opcodeString}, \"{operandString}\"),";
                }
                else
                {
                    value = $"new InstructionFilter(Mono.Cecil.Cil.OpCodes.{opcodeString}, \"{operandString}\", \"{input[index].Operand.ToString()}\"),";
                }
                BalanceMod.Logger.Log(LogLevel.Error, value);
                result.Add(value);
            }
            return result;
        }

        private static Func<object, object, bool> GetEqualityComparisonFunc(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return (object a, object b) => { return true; };
            }
            if (TypeEqualFuncDict.TryGetValue(type, out var func))
            {
                return func;
            }
            return TypeEqualFuncDict["System.String"];
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
    }
    public class InstructionFilter
    {
        //this field is not optional
        public OpCode opcode;

        //if you want to match null operand, set type to null or "null"
        //if you don't care about operand type, set type to empty string
        public string operandType;

        //if you don't care what the value is, set value to null
        public object value = null;

        public InstructionFilter(OpCode opc, string oprType)
        {
            opcode = opc;
            operandType = oprType;
        }
        public InstructionFilter(OpCode opc, string oprType, object val)
        {
            opcode = opc;
            operandType = oprType;
            value = val;
        }
    }
}
