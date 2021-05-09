using ColossalFramework.Math;
using ModsCommon.Utilities;
using System;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NodeController
{
    public class SegmentSide
    {
        private BezierTrajectory _rawBezier;
        private float _minT = 0f;
        private float _maxT = 1f;

        public SideType Type { get; }
        public SegmentEndData Data { get; }

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
        public float RawT { get; set; }
        public float CurrentT => Mathf.Clamp(RawT, MinT + DeltaT, MaxT - DeltaT);
        public float DeltaT => 0.05f / RawBezier.Length;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }

        public bool IsMinBorderT => RawT - 0.001f <= MinT;
        public bool IsMaxBorderT => RawT + 0.001f >= MaxT;
        public bool IsDefaultT => Mathf.Abs(RawT - DefaultT) < 0.001f;
        public bool IsShort => (MaxT - CurrentT) <= (1f / RawBezier.Length);

        public SegmentSide(SegmentEndData data, SideType type)
        {
            Type = type;
            Data = data;
        }
        private void Update() => Bezier = RawBezier.Cut(MinT, MaxT);
        public void Calculate(bool isMain)
        {
            var nodeData = Data.NodeData;

            var t = Mathf.Clamp(RawT, MinT + (nodeData.IsMiddleNode || Data.IsNodeLess ? 0f : DeltaT), MaxT - DeltaT);
            var position = RawBezier.Position(t);
            var direction = RawBezier.Tangent(t).normalized;

            if (!Data.IsSlope)
            {
                position.y = Data.NodeId.GetNode().m_position.y;
                direction = direction.MakeFlatNormalized();
            }
            else if (!isMain)
            {
                nodeData.GetClosest(position, out var closestPos, out var closestDir);
                position.y = closestPos.y;

                var closestLine = new StraightTrajectory(closestPos, closestPos + closestDir, false);
                var line = new StraightTrajectory(position, position - direction, false);
                var intersect = Intersection.CalculateSingle(closestLine, line);
                var intersectPos = closestPos + intersect.FirstT * closestDir;
                var newDirection = (position - intersectPos).normalized;

                direction = NormalizeDotXZ(direction, newDirection) >= 0f ? newDirection : -newDirection;
            }
            else
            {
                if (nodeData.Style.SupportSlope != SupportOption.None)
                {
                    var quaternion = Quaternion.AngleAxis(Data.SlopeAngle, direction.MakeFlat().Turn90(true));
                    direction = quaternion * direction;
                }
                if (nodeData.Style.SupportTwist != SupportOption.None)
                {
                    var ratio = Mathf.Sin(Data.TwistAngle * Mathf.Deg2Rad);
                    if (nodeData.Style.SupportStretch != SupportOption.None)
                        ratio *= Data.Stretch;

                    position.y += (Type == SideType.Left ? -1 : 1) * Data.Id.GetSegment().Info.m_halfWidth * ratio;
                }
            }

            Position = position;
            Direction = NormalizeXZ(direction);
            if (nodeData.IsEndNode)
                Direction *= Data.Stretch;
        }
        public static void FixMiddle(SegmentSide first, SegmentSide second)
        {
            var fixPosition = (first.Position + second.Position) / 2f;
            first.Position = fixPosition;
            second.Position = fixPosition;

            var fixDirection = NormalizeXZ(first.Direction - second.Direction);

            var firstFixDirection = fixDirection;
            firstFixDirection.y = first.Direction.y;
            first.Direction = firstFixDirection;

            var secondFixDirection = -fixDirection;
            secondFixDirection.y = second.Direction.y;
            second.Direction = secondFixDirection;
        }

        public void Render(OverlayData dataAllow, OverlayData dataForbidden)
        {
            if (MinT == 0f)
                RawBezier.Cut(0f, RawT).Render(dataAllow);
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;
                RawBezier.Cut(0f, Math.Min(RawT, MinT)).Render(dataForbidden);
                if (RawT - MinT >= 0.2f / RawBezier.Length)
                    RawBezier.Cut(MinT, RawT).Render(dataAllow);
            }

            RawBezier.Position(DefaultT).RenderCircle(new OverlayData(dataAllow.CameraInfo) { Color = Colors.Purple });
        }
        public void RenderCircle(OverlayData data) => Position.RenderCircle(data, data.Width ?? SegmentEndData.CornerDotRadius * 2, 0f);

        public override string ToString() => $"{Type}: {nameof(RawT)}={RawT}; {nameof(MinT)}={MinT}; {nameof(MaxT)}={MaxT}; {nameof(Position)}={Position};";
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
