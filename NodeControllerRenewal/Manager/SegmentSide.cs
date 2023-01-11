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
            public Quaternion _dirRotation;
            public float _dirRatio;
            public Vector3 _deltaPos;

            public Vector3 _maxPos;
            public Vector3 _maxDir;

            public float DeltaT
            {
                get
                {
                    var deltaT = 0.05f / _rawTrajectory.Length;
                    return deltaT;
                }
            }
            public float CurrentT
            {
                get
                {
                    var deltaT = DeltaT;
                    var currentT = Mathf.Clamp(_rawT, _minT + deltaT, _maxT - deltaT);
                    return currentT;
                }
            }

            public bool IsMinBorderT
            {
                get
                {
                    var deltaT = DeltaT;
                    return _rawT - deltaT <= _minT;
                }
            }
            public bool IsMaxBorderT
            {
                get
                {
                    var deltaT = DeltaT;
                    return _rawT + deltaT >= _maxT;
                }
            }
            public bool IsShort
            {
                get
                {
                    var isShort = (_maxT - CurrentT) <= (1f / _rawTrajectory.Length);
                    return isShort;
                }
            }

            public DataStruct(SegmentSide segmentSide)
            {
                _segmentSide = segmentSide;
                _rawTrajectory = new CombinedTrajectory(new StraightTrajectory(Vector3.zero, Vector3.zero));

                _minT = 0f;
                _maxT = 1f;
                _rawT = 0f;
                _mainT = 0f;
                _defaultT = 0f;

                _minPos = default;
                _minDir = default;
                _position = default;
                _direction = default;
                _deltaPos = default;
                _dirRotation = Quaternion.identity;
                _dirRatio = 1;
                _maxPos = default;
                _maxDir = default;
            }

            public void Set(ITrajectory trajectory)
            {
                if (_rawTrajectory.IsZero || !_rawTrajectory[1].Equals(trajectory))
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
                _minDir = _rawTrajectory.Tangent(_minT).normalized;

                _maxPos = _rawTrajectory.Position(_maxT);
                _maxDir = -_rawTrajectory.Tangent(_maxT).normalized;
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


        public Vector3 StartPos => _final._position + _final._deltaPos;
        public Vector3 StartDir => NormalizeXZ(_final._dirRotation * _final._direction) * _final._dirRatio;

        public Vector3 EndPos => _final._maxPos;
        public Vector3 EndDir => _final._maxDir;

        public Vector3 TempPos => _temp._position + _temp._deltaPos;
        public Vector3 TempDir => NormalizeXZ(_temp._dirRotation * _temp._direction) * _temp._dirRatio;

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

        public void CalculateMain()
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(_temp._rawT, _temp._minT + (nodeData.IsMiddleNode || SegmentData.FinalNodeLess ? 0f : _temp.DeltaT), _temp._maxT - _temp.DeltaT);
            var position = _temp._rawTrajectory.Position(t);
            var direction = _temp._rawTrajectory.Tangent(t).normalized;
            _temp._deltaPos = Vector3.zero;
            _temp._dirRotation = Quaternion.identity;
            _temp._dirRatio = 1f;

            switch (SegmentData.Mode)
            {
                case Mode.Flat:
                    {
                        position.y = SegmentData.NodeId.GetNode().m_position.y;
                        direction = direction.MakeFlatNormalized();

                        if (nodeData.IsEndNode)
                            _temp._dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.Slope:
                    {
                        if (nodeData.Style.SupportSlope != SupportOption.None)
                        {
                            _temp._dirRotation = Quaternion.AngleAxis(SegmentData.SlopeAngle, direction.MakeFlat().Turn90(true));
                        }
                        if (nodeData.Style.SupportTwist != SupportOption.None)
                        {
                            var ratio = Mathf.Sin(SegmentData.TwistAngle * Mathf.Deg2Rad);
                            if (nodeData.Style.SupportStretch != SupportOption.None)
                                ratio *= SegmentData.Stretch;

                            _temp._deltaPos.y += (Type == SideType.Left ? -1 : 1) * SegmentData.Id.GetSegment().Info.m_halfWidth * ratio;
                        }

                        if (nodeData.IsEndNode)
                            _temp._dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.FreeForm:
                    {
                        var deltaPos = Type switch
                        {
                            SideType.Left => SegmentData.LeftPosDelta,
                            SideType.Right => SegmentData.RightPosDelta,
                            _ => Vector3.zero,
                        };

                        var angle = direction.AbsoluteAngle();
                        _temp._deltaPos += Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;

                        var deltaDir = Type switch
                        {
                            SideType.Left => SegmentData.LeftDirDelta,
                            SideType.Right => SegmentData.RightDirDelta,
                            _ => Vector3.zero,
                        };
                        if (deltaDir.x != 0f)
                            _temp._dirRotation *= Quaternion.AngleAxis(deltaDir.x, Vector3.up);
                        if (deltaDir.y != 0f)
                            _temp._dirRotation *= Quaternion.AngleAxis(deltaDir.y, Vector3.forward);
                    }
                    break;
            }

            _temp._position = position;
            _temp._direction = direction.normalized;
        }

        public void CalculateNotMain(BezierTrajectory left, BezierTrajectory right)
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(_temp._rawT, _temp._minT + (nodeData.IsMiddleNode || SegmentData.FinalNodeLess ? 0f : _temp.DeltaT), _temp._maxT - _temp.DeltaT);
            var position = _temp._rawTrajectory.Position(t);
            var direction = _temp._rawTrajectory.Tangent(t).normalized;
            _temp._deltaPos = Vector3.zero;
            _temp._dirRotation = Quaternion.identity;
            _temp._dirRatio = 1f;

            switch (SegmentData.Mode)
            {
                case Mode.Flat:
                    {
                        position.y = SegmentData.NodeId.GetNode().m_position.y;
                        direction = direction.MakeFlatNormalized();

                        if (nodeData.IsEndNode)
                            _temp._dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.Slope:
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

                        if (nodeData.IsEndNode)
                            _temp._dirRatio *= SegmentData.Stretch;
                    }
                    break;
            }

            _temp._position = position;
            _temp._direction = direction.normalized;
        }

        private void GetClosest(BezierTrajectory left, BezierTrajectory right, Vector3 position, out Vector3 closestPos, out Vector3 closestDir, out float t)
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
        public static void FixBend(SegmentSide left, SegmentSide right)
        {
            var isLeft = left.SegmentData.NodeData.IsTwoRoads && left.SegmentData.Mode != Mode.Flat;
            var isRight = right.SegmentData.NodeData.IsTwoRoads && right.SegmentData.Mode != Mode.Flat;

            if (isLeft == isRight)
                return;
            else if (isLeft)
            {
                var bezier = new BezierTrajectory(left.MainTrajectory.StartPosition, left.MainTrajectory.StartDirection, right._temp._position, right._temp._direction, true, true, true);
                bezier.GetHitPosition(new Segment3(left._temp._position, left._temp._position + Vector3.up), out _, out var t, out _);
                left._temp._position = bezier.Position(t);
                left._temp._direction = bezier.Tangent(t).normalized;
            }
            else
            {
                var bezier = new BezierTrajectory(right.MainTrajectory.StartPosition, right.MainTrajectory.StartDirection, left._temp._position, left._temp._direction, true, true, true);
                bezier.GetHitPosition(new Segment3(right._temp._position, right._temp._position + Vector3.up), out _, out var t, out _);
                right._temp._position = bezier.Position(t);
                right._temp._direction = bezier.Tangent(t).normalized;
            }
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
