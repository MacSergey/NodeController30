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
        public SegmentSide LeftSide { get; }
        public SegmentSide RightSide { get; }
        public float AbsoluteAngle => RawSegmentBezier.StartDirection.AbsoluteAngle();

        public float SegmentMinT { get; private set; }


        public float DefaultOffset => Mathf.Max(Info.m_minCornerOffset, Info.m_halfWidth < 4f ? 0f : 8f);
        public bool DefaultIsFlat => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => DefaultIsFlat && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
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
                return ret;
            }
        }
        private float _offsetValue;
        private float _rotateValue;
        private float _minOffset;

        public float Offset
        {
            get => _offsetValue;
            set => SetOffset(value, true);
        }
        public float MinPossibleOffset { get; private set; }
        public float MaxPossibleOffset { get; private set; }
        public float MinOffset
        {
            get => _minOffset;
            private set
            {
                _minOffset = value;
                SetOffset(Offset);
            }
        }
        public float Shift { get; set; }
        public float RotateAngle
        {
            get => _rotateValue;
            set => _rotateValue = Mathf.Clamp(value, MinRotate, MaxRotate);
        }
        public float MinRotate { get; set; }
        public float MaxRotate { get; set; }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }

        public bool IsBorderOffset => Offset == MinOffset;
        public bool IsBorderRotate => RotateAngle == MinRotate || RotateAngle == MaxRotate;
        public bool IsBorderT => LeftSide.RawT >= RightSide.RawT ? LeftSide.IsBorderT : RightSide.IsBorderT;

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
        public Vector3 Direction { get; private set; }


        #endregion

        #region BASIC

        public SegmentEndData(ushort segmentId, ushort nodeId)
        {
            Id = segmentId;
            NodeId = nodeId;

            LeftSide = new SegmentSide(this, SideType.Left);
            RightSide = new SegmentSide(this, SideType.Right);

            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            CalculateSegmentBeziers(Id, out var bezier, out var leftSide, out var rightSide);
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
        }
        public void UpdateNode() => Manager.Instance.Update(NodeId);

        public void ResetToDefault(NodeStyle style, bool force)
        {
            MinPossibleOffset = style.MinOffset;
            MaxPossibleOffset = style.MaxOffset;
            if (!style.SupportOffset || force)
                SetOffset(DefaultOffset);
            else
                SetOffset(Offset);
            if (!style.SupportShift || force)
                Shift = NodeStyle.DefaultShift;
            if (!style.SupportRotate || force)
                RotateAngle = NodeStyle.DefaultRotate;
            if (!style.SupportSlope || force)
                SlopeAngle = NodeStyle.DefaultSlope;
            if (!style.SupportTwist || force)
                TwistAngle = NodeStyle.DefaultTwist;
            if (!style.SupportNoMarking || force)
                NoMarkings = NodeStyle.DefaultNoMarking;
            if (!style.SupportSlopeJunction || force)
                IsSlope = NodeStyle.DefaultSlopeJunction;

            IsSlope = !DefaultIsFlat;
            IsTwist = DefaultIsTwist;
            NoCrossings = false;

            Calculate();
        }

        private void SetOffset(float value, bool changeRotate = false)
        {
            _offsetValue = Mathf.Clamp(Math.Max(value, _minOffset), MinPossibleOffset, MaxPossibleOffset);

            if (changeRotate && IsBorderT)
                SetRotate(0f);
        }
        public void SetRotate(float value)
        {
            CalculateMinMaxRotate();
            RotateAngle = value;
        }

        #endregion

        #region CALCULATE

        public static void Update(ushort segmentId)
        {
            CalculateSegmentBeziers(segmentId, out var bezier, out var leftSide, out var rightSide);
            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);

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
        public static void CalculateSegmentBeziers(ushort segmentId, out BezierTrajectory bezier, out BezierTrajectory leftSide, out BezierTrajectory rightSide)
        {
            var segment = segmentId.GetSegment();
            GetSegmentPosAndDir(segmentId, segment.m_startNode, out var startPos, out var startDir, out var endPos, out var endDir);

            Fix(segment.m_startNode, segmentId, ref startDir);
            Fix(segment.m_endNode, segmentId, ref endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);

            var halfWidth = segment.Info.m_halfWidth;
            var startNormal = startDir.MakeFlatNormalized().Turn90(false);
            var endNormal = endDir.MakeFlatNormalized().Turn90(true);

            leftSide = new BezierTrajectory(startPos + startNormal * halfWidth, startDir, endPos + endNormal * halfWidth, endDir);
            rightSide = new BezierTrajectory(startPos - startNormal * halfWidth, startDir, endPos - endNormal * halfWidth, endDir);

            static void Fix(ushort nodeId, ushort ignoreSegmentId, ref Vector3 dir)
            {
                if (Manager.Instance[nodeId] is NodeData startData && startData.IsMiddleNode)
                {
                    var startNearSegmentId = startData.SegmentIds.First(s => s != ignoreSegmentId);
                    GetSegmentPosAndDir(startNearSegmentId, nodeId, out _, out var nearDir, out _, out _);
                    dir = (dir - nearDir).normalized;
                }
            }
        }

        private static void GetSegmentPosAndDir(ushort segmentId, ushort startNodeId, out Vector3 startPos, out Vector3 startDir, out Vector3 endPos, out Vector3 endDir)
        {
            var segment = segmentId.GetSegment();
            var isStart = segment.IsStartNode(startNodeId);

            startPos = (isStart ? segment.m_startNode : segment.m_endNode).GetNode().m_position;
            startDir = isStart ? segment.m_startDirection : segment.m_endDirection;
            endPos = (isStart ? segment.m_endNode : segment.m_startNode).GetNode().m_position;
            endDir = isStart ? segment.m_endDirection : segment.m_startDirection;

            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);
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

        public static void Update(NodeData data)
        {
            var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
            var count = endDatas.Length;

            var leftMitT = new float[count];
            var rightMinT = new float[count];
            var isMiddle = data.IsMiddleNode;

            for (var i = 0; i < count; i += 1)
            {
                if (count == 1 || isMiddle)
                {
                    leftMitT[i] = 0f;
                    rightMinT[i] = 0f;
                }
                else
                {
                    var j = (i + 1) % count;

                    var intersect = Intersection.CalculateSingle(endDatas[i].LeftSide.RawBezier, endDatas[j].RightSide.RawBezier);
                    if (intersect.IsIntersect)
                    {
                        leftMitT[i] = Mathf.Max(leftMitT[i], intersect.FirstT);
                        rightMinT[j] = Mathf.Max(rightMinT[j], intersect.SecondT);
                    }
                    intersect = Intersection.CalculateSingle(endDatas[i].LeftSide.RawBezier, endDatas[j].LeftSide.RawBezier);
                    if (intersect.IsIntersect)
                    {
                        leftMitT[i] = Mathf.Max(leftMitT[i], intersect.FirstT);
                        leftMitT[j] = Mathf.Max(leftMitT[j], intersect.SecondT);
                    }
                    intersect = Intersection.CalculateSingle(endDatas[i].RightSide.RawBezier, endDatas[j].RightSide.RawBezier);
                    if (intersect.IsIntersect)
                    {
                        rightMinT[i] = Mathf.Max(rightMinT[i], intersect.FirstT);
                        rightMinT[j] = Mathf.Max(rightMinT[j], intersect.SecondT);
                    }
                }
            }

            for (var i = 0; i < count; i += 1)
            {
                endDatas[i].LeftSide.MinT = leftMitT[i];
                endDatas[i].RightSide.MinT = rightMinT[i];

                endDatas[i].LeftSide.SetDelta = !isMiddle;
                endDatas[i].RightSide.SetDelta = !isMiddle;

                endDatas[i].Calculate();
            }
        }
        private void Calculate()
        {
            CalculateCornerOffset(LeftSide);
            CalculateCornerOffset(RightSide);
            CalculateSegmentLimit();
            CalculatePositionAndDirection();
            CalculateMinMaxRotate();
            UpdateCachedSuperElevation();
        }
        private void CalculateCornerOffset(SegmentSide side)
        {
            var t = RawSegmentBezier.Travel(0f, Offset);
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).MakeFlatNormalized().TurnDeg(90 + RotateAngle, true);

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
                SegmentMinT = intersect.FirstT;
                SegmentBezier = RawSegmentBezier.Cut(SegmentMinT, 1f);
                MinOffset = RawSegmentBezier.Cut(0f, SegmentMinT).Length;
            }
            else
            {
                SegmentMinT = 0f;
                SegmentBezier = RawSegmentBezier.Copy();
                MinOffset = 0f;
            }
        }
        private void CalculatePositionAndDirection()
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var intersect = Intersection.CalculateSingle(line, RawSegmentBezier);
            Position = RawSegmentBezier.Position(intersect.IsIntersect ? intersect.SecondT : 0f);
            Direction = RawSegmentBezier.Tangent(intersect.IsIntersect ? intersect.SecondT : 0f).normalized;
        }
        private void CalculateMinMaxRotate()
        {
            var t = RawSegmentBezier.Travel(0f, Offset);
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).MakeFlatNormalized().Turn90(false);

            var startLeft = GetAngle(LeftSide.Bezier.StartPosition - position, direction);
            var endLeft = GetAngle(LeftSide.Bezier.EndPosition - position, direction);
            var startRight = GetAngle(position - RightSide.Bezier.StartPosition, direction);
            var endRight = GetAngle(position - RightSide.Bezier.EndPosition, direction);

            MinRotate = Mathf.Clamp(Mathf.Max(startLeft, endRight), MinPossibleRotate, MaxPossibleRotate);
            MaxRotate = Mathf.Clamp(Mathf.Min(endLeft, startRight), MinPossibleRotate, MaxPossibleRotate);

            RotateAngle = RotateAngle;

            static float GetAngle(Vector3 cornerDir, Vector3 segmentDir)
            {
                var angle = Vector3.Angle(segmentDir, cornerDir);
                var sign = Mathf.Sign(Vector3.Cross(segmentDir, cornerDir).y);
                return sign * angle;
            }
        }
        private void UpdateCachedSuperElevation()
        {
            var diff = RightSide.Position - LeftSide.Position;
            var se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

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
        private float _minT;
        private BezierTrajectory _rawBezier;
        private float _rawT;

        public SideType Type { get; }
        public SegmentEndData Data { get; }
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
        public float MinT
        {
            get => _minT;
            set
            {
                _minT = value;
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
        public bool SetDelta { get; set; }
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public bool IsBorderT => RawT - 0.001f <= MinT;

        public SegmentSide(SegmentEndData data, SideType type)
        {
            Data = data;
            Type = type;
        }
        private void Update()
        {
            Bezier = RawBezier.Cut(_minT, 1f);

            var delta = SetDelta ? 0.05f / RawBezier.Length : 0f;
            var t = Mathf.Max(RawT + delta, MinT);
            var position = RawBezier.Position(t);
            var direction = RawBezier.Tangent(t);
            if (!Data.IsSlope)
            {
                position.y = RawBezier.StartPosition.y;
                direction.y = RawBezier.StartDirection.y;
            }
            direction.Normalize();

            Position = position;
            Direction = direction;
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
                if (RawT > MinT)
                    RawBezier.Cut(MinT, RawT).Render(dataAllow);
            }
        }

        public override string ToString() => $"{Type}: RawT={RawT}; MinT={MinT}; Pos={Position};";
    }
    public enum SideType : byte
    {
        Left,
        Right
    }
}
