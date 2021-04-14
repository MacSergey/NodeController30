using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using KianCommons;
using System;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModsCommon.Utilities;
using NodeController.Utilities;

namespace NodeController
{
    [Serializable]
    public class SegmentEndData : INetworkData, IOverlay
    {
        #region STATIC

        public static float CircleRadius => 2.5f;
        public static float DotRadius => 0.75f;
        public static float MinPossibleRotate => -80f;
        public static float MaxPossibleRotate => 80f;

        #endregion

        #region PROPERTIES

        public string Title => $"Segment #{Id}";

        public ushort NodeId { get; set; }
        public ushort Id { get; set; }

        public NetSegment Segment => Id.GetSegment();
        public NetInfo Info => Segment.Info;
        public NetNode Node => NodeId.GetNode();
        public NodeData NodeData => Manager.Instance[NodeId];
        public bool IsStartNode => Segment.IsStartNode(NodeId);
        public SegmentEndData Other => Manager.Instance[Segment.GetOtherNode(NodeId), Id, true];

        public BezierTrajectory RawSegmentBezier { get; private set; }
        public BezierTrajectory SegmentBezier { get; private set; }
        public SegmentSide LeftSide { get; } = new SegmentSide(SideType.Left);
        public SegmentSide RightSide { get; } = new SegmentSide(SideType.Right);
        public float AbsoluteAngle => RawSegmentBezier.StartDirection.AbsoluteAngle();

        public float SegmentLimit { get; private set; }
        public float SegmentT { get; private set; }

        public bool IsNotOverlap => LeftSide.IsNotOverlap && RightSide.IsNotOverlap;


        public float DefaultOffset => CSURUtilities.GetMinCornerOffset(Id, NodeId);
        public bool DefaultIsFlat => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => DefaultIsFlat && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool IsSlope { get; set; }
        public bool IsTwist { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= TwistAngle == 0;
                ret &= IsSlope == !DefaultIsFlat;
                ret &= IsTwist == DefaultIsTwist;

                ret &= NoCrossings == false;
                ret &= NoMarkings == false;
                ret &= NoJunctionTexture == false;
                ret &= NoJunctionProps == false;
                ret &= NoTLProps == false;
                return ret;
            }
        }
        private float _offsetValue;
        private float _rotateValue;
        private float _minRotate;
        private float _maxRotate;
        private float _minOffset;

