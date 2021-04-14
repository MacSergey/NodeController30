using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using JetBrains.Annotations;
using KianCommons;
using KianCommons.Patches;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController.Utilities
{
    public static class CornerSource
    {
        public static void CalculateCorner(NetInfo info, Vector3 startPos, Vector3 endPos, Vector3 startDir, Vector3 endDir, ushort ignoreSegmentID, ushort startNodeID, bool heightOffset, bool leftSide, out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth)
        {
            NetManager instance = Singleton<NetManager>.instance;
            var flags = NetNode.Flags.End;
            var building = 0;

            if (startNodeID != 0)
            {
                flags = instance.m_nodes.m_buffer[startNodeID].m_flags;
                building = instance.m_nodes.m_buffer[startNodeID].m_building;
            }

            cornerDirection = startDir;
            var halfWidth = (!leftSide) ? (0f - info.m_halfWidth) : info.m_halfWidth;
            smooth = (flags & NetNode.Flags.Middle) != 0;

            if ((flags & NetNode.Flags.Middle) != 0 && startNodeID != 0)
            {
                var segment = ignoreSegmentID.GetSegment();
                var isStart = segment.IsStartNode(startNodeID);
                var deltaDir = isStart ? segment.m_startDirection : segment.m_endDirection;
                cornerDirection = NormalizeXZ(cornerDirection - deltaDir);
            }

            var startCornerNormal = Vector3.Cross(cornerDirection, Vector3.up).normalized;
            if (info.m_twistSegmentEnds)
            {
                if (building != 0)
                {
                    var buildingAngle = Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_angle;
                    var buildingNormal = new Vector3(Mathf.Cos(buildingAngle), 0f, Mathf.Sin(buildingAngle));
                    startCornerNormal = Vector3.Dot(startCornerNormal, buildingNormal) < 0f ? -buildingNormal : buildingNormal;
                }
                else if ((flags & NetNode.Flags.Junction) != 0 && startNodeID != 0)
                {
                    var segmentNormal = Vector3.zero;
                    var segmentCount = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        var segmentId = instance.m_nodes.m_buffer[startNodeID].GetSegment(i);
                        if (segmentId != 0 && segmentId != ignoreSegmentID && (instance.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Untouchable) != 0)
                        {
                            var segmentDir = instance.m_segments.m_buffer[segmentId].m_startNode == startNodeID ? instance.m_segments.m_buffer[segmentId].m_startDirection : instance.m_segments.m_buffer[segmentId].m_endDirection;
                            segmentNormal = new Vector3(segmentDir.z, 0f, 0f - segmentDir.x);
                            segmentCount += 1;
                        }
                    }
                    if (segmentCount == 1)
                        startCornerNormal = Vector3.Dot(startCornerNormal, segmentNormal) < 0f ? (-segmentNormal) : segmentNormal;
                }
            }

            var leftBezier = new Bezier3() { a = startPos + startCornerNormal * halfWidth };
            var rightBezier = new Bezier3() { a = startPos - startCornerNormal * halfWidth };
            cornerPos = leftBezier.a;

            if (((flags & NetNode.Flags.Junction) != 0 && info.m_clipSegmentEnds) || (flags & (NetNode.Flags.Bend | NetNode.Flags.Outside)) != 0)
            {
                var endCornerNormal = Vector3.Cross(endDir, Vector3.up).normalized;
                leftBezier.d = endPos - endCornerNormal * halfWidth;
                rightBezier.d = endPos + endCornerNormal * halfWidth;
                NetSegment.CalculateMiddlePoints(leftBezier.a, cornerDirection, leftBezier.d, endDir, smoothStart: false, smoothEnd: false, out leftBezier.b, out leftBezier.c);
                NetSegment.CalculateMiddlePoints(rightBezier.a, cornerDirection, rightBezier.d, endDir, smoothStart: false, smoothEnd: false, out rightBezier.b, out rightBezier.c);
                var leftBezierXZ = Bezier2.XZ(leftBezier);
                var rightBezierXZ = Bezier2.XZ(rightBezier);

                float offsetT = -1f;
                float minOffsetT = -1f;
                bool flag = false;

                var segmentCount = startNodeID != 0 ? 8 : 0;
                var maxQuarterWidth = info.m_halfWidth * 0.5f;
                var segmentIndex = 0;

                for (var i = 0; i < segmentCount; i += 1)
                {
                    var currentSegmentId = instance.m_nodes.m_buffer[startNodeID].GetSegment(i);
                    if (currentSegmentId == 0 || currentSegmentId == ignoreSegmentID)
                        continue;

                    var currentInfo = instance.m_segments.m_buffer[currentSegmentId].Info;
                    var segmentStartDir = instance.m_segments.m_buffer[currentSegmentId].GetDirection(startNodeID);

                    if (currentInfo == null || info.m_clipSegmentEnds != currentInfo.m_clipSegmentEnds)
                        continue;

                    if (currentInfo.m_netAI.GetSnapElevation() > info.m_netAI.GetSnapElevation())
                    {
                        var turnAngle = 0.01f - Mathf.Min(info.m_maxTurnAngleCos, currentInfo.m_maxTurnAngleCos);
                        var angleBetween = segmentStartDir.x * startDir.x + segmentStartDir.z * startDir.z;
                        if ((info.m_vehicleTypes & currentInfo.m_vehicleTypes) == 0 || angleBetween >= turnAngle)
                            continue;
                    }
                    maxQuarterWidth = Mathf.Max(maxQuarterWidth, currentInfo.m_halfWidth * 0.5f);
                    segmentIndex++;
                }

                if (segmentIndex >= 1 || (flags & NetNode.Flags.Outside) != 0)
                {
                    for (var i = 0; i < segmentCount; i += 1)
                    {
                        var currentSegmentId = instance.m_nodes.m_buffer[startNodeID].GetSegment(i);
                        if (currentSegmentId == 0 || currentSegmentId == ignoreSegmentID)
                            continue;

                        ushort currentStartNodeId = instance.m_segments.m_buffer[currentSegmentId].m_startNode;
                        ushort currentEndNodeId = instance.m_segments.m_buffer[currentSegmentId].m_endNode;
                        var currentStartDir = instance.m_segments.m_buffer[currentSegmentId].m_startDirection;
                        var currentEndDir = instance.m_segments.m_buffer[currentSegmentId].m_endDirection;
                        if (startNodeID != currentStartNodeId)
                        {
                            var tempNodeId = currentStartNodeId;
                            currentStartNodeId = currentEndNodeId;
                            currentEndNodeId = tempNodeId;
                            var tempVector = currentStartDir;
                            currentStartDir = currentEndDir;
                            currentEndDir = tempVector;
                        }

                        var currentInfo = instance.m_segments.m_buffer[currentSegmentId].Info;
                        var currentEndPos = instance.m_nodes.m_buffer[currentEndNodeId].m_position;

                        if (currentInfo == null || info.m_clipSegmentEnds != currentInfo.m_clipSegmentEnds)
                            continue;

                        if (currentInfo.m_netAI.GetSnapElevation() > info.m_netAI.GetSnapElevation())
                        {
                            float turnAngle = 0.01f - Mathf.Min(info.m_maxTurnAngleCos, currentInfo.m_maxTurnAngleCos);
                            float angleBetween = currentStartDir.x * startDir.x + currentStartDir.z * startDir.z;
                            if ((info.m_vehicleTypes & currentInfo.m_vehicleTypes) == 0 || angleBetween >= turnAngle)
                                continue;
                        }

                        if (currentStartDir.z * cornerDirection.x - currentStartDir.x * cornerDirection.z > 0f == leftSide)
                        {
                            float currentHalfWidth = (leftSide ? 1 : -1) * Mathf.Max(maxQuarterWidth, currentInfo.m_halfWidth);

                            var currentStartDirN = Vector3.Cross(currentStartDir, Vector3.up).normalized;
                            var currentEndDirN = Vector3.Cross(currentEndDir, Vector3.up).normalized;
                            var currentBezier = new Bezier3()
                            {
                                a = startPos - currentStartDirN * currentHalfWidth,
                                d = currentEndPos + currentEndDirN * currentHalfWidth,
                            };
                            NetSegment.CalculateMiddlePoints(currentBezier.a, currentStartDir, currentBezier.d, currentEndDir, smoothStart: false, smoothEnd: false, out currentBezier.b, out currentBezier.c);

                            var currentBezierXZ = Bezier2.XZ(currentBezier);
                            if (leftBezierXZ.Intersect(currentBezierXZ, out var t, out _, 6))
                                offsetT = Mathf.Max(offsetT, t);
                            else if (leftBezierXZ.Intersect(currentBezierXZ.a, currentBezierXZ.a - XZ(currentStartDir) * 16f, out t, out _, 6))
                                offsetT = Mathf.Max(offsetT, t);
                            else if (currentBezierXZ.Intersect(leftBezierXZ.d + (leftBezierXZ.d - rightBezierXZ.d) * 0.01f, rightBezierXZ.d, out _, out _, 6))
                                offsetT = Mathf.Max(offsetT, 1f);

                            if (cornerDirection.x * currentStartDir.x + cornerDirection.z * currentStartDir.z >= -0.75f)
                                flag = true;
                        }
                        else
                        {
                            float num19 = cornerDirection.x * currentStartDir.x + cornerDirection.z * currentStartDir.z;
                            if (num19 >= 0f)
                            {
                                currentStartDir.x -= cornerDirection.x * num19 * 2f;
                                currentStartDir.z -= cornerDirection.z * num19 * 2f;
                            }
                            float currentHalfWidth = (leftSide ? 1 : -1) * Mathf.Max(maxQuarterWidth, currentInfo.m_halfWidth);

                            var currentStartDirN = Vector3.Cross(currentStartDir, Vector3.up).normalized;
                            var currentEndDirN = Vector3.Cross(currentEndDir, Vector3.up).normalized;
                            var currentBezier = new Bezier3()
                            {
                                a = startPos + currentStartDirN * currentHalfWidth,
                                d = currentEndPos - currentEndDirN * currentHalfWidth,
                            };
                            NetSegment.CalculateMiddlePoints(currentBezier.a, currentStartDir, currentBezier.d, currentEndDir, smoothStart: false, smoothEnd: false, out currentBezier.b, out currentBezier.c);
                            var currentBezierXZ = Bezier2.XZ(currentBezier);

                            if (rightBezierXZ.Intersect(currentBezierXZ, out var t3, out _, 6))
                                minOffsetT = Mathf.Max(minOffsetT, t3);
                            else if (rightBezierXZ.Intersect(currentBezierXZ.a, currentBezierXZ.a - XZ(currentStartDir) * 16f, out t3, out _, 6))
                                minOffsetT = Mathf.Max(minOffsetT, t3);
                            else if (currentBezierXZ.Intersect(leftBezierXZ.d, rightBezierXZ.d + (rightBezierXZ.d - leftBezierXZ.d) * 0.01f, out _, out _, 6))
                                minOffsetT = Mathf.Max(minOffsetT, 1f);
                        }
                    }
                    if ((flags & NetNode.Flags.Junction) != 0)
                    {
                        if (!flag)
                            offsetT = Mathf.Max(offsetT, minOffsetT);
                    }
                    else if ((flags & NetNode.Flags.Bend) != 0 && !flag)
                        offsetT = Mathf.Max(offsetT, minOffsetT);

                    if ((flags & NetNode.Flags.Outside) != 0)
                    {
                        float maxPos = 8640f;
                        var minMinPos = new Vector2(-maxPos, -maxPos);
                        var minMaxPos = new Vector2(-maxPos, maxPos);
                        var maxMaxPos = new Vector2(maxPos, maxPos);
                        var maxMinPos = new Vector2(maxPos, -maxPos);

                        if (leftBezierXZ.Intersect(minMinPos, minMaxPos, out var t5, out _, 6))
                            offsetT = Mathf.Max(offsetT, t5);
                        if (leftBezierXZ.Intersect(minMaxPos, maxMaxPos, out t5, out _, 6))
                            offsetT = Mathf.Max(offsetT, t5);
                        if (leftBezierXZ.Intersect(maxMaxPos, maxMinPos, out t5, out _, 6))
                            offsetT = Mathf.Max(offsetT, t5);
                        if (leftBezierXZ.Intersect(maxMinPos, minMinPos, out t5, out _, 6))
                            offsetT = Mathf.Max(offsetT, t5);

                        offsetT = Mathf.Clamp01(offsetT);
                    }
                    else
                    {
                        if (offsetT < 0f)
                            offsetT = info.m_halfWidth >= 4f ? leftBezierXZ.Travel(0f, 8f) : 0f;

                        var minCornerOffset = info.m_minCornerOffset;
                        if ((flags & (NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward)) != 0)
                            minCornerOffset = Mathf.Max(minCornerOffset, 8f);

                        offsetT = Mathf.Clamp01(offsetT);
                        float num23 = LengthXZ(leftBezier.Position(offsetT) - leftBezier.a);
                        offsetT = leftBezierXZ.Travel(offsetT, Mathf.Max(minCornerOffset - num23, 2f));

                        if (info.m_straightSegmentEnds)
                        {
                            if (minOffsetT < 0f)
                                minOffsetT = info.m_halfWidth >= 4f ? rightBezierXZ.Travel(0f, 8f) : 0f;

                            minOffsetT = Mathf.Clamp01(minOffsetT);
                            num23 = LengthXZ(rightBezier.Position(minOffsetT) - rightBezier.a);
                            minOffsetT = rightBezierXZ.Travel(minOffsetT, Mathf.Max(info.m_minCornerOffset - num23, 2f));
                            offsetT = Mathf.Max(offsetT, minOffsetT);
                        }
                    }
                    var heightTemp = cornerDirection.y;
                    cornerDirection = leftBezier.Tangent(offsetT);
                    cornerDirection.y = 0f;
                    cornerDirection.Normalize();
                    if (!info.m_flatJunctions)
                        cornerDirection.y = heightTemp;

                    cornerPos = leftBezier.Position(offsetT);
                    cornerPos.y = startPos.y;
                }
            }
            else if ((flags & NetNode.Flags.Junction) != 0 && info.m_minCornerOffset >= 0.01f)
            {
                startCornerNormal = Vector3.Cross(endDir, Vector3.up).normalized;
                leftBezier.d = endPos - startCornerNormal * halfWidth;
                rightBezier.d = endPos + startCornerNormal * halfWidth;
                NetSegment.CalculateMiddlePoints(leftBezier.a, cornerDirection, leftBezier.d, endDir, smoothStart: false, smoothEnd: false, out leftBezier.b, out leftBezier.c);
                NetSegment.CalculateMiddlePoints(rightBezier.a, cornerDirection, rightBezier.d, endDir, smoothStart: false, smoothEnd: false, out rightBezier.b, out rightBezier.c);
                Bezier2 leftBezierXZ = Bezier2.XZ(leftBezier);
                Bezier2 rightBezierXZ = Bezier2.XZ(rightBezier);

                float value = Mathf.Clamp01(info.m_halfWidth >= 4f ? leftBezierXZ.Travel(0f, 8f) : 0f);
                float num24 = LengthXZ(leftBezier.Position(value) - leftBezier.a);
                value = leftBezierXZ.Travel(value, Mathf.Max(info.m_minCornerOffset - num24, 2f));

                if (info.m_straightSegmentEnds)
                {
                    float value2 = Mathf.Clamp01(info.m_halfWidth >= 4f ? rightBezierXZ.Travel(0f, 8f) : 0f);
                    num24 = LengthXZ(rightBezier.Position(value2) - rightBezier.a);
                    value2 = rightBezierXZ.Travel(value2, Mathf.Max(info.m_minCornerOffset - num24, 2f));
                    value = Mathf.Max(value, value2);
                }

                var tempHeight = cornerDirection.y;
                cornerDirection = leftBezier.Tangent(value);
                cornerDirection.y = 0f;
                cornerDirection.Normalize();
                if (!info.m_flatJunctions)
                    cornerDirection.y = tempHeight;

                cornerPos = leftBezier.Position(value);
                cornerPos.y = startPos.y;
            }

            if (heightOffset && startNodeID != 0)
                cornerPos.y += instance.m_nodes.m_buffer[startNodeID].m_heightOffset * 0.015625f;
        }
    }
}
