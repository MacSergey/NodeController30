using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking.Types;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NodeController
{
    [Serializable]
    public class NodeData : INetworkData, IToXml
    {
        #region PROPERTIES

        public static string XmlName => "N";

        public string Title => string.Format(Localize.Panel_NodeId, Id);
        public string XmlSection => XmlName;
        public static NetNode.Flags SupportFlags { get; } = NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Junction | NetNode.Flags.Bend;

        public State State { get; set; } = State.None;
        public ushort Id { get; set; }
        public NodeStyle Style { get; private set; }
        public NodeStyleType Type
        {
            get => Style.Type;
            set
            {
                SetType(value, false);
                UpdateNode();
            }
        }
        private Dictionary<ushort, SegmentEndData> SegmentEnds { get; set; } = new Dictionary<ushort, SegmentEndData>();
        public IEnumerable<SegmentEndData> SegmentEndDatas => SegmentEnds.Values;

        public IEnumerable<SegmentEndData> MainSegmentEndDatas
        {
            get
            {
                foreach (var segment in MainRoad.Segments)
                {
                    if (SegmentEnds.TryGetValue(segment, out var data))
                        yield return data;
                }
            }
        }
        private Vector3 Position { get; set; }
        private Dictionary<ushort, Vector3> CentrePositions { get; set; } = new Dictionary<ushort, Vector3>();
        public float Gap { get; private set; }

        public BezierTrajectory MainBezier { get; private set; } = new BezierTrajectory(new Bezier3());

        public MainRoad MainRoad { get; private set; } = new MainRoad();

        public NetNode.Flags DefaultFlags { get; private set; }
        public NodeStyleType DefaultType { get; private set; }

        public bool IsEnd => SegmentEnds.Count == 1;
        public bool IsTwoRoads => SegmentEnds.Count == 2;
        public bool IsJunction => SegmentEnds.Count > 2;
        public IEnumerable<ushort> SegmentIds => SegmentEnds.Keys;
        public int SegmentCount => SegmentEnds.Count;

        public NetSegment FirstSegment => MainRoad.First.GetSegment();
        public NetSegment SecondSegment => MainRoad.Second.GetSegment();
        public SegmentEndData FirstMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.First, out var data) ? data : null;
        public SegmentEndData SecondMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.Second, out var data) ? data : null;

        public bool HasPedestrianLanes => SegmentEnds.Keys.Any(s => s.GetSegment().Info.m_hasPedestrianLanes);
        private int PedestrianLaneCount => SegmentEnds.Keys.Max(s => s.GetSegment().Info.PedestrianLanes());
        private float MainDot => NormalizeDotXZ(FirstSegment.GetDirection(Id), SecondSegment.GetDirection(Id));
        public bool IsStraight => IsTwoRoads && MainDot < -0.995f;
        public bool Is180 => IsTwoRoads && MainDot > 0.995f;
        public bool IsEqualWidth => IsTwoRoads && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;
        public bool HasNodeLess => SegmentEndDatas.Any(s => s.FinalNodeLess);
        public bool IsUnderground => Id.GetNode().m_flags.IsSet(NetNode.Flags.Underground);
        public bool IsSameRoad
        {
            get
            {
                var info = Id.GetNode().Info;
                return SegmentEndDatas.All(s => s.Id.GetSegment().Info == info);
            }
        }

        public float Offset
        {
            get => Style.GetOffset();
            set => Style.SetOffset(value);
        }
        public float RotateAngle
        {
            get => Style.GetRotate();
            set => Style.SetRotate(value);
        }
        public float SlopeAngle
        {
            get => Style.GetSlope();
            set => Style.SetSlope(value);
        }
        public float TwistAngle
        {
            get => Style.GetTwist();
            set => Style.SetTwist(value);
        }
        public float Shift
        {
            get => Style.GetShift();
            set => Style.SetShift(value);
        }
        public float Stretch
        {
            get => Style.GetStretch();
            set => Style.SetStretch(value);
        }
        public float StretchPercent
        {
            get => Stretch * 100f;
            set => Stretch = value / 100f;
        }
        public bool? NoMarkings
        {
            get => Style.GetNoMarkings();
            set => Style.SetNoMarkings(value);
        }
        public bool? Collision
        {
            get => Style.GetCollision();
            set => Style.SetCollision(value);
        }
        public bool? ForceNodeLess
        {
            get => Style.GetForceNodeLess();
            set => Style.SetForceNodeLess(value);
        }
        public bool IsSlopeJunctions
        {
            get => Style.GetIsSlopeJunctions();
            set => Style.SetIsSlopeJunctions(value);
        }

        public bool IsRoad => SegmentEndDatas.All(s => s.IsRoad);
        public bool IsTunnel => SegmentEndDatas.Any(s => s.IsTunnel);

        public bool IsEndNode => Type == NodeStyleType.End;
        public bool IsMiddleNode => Type == NodeStyleType.Middle;
        public bool IsBendNode => Type == NodeStyleType.Bend;
        public bool IsJunctionNode => !IsMiddleNode && !IsBendNode && !IsEndNode;
        public bool IsMoveableEnds => Style.IsMoveable;
        public bool AllowSetMainRoad => IsJunction && IsSlopeJunctions && !IsDecoration;
        public bool IsDecoration => SegmentEndDatas.Any(s => s.IsDecoration);


        public bool NeedsTransitionFlag => IsTwoRoads && (Type == NodeStyleType.Custom || Type == NodeStyleType.Crossing || Type == NodeStyleType.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => Type == NodeStyleType.Crossing && NoMarkings != true;

        #endregion

        #region BASIC

        public NodeData(ushort nodeId, NodeStyleType? nodeType = null)
        {
            if (!nodeId.GetNode().m_flags.IsSet(NetNode.Flags.Created))
                throw new NodeNotCreatedException(nodeId);

            Id = nodeId;

            UpdateSegmentEndSet();
            UpdateSegmentEnds();
            MainRoad.Update(this);
            UpdateStyle(true, nodeType);
            UpdateMainRoadSegments();
        }
        public void AfterCalculateNode()
        {
            try
            {
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Node #{Id} after calculate node");
#endif
                SetFlags();
                UpdateSegmentEndSet();
            }
            catch (Exception error)
            {
                State = State.Error;
                SingletonMod<Mod>.Logger.Error($"Node #{Id} after calculate node failed", error);
            }
        }
        public void EarlyUpdate()
        {
            try
            {
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Node #{Id} early update");
#endif
                State |= State.Dirty;

                UpdateSegmentEnds();
                MainRoad.Update(this);
                UpdateFlags();
                UpdateMainRoadSegments();
            }
            catch (Exception error)
            {
                State = State.Error;
                SingletonMod<Mod>.Logger.Error($"Node #{Id} update failed", error);
            }
        }

        private void UpdateSegmentEndSet()
        {
            var before = SegmentEnds.Values.Select(v => v.Id).ToList();
            var after = Id.GetNode().SegmentIds().ToList();

            var still = before.Intersect(after).ToArray();
            var delete = before.Except(still).ToArray();
            var add = after.Except(still).ToArray();

            var newSegmentEnds = still.Select(i => SegmentEnds[i]).ToDictionary(s => s.Id, s => s);

            if (delete.Length == 1 && add.Length == 1)
            {
                var changed = SegmentEnds[delete[0]];
                changed.Id = add[0];
                newSegmentEnds[changed.Id] = changed;
                MainRoad.Replace(delete[0], add[0]);
            }
            else
            {
                foreach (var segmentId in add)
                {
                    var newSegmentEnd = new SegmentEndData(this, segmentId);
                    newSegmentEnds[newSegmentEnd.Id] = newSegmentEnd;

                    if (Style is NodeStyle style)
                        newSegmentEnd.ResetToDefault(style, true);
                }
            }
#if DEBUG
            if (SegmentEnds.Count == 0)
                SingletonMod<Mod>.Logger.Debug($"Node #{Id} Segments: {string.Join(", ", newSegmentEnds.Keys.Select(k => k.ToString()).ToArray())}");
            else
                SingletonMod<Mod>.Logger.Debug($"Node #{Id} Segments: Before={string.Join(", ", SegmentEnds.Keys.Select(k => k.ToString()).ToArray())};\tAfter={string.Join(", ", newSegmentEnds.Keys.Select(k => k.ToString()).ToArray())}");
#endif
            SegmentEnds = newSegmentEnds;
        }
        private void UpdateSegmentEnds()
        {
            foreach (var segmentEnd in SegmentEnds.Values)
                segmentEnd.Update();

            var i = 0;
            foreach (var segmentEnd in SegmentEnds.Values.OrderByDescending(s => s.AbsoluteAngle))
            {
                segmentEnd.Index = i;
                i += 1;
            }
        }

        private void UpdateFlags()
        {
            ref var node = ref Id.GetNode();

            if (node.m_flags == NetNode.Flags.None || node.m_flags.IsFlagSet(NetNode.Flags.Outside))
                return;

            if ((node.m_flags & SupportFlags) != DefaultFlags)
                UpdateStyle(false, Style.Type);

            SetFlags();
        }
        private void SetFlags()
        {
            ref var node = ref Id.GetNode();
#if DEBUG
            var oldFlags = node.m_flags;
#endif
            //it causes junction voids
            //if (NeedsTransitionFlag)
            //    node.m_flags |= NetNode.Flags.Transition;
            //else
            //    node.m_flags &= ~NetNode.Flags.Transition;

            if (IsMiddleNode)
            {
                node.m_flags |= NetNode.Flags.Middle;
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Bend | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
            }
            else if (IsBendNode)
            {
                node.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Middle);
            }
            else if (IsJunctionNode)
            {
                node.m_flags |= NetNode.Flags.Junction;
                node.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
            }

            if (IsMiddleNode && Style.IsDefault)
                node.m_flags |= NetNode.Flags.Moveable;
            else
                node.m_flags &= ~NetNode.Flags.Moveable;
#if DEBUG
            var set = node.m_flags & ~oldFlags;
            var reset = oldFlags & ~node.m_flags;
            SingletonMod<Mod>.Logger.Debug($"Node #{Id} Flags={node.m_flags};\tSet={set};\tReset={reset}");
#endif
        }
        private void UpdateStyle(bool force, NodeStyleType? nodeType = null)
        {
            ref var node = ref Id.GetNode();
            if ((node.m_flags & NetNode.Flags.Created) != 0 && (node.m_flags & NetNode.Flags.Deleted) == 0 && (node.m_flags & SupportFlags) == 0)
                node.CalculateNode(Id);

            DefaultFlags = node.m_flags & SupportFlags;

            if ((DefaultFlags & NetNode.Flags.Middle) != 0)
                DefaultType = NodeStyleType.Middle;
            else if ((DefaultFlags & NetNode.Flags.Bend) != 0)
                DefaultType = NodeStyleType.Bend;
            else if ((DefaultFlags & NetNode.Flags.Junction) != 0)
                DefaultType = NodeStyleType.Custom;
            else if ((DefaultFlags & NetNode.Flags.End) != 0)
                DefaultType = NodeStyleType.End;
            else
                throw new NotImplementedException($"Unsupported node flags: {DefaultFlags}");

            SetType(nodeType != null && IsPossibleTypeImpl(nodeType.Value) ? nodeType.Value : DefaultType, force);
        }
        private void UpdateMainRoadSegments()
        {
            foreach (var segmentEnd in SegmentEndDatas)
            {
                segmentEnd.IsMainRoad = IsMainRoad(segmentEnd.Id);
                if (!segmentEnd.IsMainRoad && !segmentEnd.IsDecoration)
                {
                    segmentEnd.TwistAngle = Style.DefaultTwist;
                    segmentEnd.SlopeAngle = Style.DefaultSlope;
                }
            }
        }

        public void LateUpdate()
        {
            try
            {
                if ((State & State.Error) != 0)
                    return;
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Node #{Id} late update");
#endif
                var firstMain = FirstMainSegmentEnd;
                var secondMain = SecondMainSegmentEnd;

                var position = Vector3.zero;
                var centrePositions = new Dictionary<ushort, Vector3>();

                if (IsEndNode)
                {
                    firstMain.CalculateMain(out _, out _, out _, out _);
                    firstMain.AfterCalculate();

                    position = SegmentEndDatas.First().RawSegmentBezier.StartPosition;
                }
                else if (IsMiddleNode)
                {
                    firstMain.CalculateMain(out _, out _, out _, out _);
                    secondMain.CalculateMain(out _, out _, out _, out _);

                    SegmentEndData.FixMiddle(firstMain, secondMain);

                    firstMain.AfterCalculate();
                    secondMain.AfterCalculate();

                    position = Id.GetNode().m_position;
                }
                else
                {
                    firstMain.CalculateMain(out var firstLeftPos, out var firstLeftDir, out var firstRightPos, out var firstRightDir);
                    secondMain.CalculateMain(out var secondLeftPos, out var secondLeftDir, out var secondRightPos, out var secondRightDir);

                    firstMain.AfterCalculate();
                    secondMain.AfterCalculate();

                    MainBezier = new BezierTrajectory(firstMain.Position, -firstMain.Direction, secondMain.Position, -secondMain.Direction, false);
                    var leftBezier = new BezierTrajectory(firstLeftPos, -firstLeftDir, secondRightPos, -secondRightDir, false);
                    var rightBezier = new BezierTrajectory(secondLeftPos, -secondLeftDir, firstRightPos, -firstRightDir, false);

                    foreach (var segmentEnd in SegmentEndDatas)
                    {
                        if (!MainRoad.IsMain(segmentEnd.Id))
                        {
                            segmentEnd.CalculateNotMain(leftBezier, rightBezier);
                            segmentEnd.AfterCalculate();
                        }
                    }

                    if (IsSlopeJunctions)
                        position = (leftBezier.Position(0.5f) + rightBezier.Position(0.5f)) * 0.5f;
                    else
                        position = SegmentEndDatas.AverageOrDefault(s => s.Position, Id.GetNode().m_position);

                    foreach (var segmentEnd1 in SegmentEndDatas)
                    {
                        List<SegmentEndData> canConnect = new List<SegmentEndData>();
                        var info1 = segmentEnd1.Id.GetSegment().Info;
                        var class1 = info1.GetConnectionClass();

                        foreach (var segmentEnd2 in SegmentEndDatas)
                        {
                            if (segmentEnd1 == segmentEnd2)
                                continue;

                            var info2 = segmentEnd2.Id.GetSegment().Info;
                            var class2 = info2.GetConnectionClass();

                            if ((class1.m_service != class2.m_service || ((info1.m_onlySameConnectionGroup || info2.m_onlySameConnectionGroup) && (info1.m_connectGroup & info2.m_connectGroup) == 0)) && (info1.m_nodeConnectGroups & info2.m_connectGroup) == 0 && (info2.m_nodeConnectGroups & info1.m_connectGroup) == 0)
                                continue;

                            canConnect.Add(segmentEnd2);
                        }

                        if (canConnect.Count == 0)
                            centrePositions[segmentEnd1.Id] = segmentEnd1.Position;
                        else
                            centrePositions[segmentEnd1.Id] = position;
                    }
                }

                var maxGap = 0f;
                foreach (var firstData in SegmentEndDatas)
                {
                    foreach (var secondData in SegmentEndDatas)
                    {
                        CalculateGap(ref maxGap, firstData, secondData, SideType.Left, SideType.Left);
                        CalculateGap(ref maxGap, firstData, secondData, SideType.Left, SideType.Right);
                        CalculateGap(ref maxGap, firstData, secondData, SideType.Right, SideType.Left);
                        CalculateGap(ref maxGap, firstData, secondData, SideType.Right, SideType.Right);

                        static void CalculateGap(ref float gap, SegmentEndData firstData, SegmentEndData secondData, SideType firstideType, SideType secondSideType)
                        {
                            firstData.GetCorner(firstideType, out var firstPos, out _);
                            secondData.GetCorner(secondSideType, out var secondPos, out _);
                            var delta = (firstPos - secondPos).sqrMagnitude;
                            if (delta > gap)
                                gap = delta;
                        }
                    }
                }

                Position = position;
                CentrePositions = centrePositions;
                Gap = Mathf.Sqrt(maxGap) + 2f;
            }
            catch (Exception error)
            {
                State = State.Error;
                SingletonMod<Mod>.Logger.Error($"Node #{Id} update failed", error);
            }
            finally
            {
                State = (State & State.Error) != 0 ? State.Fail : State.Fine;
            }
        }

        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(Id);
        public void SetKeepDefaults()
        {
            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.SetKeepDefaults();

            UpdateNode();
        }
        public void ResetToDefault()
        {
            SetType(DefaultType, true);
            MainRoad.Auto = true;
            UpdateNode();
        }
        private void SetType(NodeStyleType type, bool force)
        {
            if (type == Style?.Type && !force)
                return;

#if DEBUG
            SingletonMod<Mod>.Logger.Debug($"Node #{Id} set Type={type}");
#endif
            Style = type.GetStyle(this);

            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault(Style, force);
        }
        public void SetMain(ushort first, ushort second)
        {
            if (ContainsSegment(first) && ContainsSegment(second))
            {
                MainRoad = new MainRoad(first, second);
                UpdateNode();
            }
        }
        public void MakeStraightEnds()
        {
            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.MakeStraight();

            UpdateNode();
        }

        #endregion

        #region UTILITIES

        public Vector3 GetPosition() => Position + Vector3.up * (Id.GetNode().m_heightOffset / 64f);
        public Vector3 GetPosition(int index)
        {
            var segmentId = Id.GetNode().GetSegment(index);
            if (CentrePositions.TryGetValue(segmentId, out var position))
                return position + Vector3.up * (Id.GetNode().m_heightOffset / 64f);
            else
                return Position;
        }

        public bool ContainsSegment(ushort segmentId) => SegmentEnds.ContainsKey(segmentId);
        public bool TryGetSegment(ushort segmentId, out SegmentEndData segmentEnd) => SegmentEnds.TryGetValue(segmentId, out segmentEnd);
        public bool IsPossibleType(NodeStyleType type) => type == Type || IsPossibleTypeImpl(type);
        private bool IsPossibleTypeImpl(NodeStyleType newNodeType)
        {
            if (IsJunction)
                return newNodeType == NodeStyleType.Custom;

            return newNodeType switch
            {
                NodeStyleType.Crossing => IsTwoRoads && IsRoad && IsEqualWidth && IsStraight && PedestrianLaneCount >= 2 && !HasNodeLess,
                NodeStyleType.UTurn => IsTwoRoads && IsRoad && !HasNodeLess && Id.GetNode().Info.IsTwoWay(),
                NodeStyleType.Stretch => IsTwoRoads && IsRoad && !IsTunnel && !HasNodeLess && IsStraight,
                NodeStyleType.Middle => IsTwoRoads && IsStraight || Is180,
                NodeStyleType.Bend => IsTwoRoads,
                NodeStyleType.Custom => !IsEnd,
                NodeStyleType.End => IsEnd,
                _ => throw new Exception("Unreachable code"),
            };
        }
        public bool IsMainRoad(ushort segmentId) => MainRoad.IsMain(segmentId);

        public override string ToString() => $"NodeData(id:{Id} type:{Type})";

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr(nameof(Id), Id);
            config.AddAttr("T", (int)Type);

            foreach (var segmentEnd in SegmentEndDatas)
                config.Add(segmentEnd.ToXml());

            config.Add(MainRoad.ToXml());

            return config;
        }
        public void FromXml(XElement config, NetObjectsMap map)
        {
            if (config.Element(MainRoad.XmlName) is XElement mainRoadConfig)
                MainRoad.FromXml(mainRoadConfig, map);

            foreach (var segmentEndConfig in config.Elements(SegmentEndData.XmlName))
            {
                var id = segmentEndConfig.GetAttrValue(nameof(SegmentEndData.Id), (ushort)0);

                if (map.TryGetSegment(id, out var targetId))
                    id = targetId;

                if (SegmentEnds.TryGetValue(id, out var segmentEnd))
                    segmentEnd.FromXml(segmentEndConfig, Style);
            }
        }

        #endregion
    }

    public enum State
    {
        None = 0,
        Fine = 1,
        Dirty = 2,
        Error = 4,
        Fail = 8,
    }

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeStyleType, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 150f;
        protected override bool IsEqual(NodeStyleType first, NodeStyleType second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeStyleType> { }
        protected override string GetDescription(NodeStyleType value) => value.Description();
    }
}
