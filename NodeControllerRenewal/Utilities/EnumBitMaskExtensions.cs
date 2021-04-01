namespace KianCommons {
    using ColossalFramework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static KianCommons.Math.MathUtil;
    using System.Reflection;
    using static KianCommons.ReflectionHelpers;

    internal static class EnumBitMaskExtensions {
        [Obsolete("this is buggy as it assumes enum is 0,1,2,3,4 ...\n" +
            "use String2EnumValue instead")]
        internal static int String2Enum<T>(string str) where T : Enum =>
            Array.IndexOf(Enum.GetNames(typeof(T)), str);
        
        internal static object String2EnumValue<T>(string str) where T : Enum =>
            Enum.Parse(typeof(T), str);
            
        internal static T Max<T>()
            where T : Enum =>
            Enum.GetValues(typeof(T)).Cast<T>().Max();

        internal static void SetBit(this ref byte b, int idx) => b |= (byte)(1 << idx);
        internal static void ClearBit(this ref byte b, int idx) => b &= ((byte)~(1 << idx));
        internal static bool GetBit(this byte b, int idx) => (b & (byte)(1 << idx)) != 0;
        internal static void SetBit(this ref byte b, int idx, bool value) {
            if (value)
                b.SetBit(idx);
            else
                b.ClearBit(idx);
        }

        internal static T GetMaxEnumValue<T>() =>
            System.Enum.GetValues(typeof(T)).Cast<T>().Max();

        internal static int GetEnumCount<T>() =>
            System.Enum.GetValues(typeof(T)).Length;

        private static void CheckEnumWithFlags<T>() {
            // copy of:
            // private static void ColossalFramework.EnumExtensions.CheckEnumWithFlags<T>()
            if (!typeof(T).IsEnum) {
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
            }
            if (!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute))) {
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
            }
        }
        private static void CheckEnumWithFlags(Type t) {
            // copy of:
            // private static void ColossalFramework.EnumExtensions.CheckEnumWithFlags<T>()
            if (!t.IsEnum) {
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", t.FullName));
            }
            if (!Attribute.IsDefined(t, typeof(FlagsAttribute))) {
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", t.FullName));
            }
        }

        internal static bool CheckFlags(this NetNode.Flags value, NetNode.Flags required, NetNode.Flags forbidden =0) =>
            (value & (required | forbidden)) == required;


        internal static bool CheckFlags(this NetSegment.Flags value, NetSegment.Flags required, NetSegment.Flags forbidden=0) =>
            (value & (required | forbidden)) == required;

        internal static bool CheckFlags(this NetLane.Flags value, NetLane.Flags required, NetLane.Flags forbidden=0) =>
            (value & (required | forbidden)) == required;

        public static IEnumerable<T> GetPow2ValuesU32<T>() where T : struct, IConvertible {
            CheckEnumWithFlags<T>();
            Array values = Enum.GetValues(typeof(T));
            foreach (object val in values) {
                if (IsPow2((ulong)val))
                    yield return (T)val;
            }
        }
        public static IEnumerable<T> ExtractPow2Flags<T>(this T flags) where T : struct, IConvertible {
            foreach (T value in GetPow2ValuesU32<T>()) {
                if (flags.IsFlagSet(value))
                    yield return value;
            }
        }

        public static IEnumerable<uint> GetPow2ValuesU32(Type enumType) {
            CheckEnumWithFlags(enumType);
            Array values = Enum.GetValues(enumType);
            foreach (object val in values) {
                if (IsPow2((uint)val))
                    yield return (uint)val;
            }
        }

        public static IEnumerable<int> GetPow2ValuesI32(Type enumType) {
            CheckEnumWithFlags(enumType);
            Array values = Enum.GetValues(enumType);
            foreach (object val in values) {
                if (IsPow2((int)val))
                    yield return (int)val;
            }
        }

        public static MemberInfo GetEnumMember(this Type enumType, object value) {
            if (enumType is null) throw new ArgumentNullException("enumType");
            string name = Enum.GetName(enumType, value);
            if (name == null)
                throw new Exception($"{enumType.GetType().Name}:{value} not found");
            return enumType.GetMember(name, ALL).FirstOrDefault() ??
                throw new Exception($"{enumType.GetType().Name}.{name} not found");
        }

        public static T[] GetEnumMemberAttributes<T>(Type enumType, object value)
            where T : Attribute {
            return enumType.GetEnumMember(value).GetAttributes<T>();
        }


        public static T[] GetEnumValues<T>() where T: struct, IConvertible =>
            Enum.GetValues(typeof(T)) as T[];
    }
}