namespace KianCommons.Math {
    using UnityEngine;
    using ColossalFramework.Math;
    public static class MathUtil {
        public const float Epsilon = 0.001f;
        public static bool EqualAprox(float a, float b, float error = Epsilon) {
            float diff = a - b;
            return (diff > -error) & (diff < error);
        }
    }
}
