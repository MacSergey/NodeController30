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
    public class SegmentEndData : INetworkData, IOverlay
    {
        #region STATIC

        public static float CircleRadius => 2.5f;
        public static float DotRadius => 1f;
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
        private SegmentSide LeftSide { get; }
        private SegmentSide RightSide { get; }
        public float AbsoluteAngle => RawSegmentBezier.StartDirection.AbsoluteAngle();

        public float SegmentMinT { get; private set; }
        public float SegmentMaxT { get; private set; }

        public float DefaultOffset => Mathf.Clamp(Mathf.Max(Info.m_minCornerOffset, Info.m_halfWidth < 4f ? 2f : 10f), MinPossibleOffset, MaxPossibleOffset);
        public bool DefaultIsSlope => !Info.m_flatJunctions && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => !DefaultIsSlope && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool IsSlope { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= TwistAngle == 0;
                ret &= IsSlope == DefaultIsSlope;

                ret &= NoCrossings == false;
                ret &= NoMarkings == false;
                return ret;
            }
        }
        private float _offsetValue;
        private float _minOffset = 0f;
        private float _maxOffse = 100f;
        private float _rotateValue;

        public float Offset
        {
            get => _offsetValue;
            set
            {
                SetOffset(value, true);
                KeepDefaults = false;
            }
        }
        public float OffsetT
        {
            get
            {
                if (Offset == MinOffset)
                    return SegmentMinT;
                else if (Offset == MaxOffset)
                    return SegmentMaxT;
                else
                    return RawSegmentBezier.Trajectory.Travel(Offset);
            }
        }
        public float MinPossibleOffset { get; private set; } = 0f;
        public float MaxPossibleOffset { get; private set; } = 100f;
        public float MinOffset
        {
            get => Mathf.Max(_minOffset, MinPossibleOffset);
            private set => _minOffset = value;
        }
        public float MaxOffset
        {
            get => Mathf.Min(_maxOffse, MaxPossibleOffset);
            private set => _maxOffse = value;
        }

        public float Shift { get; set; }
        public float RotateAngle
        {
            get => _rotateValue;
            set
            {
                SetRotate(value);
                KeepDefaults = false;
            }
        }
        public float MinRotate { get; set; }
        public float MaxRotate { get; set; }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }

        public bool IsStartBorderOffset => Offset == MinOffset;
        public bool IsEndBorderOffset => Offset == MaxOffset;
        public bool IsBorderRotate => RotateAngle == MinRotate || RotateAngle == MaxRotate;
        public bool IsMinBorderT => RotateAngle >= 0 ? LeftSide.IsMinBorderT : RightSide.IsMinBorderT;
        public bool IsMaxBorderT => RotateAngle >= 0 ? LeftSide.IsMaxBorderT : RightSide.IsMaxBorderT;
        public bool IsDefaultT => LeftSide.IsDefaultT && RightSide.IsDefaultT;
        private bool KeepDefaults { get; set; }

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

            LeftSide = new SegmentSide(SideType.Left);
            RightSide = new SegmentSide(SideType.Right);

            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            CalculateSegmentBeziers(Id, out var bezier, out var leftBezier, out var rightBezier);
            if (IsStartNode)
            {
                RawSegmentBezier = bezier;
                LeftSide.RawBezier = leftBezier;
                RightSide.RawBezier = rightBezier;
            }
            else
            {
                RawSegmentBezier = bezier.Invert();
                LeftSide.RawBezier = rightBezier.Invert();
                RightSide.RawBezier = leftBezier.Invert();
            }
        }
        public void UpdateNode() => Manager.Instance.Update(NodeId, true);

        public void ResetToDefault(NodeStyle style, bool force)
        {
            if (!style.SupportShift || force)
                Shift = NodeStyle.DefaultShift;
            if (!style.SupportSlope || force)
                SlopeAngle = NodeStyle.DefaultSlope;
            if (!style.SupportTwist || force)
                TwistAngle = NodeStyle.DefaultTwist;
            if (!style.SupportNoMarking || force)
                NoMarkings = NodeStyle.DefaultNoMarking;
            if (!style.SupportSlopeJunction || force)
                IsSlope = NodeStyle.DefaultSlopeJunction;

            MinPossibleOffset = style.MinOffset;
            MaxPossibleOffset = style.MaxOffset;

            if (!style.SupportRotate || force)
                SetRotate(NodeStyle.DefaultRotate);
            if (!style.SupportOffset || force)
                SetOffset(DefaultOffset);
            else
                SetOffset(Offset);

            KeepDefaults = true;
        }

        private void SetOffset(float value, bool changeRotate = false)
        {
            _offsetValue = Mathf.Clamp(value, MinOffset, MaxOffset);

            if (changeRotate && IsMinBorderT)
                SetRotate(0f, true);
        }
        public void SetRotate(float value, bool recalculateLimits = false)
        {
            if (recalculateLimits)
                CalculateMinMaxRotate();

            _rotateValue = Mathf.Clamp(value, MinRotate, MaxRotate);
        }

        #endregion

        #region BEZIERS

        public static void UpdateBeziers(ushort segmentId)
        {
            CalculateSegmentBeziers(segmentId, out var bezier, out var leftBezier, out var rightBezier);
            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);

            if (start != null)
            {
                start.RawSegmentBezier = bezier;
                start.LeftSide.RawBezier = leftBezier;
                start.RightSide.RawBezier = rightBezier;
            }
            if (end != null)
            {
                end.RawSegmentBezier = bezier.Invert();
                end.LeftSide.RawBezier = rightBezier.Invert();
                end.RightSide.RawBezier = leftBezier.Invert();
            }
        }
        public static void CalculateSegmentBeziers(ushort segmentId, out BezierTrajectory bezier, out BezierTrajectory leftSide, out BezierTrajectory rightSide)
        {
            var segment = segmentId.GetSegment();
            GetSegmentPosAndDir(segmentId, segment.m_startNode, out var startPos, out var startDir, out var endPos, out var endDir);

            Fix(segment.m_startNode, segmentId, ref startDir);
            Fix(segment.m_endNode, segmentId, ref endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);

            var startNormal = Vector3.Cross(startDir, Vector3.up).normalized;
            var endNormal = Vector3.Cross(endDir, Vector3.up).normalized;
            GetSegmentHalfWidth(segmentId, out var startHalfWidth, out var endHalfWidth);

            leftSide = new BezierTrajectory(startPos + startNormal * startHalfWidth, startDir, endPos - endNormal * endHalfWidth, endDir);
            rightSide = new BezierTrajectory(startPos - startNormal * startHalfWidth, startDir, endPos + endNormal * endHalfWidth, endDir);

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
        private static void GetSegmentHalfWidth(ushort segmentId, out float startWidth, out float endWidth)
        {
            var segment = segmentId.GetSegment();

            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);
            var startTwist = start?.TwistAngle ?? 0f;
            var endTwist = end?.TwistAngle ?? 0f;

            startWidth = segment.Info.m_halfWidth * Mathf.Cos(startTwist * Mathf.Deg2Rad);
            endWidth = segment.Info.m_halfWidth * Mathf.Cos(endTwist * Mathf.Deg2Rad);
        }

        #endregion

        #region LIMITS

        public static void UpdateMinLimits(NodeData data)
        {
            var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
            var count = endDatas.Length;

            var leftMitT = new float[count];
            var rightMinT = new float[count];

            var leftDefaultT = new float[count];
            var rightDefaultT = new float[count];

            if (count != 1 && !data.IsMiddleNode)
            {
                for (var i = 0; i < count; i += 1)
                {
                    var j = (i + 1) % count;

                    GetMinLimit(endDatas[i].LeftSide.RawBezier, endDatas[j].RightSide.RawBezier, ref leftMitT[i], ref rightMinT[j]);
                    GetMinLimit(endDatas[i].LeftSide.RawBezier, endDatas[j].LeftSide.RawBezier, ref leftMitT[i], ref leftMitT[j]);
                    GetMinLimit(endDatas[i].RightSide.RawBezier, endDatas[j].RightSide.RawBezier, ref rightMinT[i], ref rightMinT[j]);
                }

                Array.Copy(leftMitT, leftDefaultT, count);
                Array.Copy(rightMinT, rightDefaultT, count);

                for (var i = 0; i < count; i += 1)
                {
                    var j = (i + 1) % count;

                    GetDefaultLimit(endDatas[i].LeftSide.RawBezier, endDatas[j].RightSide.RawBezier, ref leftDefaultT[i]);
                    GetDefaultLimit(endDatas[i].RightSide.RawBezier, endDatas[j].RightSide.RawBezier, ref rightDefaultT[i]);

                    GetDefaultLimit(endDatas[j].RightSide.RawBezier, endDatas[i].LeftSide.RawBezier, ref rightDefaultT[j]);
                    GetDefaultLimit(endDatas[j].LeftSide.RawBezier, endDatas[i].LeftSide.RawBezier, ref leftDefaultT[j]);
                }

                for (var i = 0; i < count; i += 1)
                {
                    var j = (i + 1) % count;
                    var iDir = endDatas[i].RawSegmentBezier.StartDirection;
                    var jDir = endDatas[j].RawSegmentBezier.StartDirection;
                    if (iDir.x * jDir.x + iDir.z * jDir.z < -0.75)
                    {
                        leftDefaultT[i] = Mathf.Max(leftDefaultT[i], rightDefaultT[i]);
                        rightDefaultT[j] = Mathf.Max(rightDefaultT[j], leftDefaultT[j]);
                    }
                }

                for (var i = 0; i < count; i += 1)
                {
                    var defaultOffset = endDatas[i].DefaultOffset;
                    CorrectDefaultOffset(endDatas[i].LeftSide.RawBezier, ref leftDefaultT[i], defaultOffset);
                    CorrectDefaultOffset(endDatas[i].RightSide.RawBezier, ref rightDefaultT[i], defaultOffset);
                }
            }

            for (var i = 0; i < count; i += 1)
            {
                var endData = endDatas[i];

                endData.LeftSide.MinT = leftMitT[i];
                endData.RightSide.MinT = rightMinT[i];

                endData.LeftSide.DefaultT = leftDefaultT[i];
                endData.RightSide.DefaultT = rightDefaultT[i];
            }

            static void GetMinLimit(BezierTrajectory first, BezierTrajectory second, ref float firstMin, ref float secondMin)
            {
                if (Intersection.CalculateSingle(first, second, out var firstT, out var secondT))
                {
                    firstMin = Mathf.Max(firstMin, firstT);
                    secondMin = Mathf.Max(secondMin, secondT);
                }
            }
            static void GetDefaultLimit(BezierTrajectory bezier, BezierTrajectory limitBezier, ref float defaultT)
            {
                var line = new StraightTrajectory(limitBezier.StartPosition, limitBezier.StartPosition - limitBezier.StartDirection * 16f);
                var intersect = Intersection.CalculateSingle(bezier, line);
                defaultT = Mathf.Max(defaultT, intersect.IsIntersect ? intersect.FirstT : 0f);
            }
            static void CorrectDefaultOffset(BezierTrajectory bezier, ref float defaultT, float minOffset)
            {
                var distance = bezier.Distance(0f, defaultT);
                defaultT = bezier.Travel(defaultT, Mathf.Max(minOffset - distance, 0f));
            }
        }

        public static void UpdateMaxLimits(ushort segmentId)
        {
            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);

            if (start == null)
                SetNoMaxLimits(end);
            else if (end == null)
                SetNoMaxLimits(start);
            else
            {
                SetMaxLimits(start, end, SideType.Left);
                SetMaxLimits(start, end, SideType.Right);
            }

            static void SetNoMaxLimits(SegmentEndData segmentEnd)
            {
                if (segmentEnd != null)
                {
                    segmentEnd.LeftSide.MaxT = 1f;
                    segmentEnd.RightSide.MaxT = 1f;
                }
            }
            static void SetMaxLimits(SegmentEndData start, SegmentEndData end, SideType side)
            {
                var startSide = start[side];
                var endSide = end[side.Invert()];

                var startT = start.GetCornerOffset(startSide);
                var endT = end.GetCornerOffset(endSide);
                if (startT + endT > 1f)
                {
                    var delta = (startT + endT - 1f) / 2;
                    startT -= delta;
                    endT -= delta;
                }
                startSide.MaxT = Mathf.Clamp01(1f - endT - startSide.DeltaT);
                endSide.MaxT = Mathf.Clamp01(1f - startT - endSide.DeltaT);
            }
        }


        #endregion

        #region CALCULATE

        public void Calculate(bool isMain)
        {
            CalculateSegmentLimit();
            CalculateOffset();

            LeftSide.Calculate(this, isMain);
            RightSide.Calculate(this, isMain);

            CalculatePositionAndDirection();
            UpdateCachedSuperElevation();
        }

        private void CalculateSegmentLimit()
        {
            var startLimitLine = new StraightTrajectory(LeftSide.Bezier.StartPosition, RightSide.Bezier.StartPosition);
            var endLimitLine = new StraightTrajectory(LeftSide.Bezier.EndPosition, RightSide.Bezier.EndPosition);

            SegmentMinT = Intersection.CalculateSingle(RawSegmentBezier, startLimitLine, out var minFirstT, out _) ? minFirstT : 0f;
            SegmentMaxT = Intersection.CalculateSingle(RawSegmentBezier, endLimitLine, out var maxFirstT, out _) ? maxFirstT : 1f;

            MinOffset = RawSegmentBezier.Distance(0f, SegmentMinT);
            MaxOffset = RawSegmentBezier.Distance(0f, SegmentMaxT);

            SegmentBezier = RawSegmentBezier.Cut(SegmentMinT, SegmentMaxT);
        }
        private void CalculateMinMaxRotate()
        {
            var t = OffsetT;
            var position = RawSegmentBezier.Position(t);
            var direction = Vector3.Cross(RawSegmentBezier.Tangent(t), Vector3.up).normalized;

            var startLeft = GetAngle(LeftSide.Bezier.StartPosition - position, direction);
            var endLeft = GetAngle(LeftSide.Bezier.EndPosition - position, direction);
            var startRight = GetAngle(position - RightSide.Bezier.StartPosition, direction);
            var endRight = GetAngle(position - RightSide.Bezier.EndPosition, direction);

            MinRotate = Mathf.Clamp(Mathf.Max(startLeft, endRight), MinPossibleRotate, MaxPossibleRotate);
            MaxRotate = Mathf.Clamp(Mathf.Min(endLeft, startRight), MinPossibleRotate, MaxPossibleRotate);
        }
        private void CalculateOffset()
        {
            if (KeepDefaults)
            {
                LeftSide.RawT = LeftSide.DefaultT;
                RightSide.RawT = RightSide.DefaultT;

                var leftPosition = LeftSide.RawBezier.Position(LeftSide.CurrentT);
                var rightPosition = RightSide.RawBezier.Position(RightSide.CurrentT);
                var line = new StraightTrajectory(rightPosition, leftPosition);
                if (Intersection.CalculateSingle(RawSegmentBezier, line, out var t, out _))
                {
                    var offset = RawSegmentBezier.Distance(0f, t);
                    SetOffset(offset);
                    var direction = Vector3.Cross(RawSegmentBezier.Tangent(t), Vector3.up).normalized;
                    var rotate = GetAngle(line.Direction, direction);
                    SetRotate(rotate, true);
                }
                else
                {
                    SetOffset(0f);
                    SetRotate(0f, true);
                }
            }
            else
            {
                SetOffset(Offset);
                SetRotate(RotateAngle, true);

                LeftSide.RawT = GetCornerOffset(LeftSide);
                RightSide.RawT = GetCornerOffset(RightSide);
            }
        }
        private float GetAngle(Vector3 cornerDir, Vector3 segmentDir)
        {
            var angle = Vector3.Angle(segmentDir, cornerDir);
            var sign = Mathf.Sign(Vector3.Cross(segmentDir, cornerDir).y);
            return sign * angle;
        }
        private float GetCornerOffset(SegmentSide side)
        {
            var t = OffsetT;
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).MakeFlatNormalized().TurnDeg(90 + RotateAngle, true);

            var line = new StraightTrajectory(position, position + direction, false);
            var intersection = Intersection.CalculateSingle(side.RawBezier, line);

            if (intersection.IsIntersect)
                return intersection.FirstT;
            else if (RotateAngle == 0f)
                return t <= 0.5f ? 0f : 1f;
            else
                return side.Type == SideType.Left ^ RotateAngle > 0f ? 0f : 1f;
        }
        private void CalculatePositionAndDirection()
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var t = Intersection.CalculateSingle(line, RawSegmentBezier, out var fitstT, out _) ? fitstT : 0.5f;

            Position = line.Position(t);
            Direction = VectorUtils.NormalizeXZ(LeftSide.Direction * t + RightSide.Direction * (1 - t));
        }
        private void UpdateCachedSuperElevation()
        {
            var diff = RightSide.Position - LeftSide.Position;
            var se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public void GetCorner(bool isLeft, out Vector3 position, out Vector3 direction)
        {
            var side = isLeft ? LeftSide : RightSide;

            position = side.Position;
            direction = side.Direction;
        }
        public override string ToString() => $"segment:{Id} node:{NodeId}";

        #endregion

        #region RENDER

        public void Render(OverlayData data) => Render(data, data, data);
        public void Render(OverlayData contourData, OverlayData outterData, OverlayData innerData)
        {
            var data = Manager.Instance[NodeId];

            RenderÑontour(contourData);
            if (data.IsMoveableEnds)
            {
                RenderEnd(contourData, (LeftSide.Position - Position).magnitude + CircleRadius, 0f);
                RenderEnd(contourData, 0f, (RightSide.Position - Position).magnitude + CircleRadius);
                RenderOutterCircle(outterData);
                RenderInnerCircle(innerData);
            }
            else
                RenderEnd(contourData);
        }
        public void RenderAlign(OverlayData contourData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            var leftCut = leftData != null ? DotRadius : 0f;
            var rightCut = rightData != null ? DotRadius : 0f;

            RenderÑontour(contourData);
            RenderEnd(contourData, leftCut, rightCut);

            if (leftData != null)
                LeftSide.Position.RenderCircle(leftData.Value, DotRadius * 2, 0f);
            if (rightData != null)
                RightSide.Position.RenderCircle(rightData.Value, DotRadius * 2, 0f);
        }

        public void RenderSides(OverlayData dataAllow, OverlayData dataForbidden)
        {
            LeftSide.Render(dataAllow, dataForbidden);
            RightSide.Render(dataAllow, dataForbidden);
        }
        public void RenderEnd(OverlayData data, float? leftCut = null, float? rightCut = null)
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var startT = (leftCut ?? 0f) / line.Length;
            var endT = (rightCut ?? 0f) / line.Length;
            line = line.Cut(startT, 1 - endT);
            line.Render(data);
        }
        public void RenderÑontour(OverlayData data)
        {
            RenderSide(LeftSide, data);
            RenderSide(RightSide, data);

            var endSide = new StraightTrajectory(LeftSide.Bezier.EndPosition, RightSide.Bezier.EndPosition);
            endSide.Render(data);
        }
        private void RenderSide(SegmentSide side, OverlayData data)
        {
            var bezier = new BezierTrajectory(side.Position, side.Direction, side.Bezier.EndPosition, side.Bezier.EndDirection);
            bezier.Render(data);
        }

        public void RenderInnerCircle(OverlayData data) => Position.RenderCircle(data, DotRadius * 2, 0f);
        public void RenderOutterCircle(OverlayData data) => Position.RenderCircle(data, CircleRadius * 2 + 0.5f, CircleRadius * 2 - 0.5f);

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
