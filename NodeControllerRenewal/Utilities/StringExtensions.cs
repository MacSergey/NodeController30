namespace KianCommons
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class StringExtensions
    {
        /// <summary>
        /// Like To string but:
        ///  - returns "null" if object is null
        ///  - returns string.ToSTR() if object is string.
        ///  - recursively returns all items as string if object is IEnumerable
        ///  - returns id and type if object is InstanceID
        ///  - returns id and type of both key and value if obj is InstanceID->InstanceID pair
        /// </summary>
        internal static string ToSTR(this object obj)
        {
            if (obj is null) return "<null>";
            if (obj is string str)
                return str.ToSTR();
            if (obj is InstanceID instanceID)
                return instanceID.ToSTR();
            if (obj is KeyValuePair<InstanceID, InstanceID> map)
                return map.ToSTR();
            if (obj is IEnumerable list)
                return list.ToSTR();
            return obj.ToString();
        }

        /// <summary>
        ///  - returns "null" if string is null
        ///  - returns "empty" if string is empty
        ///  - returns string otherwise.
        /// </summary>
        internal static string ToSTR(this string str)
        {
            if (str == "") return "<empty>";
            if (str == null) return "<null>";
            return str;
        }

        /// <summary>
        /// returns id and type of InstanceID
        /// </summary>
        internal static string ToSTR(this InstanceID instanceID) => $"{instanceID.Type}:{instanceID.Index}";

        /// <summary>
        /// returns id and type of both key and value
        /// </summary>
        internal static string ToSTR(this KeyValuePair<InstanceID, InstanceID> map) => $"[{map.Key.ToSTR()}:{map.Value.ToSTR()}]";

        /// <summary>
        /// returns all items as string
        /// </summary>
        internal static string ToSTR<T>(this IEnumerable<T> list)
        {
            if (list == null) 
                return "<null>";
            string ret = "{ ";
            foreach (T item in list)
            {
                string s;
                if (item is KeyValuePair<InstanceID, InstanceID> map)
                    s = map.ToSTR();
                else
                    s = item?.ToString() ?? "<null>";
                ret += $"{s}, ";
            }
            ret.Remove(ret.Length - 2, 2);
            ret += " }";
            return ret;
        }
    }
}
