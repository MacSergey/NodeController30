using UnityEngine;
using System;

namespace KianCommons.Serialization
{
    [Serializable]
    public struct Vector3Serializable
    {
        public float x, y, z;
        public static implicit operator Vector3(Vector3Serializable v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable { x = v.x, y = v.y, z = v.z };
    }
}

namespace KianCommons.Math
{
    [Serializable]
    [Obsolete("use Vector3Serializable from the name space KianCommons.Serialization")]
    public struct Vector3Serializable
    {
        public float x, y, z;
        public static implicit operator Vector3(Vector3Serializable v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable { x = v.x, y = v.y, z = v.z };
        public static implicit operator Serialization.Vector3Serializable(Vector3Serializable v) => new Serialization.Vector3Serializable { x = v.x, y = v.y, z = v.z };
    }
}
