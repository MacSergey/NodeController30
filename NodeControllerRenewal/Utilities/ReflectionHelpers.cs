namespace KianCommons
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using NodeController;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class ReflectionHelpers
    {
        internal static Version VersionOf(this Type t) => t.Assembly.GetName().Version;
        internal static Version VersionOf(this object obj) => VersionOf(obj.GetType());
        internal static string Name(this Assembly assembly) => assembly.GetName().Name;

        internal static void CopyProperties(object target, object origin)
        {
            FieldInfo[] fields = origin.GetType().GetFields(ALL);
            foreach (FieldInfo fieldInfo in fields)
            {
                object value = fieldInfo.GetValue(origin);
                fieldInfo.SetValue(target, value);
            }
        }

        public const BindingFlags ALL = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty;

        public const BindingFlags ALL_Declared = ALL | BindingFlags.DeclaredOnly;
    }
}
