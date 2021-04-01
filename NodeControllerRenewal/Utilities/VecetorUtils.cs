using System;
using UnityEngine;

namespace KianCommons.Math {
    [Obsolete("Use Vector2D or Vector3D instead")]
    public static class VectorUtil {
        /// <summary>
        /// return value is between 0 to pi. v1 and v2 are interchangable.
        /// </summary>
        public static float UnsignedAngleRad(Vector2 v1, Vector2 v2) {
            //cos(a) = v1.v2 /(|v1||v2|)
            v1.Normalize();
            v2.Normalize();
            //cos(a) = v1.v2
            float dot = Vector2.Dot(v1, v2);
            float angle = Mathf.Acos(dot);
            return angle;
        }

        public static Vector2 RotateRadCCW(this Vector2 v, float angle) =>
            Vector2ByAngleRadCCW(v.magnitude, angle + v.SignedAngleRadCCW());

        /// <param name="angle">angle in rad with Vector.right in CCW direction</param>
        public static Vector2 Vector2ByAngleRadCCW(float magnitude, float angle) {
            return new Vector2(
                x: magnitude * Mathf.Cos(angle),
                y: magnitude * Mathf.Sin(angle)
                );
        }

        /// result is between -pi to +pi. angle is CCW with respect to Vector2.right
        public static float SignedAngleRadCCW(this Vector2 v) {
            v.Normalize();
            return Mathf.Acos(v.x) * Mathf.Sign(v.y);
        }

        public static float Determinent(Vector2 v1, Vector2 v2) =>
            v1.x * v2.y - v1.y * v2.x; // x1*y2 - y1*x2  

        public static Vector2 Vector2ByAgnleRad(float magnitude, float angle) {
            return new Vector2(
                x: magnitude * Mathf.Cos(angle),
                y: magnitude * Mathf.Sin(angle)
                );
        }

        /// <summary>
        /// return value is between -pi to pi. v1 and v2 are not interchangable.
        /// the angle goes CCW from v1 to v2.
        /// eg v1=0,1 v2=1,0 => angle=pi/2
        /// Note: to convert CCW to CW EITHER swap v1 and v2 OR take the negative of the result.
        /// </summary>
        public static float SignedAngleRadCCW(Vector2 v1, Vector2 v2) {
            float dot = Vector2.Dot(v1, v2);
            float det = Determinent(v1, v2);
            float angle = Mathf.Atan2(det, dot);
            return angle;
        }

        public static bool AreApprox180(Vector2 v1, Vector2 v2, float error = MathUtil.Epsilon) {
            float dot = Vector2.Dot(v1, v2);
            return MathUtil.EqualAprox(dot, -1, error);
        }

        public static Vector2 Rotate90CCW(this Vector2 v) => new Vector2(-v.y, +v.x);
        public static Vector2 PerpendicularCCW(this Vector2 v) => v.normalized.Rotate90CCW();
        public static Vector2 Rotate90CW(this Vector2 v) => new Vector2(+v.y, -v.x);
        public static Vector2 PerpendicularCW(this Vector2 v) => v.normalized.Rotate90CW();

        public static Vector2 Extend(this Vector2 v, float magnitude) => NewMagnitude(v, magnitude + v.magnitude);
        public static Vector2 NewMagnitude(this Vector2 v, float magnitude) => magnitude * v.normalized;

        public static Vector3 ToCS3D(this Vector2 v2, float h = 0) => new Vector3(v2.x, h, v2.y);
        public static Vector2 ToCS2D(this Vector3 v3) => new Vector2(v3.x, v3.z);
        public static float Height(this Vector3 v3) => v3.y;

        #region Vector3
        /// <summary>
        /// rotates horizontally (XZ) by 90 Clock Wise and sets height to zero.
        /// the resutl is normalized
        ///</summary>
        public static Vector3 NormalCW(this Vector3 v) => new Vector3(+v.z, 0, -v.x).normalized;


        /// <summary>
        /// rotates horizontally (XZ) by 90 Counter Clock Wise and sets height to zero.
        /// the resutl is normalized
        /// </summary>
        public static Vector3 NormalCCW(this Vector3 v) => new Vector3(-v.z, 0, +v.x).normalized;

        /// <summary>
        /// returns a new vector with corresponding index set to input value.
        /// </summary>
        /// <param name="index">0:x 1:y 2:z</param>
        public static Vector3 SetI(this Vector3 v, float value, int index) {
            v[index] = value;
            return v;
        }
        #endregion


        /// <summary>
        /// calculates if <paramref name="target"/> angle is to the right of 0 and <paramref name="source"/> angle.
        /// assuming input angles are (-pi,pi) CCW.
        /// assuming all angles are unique non-zero
        /// </summary>
        /// <param name="source">angle to compare with</param>
        /// <param name="target">angle being comapred</param>
        public static bool CompareAngles_CCW_Right(float source, float target) {
            if (source > 0)
                return (0 < target) & (target < source);
            return !CompareAngles_CCW_Right(-source, -target);
        }
    }
}
