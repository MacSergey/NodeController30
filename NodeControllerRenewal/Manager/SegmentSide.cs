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
        public static float Additional => 32f;
        private ITrajectory _mainTrajectory;
        private CombinedTrajectory _rawTrajectory;

        private float _minT = 0f;
        private float _maxT = 1f;
        private float _mainT = 0f;
        private float _rawT = 0f;

        public SideType Type { get; }
        public SegmentEndData SegmentData { get; }

        public ITrajectory RawTrajectory
        {
            get => _rawTrajectory;
            set
            {
                if (value != _rawTrajectory)
                {
                    var additional = new StraightTrajectory(value.StartPosition - value.StartDirection * Additional, value.StartPosition);
                    _rawTrajectory = new CombinedTrajectory(additional, value);
                    _mainTrajectory = value;
                    _minT = 0f;
                    _maxT = 1f;
                    _mainT = _rawTrajectory.Parts[1];
                    Position = _rawTrajectory.StartPosition;
                    Direction = _rawTrajectory.StartDirection;
                    Update();
                }
            }
        }
        public ITrajectory MainTrajectory => _mainTrajectory;

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
        public float MainT => _mainT;
        public float DefaultT { get; set; }

        public ITrajectory Trajectory { get; private set; }
        public float RawT
        {
            get => _rawT;
            set => _rawT = value;
        }
        public float CurrentT => Mathf.Clamp(RawT, MinT + DeltaT, MaxT - DeltaT);
        public float DeltaT => 0.05f / RawTrajectory.Length;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Vector3 MarkerPosition
        {
            get
            {
                var width = SegmentData.Width;
                if (width >= SegmentEndData.MinNarrowWidth)
                    return Position;
                else
                    return Position + Direction.Turn90(Type == SideType.Right).MakeFlatNormalized() * (SegmentEndData.MinNarrowWidth - width) / 2f;
            }
        }

        public bool IsMinBorderT => RawT - 0.001f <= MinT;
        public bool IsMaxBorderT => RawT + 0.001f >= MaxT;
        public bool IsDefaultT => Mathf.Abs(RawT - DefaultT) < 0.001f;
        public bool IsShort => (MaxT - CurrentT) <= (1f / RawTrajectory.Length);

        public SegmentSide(SegmentEndData segmentData, SideType type)
        {
            Type = type;
            SegmentData = segmentData;
        }
        private void Update() => Trajectory = RawTrajectory.Cut(MinT, MaxT);
        public void Calculate(bool isMain)
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(RawT, MinT + (nodeData.IsMiddleNode || SegmentData.IsNodeLess ? 0f : DeltaT), MaxT - DeltaT);
            var position = RawTrajectory.Position(t);
            var direction = RawTrajectory.Tangent(t).normalized;

            if (!SegmentData.IsSlope)
            {
                position.y = SegmentData.NodeId.GetNode().m_position.y;
                direction = direction.MakeFlatNormalized();
            }
            else if (!isMain)
            {
                nodeData.GetClosest(position, out var closestPos, out var closestDir, out var closestT);

                var normal = closestDir.MakeFlat().Turn90(true);
                var twist = Mathf.Lerp(-nodeData.FirstMainSegmentEnd.TwistAngle, nodeData.SecondMainSegmentEnd.TwistAngle, closestT);
                normal.y = normal.magnitude * Mathf.Sin(twist * Mathf.Deg2Rad);

                var plane = new Plane();
                plane.Set3Points(closestPos, closestPos + closestDir, closestPos + normal);
                plane.Raycast(new Ray(position, Vector3.up), out var rayT);
                position += Vector3.up * rayT;

                var point = position + direction;
                plane.Raycast(new Ray(point, Vector3.up), out rayT);
                point += Vector3.up * rayT;
                direction = point - position;
            }
            else
            {
                if (nodeData.Style.SupportSlope != SupportOption.None)
                {
                    var quaternion = Quaternion.AngleAxis(SegmentData.SlopeAngle, direction.MakeFlat().Turn90(true));
                    direction = quaternion * direction;
                }
                if (nodeData.Style.SupportTwist != SupportOption.None)
                {
                    var ratio = Mathf.Sin(SegmentData.TwistAngle * Mathf.Deg2Rad);
                    if (nodeData.Style.SupportStretch != SupportOption.None)
                        ratio *= SegmentData.Stretch;

                    position.y += (Type == SideType.Left ? -1 : 1) * SegmentData.Id.GetSegment().Info.m_halfWidth * ratio;
                }
            }

            Position = position;
            Direction = NormalizeXZ(direction);
            if (nodeData.IsEndNode)
                Direction *= SegmentData.Stretch;
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

        public void Render(OverlayData dataAllow, OverlayData dataForbidden, OverlayData dataLimit)
        {
            var deltaT = 0.2f / RawTrajectory.Length;
            if (MinT == 0f)
            {
                if (RawT >= deltaT)
                    RawTrajectory.Cut(0f, RawT).Render(dataAllow);
            }
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;

                var t = Math.Min(RawT, MinT);
                if (t >= DeltaT)
                    RawTrajectory.Cut(0f, t).Render(dataForbidden);

                if (RawT - MinT >= 0.2f / RawTrajectory.Length)
                    RawTrajectory.Cut(MinT, RawT).Render(dataAllow);
            }
            dataLimit.Color ??= Colors.Purple;
            RawTrajectory.Position(DefaultT).RenderCircle(dataLimit);
        }
        public void RenderCircle(OverlayData data)
        {
            if ((MarkerPosition - Position).sqrMagnitude > 0.25f)
            {
                var color = data.Color.HasValue ? ((Color32)data.Color.Value).SetAlpha(128) : Colors.White128;
                new StraightTrajectory(MarkerPosition, Position).Render(new OverlayData(data.CameraInfo) { Color = color });
            }
            MarkerPosition.RenderCircle(data, data.Width ?? SegmentEndData.CornerDotRadius * 2, 0f);
        }

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
