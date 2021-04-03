namespace KianCommons.Serialization
{
    using UnityEngine;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using NodeController;

    internal static class SerializationUtil
    {
        public static Version DeserializationVersion;

        static BinaryFormatter GetBinaryFormatter => new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        public static object Deserialize(byte[] data, Version version)
        {
            if (data == null || data.Length == 0)
                return null;
            try
            {
                DeserializationVersion = version;
                //Log.Debug($"SerializationUtil.Deserialize(data): data.Length={data?.Length}");
                var memoryStream = new MemoryStream();
                memoryStream.Write(data, 0, data.Length);
                memoryStream.Position = 0;
                return GetBinaryFormatter.Deserialize(memoryStream);
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
                return null;
            }
            finally
            {
                DeserializationVersion = null;
            }
        }

        public static byte[] Serialize(object obj)
        {
            if (obj == null)
                return null;
            using var memoryStream = new MemoryStream();
            GetBinaryFormatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0; // redundant
            return memoryStream.ToArray();
        }

        public static void GetObjectFields(SerializationInfo info, object instance)
        {
            var fields = instance.GetType().GetFields().Where(field => !field.IsStatic);
            foreach (FieldInfo field in fields)
            {
                var type = field.GetType();
                if (type == typeof(Vector3))
                    info.AddValue(field.Name, field.GetValue(instance), typeof(Vector3Serializable));
                else
                    info.AddValue(field.Name, field.GetValue(instance), field.FieldType);
            }
        }

        public static void SetObjectFields(SerializationInfo info, object instance)
        {
            foreach (SerializationEntry item in info)
            {
                FieldInfo field = instance.GetType().GetField(item.Name);
                if (field != null && !field.IsStatic)
                {
                    object val = Convert.ChangeType(item.Value, field.FieldType);
                    field.SetValue(instance, val);
                }
            }
        }

        public static void SetObjectProperties(SerializationInfo info, object instance)
        {
            foreach (SerializationEntry item in info)
            {
                var p = instance.GetType().GetProperty(item.Name, ReflectionHelpers.ALL);
                var setter = p?.GetSetMethod();
                if (setter != null && !setter.IsStatic)
                {
                    object val = Convert.ChangeType(item.Value, p.PropertyType);
                    p.SetValue(instance, val, null);
                }
            }
        }

        public static T GetValue<T>(this SerializationInfo info, string name) => (T)info.GetValue(name, typeof(T));

    }
}
