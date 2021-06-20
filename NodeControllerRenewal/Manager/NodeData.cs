using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
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
        //private List<SegmentEndData> RawSegmentEnds { get; set; } = new List<SegmentEndData>();
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
        public SegmentEndData this[ushort segmentId] => SegmentEnds.TryGetValue(segmentId, out var data) ? data : null;
        private Vector3 Position { get; set; }

        public BezierTrajectory MainBezier { get; private set; } = new BezierTrajectory(new Bezier3());
        public BezierTrajectory LeftMainBezier { get; private set; } = new BezierTrajectory(new Bezier3());
        public BezierTrajectory RightMainBezier { get; private set; } = new BezierTrajectory(new Bezier3());

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
        public bool HasNodeLess => SegmentEndDatas.Any(s => s.IsNodeLess);
        public bool IsUnderground => Id.GetNode().m_flags.IsSet(NetNode.Flags.Underground);

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
        public bool NoMarkings
        {
            get => Style.GetNoMarkings();
            set => Style.SetNoMarkings(value);
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


        public bool NeedsTransitionFlag => IsTwoRoads && (Type == NodeStyleType.Custom || Type == NodeStyleType.Crossing || Type == NodeStyleType.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => Type == NodeStyleType.Crossing && NoMarkings;

        #endregion

        #region BASIC

        public NodeData(ushort nodeId, NodeStyleType? nodeType = null)
        {
            if (!nodeId.GetNode().m_flags.IsSet(NetNode.Flags.Created))
                throw new NodeNotCreatedException(nodeId);

            Id = nodeId;

            UpdateSegmentEnds();
            UpdateSegmentEndsImpl();
            MainRoad.Update(this);
            UpdateStyle(true, nodeType);
            UpdateMainRoadSegments();
        }
        public void Update(bool updateFlags)
        {
            UpdateSegmentEndsImpl();
            MainRoad.Update(this);
            if (updateFlags)
                UpdateFlags();
            UpdateMainRoadSegments();
        }
        public void UpdateSegmentEnds()
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
                    var newSegmentEnd = new SegmentEndData(segmentId, Id);
                    newSegmentEnds[newSegmentEnd.Id] = newSegmentEnd;

                    if (Style is NodeStyle style)
                        newSegmentEnd.ResetToDefault(style, true);
                }
            }

            SegmentEnds = newSegmentEnds;
        }
        private void UpdateSegmentEndsImpl()
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
            var node = Id.GetNode();

            if (node.m_flags == NetNode.Flags.None || node.m_flags.IsFlagSet(NetNode.Flags.Outside))
                return;

            if (node.m_flags != DefaultFlags)
                UpdateStyle(false, Style.Type);

            if (NeedsTransitionFlag)
                node.m_flags |= NetNode.Flags.Transition;
            else
                node.m_flags &= ~NetNode.Flags.Transition;

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
        }
        private void UpdateStyle(bool force, NodeStyleType? nodeType = null)
        {
            var node = Id.GetNode();
            if (node.m_flags.IsSet(NetNode.Flags.Created) && !node.m_flags.IsSet(NetNode.Flags.Deleted) && (node.m_flags & SupportFlags) == 0)
                node.CalculateNode(Id);

            DefaultFlags = node.m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultType = NodeStyleType.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Bend))
                DefaultType = NodeStyleType.Bend;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultType = NodeStyleType.Custom;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.End))
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
                if (!segmentEnd.IsMainRoad)
                {
                    segmentEnd.TwistAngle = Style.DefaultTwist;
                    segmentEnd.SlopeAngle = Style.DefaultSlope;
                }
            }
        }

        public void LateUpdate()
        {
            foreach (var segmentEnd in SegmentEndDatas)
            {
                if (MainRoad.IsMain(segmentEnd.Id))
                    segmentEnd.Calculate(true);
            }

            if (!IsMiddleNode && !IsEndNode && FirstMainSegmentEnd is SegmentEndData first && SecondMainSegmentEnd is SegmentEndData second)
            {
                MainBezier = new BezierTrajectory(first.Position, -first.Direction, second.Position, -second.Direction, false);
                LeftMainBezier = GetBezier(first, second, SideType.Left);
                RightMainBezier = GetBezier(first, second, SideType.Right);

                static BezierTrajectory GetBezier(SegmentEndData first, SegmentEndData second, SideType side)
                {
                    first.GetCorner(side == SideType.Left, out var firstPos, out var firstDir);
                    second.GetCorner(side == SideType.Right, out var secondPos, out var secondDir);
                    return new BezierTrajectory(firstPos, -firstDir, secondPos, -secondDir, false);
                }
            }

            foreach (var segmentEnd in SegmentEndDatas)
            {
                if (!MainRoad.IsMain(segmentEnd.Id))
                    segmentEnd.Calculate(false);
            }

            var position = Id.GetNode().m_position;

            if (IsEndNode)
                position = SegmentEndDatas.First().RawSegmentBezier.StartPosition;
            else if (!IsMiddleNode)
            {
                if (IsSlopeJunctions)
                    position = (LeftMainBezier.Position(0.5f) + RightMainBezier.Position(0.5f)) / 2f;
                else
                    position = SegmentEndDatas.AverageOrDefault(s => s.Position, position);
            }
            else
                SegmentEndData.FixMiddle(FirstMainSegmentEnd, SecondMainSegmentEnd);

            Position = position;
        }

        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(Id, true);
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
        public void GetClosest(Vector3 position, out Vector3 closestPos, out Vector3 closestDir, out float t)
        {
            LeftMainBezier.Trajectory.ClosestPositionAndDirection(position, out var leftClosestPos, out var leftClosestDir, out var leftT);
            RightMainBezier.Trajectory.ClosestPositionAndDirection(position, out var rightClosestPos, out var rightClosestDir, out var rightT);

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

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeStyleType, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 150f;
        protected override bool IsEqual(NodeStyleType first, NodeStyleType second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeStyleType> { }
        protected override string GetDescription(NodeStyleType value) => value.Description();
    }
}
