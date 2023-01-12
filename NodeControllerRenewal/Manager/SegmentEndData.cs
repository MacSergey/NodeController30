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
        public static float CenterRadius => 1f;
        public static float CornerCenterRadius => 0.5f;
        public static float CornerCircleRadius => 1.5f;
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

        public bool IsChangeable => !FinalNodeLess;
        public bool IsOffsetChangeable => IsChangeable && !IsUntouchable && NodeData.Style.SupportOffset != SupportOption.None;
        public bool IsRotateChangeable => IsChangeable && NodeData.Style.SupportRotate != SupportOption.None;
        public bool IsMainRoad { get; set; }

        public bool IsRoad { get; private set; }
        public bool IsTunnel { get; private set; }
        public bool IsTrack { get; private set; }
        public bool IsPath { get; private set; }
        public bool IsDecoration { get; private set; }
        public bool IsNodeLess { get; private set; }
        public bool FinalNodeLess => IsNodeLess || ForceNodeLess == true;
        public bool IsUntouchable { get; private set; }

        public float VehicleTwist { get; private set; }

        public Mode Mode { get; set; }

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
        public float LeftOffset { set => SetCornerOffset(LeftSide, value); }
        public float RightOffset { set => SetCornerOffset(RightSide, value); }
        public float OffsetT
        {
            get
            {
                if (_offsetValue == 0f)
                    return 0f;
                else
                {
                    var lenght = RawSegmentBezier.Length;
                    if (_offsetValue >= lenght)
                        return 1f;
                    else
                    {
                        var t = RawSegmentBezier.Trajectory.Travel(_offsetValue, depth: 7);
                        return t;
                    }
                }
            }
        }

        public float MinPossibleOffset { get; private set; } = NodeStyle.MinOffset;
        public float MaxPossibleOffset { get; private set; } = NodeStyle.MaxOffset;

        public float MinOffset { get; private set; } = NodeStyle.MinOffset;
        public float MaxOffset { get; private set; } = NodeStyle.MaxOffset;

        private float Rotate
        {
            get => _rotateValue;
            set
            {
                SetRotate(value);
                KeepDefaults = false;
            }
        }

        private float MinRotate { get; set; }
        private float MaxRotate { get; set; }
        public float MinRotateAngle
        {
            get => Mode != Mode.FreeForm ? MinRotate : -180f;
        }
        public float MaxRotateAngle
        {
            get => Mode != Mode.FreeForm ? MaxRotate : 180f;
        }


        private float _slope;
        private float _twist;
        private float _shift;
        private float _stretch;

        public float RotateAngle
        {
            get
            {
                if (Mode != Mode.FreeForm)
                    return Rotate;
                else
                {
                    var originDir = LeftSide.OriginalPos.MakeFlat() - RightSide.OriginalPos.MakeFlat();
                    var currentDir = LeftSide.StartPos.MakeFlat() - RightSide.StartPos.MakeFlat();
                    var rotation = Quaternion.FromToRotation(originDir, currentDir);

                    return rotation.eulerAngles.y > 180f ? rotation.eulerAngles.y - 360 : rotation.eulerAngles.y;
                }
            }
            set
            {
                if (Mode != Mode.FreeForm)
                    Rotate = value;
                else
                {
                    var leftOriginalPos = LeftSide.OriginalPos.MakeFlat();
                    var rightOriginalPos = RightSide.OriginalPos.MakeFlat();
                    var originDir = leftOriginalPos - rightOriginalPos;
                    var length = originDir.magnitude;
                    var dir = Quaternion.AngleAxis(value, Vector3.up) * originDir.normalized;

                    var leftPos = Position + dir * length * 0.5f;
                    var leftDelta = leftPos - leftOriginalPos;
                    leftDelta = LeftSide.FromAbsoluteDeltaPos(leftDelta);
                    leftDelta.y = LeftSide.PosDelta.y;
                    LeftSide.PosDelta = leftDelta;

                    var rightPos = Position - dir * length * 0.5f;
                    var rightDelta = rightPos - rightOriginalPos;
                    rightDelta = RightSide.FromAbsoluteDeltaPos(rightDelta);
                    rightDelta.y = RightSide.PosDelta.y;
                    RightSide.PosDelta = rightDelta;
                }
            }
        }
        public float SlopeAngle
        {
            get => Mode != Mode.FreeForm ? _slope : 0f;
            set => _slope = value;
        }
        public float TwistAngle
        {
            get => Mode != Mode.FreeForm ? _twist : 0f;
            set => _twist = value;
        }
        public float Shift
        {
            get => Mode != Mode.FreeForm ? _shift : 0f;
            set => _shift = value;
        }
        public float Stretch
        {
            get => Mode != Mode.FreeForm ? _stretch : 1f;
            set => _stretch = value;
        }

        public float StretchPercent
        {
            get => Stretch * 100f;
            set => Stretch = value / 100f;
        }


        private bool? _collision;
        private bool _forceNodeLess;
        private bool? _followSlope;
        public bool? NoMarkings { get; set; }
        public bool? Collision
        {
            get => Mode != Mode.FreeForm ? _collision : true;
            set => _collision = value;
        }
        public bool? ForceNodeLess
        {
            get => _forceNodeLess;
            set => SetForceNodeless(value == true, true);
        }
        public bool? FollowSlope
        {
            get => IsMainRoad ? true : _followSlope;
            set => _followSlope = value;
        }
        public Vector3 LeftPosDelta
        {
            get => LeftSide.PosDelta;
            set => LeftSide.PosDelta = value;
        }
        public Vector3 RightPosDelta
        {
            get => RightSide.PosDelta;
            set => RightSide.PosDelta = value;
        }
        public Vector3 LeftDirDelta
        {
            get => LeftSide.DirDelta;
            set => LeftSide.DirDelta = value;
        }
        public Vector3 RightDirDelta
        {
            get => RightSide.DirDelta;
            set => RightSide.DirDelta = value;
        }

        private bool KeepDefaults
        {
            get => _keepDefault;
            set => _keepDefault = value;
        }


        public float WidthRatio => Stretch * (Mode != Mode.Flat ? Mathf.Cos(TwistAngle * Mathf.Deg2Rad) : 1f);
        public float HeightRatio => Mode != Mode.Flat ? Mathf.Tan(TwistAngle * Mathf.Deg2Rad) : 0f;

        public bool IsStartBorderOffset => Offset == MinOffset;
        public bool IsEndBorderOffset => Offset == MaxOffset;
        public bool IsBorderRotate
        {
            get
            {
                var isBorder = Rotate == MinRotate || Rotate == MaxRotate;
                return isBorder;
            }
        }
        public bool IsMinBorderT
        {
            get
            {
                var isMin = Rotate >= 0 ? LeftSide.IsMinBorderT : RightSide.IsMinBorderT;
                return isMin;
            }
        }
        public bool IsShort => LeftSide.IsShort || RightSide.IsShort;

        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData?.Type == NodeStyleType.Stretch)
                    return false; // always ignore.
                else if (NoMarkings == true)
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

            Weight = info.m_halfWidth * 2;
            if ((info.m_netAI as RoadBaseAI)?.m_highwayRules == true)
                Weight *= 1.5f;

            CalculateSegmentBeziers(Id, out var bezier, out var leftTrajectory, out var rightTrajectory);
            if (IsStartNode)
            {
                RawSegmentBezier = bezier;
                LeftSide.SetTrajectory(leftTrajectory);
                RightSide.SetTrajectory(rightTrajectory);
            }
            else
            {
                RawSegmentBezier = bezier.Invert();
                LeftSide.SetTrajectory(rightTrajectory.Invert());
                RightSide.SetTrajectory(leftTrajectory.Invert());
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
            IsNodeLess = !segment.Info.m_nodes.Any() || (IsDecoration && (!segment.Info.m_clipSegmentEnds || segment.Info.m_twistSegmentEnds));
            IsUntouchable = segment.m_flags.IsSet(NetSegment.Flags.Untouchable);
        }
        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(NodeId);

        public void SetKeepDefaults(NodeStyle style)
        {
            KeepDefaults = true;
            LeftSide.PosDelta = style.DefaultDelta;
            RightSide.PosDelta = style.DefaultDelta;
            LeftSide.DirDelta = style.DefaultDelta;
            RightSide.DirDelta = style.DefaultDelta;
        }
        public void ResetToDefault(NodeStyle style, bool force)
        {
            if (style.SupportMode == SupportOption.None || force || IsUntouchable)
                Mode = style.DefaultMode;

            if (style.SupportSlope == SupportOption.None || force || IsUntouchable)
                SlopeAngle = style.DefaultSlope;

            if (style.SupportTwist == SupportOption.None || force || IsUntouchable)
                TwistAngle = style.DefaultTwist;

            if (style.SupportShift == SupportOption.None || force || IsUntouchable)
                Shift = style.DefaultShift;

            if (style.SupportStretch == SupportOption.None || force || IsUntouchable)
                Stretch = style.DefaultStretch;

            if (style.SupportMarking == SupportOption.None || force || IsUntouchable)
                NoMarkings = style.DefaultNoMarking;

            if (style.SupportCollision == SupportOption.None || force || IsUntouchable)
                Collision = style.GetDefaultCollision(this);

            if (style.SupportFollowMainSlope == SupportOption.None || force || IsUntouchable)
                FollowSlope = style.DefaultFollowSlope;

            if (style.SupportCornerDelta == SupportOption.None || force || IsUntouchable)
            {
                LeftSide.PosDelta = style.DefaultDelta;
                RightSide.PosDelta = style.DefaultDelta;
                LeftSide.DirDelta = style.DefaultDelta;
                RightSide.DirDelta = style.DefaultDelta;
            }

            if (FinalNodeLess)
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
        private void SetCornerOffset(SegmentSide side, float offset)
        {
            KeepDefaults = false;

            var t = side.RawTrajectory.Travel(offset);
            side.RawT = Mathf.Clamp(t, side.MinT, side.MaxT);
            SetByCorners(side.Type);
        }
        private void SetForceNodeless(bool value, bool reset = false)
        {
            if (value != _forceNodeLess)
            {
                _forceNodeLess = value;
                if (reset)
                    ResetToDefault(NodeData.Style, true);
            }
        }

        #endregion

        #region BEZIERS

        public static void UpdateBeziers(ushort segmentId)
        {
            SegmentEndData start = null;
            SegmentEndData end = null;
            try
            {
                SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out start, out end);
                if (start != null && end != null)
                {
                    if ((start.NodeData.State & State.Error) != 0 && (end.NodeData.State & State.Error) != 0)
                        return;
                }
                else if (start != null)
                {
                    if ((start.NodeData.State & State.Error) != 0)
                        return;
                }
                else if (end != null)
                {
                    if ((end.NodeData.State & State.Error) != 0)
                        return;
                }
#if DEBUG && EXTRALOG
                SingletonMod<Mod>.Logger.Debug($"Segment #{segmentId} update beziers");
#endif
                CalculateSegmentBeziers(segmentId, out var bezier, out var leftBezier, out var rightBezier);

                if (start != null)
                {
                    start.RawSegmentBezier = bezier;
                    start.LeftSide.SetTrajectory(leftBezier);
                    start.RightSide.SetTrajectory(rightBezier);
                }
                if (end != null)
                {
                    end.RawSegmentBezier = bezier.Invert();
                    end.LeftSide.SetTrajectory(rightBezier.Invert());
                    end.RightSide.SetTrajectory(leftBezier.Invert());
                }
#if DEBUG && EXTRALOG
                SingletonMod<Mod>.Logger.Debug($"Segment #{segmentId}\nCentre={bezier}\nLeft={leftBezier}\nRight={rightBezier}");
#endif
            }
            catch (Exception error)
            {
                if (start != null)
                    start.NodeData.State = State.Error;
                if (end != null)
                    end.NodeData.State = State.Error;

                SingletonMod<Mod>.Logger.Error($"Segment #{segmentId} update beziers failed", error);
            }
        }
        public static void CalculateSegmentBeziers(ushort segmentId, out BezierTrajectory bezier, out ITrajectory leftSide, out ITrajectory rightSide)
        {
            GetSegmentPosAndDir(segmentId, out var startPos, out var startDir, out var endPos, out var endDir);

            bezier = new BezierTrajectory(startPos, startDir, endPos, endDir, true, true, true);

            var startNormal = Vector3.Cross(startDir, Vector3.up).normalized;
            var endNormal = Vector3.Cross(endDir, Vector3.up).normalized;
            ref var segment = ref segmentId.GetSegment();

            if (segment.Info.m_twistSegmentEnds)
            {
                GetBuildingAngle(segment.m_startNode, ref startNormal);
                GetBuildingAngle(segment.m_endNode, ref endNormal);
            }

            GetSegmentWidth(segmentId, out var startHalfWidth, out var endHalfWidth);

            leftSide = new BezierTrajectory(startPos + startNormal * startHalfWidth, startDir, endPos - endNormal * endHalfWidth, endDir, true, true, true);
            rightSide = new BezierTrajectory(startPos - startNormal * startHalfWidth, startDir, endPos + endNormal * endHalfWidth, endDir, true, true, true);
        }
        private static void GetSegmentPosAndDir(ushort segmentId, out Vector3 startPos, out Vector3 startDir, out Vector3 endPos, out Vector3 endDir)
        {
            ref var segment = ref segmentId.GetSegment();
            startPos = segment.m_startNode.GetNode().m_position;
            startDir = segment.m_startDirection;
            endPos = segment.m_endNode.GetNode().m_position;
            endDir = segment.m_endDirection;

            var startShift = 0f;
            var endShift = 0f;
            if (SingletonManager<Manager>.Instance.TryGetNodeData(segment.m_startNode, out var startData))
            {
                if (startData.TryGetSegment(segmentId, out var startSegmentData))
                    startShift = startSegmentData.Shift;
            }
            if (SingletonManager<Manager>.Instance.TryGetNodeData(segment.m_endNode, out var endData))
            {
                if (endData.TryGetSegment(segmentId, out var endSegmentData))
                    endShift = endSegmentData.Shift;
            }

            if (startShift == 0f && endShift == 0f)
                return;

            startPos += startDir.Turn90(false).MakeFlatNormalized() * startShift;
            endPos += endDir.Turn90(false).MakeFlatNormalized() * endShift;

            var dir = (endPos - startPos).MakeFlat();
            var deltaAngle = Mathf.Asin((startShift + endShift) / dir.magnitude);

            if (startData?.Style.NeedFixDirection != false)
                startDir = startDir.TurnRad(deltaAngle, true);

            if (endData?.Style.NeedFixDirection != false)
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

        private struct SideLimits
        {
            public SegmentSide side;
            public float? mainMinT;
            public float? additionalMinT;
            public float? defaultT;

            public SideLimits(SegmentSide side)
            {
                this.side = side;
                mainMinT = 0f;
                additionalMinT = null;
                defaultT = 0f;
            }

            public float FinalMinT
            {
                get
                {
                    if (mainMinT != null)
                        return side.FromMainT(mainMinT.Value);
                    else if (additionalMinT != null)
                        return side.FromAdditionalT(additionalMinT.Value);
                    else
                        return 0f;
                }
            }
            public float FinalDefaultT => defaultT != null ? side.FromMainT(defaultT.Value) : side.MainT;
            public bool MinFound => mainMinT != null || additionalMinT != null;

            public override string ToString() => $"main={mainMinT}, add={additionalMinT}, def={defaultT}";
        }
        private struct SegmentLimits
        {
            public SideLimits left;
            public SideLimits right;

            public SegmentLimits(SegmentEndData data)
            {
                left = new SideLimits(data.LeftSide);
                right = new SideLimits(data.RightSide);
            }

            public override string ToString() => $"Left: {left}; Right: {right}";
        }
        public static void UpdateMinLimits(NodeData data)
        {
            try
            {
                if ((data.State & State.Error) != 0)
                    return;
#if DEBUG && EXTRALOG
                SingletonMod<Mod>.Logger.Debug($"Node #{data.Id} update min limits");
#endif
                var endDatas = data.SegmentEndDatas.OrderBy(s => s.AbsoluteAngle).ToArray();
                var count = endDatas.Length;
                var limits = endDatas.Select(d => new SegmentLimits(d)).ToArray();

                if (count >= 2 && !data.IsMiddleNode)
                {
                    for (var leftI = 0; leftI < count; leftI += 1)
                    {
                        if (endDatas[leftI].Collision == false)
                        {
                            limits[leftI].left.mainMinT = null;
                            limits[leftI].right.mainMinT = null;
                            limits[leftI].left.defaultT = null;
                            limits[leftI].right.defaultT = null;
                            continue;
                        }

                        if (!GetNextIndex(leftI, out var rightI, endDatas))
                            continue;

                        GetMainMinLimit(endDatas[leftI], endDatas[rightI], count, ref limits[leftI].left, ref limits[rightI].right);
                        limits[leftI].left.defaultT = limits[leftI].left.mainMinT;
                        limits[rightI].right.defaultT = limits[rightI].right.mainMinT;

                        var leftDir = NormalizeXZ(endDatas[leftI].RawSegmentBezier.StartDirection);
                        var rightDir = NormalizeXZ(endDatas[rightI].RawSegmentBezier.StartDirection);
                        var cross = CrossXZ(leftDir, rightDir);
                        var dot = DotXZ(leftDir, rightDir);

                        if ((cross > 0f || dot < -0.75f) && (count > 2 || (dot > -0.999f && cross > 0.001f)))
                        {
                            if (GetPrevIndex(leftI, out var prevLeftI, endDatas))
                                GetSubMinLimit(endDatas[leftI].RightSide.MainTrajectory, endDatas[prevLeftI].LeftSide.MainTrajectory, SideType.Left, ref limits[leftI].left.defaultT);

                            if (GetNextIndex(rightI, out var nextRightI, endDatas))
                                GetSubMinLimit(endDatas[rightI].LeftSide.MainTrajectory, endDatas[nextRightI].RightSide.MainTrajectory, SideType.Right, ref limits[rightI].right.defaultT);
                        }
                    }

                    if (count >= 3)
                    {
                        for (var currentI = 0; currentI < count; currentI += 1)
                        {
                            if (endDatas[currentI].Collision == false)
                                continue;

                            if (!GetPrevIndex(currentI, out var prevI, endDatas) || !GetNextIndex(currentI, out var nextI, endDatas) || prevI == nextI)
                                continue;

                            var prevMin = Mathf.Clamp01(limits[prevI].left.mainMinT ?? 0f);
                            var nextMin = Mathf.Clamp01(limits[nextI].right.mainMinT ?? 0f);
                            var prevBezier = endDatas[prevI].LeftSide.MainTrajectory;
                            var nextBezier = endDatas[nextI].RightSide.MainTrajectory;

                            var limitBezier = new BezierTrajectory(prevBezier.Position(prevMin), -prevBezier.Tangent(prevMin), nextBezier.Position(nextMin), -nextBezier.Tangent(nextMin), true, true, true);

                            if (Intersection.CalculateSingle(endDatas[currentI].LeftSide.MainTrajectory, limitBezier, out var leftT, out _))
                            {
                                limits[currentI].left.mainMinT = Mathf.Max(limits[currentI].left.mainMinT ?? 0f, leftT);
                                limits[currentI].left.defaultT = Mathf.Max(limits[currentI].left.defaultT ?? 0f, leftT);
                            }

                            if (Intersection.CalculateSingle(endDatas[currentI].RightSide.MainTrajectory, limitBezier, out var rightT, out _))
                            {
                                limits[currentI].right.mainMinT = Mathf.Max(limits[currentI].right.mainMinT ?? 0f, rightT);
                                limits[currentI].right.defaultT = Mathf.Max(limits[currentI].right.defaultT ?? 0f, rightT);
                            }
                        }
                    }

                    for (var currentI = 0; currentI < count; currentI += 1)
                    {
                        if (endDatas[currentI].Collision == false)
                            continue;

                        if ((limits[currentI].left.defaultT == null) != (limits[currentI].right.defaultT == null))
                        {
                            if (limits[currentI].left.defaultT == null)
                            {
                                if (limits[currentI].right.defaultT.Value == 0)
                                    limits[currentI].left.defaultT = 0;
                            }
                            else if (limits[currentI].right.defaultT == null)
                            {
                                if (limits[currentI].left.defaultT.Value == 0)
                                    limits[currentI].right.defaultT = 0;
                            }
                        }

                        var minCornerOffset = endDatas[currentI].GetMinCornerOffset(data.Style.DefaultOffset);
                        var defaultOffset = endDatas[currentI].Id.GetSegment().Info.m_halfWidth < 4f ? 0f : 8f;
                        var additionalOffset = data.Style.AdditionalOffset;

                        CorrectDefaultOffset(endDatas[currentI].LeftSide.MainTrajectory, ref limits[currentI].left.defaultT, count == 2, defaultOffset, minCornerOffset, additionalOffset);
                        CorrectDefaultOffset(endDatas[currentI].RightSide.MainTrajectory, ref limits[currentI].right.defaultT, count == 2, defaultOffset, minCornerOffset, additionalOffset);

                        if (!limits[currentI].left.MinFound && GetPrevIndex(currentI, out var prevI, endDatas))
                        {
                            if (Intersection.CalculateSingle(endDatas[currentI].LeftSide.AdditionalTrajectory, endDatas[prevI].RightSide.RawTrajectory, out var leftT, out _))
                                limits[currentI].left.additionalMinT = leftT;
                        }

                        if (!limits[currentI].right.MinFound && GetNextIndex(currentI, out var nextI, endDatas))
                        {
                            if (Intersection.CalculateSingle(endDatas[currentI].RightSide.AdditionalTrajectory, endDatas[nextI].LeftSide.RawTrajectory, out var rightT, out _))
                                limits[currentI].right.additionalMinT = rightT;
                        }
                    }
                }
                else if (count == 1)
                {
                    limits[0].left.mainMinT = null;
                    limits[0].right.mainMinT = null;
                }

                for (var i = 0; i < count; i += 1)
                {
                    var endData = endDatas[i];

                    if (endData.Mode == Mode.FreeForm)
                    {
                        endData.LeftSide.MinT = endData.LeftSide.MainT;
                        endData.RightSide.MinT = endData.RightSide.MainT;
                    }
                    else if (!endData.FinalNodeLess)
                    {
                        endData.LeftSide.MinT = limits[i].left.FinalMinT;
                        endData.RightSide.MinT = limits[i].right.FinalMinT;
                    }
                    else
                    {
                        endData.LeftSide.MinT = endData.LeftSide.MainT;
                        endData.RightSide.MinT = endData.RightSide.MainT;
                    }

                    if (!endData.FinalNodeLess && count >= 2)
                    {
                        endData.LeftSide.DefaultT = limits[i].left.FinalDefaultT;
                        endData.RightSide.DefaultT = limits[i].right.FinalDefaultT;
                    }
                    else
                    {
                        endData.LeftSide.DefaultT = endData.LeftSide.MainT;
                        endData.RightSide.DefaultT = endData.RightSide.MainT;
                    }
                }
            }
            catch (Exception error)
            {
                data.State = State.Error;
                SingletonMod<Mod>.Logger.Error($"Node #{data.Id} update min limits failed", error);
            }

            static bool GetPrevIndex(int index, out int prev, SegmentEndData[] endDatas)
            {
                prev = index.PrevIndex(endDatas.Length);
                while (prev != index && endDatas[prev].Collision == false)
                    prev = prev.PrevIndex(endDatas.Length);

                return prev != index;
            }

            static bool GetNextIndex(int index, out int next, SegmentEndData[] endDatas)
            {
                next = index.NextIndex(endDatas.Length);
                while (next != index && endDatas[next].Collision == false)
                    next = next.NextIndex(endDatas.Length);

                return next != index;
            }
        }
        private static void GetMainMinLimit(SegmentEndData leftData, SegmentEndData rightData, int count, ref SideLimits leftLimit, ref SideLimits rightLimit)
        {
            var iBezier = leftData.LeftSide.MainTrajectory;
            var jBezier = rightData.RightSide.MainTrajectory;

            if (Intersection.CalculateSingle(iBezier, jBezier, out var leftMinT, out var rightMinT))
            {
                leftLimit.mainMinT = leftMinT;
                rightLimit.mainMinT = rightMinT;
                return;
            }

            if (count == 2)
            {
                var middleDir = iBezier.StartPosition - jBezier.StartPosition;
                if (NormalizeDotXZ(iBezier.StartDirection, middleDir) >= 0.999f && NormalizeDotXZ(middleDir, -jBezier.StartDirection) >= 0.999f)
                {
                    leftLimit.mainMinT = 0f;
                    rightLimit.mainMinT = 0f;
                    return;
                }
            }

            GetMainMinLimit(leftData, rightData, SideType.Left, ref leftLimit.mainMinT, ref rightLimit.additionalMinT);
            GetMainMinLimit(rightData, leftData, SideType.Right, ref rightLimit.mainMinT, ref leftLimit.additionalMinT);
        }
        private static void GetMainMinLimit(SegmentEndData data, SegmentEndData otherData, SideType side, ref float? mainMinT, ref float? otherAdditionalMinT)
        {
            var bezier = data[side].MainTrajectory;
            var line = otherData[side.Invert()].AdditionalTrajectory;

            if (Intersection.CalculateSingle(bezier, line, out var minT, out var lineT) /*&& lineT >= 0f && lineT <= 16f*/)
            {
                otherAdditionalMinT = lineT;

                if (lineT <= 16f / line.Length)
                {
                    var dir = bezier.Tangent(minT);
                    var dot = NormalizeDotXZ(dir, line.StartDirection);
                    var cross = NormalizeCrossXZ(dir, line.StartDirection);
                    var minAngle = Mathf.Clamp(1 - lineT / 1600f, 0.99f, 0.999f);
                    if (Mathf.Abs(dot) < minAngle && (cross >= 0f ^ dot >= 0f ^ side == SideType.Left))
                    {
                        mainMinT = minT;
                        return;
                    }
                }
            }

            var endLine = new StraightTrajectory(otherData.LeftSide.MainTrajectory.EndPosition, otherData.RightSide.MainTrajectory.EndPosition).Cut(0.01f, 0.99f);
            if (Intersection.CalculateSingle(bezier, endLine, out minT, out _))
            {
                mainMinT = minT;
                return;
            }

            var startLine = new StraightTrajectory(otherData.LeftSide.MainTrajectory.StartPosition, otherData.RightSide.MainTrajectory.StartPosition).Cut(0.01f, 0.99f);
            if (Intersection.CalculateSingle(bezier, startLine, out minT, out _))
            {
                mainMinT = minT;
                return;
            }

            mainMinT = null;
        }
        private static void GetSubMinLimit(ITrajectory main, ITrajectory sub, SideType side, ref float? defaultT)
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

                sub = new BezierTrajectory(point + (side == SideType.Left ? halfWidth : -halfWidth) * mirrorNormal, mirror, sub.EndPosition, sub.EndDirection, true, true, true);
            }

            if (Intersection.CalculateSingle(main, sub, out var t, out _))
            {
                defaultT = Mathf.Max(defaultT ?? 0f, t);
                return;
            }
            if (Intersection.CalculateSingle(main, new StraightTrajectory(sub.StartPosition, sub.StartPosition - sub.StartDirection * 16f), out t, out _))
            {
                defaultT = Mathf.Max(defaultT ?? 0f, t);
                return;
            }
        }
        private static void CorrectDefaultOffset(ITrajectory trajectory, ref float? defaultT, bool alwaysDefault, float defaultOffset, float minCornerOffset, float additionalOffset)
        {
            if (defaultT == null || alwaysDefault)
                defaultT = Mathf.Max(defaultT ?? 0f, trajectory.Travel(Mathf.Min(defaultOffset, minCornerOffset)));

            var distance = trajectory.Distance(0f, defaultT.Value);
            defaultT = trajectory.Travel(defaultT.Value, Mathf.Max(minCornerOffset - distance, additionalOffset));
        }

        public static void UpdateMaxLimits(ushort segmentId)
        {
            SegmentEndData start = null;
            SegmentEndData end = null;
            try
            {
                SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out start, out end);
                if (start != null && end != null)
                {
                    if ((start.NodeData.State & State.Error) != 0 && (end.NodeData.State & State.Error) != 0)
                        return;
                }
                else if (start != null)
                {
                    if ((start.NodeData.State & State.Error) != 0)
                        return;
                }
                else if (end != null)
                {
                    if ((end.NodeData.State & State.Error) != 0)
                        return;
                }
#if DEBUG && EXTRALOG
                SingletonMod<Mod>.Logger.Debug($"Segment #{segmentId} update max limits");
#endif

                if (start == null)
                    SetNoMaxLimits(end);
                else if (end == null)
                    SetNoMaxLimits(start);
                else
                {
                    SetMaxLimits(start, end, SideType.Left);
                    SetMaxLimits(start, end, SideType.Right);
                }
            }
            catch (Exception error)
            {
                if (start != null)
                    start.NodeData.State = State.Error;
                if (end != null)
                    end.NodeData.State = State.Error;

                SingletonMod<Mod>.Logger.Error($"Segment #{segmentId} update max limits failed", error);
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

                var startT = start.GetCornerOffset(startSide, start.OffsetT);
                var endT = end.GetCornerOffset(endSide, end.OffsetT);

                var startMainT = startSide.ToMainT(startT);
                var endMainT = endSide.ToMainT(endT);

                if (startMainT + endMainT > 1f)
                {
                    var delta = (startMainT + endMainT - 1f) / 2;
                    startMainT -= delta;
                    endMainT -= delta;
                }

                startT = Mathf.Clamp01(startSide.FromMainT(1f - endMainT));
                endT = Mathf.Clamp01(endSide.FromMainT(1f - startMainT));

                startSide.MaxT = startT;
                endSide.MaxT = endT;
            }
        }


        #endregion

        #region CALCULATE

        public void CalculateMain()
        {
            CalculateSegmentLimit();
            CalculateOffset();

            LeftSide.CalculateMain();
            RightSide.CalculateMain();
        }
        public void CalculateNotMain(BezierTrajectory left, BezierTrajectory right)
        {
            CalculateSegmentLimit();
            CalculateOffset();

            if (IsDecoration || IsMainRoad || Mode == Mode.FreeForm || FollowSlope == false)
            {
                LeftSide.CalculateMain();
                RightSide.CalculateMain();
            }
            else
            {
                LeftSide.CalculateNotMain(left, right);
                RightSide.CalculateNotMain(left, right);
            }
        }
        public static void AfterCalculate(ushort segmentId)
        {
            SegmentEndData start = null;
            SegmentEndData end = null;
            try
            {
                SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out start, out end);
                if (start != null && end != null)
                {
                    SegmentSide.FixBend(start.LeftSide, end.RightSide);
                    SegmentSide.FixBend(end.LeftSide, start.RightSide);
                }
            }
            catch (Exception error)
            {
                if (start != null)
                    start.NodeData.State = State.Error;
                if (end != null)
                    end.NodeData.State = State.Error;

                SingletonMod<Mod>.Logger.Error($"Segment #{segmentId} after calculate failed", error);
            }
        }
        public void AfterCalculate()
        {
            LeftSide.AfterCalculate();
            RightSide.AfterCalculate();

            CalculatePositionAndDirection();
            UpdateVehicleTwist();
        }

        private void CalculateSegmentLimit()
        {
            var startLimitLine = new StraightTrajectory(LeftSide.MinTempPos, RightSide.MinTempPos);
            var endLimitLine = new StraightTrajectory(LeftSide.MaxTempPos, RightSide.MaxTempPos);

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

            var startLeft = GetAngle(LeftSide.MinTempPos - position, direction);
            var endLeft = GetAngle(LeftSide.MaxTempPos - position, direction);
            var startRight = GetAngle(position - RightSide.MinTempPos, direction);
            var endRight = GetAngle(position - RightSide.MaxTempPos, direction);

            MinRotate = Mathf.Clamp(Mathf.Max(startLeft, endRight), NodeStyle.MinRotate, NodeStyle.MaxRotate);
            MaxRotate = Mathf.Clamp(Mathf.Min(endLeft, startRight), NodeStyle.MinRotate, NodeStyle.MaxRotate);

#if DEBUG && EXTRALOG
            if (Settings.ExtraDebug && Settings.SegmentId == Id)
            {
                SingletonMod<Mod>.Logger.Debug($"LS: {LeftSide.MinPos}, LE: {LeftSide.MaxPos}, RS: {RightSide.MinPos}, RE: {RightSide.MaxPos}");
                SingletonMod<Mod>.Logger.Debug($"LS: {startLeft}, LE: {endLeft}, RS: {startRight}, RE: {endRight}, Min: {MinRotate}, Max: {MaxRotate}, IsMin: {IsMinBorderT}");
            }
#endif
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
                SetRotate(Rotate, true);

                var t = OffsetT;
                LeftSide.RawT = GetCornerOffset(LeftSide, t);
                RightSide.RawT = GetCornerOffset(RightSide, t);
            }
        }
        private void SetByCorners(SideType? prioritySide = null)
        {
            var leftPosition = LeftSide.RawTrajectory.Position(LeftSide.CurrentTempT);
            var rightPosition = RightSide.RawTrajectory.Position(RightSide.CurrentTempT);

            var line = new StraightTrajectory(rightPosition, leftPosition);
            var intersect = Intersection.CalculateSingle(RawSegmentBezier, line, out var t, out _);

            if (!intersect && prioritySide != null)
            {
                var side = this[prioritySide.Value];
                var anotherSide = this[prioritySide.Value.Invert()];

                var sidePosition = side.RawTrajectory.Position(side.CurrentTempT);
                var position = RawSegmentBezier.Position(0.05f / RawSegmentBezier.Length);

                if (Intersection.CalculateSingle(anotherSide.RawTrajectory, new StraightTrajectory(sidePosition, position, false), out var anotherT, out _))
                {
                    anotherSide.RawT = Mathf.Clamp(anotherT, anotherSide.MinT, anotherSide.MaxT);
                    t = 0f;
                    intersect = true;
                }
            }

            if (intersect)
            {
                var offset = t == 0f ? 0f : RawSegmentBezier.Trajectory.Cut(0f, t).Length(1, 7);
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
                    SetRotate(Rotate, true);
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
            var direction = RawSegmentBezier.Tangent(t).MakeFlatNormalized().TurnDeg(90 + Rotate, true);

            var line = new StraightTrajectory(position, position + direction, false);
            var intersections = Intersection.Calculate(side.RawTrajectory, line);

            if (intersections.Any(i => i.IsIntersect))
            {
                var intersect = intersections.Aggregate((i, j) => Mathf.Abs(i.FirstT - t) < Mathf.Abs(j.FirstT - t) ? i : j);
                return intersect.FirstT;
            }
            else if (Rotate == 0f)
                return t <= 0.5f ? 0f : 1f;
            else
                return side.Type == SideType.Left ^ Rotate > 0f ? 0f : 1f;
        }
        private void CalculatePositionAndDirection()
        {
            var line = new StraightTrajectory(LeftSide.StartPos, RightSide.StartPos);
            float t;

            if (Mode == Mode.FreeForm)
                t = 0.5f;
            else if (Intersection.CalculateSingle(line, RawSegmentBezier, out var firstT, out _))
                t = firstT;
            else if (Intersection.CalculateSingle(line, new StraightTrajectory(RawSegmentBezier.StartPosition, RawSegmentBezier.StartPosition + RawSegmentBezier.StartDirection, false), out firstT, out _))
                t = firstT;
            else
                t = 0.5f;

            Position = line.Position(t);
            Direction = NormalizeXZ(LeftSide.StartDir * t + RightSide.StartDir * (1 - t));
        }
        private void UpdateVehicleTwist()
        {
            var diff = RightSide.StartPos - LeftSide.StartPos;
            VehicleTwist = Mathf.Atan2(diff.y, LengthXZ(diff)) * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public float GetMinCornerOffset(float styleOffset) => Mathf.Clamp(Mathf.Max(Id.GetSegment().Info.m_netAI.GetMinCornerOffset(Id, ref Id.GetSegment(), NodeId, ref NodeId.GetNode()), styleOffset), MinPossibleOffset, MaxPossibleOffset);
        public void GetCorner(SideType sideType, out Vector3 position, out Vector3 direction)
        {
            var side = sideType == SideType.Left ? LeftSide : RightSide;

            position = side.StartPos;
            direction = side.StartDir;
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
            var position = side.RawTrajectory.Position(side.CurrentTempT);
            var direction = side.RawTrajectory.Tangent(side.CurrentTempT).Turn90(true);
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

        public void Render(OverlayData data)
        {
            Render(data, data, data);
            LeftSide.RenderCenter(data);
            RightSide.RenderCenter(data);
        }
        public void Render(OverlayData contourData, OverlayData circleData, OverlayData centerData)
        {
            SingletonManager<Manager>.Instance.TryGetNodeData(NodeId, out var data);

            RenderContour(contourData);
            if (data.IsMoveableEnds && IsChangeable)
            {
                if (!IsNarrow)
                {
                    RenderStart(contourData, LengthXZ(LeftSide.StartPos - Position) + CircleRadius, 0f);
                    RenderStart(contourData, 0f, LengthXZ(RightSide.StartPos - Position) + CircleRadius);
                    RenderCircle(circleData);
                }
                else
                {
                    RenderStart(contourData);
                }

                if (IsOffsetChangeable)
                    RenderCenter(centerData);
                else if (IsRotateChangeable)
                    Position.RenderCircle(centerData, CenterRadius * 2, CenterRadius * 1.2f);
            }
            else
                RenderStart(contourData);
        }
        public void RenderAlign(OverlayData contourData, OverlayData? leftData = null, OverlayData? rightData = null)
        {
            RenderContour(contourData);
            RenderStart(contourData);

            if (leftData != null)
                LeftSide.RenderCenter(leftData.Value);
            if (rightData != null)
                RightSide.RenderCenter(rightData.Value);
        }

        public void RenderContour(OverlayData data)
        {
            RenderSide(LeftSide, data);
            RenderSide(RightSide, data);
            RenderEnd(data);
        }
        public void RenderStart(OverlayData data, float? leftCut = null, float? rightCut = null)
        {
            var line = new StraightTrajectory(LeftSide.StartPos, RightSide.StartPos);
            var length = LengthXZ(line.StartPosition - line.EndPosition);
            var height = Mathf.Abs(line.StartPosition.y - line.EndPosition.y);
            var angle = Mathf.Atan(height / length);
            var startT = (leftCut ?? 0f) / Mathf.Cos(angle) / line.Length;
            var endT = (rightCut ?? 0f) / Mathf.Cos(angle) / line.Length;
            line = line.Cut(startT, 1 - endT);
            line.Render(data);
        }
        private void RenderSide(SegmentSide side, OverlayData data)
        {
            var bezier = new BezierTrajectory(side.StartPos, side.StartDir, side.EndPos, side.EndDir, true, true, true);
            bezier.Render(data);
        }
        private void RenderEnd(OverlayData data)
        {
            var endSide = new StraightTrajectory(LeftSide.EndPos, RightSide.EndPos);
            endSide.Render(data);
        }
        public void RenderCenter(OverlayData data) => Position.RenderCircle(data, CenterRadius * 2, 0f);
        public void RenderCircle(OverlayData data) => Position.RenderCircle(data, CircleRadius * 2 + 0.5f, CircleRadius * 2 - 0.5f);
        public void RenderGuides(OverlayData dataAllow, OverlayData dataForbidden, OverlayData dataDefault)
        {
            LeftSide.RenderGuides(dataAllow, dataForbidden, dataDefault);
            RightSide.RenderGuides(dataAllow, dataForbidden, dataDefault);
        }

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr(nameof(Id), Id);
            config.AddAttr("IS", Mode);
            config.AddAttr("O", _offsetValue);
            config.AddAttr("RA", _rotateValue);
            config.AddAttr("SA", SlopeAngle);
            config.AddAttr("TA", TwistAngle);
            config.AddAttr("S", Shift);
            config.AddAttr("ST", Stretch);
            config.AddAttr("NM", NoMarkings == true ? 1 : 0);
            config.AddAttr("CL", Collision == true ? 1 : 0);
            config.AddAttr("FNL", ForceNodeLess == true ? 1 : 0);
            config.AddAttr("KD", KeepDefaults ? 1 : 0);
            config.AddAttr("FS", FollowSlope == true ? 1 : 0);
            //config.AddAttr("DH", DeltaHeight);

            return config;
        }

        public void FromXml(XElement config, NodeStyle style)
        {
            if (style.SupportMode != SupportOption.None)
                Mode = (Mode)config.GetAttrValue("IS", (int)style.DefaultMode);

            if (style.SupportSlope != SupportOption.None && !IsUntouchable)
                SlopeAngle = config.GetAttrValue("SA", style.DefaultSlope);

            if (style.SupportTwist != SupportOption.None && !IsUntouchable)
                TwistAngle = config.GetAttrValue("TA", style.DefaultTwist);

            if (style.SupportShift != SupportOption.None && !IsUntouchable)
                Shift = config.GetAttrValue("S", style.DefaultShift);

            if (style.SupportStretch != SupportOption.None && !IsUntouchable)
                Stretch = config.GetAttrValue("ST", style.DefaultStretch);

            if (style.SupportMarking != SupportOption.None && !IsUntouchable)
                NoMarkings = config.GetAttrValue("NM", style.DefaultNoMarking ? 1 : 0) == 1;

            if (style.SupportCollision != SupportOption.None && !IsUntouchable)
                Collision = config.GetAttrValue("CL", style.GetDefaultCollision(this) ? 1 : 0) == 1;

            if (style.SupportForceNodeless != SupportOption.None && !IsUntouchable)
                SetForceNodeless(config.GetAttrValue("FNL", style.DefaultForceNodeLess ? 1 : 0) == 1);

            if (style.SupportFollowMainSlope != SupportOption.None && !IsUntouchable)
                FollowSlope = config.GetAttrValue("FS", style.DefaultFollowSlope ? 1 : 0) == 1;

            //if (style.SupportDeltaHeight != SupportOption.None && !IsUntouchable)
            //    DeltaHeight = config.GetAttrValue("DH", style.DefaultDeltaHeight);

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
