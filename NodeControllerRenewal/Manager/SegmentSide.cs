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
        private struct DataStruct
        {
            private SegmentSide _segmentSide;

            public CombinedTrajectory _rawTrajectory;

            public float _minT;
            public float _maxT;
            public float _mainT;
            public float _rawT;
            public float _defaultT;

            public Vector3 _minPos;
            public Vector3 _minDir;

            public Vector3 _position;
            public Vector3 _direction;

            public Vector3 _maxPos;
            public Vector3 _maxDir;

            public float DeltaT => 0.05f / _rawTrajectory.Length;
            public float CurrentT => Mathf.Clamp(_rawT, _minT + DeltaT, _maxT - DeltaT);

            public bool IsMinBorderT => _rawT - DeltaT <= _minT;
            public bool IsMaxBorderT => _rawT + DeltaT >= _maxT;
            public bool IsShort => (_maxT - CurrentT) <= (1f / _rawTrajectory.Length);

            public DataStruct(SegmentSide segmentSide)
            {
                _segmentSide = segmentSide;
                _rawTrajectory = null;

                _minT = 0f;
                _maxT = 1f;
                _rawT = 0f;
                _mainT = 0f;
                _defaultT = 0f;

                _minPos = default;
                _minDir = default;
                _position = default;
                _direction = default;
                _maxPos = default;
                _maxDir = default;
            }

            public void Set(ITrajectory trajectory)
            {
                if (_rawTrajectory == null || !_rawTrajectory[1].Equals(trajectory))
                {
                    var mainTrajectory = trajectory;
                    var additionalTrajectory = new StraightTrajectory(trajectory.StartPosition - trajectory.StartDirection * _segmentSide.AdditionalLength, trajectory.StartPosition);
                    _rawTrajectory = new CombinedTrajectory(additionalTrajectory, mainTrajectory);

                    _minT = 0f;
                    _maxT = 1f;
                    _mainT = _rawTrajectory.Parts[1];

                    Update();
                }
            }

            public void Update()
            {
                _minPos = _rawTrajectory.Position(_minT);
                _minDir = _rawTrajectory.Tangent(_minT);
                
                _maxPos = _rawTrajectory.Position(_maxT);
                _maxDir = -_rawTrajectory.Tangent(_maxT);
            }
        }

        public float AdditionalLength => Mathf.Max(SegmentData.Id.GetSegment().Info.m_halfWidth / Mathf.Cos(75f * Mathf.Deg2Rad), 16f);

        public SideType Type { get; }
        public SegmentEndData SegmentData { get; }

        private DataStruct _final;
        private DataStruct _temp;

        public CombinedTrajectory RawTrajectory => _temp._rawTrajectory;
        public ITrajectory MainTrajectory => _temp._rawTrajectory[1];
        public ITrajectory AdditionalTrajectory => _temp._rawTrajectory[0];

        public float MinT
        {
            get => _temp._minT;
            set
            {
                if (value != _temp._minT)
                {
                    _temp._minT = value;
                    _temp.Update();
                }
            }
        }
        public float MaxT
        {
            get => _temp._maxT;
            set
            {
                if (Mathf.Abs(value - _temp._maxT) > 0.001f)
                {
                    _temp._maxT = value;
                    _temp.Update();
                }
            }
        }
        public float MainT => _temp._mainT;
        public float DefaultT
        {
            get => _temp._defaultT;
            set => _temp._defaultT = value;
        }

        public float RawT
        {
            set => _temp._rawT = value;
        }
        public float CurrentT => _temp.CurrentT;
        public float DeltaT => _temp.DeltaT;

        public Vector3 MinPos => _temp._minPos;
        public Vector3 MinDir => _temp._minDir;

        public Vector3 MaxPos => _temp._maxPos;
        public Vector3 MaxDir => _temp._maxDir;


        public Vector3 StartPos => _final._position;
        public Vector3 StartDir => _final._direction;

        public Vector3 EndPos => _final._maxPos;
        public Vector3 EndDir => _final._maxDir;

        public Vector3 MarkerPos
        {
            get
            {
                var width = SegmentData.Width;
                if (width >= SegmentEndData.MinNarrowWidth)
                    return StartPos;
                else
                    return StartPos + StartDir.Turn90(Type == SideType.Right).MakeFlatNormalized() * (SegmentEndData.MinNarrowWidth - width) / 2f;
            }
        }

        public bool IsMinBorderT => _temp.IsMinBorderT;
        public bool IsMaxBorderT => _temp.IsMaxBorderT;
        public bool IsShort => _temp.IsShort;

        public SegmentSide(SegmentEndData segmentData, SideType type)
        {
            Type = type;
            SegmentData = segmentData;

            _temp = new DataStruct(this);
            _final = new DataStruct(this);
        }

        public void SetTrajectory(ITrajectory trajectory)
        {
            _temp.Set(trajectory);
        }

        public void CalculateMain(out Vector3 position, out Vector3 direction)
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(_temp._rawT, _temp._minT + (nodeData.IsMiddleNode || SegmentData.IsNodeLess ? 0f : _temp.DeltaT), _temp._maxT - _temp.DeltaT);
            position = _temp._rawTrajectory.Position(t);
            direction = _temp._rawTrajectory.Tangent(t).normalized;

            if (!SegmentData.IsSlope)
            {
                position.y = SegmentData.NodeId.GetNode().m_position.y;
                direction = direction.MakeFlatNormalized();
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

            direction = NormalizeXZ(direction);
            if (nodeData.IsEndNode)
                direction *= SegmentData.Stretch;

            _temp._position = position;
            _temp._direction = direction;
        }

        public void CalculateNotMain(BezierTrajectory left, BezierTrajectory right)
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(_temp._rawT, _temp._minT + (nodeData.IsMiddleNode || SegmentData.IsNodeLess ? 0f : _temp.DeltaT), _temp._maxT - _temp.DeltaT);
            var position = _temp._rawTrajectory.Position(t);
            var direction = _temp._rawTrajectory.Tangent(t).normalized;

            if (!SegmentData.IsSlope)
            {
                position.y = SegmentData.NodeId.GetNode().m_position.y;
                direction = direction.MakeFlatNormalized();
            }
            else
            {
                GetClosest(left, right, position, out var closestPos, out var closestDir, out var closestT);

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

            direction = NormalizeXZ(direction);
            if (nodeData.IsEndNode)
                direction *= SegmentData.Stretch;

            _temp._position = position;
            _temp._direction = direction;
        }
        public void GetClosest(BezierTrajectory left, BezierTrajectory right, Vector3 position, out Vector3 closestPos, out Vector3 closestDir, out float t)
        {
            left.Trajectory.ClosestPositionAndDirection(position, out var leftClosestPos, out var leftClosestDir, out var leftT);
            right.Trajectory.ClosestPositionAndDirection(position, out var rightClosestPos, out var rightClosestDir, out var rightT);

            if ((leftClosestPos - position).sqrMagnitude < (rightClosestPos - position).sqrMagnitude)
            {
                closestPos = leftClosestPos;
                closestDir = leftClosestDir;
                t = leftT;
            }
            else
            {
                closestPos = rightClosestPos;
                closestDir = rightClosestDir;
                t = rightT;
            }
        }

        public void AfterCalculate()
        {
            _final = _temp;
        }

        public float FromMainT(float t) => _temp._rawTrajectory.FromPartT(1, t);
        public float FromAdditionalT(float t) => _temp._rawTrajectory.FromPartT(0, t);

        public float ToMainT(float t) => _temp._rawTrajectory.ToPartT(1, t);
        public float ToAdditionalT(float t) => _temp._rawTrajectory.ToPartT(0, t);

        public static void FixMiddle(SegmentSide first, SegmentSide second)
        {
            var fixPosition = (first._temp._position + second._temp._position) / 2f;
            first._temp._position = fixPosition;
            second._temp._position = fixPosition;

            var fixDirection = NormalizeXZ(first._temp._direction - second._temp._direction);

            var firstFixDirection = fixDirection;
            firstFixDirection.y = first._temp._direction.y;
            first._temp._direction = firstFixDirection;

            var secondFixDirection = -fixDirection;
            secondFixDirection.y = second._temp._direction.y;
            second._temp._direction = secondFixDirection;
        }

        public void Render(OverlayData dataAllow, OverlayData dataForbidden, OverlayData dataLimit)
        {
            var deltaT = 0.2f / _final._rawTrajectory.Length;
            if (_final._minT == 0f)
            {
                if (_final._rawT >= deltaT)
                    _final._rawTrajectory.Cut(0f, _final._rawT).Render(dataAllow);
            }
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;

                var t = Math.Min(_final._rawT, _final._minT);
                if (t >= _final.DeltaT)
                    _final._rawTrajectory.Cut(0f, t).Render(dataForbidden);

                if (_final._rawT - _final._minT >= 0.2f / _final._rawTrajectory.Length)
                    _final._rawTrajectory.Cut(_final._minT, _final._rawT).Render(dataAllow);
            }
            dataLimit.Color ??= Colors.Purple;
            _final._rawTrajectory.Position(_final._defaultT).RenderCircle(dataLimit);
        }
        public void RenderCircle(OverlayData data)
        {
            var markerPosition = MarkerPos;
            if ((markerPosition - _final._position).sqrMagnitude > 0.25f)
            {
                var color = data.Color.HasValue ? ((Color32)data.Color.Value).SetAlpha(128) : Colors.White128;
                new StraightTrajectory(markerPosition, _final._position).Render(new OverlayData(data.CameraInfo) { Color = color });
            }
            markerPosition.RenderCircle(data, data.Width ?? SegmentEndData.CornerDotRadius * 2, 0f);
        }

        public override string ToString() => $"{Type}: {nameof(_final._rawT)}={_final._rawT}; {nameof(_final._minT)}={_final._minT}; {nameof(_final._maxT)}={_final._maxT}; {nameof(_final._position)}={_final._position};";
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
