using System;
using UnityEngine;

namespace KianCommons.Math
{
    [Obsolete("Use Vector2D or Vector3D instead")]
    public static class VectorUtil
    {
        #region Vector3
        /// <summary>
        /// returns a new vector with corresponding index set to input value.
        /// </summary>
        /// <param name="index">0:x 1:y 2:z</param>
        public static Vector3 SetI(this Vector3 v, float value, int index)
        {
            v[index] = value;
            return v;
        }
        #endregion
    }
}
