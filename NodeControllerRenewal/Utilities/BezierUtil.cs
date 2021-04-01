namespace KianCommons.Math
{
    using ColossalFramework.Math;
    using UnityEngine;
    using static MathUtil;

    public static class BezierUtil
    {
        public static string STR(this Bezier2 bezier)
        {
            return $"Bezier2(" + bezier.a + ", " + bezier.b + ", " + bezier.c + ", " + bezier.d + ")";
        }

        public static float ArcLength(this Bezier3 beizer, float step = 0.1f)
        {
            float ret = 0;
            float t;
            for (t = step; t < 1f; t += step)
            {
                float len = (beizer.Position(t) - beizer.Position(t - step)).magnitude;
                ret += len;
            }
            {
                float len = (beizer.d - beizer.Position(t - step)).magnitude;
                ret += len;
            }
            return ret;
        }

        /// <summary>
        /// Travels some distance on beizer and calculates the point and tangent at that distance.
        /// </summary>
        /// <param name="distance">distance to travel on the arc in meteres</param>
        /// <param name="tangent">normalized tangent on the curve toward the end of the beizer.</param>
        /// <returns>point on the curve at the given distance.</returns>
        public static Vector3 Travel2(this Bezier3 beizer, float distance, out Vector3 tangent)
        {
            float t = beizer.Travel(0, distance);
            tangent = beizer.Tangent(t).normalized;
            return beizer.Position(t);
        }

        /// <summary>
        /// Travels some distance on beizer and calculates the point and tangent at that distance.
        /// </summary>
        /// <param name="distance">distance to travel on the arc in meteres</param>
        /// <param name="tangent">normalized tangent on the curve toward the end of the beizer.</param>
        /// <returns>point on the curve at the given distance.</returns>
        public static Vector2 Travel2(this Bezier2 beizer, float distance, out Vector2 tangent)
        {
            if (beizer.IsStraight())
            {
                tangent = (beizer.d - beizer.a).normalized;
                return beizer.TravelStraight(distance);
            }
            float t = beizer.Travel(0, distance);
            tangent = beizer.Tangent(t).normalized;
            return beizer.Position(t);
        }

        static Vector2 TravelStraight(this Bezier2 beizer, float length)
        {
            float r = length / beizer.ArcLength();
            return beizer.a + r * (beizer.d - beizer.a);
        }


        public static bool IsStraight(this Bezier2 beizer)
        {
            return false;
        }

        public static float ArcLength(this Bezier2 bezier, float step = 0.1f)
        {
            if (bezier.IsStraight())
            {
                return (bezier.d - bezier.a).magnitude;
            }
            float ret = 0;
            float t;
            for (t = step; t < 1f; t += step)
            {
                float len = (bezier.Position(t) - bezier.Position(t - step)).magnitude;
                ret += len;
            }
            {
                float len = (bezier.d - bezier.Position(t - step)).magnitude;
                ret += len;
            }
            if (EqualAprox(ret, 0))
                return 0f;
            return ret;
        }

        public static float ArcLength(this Bezier2 bezier, Vector2 point, float step = 0.1f)
        {
            if (bezier.IsStraight())
            {
                return (point - bezier.a).magnitude;
            }
            float ret = 0;
            float t;
            for (t = step; t <= 1f + Epsilon; t += step)
            {
                var p0 = bezier.Position(t - step);
                float len = (bezier.Position(t) - p0).magnitude;
                float len2 = (point - p0).magnitude;
                if (len2 <= len + Epsilon)
                {
                    ret += len2;
                    return ret;
                }
                ret += len;
            }
            return ret;
        }

        public static Bezier2 ToCSBezier2(this Bezier3 bezier)
        {
            return new Bezier2
            {
                a = bezier.a.ToCS2D(),
                b = bezier.b.ToCS2D(),
                c = bezier.c.ToCS2D(),
                d = bezier.d.ToCS2D(),
            };
        }

        /// <param name="startDir">should be going toward the end of the bezier.</param>
        /// <param name="endDir">should be going toward the start of the  bezier.</param>
        /// <returns></returns>
        public static Bezier2 Bezier2ByDir(Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir)
        {
            NetSegment.CalculateMiddlePoints(
                startPos.ToCS3D(), startDir.ToCS3D(),
                endPos.ToCS3D(), endDir.ToCS3D(),
                false, false,
                out Vector3 MiddlePoint1, out Vector3 MiddlePoint2);
            return new Bezier2
            {
                a = startPos,
                d = endPos,
                b = MiddlePoint1.ToCS2D(),
                c = MiddlePoint2.ToCS2D()
            };
        }

        /// <param name="startDir">should be going toward the end of the bezier.</param>
        /// <param name="endDir">should be going toward the start of the  bezier.</param>
        /// <returns></returns>
        public static Bezier3 Bezier3ByDir(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir)
        {
            NetSegment.CalculateMiddlePoints(
                startPos, startDir,
                endPos, endDir,
                false, false,
                out Vector3 MiddlePoint1, out Vector3 MiddlePoint2);
            return new Bezier3
            {
                a = startPos,
                d = endPos,
                b = MiddlePoint1,
                c = MiddlePoint2,
            };
        }

        /// <summary>
        /// results are normalized.
        /// fast for t==0 or t==1
        /// </summary>
        /// <param name="lefSide">is normal to the left of tangent (going away from origin) </param>
        public static void NormalTangent(this ref Bezier2 bezier, float t, bool lefSide, out Vector2 normal, out Vector2 tangent)
        {
            if (MathUtil.EqualAprox(t, 0))
            {
                tangent = bezier.b - bezier.a;
            }
            else if (MathUtil.EqualAprox(t, 0))
            {
                tangent = bezier.d - bezier.c;
            }
            else
            {
                tangent = bezier.Tangent(t);
            }
            tangent.Normalize();
            normal = tangent.Rotate90CW();
            if (lefSide) normal = -normal;
        }

        /// <summary>
        /// results are normalized.
        /// fast for t==0 or t==1
        /// </summary>
        /// <param name="lefSide">is normal to the left of tangent (going away from origin) </param>
        public static void NormalTangent(this ref Bezier3 bezier, float t, bool lefSide, out Vector3 normal, out Vector3 tangent)
        {
            if (MathUtil.EqualAprox(t, 0))
            {
                tangent = bezier.b - bezier.a;
            }
            else if (MathUtil.EqualAprox(t, 0))
            {
                tangent = bezier.d - bezier.c;
            }
            else
            {
                tangent = bezier.Tangent(t);
            }
            tangent.Normalize();
            normal = tangent.NormalCW();
            if (lefSide) normal = -normal;
        }

        public static float GetClosestT(this ref Bezier3 bezier, Vector3 pos)
        {
            float minDistance = 1E+11f;
            float t = 0f;
            Vector3 a = bezier.a;
            for (int i = 1; i <= 16; i++)
            {
                Vector3 vector = bezier.Position((float)i / 16f);
                float distance = Segment3.DistanceSqr(a, vector, pos, out float u);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    t = (i - 1 + u) / 16f;
                }
                a = vector;
            }

            float num = 0.03125f;
            for (int j = 0; j < 4; j++)
            {
                Vector3 pos1 = bezier.Position(Mathf.Max(0f, t - num));
                Vector3 vector2 = bezier.Position(t);
                Vector3 b = bezier.Position(Mathf.Min(1f, t + num));
                float distance1 = Segment3.DistanceSqr(pos1, vector2, pos, out float u);
                float distance2 = Segment3.DistanceSqr(vector2, b, pos, out float u2);
                t = ((!(distance1 < distance2)) ? Mathf.Min(1f, t + num * u2) : Mathf.Max(0f, t - num * (1f - u)));
                num *= 0.5f;
            }
            return t;
        }

        /// <param name="bLeft">going from start to end</param>
        public static Bezier2 CalculateParallelBezier(this Bezier2 bezier, float sideDistance, bool bLeft)
        {
            bezier.NormalTangent(0, bLeft, out Vector2 normalStart, out Vector2 tangentStart);
            bezier.NormalTangent(1, bLeft, out Vector2 normalEnd, out Vector2 tangentEnd);
            return BezierUtil.Bezier2ByDir(
                bezier.a + sideDistance * normalStart, tangentStart,
                bezier.d + sideDistance * normalEnd, -tangentEnd);
        }

        /// <param name="bLeft">going from start to end</param>
        public static Bezier3 CalculateParallelBezier(this Bezier3 bezier, float sideDistance, bool bLeft)
        {
            bezier.NormalTangent(0, bLeft, out Vector3 normalStart, out Vector3 tangentStart);
            bezier.NormalTangent(1, bLeft, out Vector3 normalEnd, out Vector3 tangentEnd);
            return BezierUtil.Bezier3ByDir(
                bezier.a + sideDistance * normalStart, tangentStart,
                bezier.d + sideDistance * normalEnd, -tangentEnd);
        }

        public static Bezier3 ToCSBezier3(this Bezier2 bezier)
        {
            return new Bezier3
            {
                a = NetUtil.Get3DPos(bezier.a),
                b = NetUtil.Get3DPos(bezier.b),
                c = NetUtil.Get3DPos(bezier.c),
                d = NetUtil.Get3DPos(bezier.d),
            };
        }

        public static Bezier3 ToCSBezier3(this Bezier2 bezier, float h)
        {
            return new Bezier3
            {
                a = bezier.a.ToCS3D(h),
                b = bezier.b.ToCS3D(h),
                c = bezier.c.ToCS3D(h),
                d = bezier.d.ToCS3D(h),
            };
        }

        public static Vector3 Mirror(Vector3 pos, Bezier3 bezier)
        {
            float t = bezier.GetClosestT(pos);
            var pos0 = bezier.Position(t);
            var diff = pos0 - pos;
            var pos2 = pos0 + diff;
            return pos2;
        }

        public static Vector2 Mirror(Vector2 point, Bezier2 bezier)
        {
            return Mirror(point.ToCS3D(0), bezier.ToCSBezier3(0)).ToCS2D();
        }

    }
}
