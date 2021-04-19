using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using System;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModsCommon.Utilities;
using NodeController.Utilities;

namespace NodeController
{
    public class SegmentSide
    {
        private BezierTrajectory _rawBezier;
        private float _minT = 0f;
        private float _maxT = 1f;

        public SideType Type { get; }

        public BezierTrajectory RawBezier
        {
            get => _rawBezier;
            set
            {
                if (value != _rawBezier)
                {
                    _rawBezier = value;
                    _minT = 0f;
                    _maxT = 1f;
                    Position = _rawBezier.StartPosition;
                    Direction = _rawBezier.StartDirection;
                    Update();
                }
            }
        }
        public float MinT
        {
            get => _minT;
            set
            {
                if (value != _minT)
                {
                    _minT = value;
                    Update();
                }
            }
        }
        public float MaxT
        {
            get => _maxT;
            set
            {
                if (Mathf.Abs(value - _maxT) > 0.001f)
                {
                    _maxT = value;
                    Update();
                }
            }
        }
        public float DefaultT { get; set; }

        public BezierTrajectory Bezier { get; private set; }
        public float CurrentT { get; set; }
        public float DeltaT => 0.05f / RawBezier.Length;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }

        public bool IsBorderT => CurrentT - 0.001f <= MinT;

        public SegmentSide(SideType type)
        {
            Type = type;
        }
        private void Update() => Bezier = RawBezier.Cut(MinT, MaxT);
        public void Calculate(SegmentEndData data, bool isMain)
        {
            var nodeData = data.NodeData;

            var t = Mathf.Clamp(CurrentT, MinT + (!nodeData.IsMiddleNode ? DeltaT : 0f), MaxT - DeltaT);
            var position = RawBezier.Position(t);
            var direction = RawBezier.Tangent(t).normalized;

            if (nodeData.IsMiddleNode || nodeData.IsEndNode)
            {
                var quaternion = Quaternion.AngleAxis(data.SlopeAngle, direction.MakeFlat().Turn90(true));
                direction = quaternion * direction;

                position.y += (Type == SideType.Left ? -1 : 1) * data.Info.m_halfWidth * Mathf.Sin(data.TwistAngle * Mathf.Deg2Rad);
            }
            else if (!data.IsSlope)
            {
                position.y = data.Node.m_position.y;
                direction = direction.MakeFlatNormalized();
            }
            else if (!isMain)
            {
                GetClosest(nodeData, position, out var closestPos, out var closestDir);
                position.y = closestPos.y;

                var closestLine = new StraightTrajectory(closestPos, closestPos + closestDir, false);
                var line = new StraightTrajectory(position, position - direction, false);
                var intersect = Intersection.CalculateSingle(closestLine, line);
                var intersectPos = closestPos + intersect.FirstT * closestDir;
                direction = (position - intersectPos).normalized;
            }

            Position = position;
            Direction = VectorUtils.NormalizeXZ(direction);
        }
        private void GetClosest(NodeData nodeData, Vector3 position, out Vector3 closestPos, out Vector3 closestDir)
        {
            nodeData.LeftMainBezier.Trajectory.ClosestPositionAndDirection(position, out var leftClosestPos, out var leftClosestDir, out _);
            nodeData.RightMainBezier.Trajectory.ClosestPositionAndDirection(position, out var rightClosestPos, out var rightClosestDir, out _);

            if ((leftClosestPos - position).sqrMagnitude < (rightClosestPos - position).sqrMagnitude)
            {
                closestPos = leftClosestPos;
                closestDir = leftClosestDir;
            }
            else
            {
                closestPos = rightClosestPos;
                closestDir = rightClosestDir;
            }
        }

        public void Render(OverlayData dataAllow, OverlayData dataForbidden)
        {
            if (MinT == 0f)
                RawBezier.Cut(0f, CurrentT).Render(dataAllow);
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;
                RawBezier.Cut(0f, Math.Min(CurrentT, MinT)).Render(dataForbidden);
                if (CurrentT - MinT >= 0.2f / RawBezier.Length)
                    RawBezier.Cut(MinT, CurrentT).Render(dataAllow);
            }

            RawBezier.Position(DefaultT).RenderCircle(new OverlayData(dataAllow.CameraInfo) { Color = Colors.Purple });
        }

        public override string ToString() => $"{Type}: {nameof(CurrentT)}={CurrentT}; {nameof(MinT)}={MinT}; {nameof(MaxT)}={MaxT}; {nameof(Position)}={Position};";
    }

    public enum SideType : byte
    {
        Left,
        Right
    }
    public static class SideTypeExtension
    {
        public static SideType Invert(this SideType side) => side == SideType.Left ? SideType.Right : SideType.Left;
    }
}