        public float Offset
        {
            get => _offsetValue;
            set => _offsetValue = Math.Max(value, _minOffset);
        }
        public float MinOffset
        {
            get => _minOffset;
            private set
            {
                _minOffset = value;
                Offset = Offset;
            }
        }
        public float Shift { get; set; }
        public float RotateAngle
        {
            get => _rotateValue;
            set => _rotateValue = Mathf.Clamp(value, MinRotate, MaxRotate);
        }
        public float MinRotate
        {
            get => _minRotate;
            private set
            {
                _minRotate = value;
                RotateAngle = RotateAngle;
            }
        }
        public float MaxRotate
        {
            get => _maxRotate;
            private set
            {
                _maxRotate = value;
                RotateAngle = RotateAngle;
            }
        }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }

        public bool IsBorderOffset => Offset == MinOffset;
        public bool IsBorderRotate => RotateAngle == MinRotate || RotateAngle == MaxRotate;

        public bool CanModifyTwist => CanTwist(Id, NodeId);
        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData != null && NodeData.Type == NodeStyleType.Stretch)
                    return false; // always ignore.
                else if (NoMarkings)
                    return true; // always hide
                else
                    return null; // default.
            }
        }

        public SegmentSide this[SideType side] => side switch
        {
            SideType.Left => LeftSide,
            SideType.Right => RightSide,
            _ => throw new NotImplementedException(),
        };

        public Vector3 Position { get; private set; }
        public Vector3 Direction => (RightSide.Direction + LeftSide.Direction).normalized;
        public Vector3 EndDirection => (RightSide.Position - LeftSide.Position).normalized;


        #endregion

        #region BASIC

        public SegmentEndData(ushort segmentId, ushort nodeId)
        {
            Id = segmentId;
            NodeId = nodeId;

            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            GetSegmentBeziers(Id, out var bezier, out var leftSide, out var rightSide);
            if (IsStartNode)
            {
                RawSegmentBezier = bezier;
                LeftSide.RawBezier = leftSide;
                RightSide.RawBezier = rightSide;
            }
            else
            {
                RawSegmentBezier = bezier.Invert();
                LeftSide.RawBezier = rightSide.Invert();
                RightSide.RawBezier = leftSide.Invert();
            }
            CalculateMinMaxRotate();

            ResetToDefault();
        }
        public void UpdateNode() => Manager.Instance.Update(NodeId);

        public void ResetToDefault()
        {
            Offset = DefaultOffset;

            IsSlope = !DefaultIsFlat;
            IsTwist = DefaultIsTwist;
            NoCrossings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
        }

        public static void UpdateSegmentBezier(ushort segmentId)
        {
            GetSegmentBeziers(segmentId, out var bezier, out var leftSide, out var rightSide);

            Manager.GetSegmentData(segmentId, out var start, out var end);
            if (start != null)
            {
                start.RawSegmentBezier = bezier;
                start.LeftSide.RawBezier = leftSide;
                start.RightSide.RawBezier = rightSide;
            }
            if (end != null)
            {
                end.RawSegmentBezier = bezier.Invert();
                end.LeftSide.RawBezier = rightSide.Invert();
                end.RightSide.RawBezier = leftSide.Invert();
            }
        }
        private static void GetSegmentBeziers(ushort segmentId, out BezierTrajectory bezier, out BezierTrajectory leftSide, out BezierTrajectory rightSide)
        {
            var segment = segmentId.GetSegment();

            var startPos = segment.m_startNode.GetNode().m_position;
            var startDir = segment.m_startDirection;
            var endPos = segment.m_endNode.GetNode().m_position;
            var endDir = segment.m_endDirection;
            ShiftSegment(true, segmentId, ref startPos, ref startDir, ref endPos, ref endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);

            var halfWidth = segment.Info.m_halfWidth;
            var startNormal = startDir.Turn90(false);
            var endNormal = endDir.Turn90(true);

            leftSide = new BezierTrajectory(startPos + startNormal * halfWidth, startDir, endPos + endNormal * halfWidth, endDir);
            rightSide = new BezierTrajectory(startPos - startNormal * halfWidth, startDir, endPos - endNormal * halfWidth, endDir);
        }
        private static void ShiftSegment(bool isStart, ushort segmentId, ref Vector3 startPos, ref Vector3 startDir, ref Vector3 endPos, ref Vector3 endDir)
        {
            Manager.GetSegmentData(segmentId, out var start, out var end);
            var startShift = (isStart ? start : end)?.Shift ?? 0f;
            var endShift = (isStart ? end : start)?.Shift ?? 0f;

            if (startShift == 0f && endShift == 0f)
                return;

            var shift = (startShift + endShift) / 2;
            var dir = endPos - startPos;
            var sin = shift / dir.XZ().magnitude;
            var deltaAngle = Mathf.Asin(sin);
            var normal = dir.TurnRad(Mathf.PI / 2 + deltaAngle, true).normalized;

            startPos -= normal * startShift;
            endPos += normal * endShift;
            startDir = startDir.TurnRad(deltaAngle, true);
            endDir = endDir.TurnRad(deltaAngle, true);
        }
        public static void CalculateLimits(NodeData data)
        {
            var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
            var count = endDatas.Length;

            var leftLimits = new float[count];
            var rightLimits = new float[count];

            for (var i = 0; i < count; i += 1)
            {
                var j = (i + 1) % count;

                var intersect = Intersection.CalculateSingle(endDatas[i].LeftSide.RawBezier, endDatas[j].RightSide.RawBezier);
                if (intersect.IsIntersect)
                {
                    leftLimits[i] = Mathf.Max(leftLimits[i], intersect.FirstT);
                    rightLimits[j] = Mathf.Max(rightLimits[j], intersect.SecondT);
                }
                intersect = Intersection.CalculateSingle(endDatas[i].LeftSide.RawBezier, endDatas[j].LeftSide.RawBezier);
                if (intersect.IsIntersect)
                {
                    leftLimits[i] = Mathf.Max(leftLimits[i], intersect.FirstT);
                    leftLimits[j] = Mathf.Max(leftLimits[j], intersect.SecondT);
                }
                intersect = Intersection.CalculateSingle(endDatas[i].RightSide.RawBezier, endDatas[j].RightSide.RawBezier);
                if (intersect.IsIntersect)
                {
                    rightLimits[i] = Mathf.Max(rightLimits[i], intersect.FirstT);
                    rightLimits[j] = Mathf.Max(rightLimits[j], intersect.SecondT);
                }
            }
            for (var i = 0; i < count; i += 1)
            {
                endDatas[i].LeftSide.LimitT = leftLimits[i];
                endDatas[i].RightSide.LimitT = rightLimits[i];
                endDatas[i].Calculate();
            }
        }
        private void Calculate()
        {
            CalculateCornerOffset(LeftSide);
            CalculateCornerOffset(RightSide);
            CalculateSegmentLimit();
            CalculatePosition();
            CalculateMinMaxRotate();
            UpdateCachedSuperElevation();
        }
        private void CalculateCornerOffset(SegmentSide side)
        {
            var t = RawSegmentBezier.Travel(0f, Offset);
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).TurnDeg(90 + RotateAngle, true);

            var line = new StraightTrajectory(position, position + direction, false);
            var intersection = Intersection.CalculateSingle(side.RawBezier, line);

            if (intersection.IsIntersect)
                side.RawT = intersection.FirstT;
            else if (RotateAngle == 0f)
                side.RawT = t <= 0.5f ? 0f : 1f;
            else
                side.RawT = side.Type == SideType.Left ^ RotateAngle > 0f ? 0f : 1f;
        }
        private void CalculateSegmentLimit()
        {
            var limitLine = new StraightTrajectory(LeftSide.Bezier.StartPosition, RightSide.Bezier.StartPosition);
            var intersect = Intersection.CalculateSingle(RawSegmentBezier, limitLine);
            if (intersect.IsIntersect)
            {
                SegmentLimit = intersect.FirstT;
                SegmentBezier = RawSegmentBezier.Cut(SegmentLimit, 1f);
                MinOffset = RawSegmentBezier.Cut(0f, SegmentLimit).Length;
            }
            else
            {
                SegmentLimit = 0f;
                SegmentBezier = RawSegmentBezier.Copy();
                MinOffset = 0f;
            }
        }
        private void CalculateMinMaxRotate()
        {
            var t = RawSegmentBezier.Travel(0f, Offset);
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).Turn90(false);

            var startLeft = GetAngle(LeftSide.Bezier.StartPosition - position, direction);
            var endLeft = GetAngle(LeftSide.Bezier.EndPosition - position, direction);
            var startRight = GetAngle(RightSide.Bezier.StartPosition - position, direction);
            var endRight = GetAngle(RightSide.Bezier.EndPosition - position, direction);

            MinRotate = Mathf.Max(startLeft, FixAngle(endRight), MinPossibleRotate);
            MaxRotate = Mathf.Min(endLeft, FixAngle(startRight), MaxPossibleRotate);

            RotateAngle = RotateAngle;

            static float GetAngle(Vector3 cornerDir, Vector3 segmentDir)
            {
                var angle = Vector3.Angle(segmentDir, cornerDir);
                var sign = Mathf.Sign(Vector3.Cross(segmentDir, cornerDir).y);

                return sign * angle;
            }
            static float FixAngle(float angle) => angle < 0 ? angle + 180f : angle - 180f;
        }
        private void CalculatePosition()
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var intersect = Intersection.CalculateSingle(line, RawSegmentBezier);
            Position = line.Position(intersect.IsIntersect ? intersect.FirstT : 0.5f);
        }
        private void UpdateCachedSuperElevation()
        {
            var diff = RightSide.Position - LeftSide.Position;
            var se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public static bool CanTwist(ushort segmentId, ushort nodeId)
        {
            var segmentIds = nodeId.GetNode().SegmentIds().ToArray();

            if (segmentIds.Length == 1)
                return false;

            var segment = segmentId.GetSegment();
            var firstSegmentId = segment.GetLeftSegment(nodeId);
            var secondSegmentId = segment.GetRightSegment(nodeId);
            var nodeData = Manager.Instance[nodeId];
            var segmentEnd1 = nodeData[firstSegmentId];
            var segmentEnd2 = nodeData[secondSegmentId];

            bool flat1 = !segmentEnd1?.IsSlope ?? firstSegmentId.GetSegment().Info.m_flatJunctions;
            bool flat2 = !segmentEnd2?.IsSlope ?? secondSegmentId.GetSegment().Info.m_flatJunctions;
            if (flat1 && flat2)
                return false;

            if (segmentIds.Length == 2)
            {
                var dir1 = firstSegmentId.GetSegment().GetDirection(nodeId);
                var dir = segmentId.GetSegment().GetDirection(nodeId);
                if (Mathf.Abs(VectorUtils.DotXZ(dir, dir1)) > 0.999f)
                    return false;
            }

            return true;
        }
        public override string ToString() => $"segment:{Id} node:{NodeId}";

        #endregion

        #region RENDER

        public void Render(OverlayData data) => Render(data, data, data);
        public void Render(OverlayData contourData, OverlayData outterData, OverlayData innerData)
        {
            var data = Manager.Instance[NodeId];

            RenderOther(contourData);
            if (data.IsMoveableEnds)
            {
                RenderCutEnd(contourData);
                RenderOutterCircle(outterData);
                RenderInnerCircle(innerData);
            }
            else
                RenderEnd(contourData);
        }
        public void RenderSides(OverlayData dataAllow, OverlayData dataForbidden)
        {
            LeftSide.Render(dataAllow, dataForbidden);
            RightSide.Render(dataAllow, dataForbidden);
        }
        private void RenderCutEnd(OverlayData data)
        {
            var leftLine = new StraightTrajectory(LeftSide.Position, Position);
            leftLine = leftLine.Cut(0f, 1f - (CircleRadius / leftLine.Length));
            leftLine.Render(data);

            var rightLine = new StraightTrajectory(RightSide.Position, Position);
            rightLine = rightLine.Cut(0f, 1f - (CircleRadius / rightLine.Length));
            rightLine.Render(data);
        }
        private void RenderEnd(OverlayData data) => new StraightTrajectory(LeftSide.Position, RightSide.Position).Render(data);
        private void RenderOther(OverlayData data)
        {
            if (Other is SegmentEndData otherSegmentData)
            {
                var otherLeftCorner = otherSegmentData[SideType.Left];
                var otherRightCorner = otherSegmentData[SideType.Right];

                var leftSide = new BezierTrajectory(LeftSide.Position, LeftSide.Direction, otherRightCorner.Position, otherRightCorner.Direction);
                leftSide.Render(data);
                var rightSide = new BezierTrajectory(RightSide.Position, RightSide.Direction, otherLeftCorner.Position, otherLeftCorner.Direction);
                rightSide.Render(data);
                var endSide = new StraightTrajectory(otherLeftCorner.Position, otherRightCorner.Position);
                endSide.Render(data);
            }
        }

        private void RenderInnerCircle(OverlayData data) => RenderCircle(data, DotRadius * 2, 0f);
        private void RenderOutterCircle(OverlayData data) => RenderCircle(data, CircleRadius * 2 + 0.5f, CircleRadius * 2 - 0.5f);

        private void RenderCircle(OverlayData data) => Position.RenderCircle(data);
        private void RenderCircle(OverlayData data, float from, float to)
        {
            data.Width = from;
            do
            {
                RenderCircle(data);
                data.Width = Mathf.Max(data.Width.Value - 0.43f, to);
            }
            while (data.Width > to);
        }

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
    public class SegmentSide
    {
        private float _limitT;
        private BezierTrajectory _rawBezier;
        private float _rawT;

        public SideType Type { get; }
        public BezierTrajectory RawBezier
        {
            get => _rawBezier;
            set
            {
                _rawBezier = value;
                Update();
            }
        }
        public BezierTrajectory Bezier { get; private set; }
        public float LimitT
        {
            get => _limitT;
            set
            {
                _limitT = value;
                Update();
            }
        }
        public float RawT
        {
            get => _rawT;
            set
            {
                _rawT = value;
                Update();
            }
        }
        private float T => Mathf.Max(RawT, LimitT);

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }

        public bool IsNotOverlap => RawT >= LimitT;

        public SegmentSide(SideType type)
        {
            Type = type;
        }
        private void Update()
        {
            Bezier = RawBezier.Cut(_limitT, 1f);

            var t = T;
            if (RawT <= LimitT)
                t += RawBezier.Travel(0f, 0.01f);

            Position = RawBezier.Position(t);
            Direction = RawBezier.Tangent(t);
        }

        public void Render(OverlayData dataAllow, OverlayData dataForbidden)
        {
            if (LimitT == 0f)
                RawBezier.Cut(0f, RawT).Render(dataAllow);
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;
                RawBezier.Cut(0f, Math.Min(RawT, LimitT)).Render(dataForbidden);
                if (RawT > LimitT)
                    RawBezier.Cut(LimitT, RawT).Render(dataAllow);
            }
        }
    }
    public enum SideType : byte
    {
        Left,
        Right
    }
}
