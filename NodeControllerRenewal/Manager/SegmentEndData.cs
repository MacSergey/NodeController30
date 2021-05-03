using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController
{
    public class SegmentEndData : INetworkData, IOverlay, IToXml
    {
        #region STATIC

        public static float CircleRadius => 2.5f;
        public static float CenterDotRadius => 1f;
        public static float CornerDotRadius => 0.5f;
        public static float MinPossibleRotate => -80f;
        public static float MaxPossibleRotate => 80f;
        public static string XmlName => "SE";

        public static Color32[] OverlayColors { get; } = new Color32[]
        {
            Colors.GetOverlayColor(Colors.Overlay.Red, 255),
            Colors.GetOverlayColor(Colors.Overlay.Blue, 255),
            Colors.GetOverlayColor(Colors.Overlay.Lime, 255),
            Colors.GetOverlayColor(Colors.Overlay.Orange, 255),
            Colors.GetOverlayColor(Colors.Overlay.Purple, 255),
            Colors.GetOverlayColor(Colors.Overlay.SkyBlue, 255),
            Colors.GetOverlayColor(Colors.Overlay.Pink, 255),
            Colors.GetOverlayColor(Colors.Overlay.Turquoise, 255),
        };

        #endregion

        #region PROPERTIES

        public string Title => $"Segment #{Id}";
        public string XmlSection => XmlName;

        public ushort NodeId { get; set; }
        public ushort Id { get; set; }
        public int Index { get; set; }
        public Color32 Color => OverlayColors[Index];

        public NodeData NodeData => SingletonManager<Manager>.Instance[NodeId];
        public bool IsStartNode => Id.GetSegment().IsStartNode(NodeId);
        public SegmentEndData Other => SingletonManager<Manager>.Instance[Id.GetSegment().GetOtherNode(NodeId), Id, true];

        public BezierTrajectory RawSegmentBezier { get; private set; }
        public BezierTrajectory SegmentBezier { get; private set; }
        private SegmentSide LeftSide { get; }
        private SegmentSide RightSide { get; }
        public float AbsoluteAngle => RawSegmentBezier.StartDirection.AbsoluteAngle();


        public bool DefaultIsSlope => !Id.GetSegment().Info.m_flatJunctions && !NodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => !DefaultIsSlope && !NodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool IsMoveable => !IsNodeLess;
        public bool IsMainRoad { get; set; }

        public bool IsNodeLess { get; }
        public int PedestrianLaneCount { get; }
        public float VehicleTwist { get; private set; }

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
        public float LeftOffset
        {
            set
            {
                SetCornerOffset(value, SideType.Left);
                KeepDefaults = false;
            }
        }
        public float RightOffset
        {
            set
            {
                SetCornerOffset(value, SideType.Right);
                KeepDefaults = false;
            }
        }
        public float OffsetT => RawSegmentBezier.Trajectory.Travel(Offset);
        public float MinPossibleOffset { get; private set; } = 0f;
        public float MaxPossibleOffset { get; private set; } = 1000f;
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
        public float Shift { get; set; }
        public float Stretch { get; set; }
        public float StretchPercent
        {
            get => Stretch * 100f;
            set => Stretch = value / 100f;
        }
        public bool NoMarkings { get; set; }
        public bool IsSlope { get; set; }
        private bool KeepDefaults { get; set; }


        public float WidthRatio => Stretch * (IsSlope ? Mathf.Cos(TwistAngle * Mathf.Deg2Rad) : 1f);
        public float HeightRatio => IsSlope ? Mathf.Sin(TwistAngle * Mathf.Deg2Rad) : 0f;

        public bool IsStartBorderOffset => Offset == MinOffset;
        public bool IsEndBorderOffset => Offset == MaxOffset;
        public bool IsBorderRotate => RotateAngle == MinRotate || RotateAngle == MaxRotate;
        public bool IsMinBorderT => RotateAngle >= 0 ? LeftSide.IsMinBorderT : RightSide.IsMinBorderT;
        public bool IsMaxBorderT => RotateAngle >= 0 ? LeftSide.IsMaxBorderT : RightSide.IsMaxBorderT;
        public bool IsDefaultT => LeftSide.IsDefaultT && RightSide.IsDefaultT;

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

            var info = Id.GetSegment().Info;
            IsNodeLess = !info.m_nodes.Any();
            PedestrianLaneCount = info.PedestrianLanes();

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
        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(NodeId, true);

        public void ResetToDefault(NodeStyle style, bool force)
        {
            if (style.SupportSlope == SupportOption.None || force)
                SlopeAngle = style.DefaultSlope;

            if (style.SupportTwist == SupportOption.None || force)
                TwistAngle = style.DefaultTwist;

            if (style.SupportShift == SupportOption.None || force)
                Shift = style.DefaultShift;

            if (style.SupportStretch == SupportOption.None || force)
                Stretch = style.DefaultStretch;

            if (style.SupportNoMarking == SupportOption.None || force)
                NoMarkings = style.DefaultNoMarking;

            if (style.SupportSlopeJunction == SupportOption.None || force)
                IsSlope = style.DefaultSlopeJunction;


            if (IsNodeLess)
            {
                MinPossibleOffset = 0f;
                MaxPossibleOffset = 0f;
            }
            else if (style.SupportOffset == SupportOption.None)
            {
                MinPossibleOffset = style.DefaultOffset;
                MaxPossibleOffset = style.DefaultOffset;
            }
            else
            {
                MinPossibleOffset = NodeStyle.MinOffset;
                MaxPossibleOffset = NodeStyle.MaxOffset;
            }


            if (style.SupportRotate == SupportOption.None || force)
                SetRotate(style.DefaultRotate);

            if (style.SupportOffset == SupportOption.None)
                SetOffset(style.DefaultOffset);
            else if (force)
                SetOffset(GetMinCornerOffset(style.DefaultOffset));
            else
                SetOffset(Offset);

            if (force)
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
        private void SetCornerOffset(float value, SideType sideType)
        {
            var side = this[sideType];
            side.RawT = side.RawBezier.Travel(value);

            SetByCorners();
        }

        #endregion

        #region BEZIERS

        public static void UpdateBeziers(ushort segmentId)
        {
            CalculateSegmentBeziers(segmentId, out var bezier, out var leftBezier, out var rightBezier);
            SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out var start, out var end);

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
            GetSegmentPosAndDir(segmentId, out var startPos, out var startDir, out var endPos, out var endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);

            var startNormal = Vector3.Cross(startDir, Vector3.up).normalized;
            var endNormal = Vector3.Cross(endDir, Vector3.up).normalized;

            GetSegmentWidth(segmentId, out var startHalfWidth, out var endHalfWidth);

            leftSide = new BezierTrajectory(startPos + startNormal * startHalfWidth, startDir, endPos - endNormal * endHalfWidth, endDir);
            rightSide = new BezierTrajectory(startPos - startNormal * startHalfWidth, startDir, endPos + endNormal * endHalfWidth, endDir);
        }
        private static void GetSegmentPosAndDir(ushort segmentId, out Vector3 startPos, out Vector3 startDir, out Vector3 endPos, out Vector3 endDir)
        {
            ref var segment = ref segmentId.GetSegment();
            startPos = segment.m_startNode.GetNode().m_position;
            startDir = segment.m_startDirection;
            endPos = segment.m_endNode.GetNode().m_position;
            endDir = segment.m_endDirection;

            var start = SingletonManager<Manager>.Instance[segment.m_startNode];
            var end = SingletonManager<Manager>.Instance[segment.m_endNode];

            var startShift = start?[segmentId]?.Shift ?? 0f;
            var endShift = end?[segmentId]?.Shift ?? 0f;

            if (startShift == 0f && endShift == 0f)
                return;

            var shift = (startShift + endShift) / 2;
            var dir = (endPos - startPos).MakeFlat();
            var deltaAngle = Mathf.Asin(shift / dir.magnitude);
            var normal = dir.Turn90(true).normalized;

            startPos -= normal * startShift;
            endPos += normal * endShift;

            if (start?.IsIndividuallyShift != false)
                startDir = startDir.TurnRad(deltaAngle, true);

            if (end?.IsIndividuallyShift != false)
                endDir = endDir.TurnRad(deltaAngle, true);
        }

        #endregion

        #region LIMITS

        public static void UpdateMinLimits(NodeData data)
        {
            var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
            var count = endDatas.Length;

            var leftMainMitT = Empty(count);
            var rightMainMinT = Empty(count);

            var leftSubMitT = Empty(count);
            var rightSubMinT = Empty(count);

            var leftDefaultT = Empty(count);
            var rightDefaultT = Empty(count);

            if (count != 1 && !data.IsMiddleNode)
            {
                for (var i = 0; i < count; i += 1)
                {
                    var j = i.NextIndex(count);
                    GetMainMinLimit(endDatas[i].LeftSide.RawBezier, endDatas[j].RightSide.RawBezier, out leftMainMitT[i], out rightMainMinT[j]);
                    leftSubMitT[i] = GetSubMinLimit(endDatas[i].RightSide.RawBezier, endDatas[i.PrevIndex(count)].LeftSide.RawBezier, SideType.Left);
                    rightSubMinT[j] = GetSubMinLimit(endDatas[j].LeftSide.RawBezier, endDatas[j.NextIndex(count)].RightSide.RawBezier, SideType.Right);
                }

                for (var i = 0; i < count; i += 1)
                {
                    var j = i.NextIndex(count);

                    var iDir = NormalizeXZ(endDatas[i].RawSegmentBezier.StartDirection);
                    var jDir = NormalizeXZ(endDatas[j].RawSegmentBezier.StartDirection);

                    if (iDir.x * jDir.x + iDir.z * jDir.z < -0.75f || iDir.x * jDir.z - iDir.z * jDir.x <= 0f)
                    {
                        leftDefaultT[i] = Mathf.Max(leftMainMitT[i], leftSubMitT[i]);
                        rightDefaultT[j] = Mathf.Max(rightMainMinT[j], rightSubMinT[j]);
                    }
                    else
                    {
                        leftDefaultT[i] = leftMainMitT[i];
                        rightDefaultT[j] = rightMainMinT[j];
                    }
                }

                for (var i = 0; i < count; i += 1)
                {
                    var minCornerOffset = endDatas[i].GetMinCornerOffset(data.Style.DefaultOffset);
                    var defaultOffset = endDatas[i].Id.GetSegment().Info.m_halfWidth < 4f ? 0f : 8f;
                    var additionalOffset = endDatas[i].IsNodeLess ? 0f : data.Style.AdditionalOffset;

                    if (leftDefaultT[i] <= 0f && rightDefaultT[i] <= 0f)
                        leftDefaultT[i] = rightDefaultT[i] = Mathf.Max(leftDefaultT[i], rightDefaultT[i]);

                    CorrectDefaultOffset(endDatas[i].LeftSide.RawBezier, ref leftDefaultT[i], defaultOffset, minCornerOffset, additionalOffset);
                    CorrectDefaultOffset(endDatas[i].RightSide.RawBezier, ref rightDefaultT[i], defaultOffset, minCornerOffset, additionalOffset);
                }
            }

            for (var i = 0; i < count; i += 1)
            {
                var endData = endDatas[i];

                endData.LeftSide.MinT = Mathf.Clamp01(leftMainMitT[i]);
                endData.RightSide.MinT = Mathf.Clamp01(rightMainMinT[i]);

                endData.LeftSide.DefaultT = Mathf.Clamp01(leftDefaultT[i]);
                endData.RightSide.DefaultT = Mathf.Clamp01(rightDefaultT[i]);
            }


            static void CorrectDefaultOffset(BezierTrajectory bezier, ref float defaultT, float defaultOffset, float minCornerOffset, float additionalOffset)
            {
                if (defaultT < 0f)
                    defaultT = bezier.Travel(defaultOffset);

                var distance = bezier.Distance(0f, defaultT);
                defaultT = bezier.Travel(defaultT, Mathf.Max(minCornerOffset - distance, additionalOffset));
            }
            static float[] Empty(int count) => Enumerable.Range(0, count).Select(_ => -1f).ToArray();
        }
        private static void GetMainMinLimit(BezierTrajectory iBezier, BezierTrajectory jBezier, out float iMinT, out float jMint)
        {
            if (!Intersection.CalculateSingle(iBezier, jBezier, out iMinT, out jMint))
            {
                var jLine = new StraightTrajectory(jBezier.StartPosition, jBezier.StartPosition - jBezier.StartDirection * 16f);
                iMinT = Intersection.CalculateSingle(iBezier, jLine, out var iT, out _) ? iT : -1;

                var iLine = new StraightTrajectory(iBezier.StartPosition, iBezier.StartPosition - iBezier.StartDirection * 16f);
                jMint = Intersection.CalculateSingle(iLine, jBezier, out _, out var jT) ? jT : -1;
            }
        }
        private static float GetSubMinLimit(BezierTrajectory main, BezierTrajectory sub, SideType side)
        {
            var dot = DotXZ(NormalizeXZ(main.StartDirection), NormalizeXZ(sub.StartDirection));
            if (dot >= 0f)
            {
                var mainNormal = Vector3.Cross(main.StartDirection, Vector3.up);
                var subNormal = Vector3.Cross(sub.StartDirection, Vector3.up);

                var mainLine = new StraightTrajectory(main.StartPosition, main.StartPosition + mainNormal, false);
                var subLine = new StraightTrajectory(sub.StartPosition, sub.StartPosition + subNormal, false);

                if (!Intersection.CalculateSingle(mainLine, subLine, out var aLineT, out _))
                    return -1;

                var point = mainLine.Position(aLineT);

                var mirror = sub.StartDirection - main.StartDirection * dot * 2;
                var mirrorNormal = Vector3.Cross(mirror, Vector3.up);
                var halfWidth = LengthXZ(sub.StartPosition - point);

                sub = new BezierTrajectory(point + (side == SideType.Left ? halfWidth : -halfWidth) * mirrorNormal, mirror, sub.EndPosition, sub.EndDirection);
            }

            if (Intersection.CalculateSingle(main, sub, out var t, out _))
                return t;
            else if (Intersection.CalculateSingle(main, new StraightTrajectory(sub.StartPosition, sub.StartPosition - sub.StartDirection * 16f), out t, out _))
                return t;
            else
                return -1;
        }

        public static void UpdateMaxLimits(ushort segmentId)
        {
            SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out var start, out var end);

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

            LeftSide.Calculate(isMain);
            RightSide.Calculate(isMain);

            CalculatePositionAndDirection();
            UpdateVehicleTwist();
        }

        private void CalculateSegmentLimit()
        {
            var startLimitLine = new StraightTrajectory(LeftSide.Bezier.StartPosition, RightSide.Bezier.StartPosition);
            var endLimitLine = new StraightTrajectory(LeftSide.Bezier.EndPosition, RightSide.Bezier.EndPosition);

            var segmentMinT = Intersection.CalculateSingle(RawSegmentBezier, startLimitLine, out var minFirstT, out _) ? minFirstT : 0f;
            var segmentMaxT = Intersection.CalculateSingle(RawSegmentBezier, endLimitLine, out var maxFirstT, out _) ? maxFirstT : 1f;

            MinOffset = RawSegmentBezier.Distance(0f, segmentMinT);
            MaxOffset = RawSegmentBezier.Distance(0f, segmentMaxT);

            SegmentBezier = RawSegmentBezier.Cut(segmentMinT, segmentMaxT);
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

                SetByCorners();
            }
            else
            {
                SetOffset(Offset);
                SetRotate(RotateAngle, true);

                LeftSide.RawT = GetCornerOffset(LeftSide);
                RightSide.RawT = GetCornerOffset(RightSide);
            }
        }
        private void SetByCorners()
        {
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
            Direction = NormalizeXZ(LeftSide.Direction * t + RightSide.Direction * (1 - t));
        }
        private void UpdateVehicleTwist()
        {
            var diff = RightSide.Position - LeftSide.Position;
            VehicleTwist = Mathf.Atan2(diff.y, LengthXZ(diff)) * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public float GetMinCornerOffset(float styleOffset) => Mathf.Clamp(Mathf.Max(Id.GetSegment().Info.m_minCornerOffset, styleOffset), MinPossibleOffset, MaxPossibleOffset);
        public void GetCorner(bool isLeft, out Vector3 position, out Vector3 direction)
        {
            var side = isLeft ? LeftSide : RightSide;

            position = side.Position;
            direction = side.Direction;
        }
        public void MakeStraight()
        {
            KeepDefaults = false;

            if (MinRotate <= 0f && 0f <= MaxRotate)
                SetRotate(0f);
            else
            {
                var left = GetStraightT(LeftSide);
                var right = GetStraightT(RightSide);
                var offset = RawSegmentBezier.Distance(0f, Mathf.Max(left, right));
                SetOffset(offset);
                SetRotate(0f, true);
            }
        }
        private float GetStraightT(SegmentSide side)
        {
            var position = side.RawBezier.Position(side.CurrentT);
            var direction = side.RawBezier.Tangent(side.CurrentT).Turn90(true);
            var line = new StraightTrajectory(position, position + direction, false);
            return Intersection.CalculateSingle(RawSegmentBezier, line, out var t, out _) ? t : 0f;
        }


        public static void GetSegmentWidth(ushort segmentId, float position, out float startWidth, out float endWidth)
        {
            SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out var start, out var end);

            startWidth = position * (start?.WidthRatio ?? 1f);
            endWidth = position * (end?.WidthRatio ?? 1f);
        }
        public static void GetSegmentWidth(ushort segmentId, out float startWidth, out float endWidth)
        {
            ref var segment = ref segmentId.GetSegment();
            GetSegmentWidth(segmentId, segment.Info.m_halfWidth, out startWidth, out endWidth);
        }
        public static float GetSegmentWidth(ushort segmentId, float t)
        {
            GetSegmentWidth(segmentId, out var start, out var end);
            return Mathf.Lerp(start, end, t);
        }
        public static float GetSegmentWidth(ushort segmentId, float position, float t)
        {
            GetSegmentWidth(segmentId, position, out var start, out var end);
            return Mathf.Lerp(start, end, t);
        }
        public static void FixMiddle(SegmentEndData first, SegmentEndData second)
        {
            SegmentSide.FixMiddle(first.LeftSide, second.RightSide);
            SegmentSide.FixMiddle(first.RightSide, second.LeftSide);
        }
        public override string ToString() => $"segment:{Id} node:{NodeId}";

        #endregion

        #region RENDER

        public void Render(OverlayData data) => Render(data, data, data, data, data);
        public void Render(OverlayData contourData, OverlayData outterData, OverlayData innerData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            var data = SingletonManager<Manager>.Instance[NodeId];

            RenderÑontour(contourData);
            if (data.IsMoveableEnds && IsMoveable)
            {
                RenderEnd(contourData, LengthXZ(LeftSide.Position - Position) + CircleRadius, 0f);
                RenderEnd(contourData, 0f, LengthXZ(RightSide.Position - Position) + CircleRadius);
                RenderOutterCircle(outterData);
                RenderInnerCircle(innerData);
                if (leftData != null)
                    LeftSide.RenderCircle(leftData.Value);
                if (rightData != null)
                    RightSide.RenderCircle(rightData.Value);
            }
            else
                RenderEnd(contourData);
        }
        public void RenderAlign(OverlayData contourData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            RenderÑontour(contourData);
            RenderEnd(contourData);

            if (leftData != null)
                LeftSide.RenderCircle(leftData.Value);
            if (rightData != null)
                RightSide.RenderCircle(rightData.Value);
        }

        public void RenderSides(OverlayData dataAllow, OverlayData dataForbidden)
        {
            LeftSide.Render(dataAllow, dataForbidden);
            RightSide.Render(dataAllow, dataForbidden);
        }
        public void RenderEnd(OverlayData data, float? leftCut = null, float? rightCut = null)
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var length = LengthXZ(line.StartPosition - line.EndPosition);
            var height = Mathf.Abs(line.StartPosition.y - line.EndPosition.y);
            var angle = Mathf.Atan(height / length);
            var startT = (leftCut ?? 0f) / Mathf.Cos(angle) / line.Length;
            var endT = (rightCut ?? 0f) / Mathf.Cos(angle) / line.Length;
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

        public void RenderInnerCircle(OverlayData data) => Position.RenderCircle(data, CenterDotRadius * 2, 0f);
        public void RenderOutterCircle(OverlayData data) => Position.RenderCircle(data, CircleRadius * 2 + 0.5f, CircleRadius * 2 - 0.5f);

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr(nameof(Id), Id);
            config.AddAttr("O", _offsetValue);
            config.AddAttr("RA", _rotateValue);
            config.AddAttr("SA", SlopeAngle);
            config.AddAttr("TA", TwistAngle);
            config.AddAttr("S", Shift);
            config.AddAttr("ST", Stretch);
            config.AddAttr("NM", NoMarkings ? 1 : 0);
            config.AddAttr("IS", IsSlope ? 1 : 0);
            config.AddAttr("KD", KeepDefaults ? 1 : 0);

            return config;
        }

        public void FromXml(XElement config, NodeStyle style)
        {
            if (style.SupportSlope != SupportOption.None)
                SlopeAngle = config.GetAttrValue("SA", style.DefaultSlope);

            if (style.SupportTwist != SupportOption.None)
                TwistAngle = config.GetAttrValue("TA", style.DefaultTwist);

            if (style.SupportShift != SupportOption.None)
                Shift = config.GetAttrValue("S", style.DefaultShift);

            if (style.SupportStretch != SupportOption.None)
                Stretch = config.GetAttrValue("ST", style.DefaultStretch);

            if (style.SupportNoMarking != SupportOption.None)
                NoMarkings = config.GetAttrValue("NM", style.DefaultNoMarking ? 1 : 0) == 1;

            if (style.SupportSlopeJunction != SupportOption.None)
                IsSlope = config.GetAttrValue("IS", style.DefaultSlopeJunction ? 1 : 0) == 1;

            KeepDefaults = config.GetAttrValue("KD", 0) == 1;

            if (style.SupportOffset != SupportOption.None)
                SetOffset(config.GetAttrValue("O", GetMinCornerOffset(style.DefaultOffset)));
            else
                SetOffset(config.GetAttrValue("O", style.DefaultOffset));

            if (style.SupportRotate != SupportOption.None)
                SetRotate(config.GetAttrValue("RA", style.DefaultRotate), true);
        }

        #endregion
    }
}
