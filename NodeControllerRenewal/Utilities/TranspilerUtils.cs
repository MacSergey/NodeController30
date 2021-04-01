using HarmonyLib;
using NodeController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace KianCommons.Patches
{
    public static class TranspilerUtils
    {

        public const BindingFlags ALL = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty;

        public static string FullName(MethodBase m) =>
            m.DeclaringType.FullName + "::" + m.Name;

        /// <typeparam name="TDelegate">delegate type</typeparam>
        /// <returns>Type[] represeting arguments of the delegate.</returns>
        internal static Type[] GetParameterTypes<TDelegate>() where TDelegate : Delegate
            => typeof(TDelegate).GetMethod("Invoke").GetParameters().Select(p => p.ParameterType).ToArray();

        /// <summary>
        /// Gets directly declared method based on a delegate that has
        /// the same name and the same args as the target method.
        /// </summary>
        /// <param name="type">the class/type where the method is delcared</param>
        internal static MethodInfo DeclaredMethod<TDelegate>
            (Type type, bool throwOnError = true) where TDelegate : Delegate =>
            DeclaredMethod<TDelegate>(type, typeof(TDelegate).Name, throwOnError);

        /// <summary>
        /// Gets directly declared method based on a delegate that has
        /// the same name as the target method
        /// </summary>
        /// <param name="type">the class/type where the method is delcared</param>
        /// <param name="name">the name of the method</param>
        internal static MethodInfo DeclaredMethod<TDelegate>(Type type, string name, bool throwOnError = false)
            where TDelegate : Delegate
        {
            return type.GetMethod(name, binding: ReflectionHelpers.ALL_Declared, types: GetParameterTypes<TDelegate>(), throwOnError: true);
        }

        /// <summary>
        /// like DeclaredMethod but throws suitable exception if method not found.
        /// </summary>
        [Obsolete("use reflection helpers instead")]
        internal static MethodInfo GetMethod(Type type, string name) =>
            type.GetMethod(name, ReflectionHelpers.ALL_Declared)
            ?? throw new Exception($"Method not found: {type.Name}.{name}");

        internal static MethodInfo GetCoroutineMoveNext(Type declaringType, string name)
        {
            try
            {
                Type t = declaringType.GetNestedTypes(ALL)
                    .Single(_t => _t.Name.Contains($"<{name}>"));
                return ReflectionHelpers.GetMethod(t, "MoveNext");
            }
            catch (Exception ex)
            {
                var types = declaringType?.GetNestedTypes(ALL).Where(_t => _t.Name.Contains($"<{name}>")).Select(_t => _t.FullName);
                Mod.Logger.Error($"the following types contian '<{name}>': " + types.ToSTR(), ex);
                return null;
            }
        }

        public static List<CodeInstruction> ToCodeList(this IEnumerable<CodeInstruction> instructions)
        {
            var originalCodes = new List<CodeInstruction>(instructions);
            var codes = new List<CodeInstruction>(originalCodes); // is this redundant?
            return codes;
        }

        public static CodeInstruction GetLDArg(MethodBase method, string argName, bool throwOnError = true)
        {
            if (!throwOnError && !HasParameter(method, argName))
                return null;

            byte idx = (byte)GetArgLoc(method, argName);
            if (idx == 0)
                return new CodeInstruction(OpCodes.Ldarg_0);
            else if (idx == 1)
                return new CodeInstruction(OpCodes.Ldarg_1);
            else if (idx == 2)
                return new CodeInstruction(OpCodes.Ldarg_2);
            else if (idx == 3)
                return new CodeInstruction(OpCodes.Ldarg_3);
            else
                return new CodeInstruction(OpCodes.Ldarg_S, idx);
        }

        /// <returns>
        /// returns the argument location to be used in LdArg instruction.
        /// </returns>
        public static byte GetArgLoc(this MethodBase method, string argName)
        {
            byte idx = (byte)GetParameterLoc(method, argName);
            if (!method.IsStatic)
                idx++; // first argument is object instance.
            return idx;
        }

        /// <summary>
        /// Post condtion: for instnace method add one to get argument location
        /// </summary>
        public static byte GetParameterLoc(MethodBase method, string name)
        {
            var parameters = method.GetParameters();
            for (byte i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].Name == name)
                    return i;
            }
            throw new Exception($"did not found parameter with name:<{name}>");
        }

        public static bool HasParameter(MethodBase method, string name) => method.GetParameters().Any(p => p.Name == name);

        /// <summary>
        /// shortcut for a.opcode == b.opcode && a.operand == b.operand
        /// </summary>
        public static bool IsSameInstruction(this CodeInstruction a, CodeInstruction b)
        {
            if (a.opcode == b.opcode)
            {
                if (a.operand == b.operand)
                {
                    return true;
                }

                // This special code is needed for some reason because the == operator doesn't work on System.Byte
                return (a.operand is byte aByte && b.operand is byte bByte && aByte == bByte);
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Get the instruction to load the variable which is stored here.
        /// </summary>
        public static CodeInstruction BuildLdLocFromStLoc(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0)
                return new CodeInstruction(OpCodes.Ldloc_0);
            else if (instruction.opcode == OpCodes.Stloc_1)
                return new CodeInstruction(OpCodes.Ldloc_1);
            else if (instruction.opcode == OpCodes.Stloc_2)
                return new CodeInstruction(OpCodes.Ldloc_2);
            else if (instruction.opcode == OpCodes.Stloc_3)
                return new CodeInstruction(OpCodes.Ldloc_3);
            else if (instruction.opcode == OpCodes.Stloc_S)
                return new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
            else if (instruction.opcode == OpCodes.Stloc)
                return new CodeInstruction(OpCodes.Ldloc, instruction.operand);
            else
                throw new Exception("instruction is not stloc! : " + instruction);
        }

        public static CodeInstruction BuildStLocFromLdLoc(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0)
                return new CodeInstruction(OpCodes.Stloc_0);
            else if (instruction.opcode == OpCodes.Ldloc_1)
                return new CodeInstruction(OpCodes.Stloc_1);
            else if (instruction.opcode == OpCodes.Ldloc_2)
                return new CodeInstruction(OpCodes.Stloc_2);
            else if (instruction.opcode == OpCodes.Ldloc_3)
                return new CodeInstruction(OpCodes.Stloc_3);
            else if (instruction.opcode == OpCodes.Ldloc_S)
                return new CodeInstruction(OpCodes.Stloc_S, instruction.operand);
            else if (instruction.opcode == OpCodes.Ldloc)
                return new CodeInstruction(OpCodes.Stloc, instruction.operand);
            else
                throw new Exception($"instruction is not ldloc! : {instruction}");
        }

        internal static string IL2STR(this IEnumerable<CodeInstruction> instructions)
        {
            string ret = "";
            foreach (var code in instructions)
            {
                ret += code + "\n";
            }
            return ret;
        }

        public class InstructionNotFoundException : Exception
        {
            public InstructionNotFoundException() : base() { }
            public InstructionNotFoundException(string m) : base(m) { }
        }

        /// <param name="count">Number of occurances. Negative count searches backward</param>
        public static int Search(
            this List<CodeInstruction> codes,
            Func<CodeInstruction, bool> predicate,
            int startIndex = 0, int count = 1, bool throwOnError = true)
        {
            return codes.Search(
                (int i) => predicate(codes[i]),
                startIndex: startIndex,
                count: count,
                throwOnError: throwOnError);

        }

        /// <param name="count">negative count searches backward</param>
        public static int Search(
            this List<CodeInstruction> codes,
            Func<int, bool> predicate,
            int startIndex = 0, int count = 1, bool throwOnError = true)
        {
            if (count == 0)
                throw new ArgumentOutOfRangeException("count can't be zero");
            int dir = count > 0 ? 1 : -1;
            int counter = System.Math.Abs(count);
            int n = 0;
            int index = startIndex;

            for (; 0 <= index && index < codes.Count; index += dir)
            {
                if (predicate(index))
                {
                    if (++n == counter)
                        break;
                }
            }
            if (n != counter)
            {
                if (throwOnError == true)
                {
                    throw new InstructionNotFoundException($"count: found={n} requested={count}");
                }
                else
                {
                    Mod.Logger.Debug("Did not found instruction[s].\n" + Environment.StackTrace);
                    return -1;
                }
            }
            Mod.Logger.Debug("Found : \n" + new[] { codes[index], codes[index + 1] }.IL2STR());
            return index;
        }

        [Obsolete]
        public static int SearchInstruction(List<CodeInstruction> codes, CodeInstruction instruction, int index, int dir = +1, int counter = 1)
        {
            try
            {
                return SearchGeneric(codes, idx => IsSameInstruction(codes[idx], instruction), index, dir, counter);
            }
            catch (InstructionNotFoundException)
            {
                throw new InstructionNotFoundException(" Did not found instruction: " + instruction);
            }
        }

        public static int SearchGeneric(List<CodeInstruction> codes, Func<int, bool> predicate, int index, int dir = +1, int counter = 1, bool throwOnError = true)
        {

            int count = 0;
            for (; 0 <= index && index < codes.Count; index += dir)
            {
                if (predicate(index))
                {
                    if (++count == counter)
                        break;
                }
            }
            if (count != counter)
            {
                if (throwOnError == true)
                    throw new InstructionNotFoundException(" Did not found instruction[s].");
                else
                {
                    Mod.Logger.Debug("Did not found instruction[s].\n" + Environment.StackTrace);
                    return -1;
                }
            }
            Mod.Logger.Debug("Found : \n" + new[] { codes[index], codes[index + 1] }.IL2STR());
            return index;
        }

        [Obsolete("unreliable")]
        public static Label GetContinueLabel(List<CodeInstruction> codes, int index, int counter = 1, int dir = -1)
        {
            // continue command is in form of branch into the end of for loop.
            index = SearchGeneric(codes, idx => codes[idx].Branches(out _), index, dir: dir, counter: counter);
            return (Label)codes[index].operand;
        }

        [Obsolete("use harmoyn extension Branches() instead")]
        public static bool IsBR32(OpCode opcode)
        {
            return opcode == OpCodes.Br || opcode == OpCodes.Brtrue || opcode == OpCodes.Brfalse || opcode == OpCodes.Beq;
        }

        public static void MoveLabels(CodeInstruction source, CodeInstruction target)
        {
            // move labels
            var labels = source.labels;
            target.labels.AddRange((IEnumerable<Label>)labels);
            labels.Clear();
        }

        /// <summary>
        /// replaces one instruction at the given index with multiple instrutions
        /// </summary>
        public static void ReplaceInstructions(List<CodeInstruction> codes, CodeInstruction[] insertion, int index)
        {
            foreach (var code in insertion)
            {
                if (code == null)
                    throw new Exception("Bad Instructions:\n" + insertion.IL2STR());
            }

            Mod.Logger.Debug($"replacing <{codes[index]}>\nInsert between: <{codes[index - 1]}>  and  <{codes[index + 1]}>");

            MoveLabels(codes[index], insertion[0]);
            codes.RemoveAt(index);
            codes.InsertRange(index, insertion);

            Mod.Logger.Debug("Replacing with\n" + insertion.IL2STR());
            Mod.Logger.Debug("PEEK (RESULTING CODE):\n" + codes.GetRange(index - 4, insertion.Length + 8).IL2STR());
        }

        public static void InsertInstructions(List<CodeInstruction> codes, CodeInstruction[] insertion, int index, bool moveLabels = true)
        {
            foreach (var code in insertion)
            {
                if (code == null)
                    throw new Exception("Bad Instructions:\n" + insertion.IL2STR());
            }
            Mod.Logger.Debug($"Insert point:\n between: <{codes[index - 1]}>  and  <{codes[index]}>");

            MoveLabels(codes[index], insertion[0]);
            codes.InsertRange(index, insertion);

            Mod.Logger.Debug("\n" + insertion.IL2STR());
            Mod.Logger.Debug("PEEK:\n" + codes.GetRange(index - 4, insertion.Length + 12).IL2STR());
        }
    }

    internal static class TranspilerExtensions
    {
        public static void InsertInstructions(this List<CodeInstruction> codes, int index, CodeInstruction[] insertion, bool moveLabels = true)
        {
            TranspilerUtils.InsertInstructions(codes, insertion, index, moveLabels);
        }

        public static void InsertInstructions(this List<CodeInstruction> codes, int index, IEnumerable<CodeInstruction> insertion, bool moveLabels = true)
        {
            TranspilerUtils.InsertInstructions(codes, insertion.ToArray(), index, moveLabels);
        }

        public static void InsertInstructions(this List<CodeInstruction> codes, int index, CodeInstruction insertion, bool moveLabels = true)
        {
            TranspilerUtils.InsertInstructions(codes, new[] { insertion }, index, moveLabels);
        }


        /// <summary>
        /// replaces one instruction at the given index with multiple instrutions
        /// </summary>
        public static void ReplaceInstruction(this List<CodeInstruction> codes, int index, CodeInstruction[] insertion)
        {
            TranspilerUtils.ReplaceInstructions(codes, insertion, index);
        }

        public static void ReplaceInstruction(this List<CodeInstruction> codes, int index, IEnumerable<CodeInstruction> insertion)
        {
            TranspilerUtils.ReplaceInstructions(codes, insertion.ToArray(), index);
        }

        public static void ReplaceInstruction(this List<CodeInstruction> codes, int index, CodeInstruction insertion)
        {
            TranspilerUtils.ReplaceInstructions(codes, new[] { insertion }, index);
        }

        public static bool IsLdLoc(this CodeInstruction code, out int loc)
        {
            if (code.opcode == OpCodes.Ldloc_0)
                loc = 0;
            else if (code.opcode == OpCodes.Ldloc_1)
                loc = 1;
            else if (code.opcode == OpCodes.Ldloc_2)
                loc = 2;
            else if (code.opcode == OpCodes.Ldloc_3)
                loc = 3;
            else if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc)
            {
                if (code.operand is LocalBuilder lb)
                    loc = lb.LocalIndex;
                else
                    loc = (int)code.operand;
            }
            else
            {
                loc = -1;
                return false;
            }
            return true;
        }

        public static bool IsLdLoc(this CodeInstruction code, int loc)
        {
            if (!code.IsLdLoc(out int loc0))
                return false;
            return loc == loc0;
        }

        public static bool IsStLoc(this CodeInstruction code, out int loc)
        {
            if (code.opcode == OpCodes.Stloc_0)
                loc = 0;
            else if (code.opcode == OpCodes.Stloc_1)
                loc = 1;
            else if (code.opcode == OpCodes.Stloc_2)
                loc = 2;
            else if (code.opcode == OpCodes.Stloc_3)
                loc = 3;
            else if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Stloc)
            {
                if (code.operand is LocalBuilder lb)
                    loc = lb.LocalIndex;
                else
                    loc = (int)code.operand;
            }
            else
            {
                loc = -1;
                return false;
            }
            return true;
        }

        public static bool IsStLoc(this CodeInstruction code, int loc)
        {
            if (!code.IsStLoc(out int loc0))
                return false;
            return loc == loc0;
        }

        public static bool IsLdLoc(this CodeInstruction code, Type type)
        {
            return code.IsLdloc()
                && code.operand is LocalBuilder lb
                && lb.LocalType == type;
        }

        public static bool IsLdLocA(this CodeInstruction code, Type type, out int loc)
        {
            bool isldloca =
                code.opcode == OpCodes.Ldloca ||
                code.opcode == OpCodes.Ldloca_S;
            if (isldloca && code.operand is LocalBuilder lb && lb.LocalType == type)
            {
                loc = lb.LocalIndex;
                return true;
            }
            loc = -1;
            return false;

        }
        public static bool IsStLoc(this CodeInstruction code, Type type)
        {
            return code.IsStloc()
                && code.operand is LocalBuilder lb
                && lb.LocalType == type;
        }

        public static bool LoadsConstant(this CodeInstruction code, string value)
        {
            return code.opcode == OpCodes.Ldstr
                && code.operand is string str
                && str == value;
        }
    }
}
