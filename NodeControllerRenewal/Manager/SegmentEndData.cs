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
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NodeController
{
    public class SegmentEndData : INetworkData, IOverlay, IToXml
    {
        #region STATIC

        public static float CircleRadius => 2.5f;
        public static float CenterDotRadius => 1f;
        public static float CornerDotRadius => 0.5f;
        public static float MinNarrowWidth => 8f;

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

        public NodeData NodeData { get; }
        public ushort NodeId => NodeData.Id;
        public ushort Id { get; set; }
        public int Index { get; set; }
        public Color32 Color => OverlayColors[Index];

        public bool IsStartNode => Id.GetSegment().IsStartNode(NodeId);

        public BezierTrajectory RawSegmentBezier { get; private set; }
        public BezierTrajectory SegmentBezier { get; private set; }
        private SegmentSide LeftSide { get; }
        private SegmentSide RightSide { get; }
        public float AbsoluteAngle { get; private set; }
        public float Weight { get; }


        public bool DefaultIsSlope => !Id.GetSegment().Info.m_flatJunctions && !NodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => !DefaultIsSlope && !NodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);

        public bool IsChangeable => !IsNodeLess;
        public bool IsOffsetChangeable => IsChangeable && !IsUntouchable && NodeData.Style.SupportOffset != SupportOption.None;
        public bool IsRotateChangeable => IsChangeable && NodeData.Style.SupportRotate != SupportOption.None;
        public bool IsMainRoad { get; set; }

        public bool IsRoad { get; private set; }
        public bool IsTunnel { get; private set; }
        public bool IsTrack { get; private set; }
        public bool IsPath { get; private set; }
        public bool IsDecoration { get; private set; }
        public bool IsNodeLess { get; private set; }
        public bool IsUntouchable { get; private set; }

        public int PedestrianLaneCount { get; }
        public float VehicleTwist { get; private set; }

        private float _offsetValue;
        private float _rotateValue;
        private bool _keepDefault;

        public float Offset
        {
            get => _offsetValue;
            set
            {
                SetOffset(value, changeRotate: true);
                KeepDefaults = false;
            }
        }
        public float LeftOffset { set => SetCornerOffset(LeftSide, RightSide, value); }
        public float RightOffset { set => SetCornerOffset(RightSide, LeftSide, value); }
        public float OffsetT => RawSegmentBezier.Trajectory.Travel(_offsetValue, depth: 7);

        public float MinPossibleOffset { get; private set; } = NodeStyle.MinOffset;
        public float MaxPossibleOffset { get; private set; } = NodeStyle.MaxOffset;

        public float MinOffset { get; private set; } = NodeStyle.MinOffset;
        public float MaxOffset { get; private set; } = NodeStyle.MaxOffset;
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
        private bool KeepDefaults
        {
            get => _keepDefault /*|| IsUntouchable*/;
            set => _keepDefault = value;
        }


        public float WidthRatio => Stretch * (IsSlope ? Mathf.Cos(TwistAngle * Mathf.Deg2Rad) : 1f);
        public float HeightRatio => IsSlope ? Mathf.Sin(TwistAngle * Mathf.Deg2Rad) : 0f;

        public bool IsStartBorderOffset => Offset == MinOffset;
        public bool IsEndBorderOffset => Offset == MaxOffset;
        public bool IsBorderRotate => RotateAngle == MinRotate || RotateAngle == MaxRotate;
        public bool IsMinBorderT => RotateAngle >= 0 ? LeftSide.IsMinBorderT : RightSide.IsMinBorderT;
        public bool IsMaxBorderT => RotateAngle >= 0 ? LeftSide.IsMaxBorderT : RightSide.IsMaxBorderT;
        public bool IsShort => LeftSide.IsShort || RightSide.IsShort;
        public bool IsDefaultT => LeftSide.IsDefaultT && RightSide.IsDefaultT;

        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData?.Type == NodeStyleType.Stretch)
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
        public float Width => Id.GetSegment().Info.m_halfWidth * 2f * WidthRatio;
        public float Length => (LeftSide.Position - RightSide.Position).magnitude;
        public bool IsNarrow => Width < MinNarrowWidth;

        #endregion

        #region BASIC

        public SegmentEndData(NodeData nodeData, ushort segmentId)
        {
            NodeData = nodeData;
            Id = segmentId;

            LeftSide = new SegmentSide(this, SideType.Left);
            RightSide = new SegmentSide(this, SideType.Right);

            var segment = Id.GetSegment();
            var info = segment.Info;

            PedestrianLaneCount = info.PedestrianLanes();
            Weight = info.m_halfWidth * 2;
            if ((info.m_netAI as RoadBaseAI)?.m_highwayRules == true)
                Weight *= 1.5f;

            CalculateSegmentBeziers(Id, out var bezier, out var leftTrajectory, out var rightTrajectory);
            if (IsStartNode)
            {
                RawSegmentBezier = bezier;
                LeftSide.RawTrajectory = leftTrajectory;
                RightSide.RawTrajectory = rightTrajectory;
            }
            else
            {
                RawSegmentBezier = bezier.Invert();
                LeftSide.RawTrajectory = rightTrajectory.Invert();
                RightSide.RawTrajectory = leftTrajectory.Invert();
            }
        }
        public void Update()
        {
            ref var segment = ref Id.GetSegment();

            AbsoluteAngle = (segment.IsStartNode(NodeId) ? segment.m_startDirection : segment.m_endDirection).AbsoluteAngle();
            var ai = segment.Info.m_netAI;
            IsRoad = ai is RoadBaseAI;
            IsTunnel = ai is RoadTunnelAI;
            IsTrack = ai is TrainTrackBaseAI || ai is MetroTrackBaseAI;
            IsPath = ai is PedestrianPathAI || ai is PedestrianBridgeAI || ai is PedestrianTunnelAI;
            IsDecoration = ai is DecorationWallAI;
            IsMainRoad = false;
            IsNodeLess = !segment.Info.m_clipSegmentEnds || segment.Info.m_twistSegmentEnds || !segment.Info.m_nodes.Any();
            IsUntouchable = segment.m_flags.IsSet(NetSegment.Flags.Untouchable);
        }
        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(NodeId, true);

        public void SetKeepDefaults() => KeepDefaults = true;
        public void ResetToDefault(NodeStyle style, bool force)
        {
            if (style.SupportSlope == SupportOption.None || force || IsUntouchable)
                SlopeAngle = style.DefaultSlope;

            if (style.SupportTwist == SupportOption.None || force || IsUntouchable)
                TwistAngle = style.DefaultTwist;

            if (style.SupportShift == SupportOption.None || force || IsUntouchable)
                Shift = style.DefaultShift;

            if (style.SupportStretch == SupportOption.None || force || IsUntouchable)
                Stretch = style.DefaultStretch;

            if (style.SupportNoMarking == SupportOption.None || force || IsUntouchable)
                NoMarkings = style.DefaultNoMarking;

            if (style.SupportSlopeJunction == SupportOption.None || force || IsUntouchable)
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

            if (style.SupportRotate == SupportOption.None || force || !IsRotateChangeable)
                SetRotate(style.DefaultRotate);

            if (style.SupportOffset == SupportOption.None)
                SetOffset(style.DefaultOffset);
            else if (force || IsUntouchable)
                SetOffset(GetMinCornerOffset(style.DefaultOffset));
            else
                SetOffset(Offset);

            if (force || style.OnlyKeepDefault)
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
        private void SetCornerOffset(SegmentSide side, SegmentSide another, float offset)
        {
            KeepDefaults = false;

            var t = side.RawTrajectory.Travel(offset);
            side.RawT = Mathf.Clamp(t, side.MinT, side.MaxT);
            SetByCorners(side.Type);
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
                start.LeftSide.RawTrajectory = leftBezier;
                start.RightSide.RawTrajectory = rightBezier;
            }
            if (end != null)
            {
                end.RawSegmentBezier = bezier.Invert();
                end.LeftSide.RawTrajectory = rightBezier.Invert();
                end.RightSide.RawTrajectory = leftBezier.Invert();
            }
        }
        public static void CalculateSegmentBeziers(ushort segmentId, out BezierTrajectory bezier, out ITrajectory leftSide, out ITrajectory rightSide)
        {
            GetSegmentPosAndDir(segmentId, out var startPos, out var startDir, out var endPos, out var endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);

            var startNormal = Vector3.Cross(startDir, Vector3.up).normalized;
            var endNormal = Vector3.Cross(endDir, Vector3.up).normalized;
            ref var segment = ref segmentId.GetSegment();

            if (segment.Info.m_twistSegmentEnds)
            {
                GetBuildingAngle(segment.m_startNode, ref startNormal);
                GetBuildingAngle(segment.m_endNode, ref endNormal);
            }

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

            startPos += startDir.Turn90(false).MakeFlatNormalized() * startShift;
            endPos += endDir.Turn90(false).MakeFlatNormalized() * endShift;

            var dir = (endPos - startPos).MakeFlat();
            var deltaAngle = Mathf.Asin((startShift + endShift) / dir.magnitude);

            if (start?.Style.NeedFixDirection != false)
                startDir = startDir.TurnRad(deltaAngle, true);

            if (end?.Style.NeedFixDirection != false)
                endDir = endDir.TurnRad(deltaAngle, true);
        }
        private static void GetBuildingAngle(ushort nodeId, ref Vector3 normal)
        {
            var buildingId = nodeId.GetNode().m_building;
            if (buildingId != 0)
            {
                var buildingAngle = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_angle;
                var buildingNormal = new Vector3(Mathf.Cos(buildingAngle), 0f, Mathf.Sin(buildingAngle));
                normal = Vector3.Dot(normal, buildingNormal) < 0f ? -buildingNormal : buildingNormal;
            }
        }

        #endregion

        #region LIMITS

        public static void UpdateMinLimits(NodeData data)
        {
            var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
            var count = endDatas.Length;

            var leftMainMinT = new float[count];
            var rightMainMinT = new float[count];
            var leftDefaultT = new float[count];
            var rightDefaultT = new float[count];

            if (count >= 2 && !data.IsMiddleNode)
            {
                for (var i = 0; i < count; i += 1)
                {
                    var j = i.NextIndex(count);

                    if (endDatas[i].IsTrack && endDatas[j].IsTrack)
                        continue;

                    GetMainMinLimit(endDatas[i], endDatas[j], count, ref leftMainMinT[i], ref rightMainMinT[j]);
                    leftDefaultT[i] = leftMainMinT[i];
                    rightDefaultT[j] = rightMainMinT[j];

                    var iDir = NormalizeXZ(endDatas[i].RawSegmentBezier.StartDirection);
                    var jDir = NormalizeXZ(endDatas[j].RawSegmentBezier.StartDirection);
                    var cross = CrossXZ(iDir, jDir);
                    var dot = DotXZ(iDir, jDir);

                    if ((cross > 0f || dot < -0.75f) && (count > 2 || (dot > -0.999f && cross > 0.001f)))
                    {
                        GetSubMinLimit(endDatas[i].RightSide.MainTrajectory, endDatas[i.PrevIndex(count)].LeftSide.MainTrajectory, SideType.Left, ref leftDefaultT[i]);
                        GetSubMinLimit(endDatas[j].LeftSide.MainTrajectory, endDatas[j.NextIndex(count)].RightSide.MainTrajectory, SideType.Right, ref rightDefaultT[j]);
                    }
                }

                if (count >= 3)
                {
                    for (var j = 0; j < count; j += 1)
                    {
                        var i = j.PrevIndex(count);
                        var k = j.NextIndex(count);
                        var iMin = Mathf.Clamp01(leftMainMinT[i]);
                        var kMin = Mathf.Clamp01(rightMainMinT[k]);
                        var iBezier = endDatas[i].LeftSide.MainTrajectory;
                        var kBezier = endDatas[k].RightSide.MainTrajectory;

                        var limitBezier = new BezierTrajectory(iBezier.Position(iMin), -iBezier.Tangent(iMin), kBezier.Position(kMin), -kBezier.Tangent(kMin));

                        if (Intersection.CalculateSingle(endDatas[j].LeftSide.MainTrajectory, limitBezier, out var leftT, out _))
                        {
                            leftMainMinT[j] = Mathf.Max(leftMainMinT[j], leftT);
                            leftDefaultT[j] = Mathf.Max(leftDefaultT[j], leftT);
                        }

                        if (Intersection.CalculateSingle(endDatas[j].RightSide.MainTrajectory, limitBezier, out var rightT, out _))
                        {
                            rightMainMinT[j] = Mathf.Max(rightMainMinT[j], rightT);
                            rightDefaultT[j] = Mathf.Max(rightDefaultT[j], rightT);
                        }
                    }
                }

                for (var i = 0; i < count; i += 1)
                {
                    var minCornerOffset = endDatas[i].GetMinCornerOffset(data.Style.DefaultOffset);
                    var defaultOffset = endDatas[i].Id.GetSegment().Info.m_halfWidth < 4f ? 0f : 8f;
                    var additionalOffset = data.Style.AdditionalOffset;

                    if (leftDefaultT[i] <= 0f && rightDefaultT[i] <= 0f)
                        leftDefaultT[i] = rightDefaultT[i] = Mathf.Max(leftDefaultT[i], rightDefaultT[i]);

                    CorrectDefaultOffset(endDatas[i].LeftSide.MainTrajectory, ref leftDefaultT[i], count == 2, defaultOffset, minCornerOffset, additionalOffset);
                    CorrectDefaultOffset(endDatas[i].RightSide.MainTrajectory, ref rightDefaultT[i], count == 2, defaultOffset, minCornerOffset, additionalOffset);

                    if (leftMainMinT[i] != -1)
                        leftMainMinT[i] = endDatas[i].LeftSide.FromMainT(leftMainMinT[i]);

                    if (rightMainMinT[i] != -1)
                        rightMainMinT[i] = endDatas[i].RightSide.FromMainT(rightMainMinT[i]);

                    leftDefaultT[i] = endDatas[i].LeftSide.FromMainT(leftDefaultT[i]);
                    rightDefaultT[i] = endDatas[i].RightSide.FromMainT(rightDefaultT[i]);
                }

                if (count >= 2)
                {
                    for (var j = 0; j < count; j += 1)
                    {
                        var i = j.PrevIndex(count);
                        var k = j.NextIndex(count);

                        if (leftMainMinT[j] == -1 && Intersection.CalculateSingle(endDatas[j].LeftSide.AdditionalTrajectory, endDatas[i].RightSide.RawTrajectory, out var leftT, out _))
                            leftMainMinT[j] = endDatas[j].LeftSide.FromAdditionalT(leftT);

                        if (rightMainMinT[j] == -1 && Intersection.CalculateSingle(endDatas[j].RightSide.AdditionalTrajectory, endDatas[k].LeftSide.RawTrajectory, out var rightT, out _))
                            rightMainMinT[j] = endDatas[j].RightSide.FromAdditionalT(rightT);
                    }
                }
            }

            for (var i = 0; i < count; i += 1)
            {
                var endData = endDatas[i];

                if(!endData.IsNodeLess)
                {
                    endData.LeftSide.MinT = leftMainMinT[i];
                    endData.RightSide.MinT = rightMainMinT[i];
                }
                else
                {
                    endData.LeftSide.MinT = endData.LeftSide.MainT;
                    endData.RightSide.MinT = endData.RightSide.MainT;
                }

                if(!endData.IsNodeLess && count >= 2)
                {
                    endData.LeftSide.DefaultT = leftDefaultT[i];
                    endData.RightSide.DefaultT = rightDefaultT[i];
                }
                else
                {
                    endData.LeftSide.DefaultT = endData.LeftSide.MainT;
                    endData.RightSide.DefaultT = endData.RightSide.MainT;
                }
            }
        }
        private static void GetMainMinLimit(SegmentEndData iData, SegmentEndData jData, int count, ref float iMinT, ref float jMinT)
        {
            var iBezier = iData.LeftSide.MainTrajectory;
            var jBezier = jData.RightSide.MainTrajectory;

            if (Intersection.CalculateSingle(iBezier, jBezier, out iMinT, out jMinT))
                return;

            if (count == 2)
            {
                var middleDir = iBezier.StartPosition - jBezier.StartPosition;
                if (NormalizeDotXZ(iBezier.StartDirection, middleDir) >= 0.999f && NormalizeDotXZ(middleDir, -jBezier.StartDirection) >= 0.999f)
                {
                    iMinT = 0f;
                    jMinT = 0f;
                    return;
                }
            }

            GetMainMinLimit(iData, jData, SideType.Left, ref iMinT);
            GetMainMinLimit(jData, iData, SideType.Right, ref jMinT);
        }
        private static void GetMainMinLimit(SegmentEndData data, SegmentEndData otherData, SideType side, ref float minT)
        {
            var bezier = data[side].MainTrajectory;
            var otherBezier = otherData[side.Invert()].MainTrajectory;
            var line = new StraightTrajectory(otherBezier.StartPosition, otherBezier.StartPosition - otherBezier.StartDirection, false);

            if (Intersection.CalculateSingle(bezier, line, out minT, out var lineT) && lineT >= 0f && lineT <= 16f)
            {
                var dir = bezier.Tangent(minT);
                var dot = NormalizeDotXZ(dir, line.StartDirection);
                var cross = NormalizeCrossXZ(dir, line.StartDirection);
                var minAngle = Mathf.Clamp(1 - lineT / 1600f, 0.99f, 0.999f);
                if (Mathf.Abs(dot) < minAngle && (cross >= 0f ^ dot >= 0f ^ side == SideType.Left))
                    return;
            }

            var endLine = new StraightTrajectory(otherData.LeftSide.MainTrajectory.EndPosition, otherData.RightSide.MainTrajectory.EndPosition).Cut(0.01f, 0.99f);
            if (Intersection.CalculateSingle(bezier, endLine, out minT, out _))
                return;

            var startLine = new StraightTrajectory(otherData.LeftSide.MainTrajectory.StartPosition, otherData.RightSide.MainTrajectory.StartPosition).Cut(0.01f, 0.99f);
            if (Intersection.CalculateSingle(bezier, startLine, out minT, out _))
                return;

            minT = -1;
        }
        private static void GetSubMinLimit(ITrajectory main, ITrajectory sub, SideType side, ref float defaultT)
        {
            var dot = DotXZ(NormalizeXZ(main.StartDirection), NormalizeXZ(sub.StartDirection));
            if (dot >= 0f)
            {
                var mainNormal = Vector3.Cross(main.StartDirection, Vector3.up);
                var subNormal = Vector3.Cross(sub.StartDirection, Vector3.up);

                var mainLine = new StraightTrajectory(main.StartPosition, main.StartPosition + mainNormal, false);
                var subLine = new StraightTrajectory(sub.StartPosition, sub.StartPosition + subNormal, false);

                if (!Intersection.CalculateSingle(mainLine, subLine, out var aLineT, out _))
                    return;

                var point = mainLine.Position(aLineT);

                var mirror = sub.StartDirection - main.StartDirection * dot * 2;
                var mirrorNormal = Vector3.Cross(mirror, Vector3.up);
                var halfWidth = LengthXZ(sub.StartPosition - point);

                sub = new BezierTrajectory(point + (side == SideType.Left ? halfWidth : -halfWidth) * mirrorNormal, mirror, sub.EndPosition, sub.EndDirection);
            }

            if (Intersection.CalculateSingle(main, sub, out var t, out _))
                defaultT = Mathf.Max(defaultT, t);
            else if (Intersection.CalculateSingle(main, new StraightTrajectory(sub.StartPosition, sub.StartPosition - sub.StartDirection * 16f), out t, out _))
                defaultT = Mathf.Max(defaultT, t);
        }
        private static void CorrectDefaultOffset(ITrajectory trajectory, ref float defaultT, bool alwaysDefault, float defaultOffset, float minCornerOffset, float additionalOffset)
        {
            if (defaultT < 0f || alwaysDefault)
                defaultT = Mathf.Max(defaultT, trajectory.Travel(Mathf.Min(defaultOffset, minCornerOffset)));

            var distance = trajectory.Distance(0f, defaultT);
            defaultT = trajectory.Travel(defaultT, Mathf.Max(minCornerOffset - distance, additionalOffset));
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

                var startT = (start.GetCornerOffset(startSide, start.OffsetT) - startSide.MainT) / (1f - startSide.MainT);
                var endT = (end.GetCornerOffset(endSide, end.OffsetT) - endSide.MainT) / (1f - endSide.MainT);
                if (startT + endT > 1f)
                {
                    var delta = (startT + endT - 1f) / 2;
                    startT -= delta;
                    endT -= delta;
                }
                endT = (1f - endT) * (1f - startSide.MainT) + startSide.MainT - startSide.DeltaT;
                startT = (1f - startT) * (1f - endSide.MainT) + endSide.MainT - endSide.DeltaT;
                startSide.MaxT = Mathf.Clamp01(endT);
                endSide.MaxT = Mathf.Clamp01(startT);
            }
        }


        #endregion

        #region CALCULATE

        public void Calculate(bool isMain)
        {
            CalculateSegmentLimit();
            CalculateOffset();

            LeftSide.Calculate(isMain || IsDecoration);
            RightSide.Calculate(isMain || IsDecoration);

            CalculatePositionAndDirection();
            UpdateVehicleTwist();
        }

        private void CalculateSegmentLimit()
        {
            var startLimitLine = new StraightTrajectory(LeftSide.Trajectory.StartPosition, RightSide.Trajectory.StartPosition);
            var endLimitLine = new StraightTrajectory(LeftSide.Trajectory.EndPosition, RightSide.Trajectory.EndPosition);

            var segmentMinT = Intersection.CalculateSingle(RawSegmentBezier, startLimitLine, out var minFirstT, out _) ? minFirstT : 0f;
            var segmentMaxT = Intersection.CalculateSingle(RawSegmentBezier, endLimitLine, out var maxFirstT, out _) ? maxFirstT : 1f;

            MinOffset = Mathf.Max(RawSegmentBezier.Distance(0f, segmentMinT), MinPossibleOffset);
            MaxOffset = Mathf.Min(RawSegmentBezier.Distance(0f, segmentMaxT), MaxPossibleOffset);

            SegmentBezier = RawSegmentBezier.Cut(segmentMinT, segmentMaxT);
        }
        private void CalculateMinMaxRotate()
        {
            var t = OffsetT;
            var position = RawSegmentBezier.Position(t);
            var direction = Vector3.Cross(RawSegmentBezier.Tangent(t), Vector3.up).normalized;

            var startLeft = GetAngle(LeftSide.Trajectory.StartPosition - position, direction);
            var endLeft = GetAngle(LeftSide.Trajectory.EndPosition - position, direction);
            var startRight = GetAngle(position - RightSide.Trajectory.StartPosition, direction);
            var endRight = GetAngle(position - RightSide.Trajectory.EndPosition, direction);

            MinRotate = Mathf.Clamp(Mathf.Max(startLeft, endRight), NodeStyle.MinRotate, NodeStyle.MaxRotate);
            MaxRotate = Mathf.Clamp(Mathf.Min(endLeft, startRight), NodeStyle.MinRotate, NodeStyle.MaxRotate);
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

                var t = OffsetT;
                LeftSide.RawT = GetCornerOffset(LeftSide, t);
                RightSide.RawT = GetCornerOffset(RightSide, t);
            }
        }
        private void SetByCorners(SideType? prioritySide = null)
        {
            var leftPosition = LeftSide.RawTrajectory.Position(LeftSide.CurrentT);
            var rightPosition = RightSide.RawTrajectory.Position(RightSide.CurrentT);

            var line = new StraightTrajectory(rightPosition, leftPosition);
            var intersect = Intersection.CalculateSingle(RawSegmentBezier, line, out var t, out _);

            if(!intersect && prioritySide != null)
            {
                var side = this[prioritySide.Value];
                var anotherSide = this[prioritySide.Value.Invert()];

                var sidePosition = side.RawTrajectory.Position(side.CurrentT);
                var position = RawSegmentBezier.Position(0.05f / RawSegmentBezier.Length);

                if(Intersection.CalculateSingle(anotherSide.RawTrajectory, new StraightTrajectory(sidePosition, position, false), out var anotherT, out _))
                {
                    anotherSide.RawT = Mathf.Clamp(anotherT, anotherSide.MinT, anotherSide.MaxT);

                    var anotherPosition = anotherSide.RawTrajectory.Position(anotherSide.CurrentT);
                    line = new StraightTrajectory(sidePosition, anotherPosition);
                    intersect = Intersection.CalculateSingle(RawSegmentBezier, line, out t, out _);
                }
            }

            if (intersect)
            {
                var offset = RawSegmentBezier.Trajectory.Cut(0f, t).Length(1, 7);
                SetOffset(offset);
                var direction = Vector3.Cross(RawSegmentBezier.Tangent(t).MakeFlatNormalized(), Vector3.up);
                var rotate = GetAngle(line.Direction, direction);
                SetRotate(rotate, true);
            }
            else
            {
                SetOffset(0f);

                var additional = new StraightTrajectory(RawSegmentBezier.StartPosition, RawSegmentBezier.StartPosition - RawSegmentBezier.StartDirection, false);
                if (Intersection.CalculateSingle(additional, line, out t, out _) && Math.Abs(t) <= 0.05f)
                    SetRotate(RotateAngle, true);
                else
                    SetRotate(0f, true);
            }
        }

        private float GetAngle(Vector3 cornerDir, Vector3 segmentDir)
        {
            var first = NormalizeXZ(cornerDir);
            var second = NormalizeXZ(segmentDir);

            var sign = -Mathf.Sign(CrossXZ(first, second));
            var angle = Mathf.Acos(Mathf.Clamp(DotXZ(first, second), -1f, 1f));

            return sign * angle * Mathf.Rad2Deg;
        }
        private float GetCornerOffset(SegmentSide side, float t)
        {
            var position = RawSegmentBezier.Position(t);
            var direction = RawSegmentBezier.Tangent(t).MakeFlatNormalized().TurnDeg(90 + RotateAngle, true);

            var line = new StraightTrajectory(position, position + direction, false);
            var intersections = Intersection.Calculate(side.RawTrajectory, line);

            if (intersections.Any(i => i.IsIntersect))
            {
                var intersect = intersections.Aggregate((i, j) => Mathf.Abs(i.FirstT - t) < Mathf.Abs(j.FirstT - t) ? i : j);
                return intersect.FirstT;
            }
            else if (RotateAngle == 0f)
                return t <= 0.5f ? 0f : 1f;
            else
                return side.Type == SideType.Left ^ RotateAngle > 0f ? 0f : 1f;
        }
        private void CalculatePositionAndDirection()
        {
            var line = new StraightTrajectory(LeftSide.Position, RightSide.Position);
            var t = 0.5f;

            if (Intersection.CalculateSingle(line, RawSegmentBezier, out var firstT, out _))
                t = firstT;
            else if (Intersection.CalculateSingle(line, new StraightTrajectory(RawSegmentBezier.StartPosition, RawSegmentBezier.StartPosition + RawSegmentBezier.StartDirection, false), out firstT, out _))
                t = firstT;

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
            var position = side.RawTrajectory.Position(side.CurrentT);
            var direction = side.RawTrajectory.Tangent(side.CurrentT).Turn90(true);
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

        public Color32 OverlayColor => IsShort ? Colors.Red : Colors.Green;

        public void Render(OverlayData data) => Render(data, data, data, data, data);
        public void Render(OverlayData contourData, OverlayData outterData, OverlayData innerData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            var data = SingletonManager<Manager>.Instance[NodeId];

            RenderContour(contourData);
            if (data.IsMoveableEnds && IsChangeable)
            {
                if (!IsNarrow)
                {
                    RenderEnd(contourData, LengthXZ(LeftSide.Position - Position) + CircleRadius, 0f);
                    RenderEnd(contourData, 0f, LengthXZ(RightSide.Position - Position) + CircleRadius);
                    RenderOutterCircle(outterData);
                }
                else
                    RenderEnd(contourData);

                if (IsOffsetChangeable)
                {
                    RenderInnerCircle(innerData);
                    if (leftData != null)
                        LeftSide.RenderCircle(leftData.Value);
                    if (rightData != null)
                        RightSide.RenderCircle(rightData.Value);
                }
                else if (IsRotateChangeable)
                    Position.RenderCircle(innerData, CenterDotRadius * 2, CenterDotRadius * 1.2f);
            }
            else
                RenderEnd(contourData);
        }
        public void RenderAlign(OverlayData contourData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            RenderContour(contourData);
            RenderEnd(contourData);

            if (leftData != null)
                LeftSide.RenderCircle(leftData.Value);
            if (rightData != null)
                RightSide.RenderCircle(rightData.Value);
        }

        public void RenderSides(OverlayData dataAllow, OverlayData dataForbidden, OverlayData dataLimit)
        {
            LeftSide.Render(dataAllow, dataForbidden, dataLimit);
            RightSide.Render(dataAllow, dataForbidden, dataLimit);
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
        public void RenderContour(OverlayData data)
        {
            RenderSide(LeftSide, data);
            RenderSide(RightSide, data);

            var endSide = new StraightTrajectory(LeftSide.Trajectory.EndPosition, RightSide.Trajectory.EndPosition);
            endSide.Render(data);
        }
        private void RenderSide(SegmentSide side, OverlayData data)
        {
            var bezier = new BezierTrajectory(side.Position, side.Direction, side.Trajectory.EndPosition, side.Trajectory.EndDirection);
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
            if (style.SupportSlope != SupportOption.None && !IsUntouchable)
                SlopeAngle = config.GetAttrValue("SA", style.DefaultSlope);

            if (style.SupportTwist != SupportOption.None && !IsUntouchable)
                TwistAngle = config.GetAttrValue("TA", style.DefaultTwist);

            if (style.SupportShift != SupportOption.None && !IsUntouchable)
                Shift = config.GetAttrValue("S", style.DefaultShift);

            if (style.SupportStretch != SupportOption.None && !IsUntouchable)
                Stretch = config.GetAttrValue("ST", style.DefaultStretch);

            if (style.SupportNoMarking != SupportOption.None && !IsUntouchable)
                NoMarkings = config.GetAttrValue("NM", style.DefaultNoMarking ? 1 : 0) == 1;

            if (style.SupportSlopeJunction != SupportOption.None)
                IsSlope = config.GetAttrValue("IS", style.DefaultSlopeJunction ? 1 : 0) == 1;

            KeepDefaults = style.OnlyKeepDefault && config.GetAttrValue("KD", 0) == 1;

            if (style.SupportOffset != SupportOption.None)
                SetOffset(config.GetAttrValue("O", GetMinCornerOffset(style.DefaultOffset)));
            else
                SetOffset(config.GetAttrValue("O", style.DefaultOffset));

            if (style.SupportRotate != SupportOption.None && !IsUntouchable)
                SetRotate(config.GetAttrValue("RA", style.DefaultRotate), true);
        }

        #endregion
    }
}
