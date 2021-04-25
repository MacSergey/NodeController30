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

namespace NodeController
{
    [Serializable]
    public class NodeData : INetworkData, IToXml
    {
        #region PROPERTIES

        public static string XmlName => "N";

        public string Title => string.Format(Localize.Panel_NodeId, Id);
        public string XmlSection => XmlName;

        public ushort Id { get; set; }
        public ref NetNode Node => ref Id.GetNode();
        public NetInfo Info => Node.Info;
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
                yield return FirstMainSegmentEnd;
                yield return SecondMainSegmentEnd;
            }
        }
        public SegmentEndData this[ushort segmentId] => SegmentEnds.TryGetValue(segmentId, out var data) ? data : null;
        public Vector3 Position { get; private set; }

        public BezierTrajectory MainBezier { get; private set; } = new BezierTrajectory(new Bezier3());
        public BezierTrajectory LeftMainBezier { get; private set; } = new BezierTrajectory(new Bezier3());
        public BezierTrajectory RightMainBezier { get; private set; } = new BezierTrajectory(new Bezier3());

        private MainRoad MainRoad { get; set; } = new MainRoad();

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
        private float MainDot => DotXZ(FirstSegment.GetDirection(Id), SecondSegment.GetDirection(Id));
        public bool IsStraight => IsTwoRoads && MainDot < -0.99f;
        public bool Is180 => IsTwoRoads && MainDot > 0.99f;
        public bool IsEqualWidth => IsTwoRoads && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;
        public bool HasNodeLess => SegmentEndDatas.Any(s => s.IsNodeLess);

        public float Offset
        {
            get => Style.GetOffset();
            set
            {
                Style.SetOffset(value);
                UpdateNode();
            }
        }
        public float RotateAngle
        {
            get => Style.GetRotate();
            set
            {
                Style.SetRotate(value);
                UpdateNode();
            }
        }
        public float SlopeAngle
        {
            get => Style.GetSlope();
            set
            {
                Style.SetSlope(value);
                UpdateNode();
            }
        }
        public float TwistAngle
        {
            get => Style.GetTwist();
            set
            {
                Style.SetTwist(value);
                UpdateNode();
            }
        }
        public float Shift
        {
            get => Style.GetShift();
            set
            {
                Style.SetShift(value);
                UpdateNode();
            }
        }
        public float Stretch
        {
            get => Style.GetStretch();
            set
            {
                Style.SetStretch(value);
                UpdateNode();
            }
        }
        public float StretchPercent
        {
            get => Stretch * 100f;
            set => Stretch = value / 100f;
        }
        public bool NoMarkings
        {
            get => Style.GetNoMarkings();
            set
            {
                Style.SetNoMarkings(value);
                UpdateNode();
            }
        }
        public bool IsSlopeJunctions
        {
            get => Style.GetIsSlopeJunctions();
            set
            {
                Style.SetIsSlopeJunctions(value);
                UpdateNode();
            }
        }

        public bool IsCSUR => Info.IsCSUR();
        public bool IsRoad => Info.m_netAI is RoadBaseAI;

        public bool IsEndNode => Type == NodeStyleType.End;
        public bool IsMiddleNode => Type == NodeStyleType.Middle;
        public bool IsBendNode => Type == NodeStyleType.Bend;
        public bool IsJunctionNode => !IsMiddleNode && !IsBendNode && !IsEndNode;
        public bool IsMoveableEnds => Style.IsMoveable;
        public bool IsIndividuallyShift => Style.SupportShift.IsSet(SupportOption.Individually);


        public bool CanModifyTextures => IsRoad && !IsCSUR;
        public bool NeedsTransitionFlag => IsTwoRoads && (Type == NodeStyleType.Custom || Type == NodeStyleType.Crossing || Type == NodeStyleType.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => Type == NodeStyleType.Crossing && CrossingIsRemoved(MainRoad.First) && CrossingIsRemoved(MainRoad.Second);

        #endregion

        #region BASIC

        public NodeData(ushort nodeId, NodeStyleType? nodeType = null)
        {
            Id = nodeId;
            UpdateSegmentEnds();
            UpdateMainRoad();
            UpdateStyle(true, nodeType);
        }
        public void Update(bool flags = true)
        {
            UpdateSegmentEnds();
            UpdateMainRoad();
            if (flags)
                UpdateFlags();
        }
        private void UpdateSegmentEnds()
        {
            var before = SegmentEnds.Values.Select(v => v.Id).ToList();
            var after = Node.SegmentIds().ToList();

            var still = before.Intersect(after).ToArray();
            var delete = before.Except(still).ToArray();
            var add = after.Except(still).ToArray();

            var newSegmentsEnd = still.ToDictionary(i => i, i => SegmentEnds[i]);

            if (delete.Length == 1 && add.Length == 1)
            {
                var changed = SegmentEnds[delete[0]];
                changed.Id = add[0];
                newSegmentsEnd.Add(changed.Id, changed);
                MainRoad.Replace(delete[0], add[0]);
            }
            else
            {
                foreach (var segmentId in add)
                {
                    var newsegmentEnd = new SegmentEndData(segmentId, Id);
                    newSegmentsEnd.Add(segmentId, newsegmentEnd);

                    if (Style is NodeStyle style)
                        newsegmentEnd.ResetToDefault(style, true);
                }
            }

            SegmentEnds = newSegmentsEnd;
        }
        private void UpdateMainRoad()
        {
            if (!ContainsSegment(MainRoad.First))
                MainRoad.First = FindMain(MainRoad.Second);
            if (!ContainsSegment(MainRoad.Second))
                MainRoad.Second = FindMain(MainRoad.First);
        }
        private void UpdateFlags()
        {
            ref var node = ref Id.GetNode();

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
            DefaultFlags = Node.m_flags;

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

                first.GetCorner(true, out var firstLeftPos, out var firstLeftDir);
                second.GetCorner(false, out var secondRightPos, out var secondRightDir);
                LeftMainBezier = new BezierTrajectory(firstLeftPos, -firstLeftDir, secondRightPos, -secondRightDir, false);

                first.GetCorner(false, out var firstRightPos, out var firstRightDir);
                second.GetCorner(true, out var secondLeftPos, out var secondLeftDir);
                RightMainBezier = new BezierTrajectory(firstRightPos, -firstRightDir, secondLeftPos, -secondLeftDir, false);
            }

            foreach (var segmentEnd in SegmentEndDatas)
            {
                if (!MainRoad.IsMain(segmentEnd.Id))
                    segmentEnd.Calculate(false);
            }

            if (IsEndNode)
                Position = SegmentEndDatas.First().RawSegmentBezier.StartPosition;
            else if (!IsMiddleNode)
                Position = (LeftMainBezier.Position(0.5f) + RightMainBezier.Position(0.5f)) / 2f;
            else
            {
                SegmentEndData.FixMiddle(FirstMainSegmentEnd, SecondMainSegmentEnd);
                Position = Node.m_position;
            }
        }

        private ushort FindMain(ushort ignore)
        {
            var main = SegmentEnds.Values.Aggregate(default(SegmentEndData), (i, j) => CompareSegmentEnds(i, j, ignore));
            return main?.Id ?? 0;
        }
        private SegmentEndData CompareSegmentEnds(SegmentEndData first, SegmentEndData second, ushort ignore)
        {
            if (!IsValid(first))
                return IsValid(second) ? second : null;
            else if (!IsValid(second))
                return first;

            var firstInfo = first.Id.GetSegment().Info;
            var secondInfo = second.Id.GetSegment().Info;

            int result;

            if ((result = firstInfo.m_flatJunctions.CompareTo(secondInfo.m_flatJunctions)) == 0)
                if ((result = firstInfo.m_forwardVehicleLaneCount.CompareTo(secondInfo.m_forwardVehicleLaneCount)) == 0)
                    if ((result = firstInfo.m_halfWidth.CompareTo(secondInfo.m_halfWidth)) == 0)
                        result = ((firstInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false).CompareTo((secondInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false);

            return result >= 0 ? first : second;

            bool IsValid(SegmentEndData end) => end != null && end.Id != ignore;
        }
        public void UpdateNode() => SingletonManager<Manager>.Instance.Update(Id, true);
        public void ResetToDefault()
        {
            ResetToDefaultImpl(true);
            UpdateNode();
        }
        private void ResetToDefaultImpl(bool force)
        {
            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault(Style, force);
        }

        private void SetType(NodeStyleType type, bool force)
        {
            if (type == Style?.Type)
                return;

            Style = type.GetStyle(this);

            ResetToDefaultImpl(force);
        }
        public void SetMain(ushort first, ushort second)
        {
            if (ContainsSegment(first) && ContainsSegment(second))
            {
                MainRoad = new MainRoad(first, second);
                UpdateNode();
            }
        }

        #endregion

        #region UTILITIES

        public bool ContainsSegment(ushort segmentId) => SegmentEnds.ContainsKey(segmentId);
        public bool IsPossibleType(NodeStyleType type) => type == Type || IsPossibleTypeImpl(type);
        private bool IsPossibleTypeImpl(NodeStyleType newNodeType)
        {
            if (IsJunction || IsCSUR)
                return newNodeType == NodeStyleType.Custom;

            return newNodeType switch
            {
                NodeStyleType.Crossing => IsRoad && IsTwoRoads && IsEqualWidth && IsStraight && PedestrianLaneCount >= 2 && !HasNodeLess,
                NodeStyleType.UTurn => IsRoad && IsTwoRoads && !HasNodeLess && Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0,
                NodeStyleType.Stretch => IsRoad && IsTwoRoads && CanModifyTextures && IsStraight,
                NodeStyleType.Middle => IsTwoRoads && IsStraight || Is180,
                NodeStyleType.Bend => IsTwoRoads,
                NodeStyleType.Custom => !IsEnd,
                NodeStyleType.End => IsEnd,
                _ => throw new Exception("Unreachable code"),
            };
        }
        public void GetClosest(Vector3 position, out Vector3 closestPos, out Vector3 closestDir)
        {
            LeftMainBezier.Trajectory.ClosestPositionAndDirection(position, out var leftClosestPos, out var leftClosestDir, out _);
            RightMainBezier.Trajectory.ClosestPositionAndDirection(position, out var rightClosestPos, out var rightClosestDir, out _);

            if ((leftClosestPos - position).sqrMagnitude < (rightClosestPos - position).sqrMagnitude)
            {
                closestPos = leftClosestPos;
                closestDir = leftClosestDir;
            }
            else
            {
                closestPos = rightClosestPos;
                closestDir = rightClosestDir;
            }
        }

        private bool CrossingIsRemoved(ushort segmentId) => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(Id, segmentId);

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
        public void FromXml(XElement config, Dictionary<InstanceID, InstanceID> map = null)
        {
            if (config.Element(MainRoad.XmlName) is XElement mainRoadConfig)
                MainRoad.FromXml(mainRoadConfig);

            foreach (var segmentEndConfig in config.Elements(SegmentEndData.XmlName))
            {
                var id = segmentEndConfig.GetAttrValue(nameof(SegmentEndData.Id), (ushort)0);

                if (map != null && map.TryGetValue(new InstanceID() { NetSegment = id }, out var instance))
                    id = instance.NetSegment;

                if (SegmentEnds.TryGetValue(id, out var segmentEnd))
                    segmentEnd.FromXml(segmentEndConfig, Style);
            }
        }

        public static bool FromXml(XElement config, out NodeData data)
        {
            var id = config.GetAttrValue("Id", (ushort)0);
            var type = (NodeStyleType)config.GetAttrValue("T", (int)NodeStyleType.Custom);

            if (id != 0 && id <= NetManager.MAX_NODE_COUNT)
            {
                try
                {
                    data = new NodeData(id, type);
                    data.FromXml(config);
                    return true;
                }
                catch { }
            }

            data = null;
            return false;
        }

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetNodeTypeProperty(parent, refresh);
            Style.GetUIComponents(parent, refresh);
        }

        private NodeTypePropertyPanel GetNodeTypeProperty(UIComponent parent, Action refresh)
        {
            var typeProperty = ComponentPool.Get<NodeTypePropertyPanel>(parent);
            typeProperty.Text = Localize.Option_Type;
            typeProperty.Init(IsPossibleType);
            typeProperty.SelectedObject = Type;
            typeProperty.OnSelectObjectChanged += (value) =>
            {
                Type = value;
                refresh();
            };

            return typeProperty;
        }

        #endregion

    }
    public class MainRoad : IToXml, IFromXml
    {
        public static string XmlName => "MR";
        public string XmlSection => XmlName;

        public ushort First { get; set; }
        public ushort Second { get; set; }

        public bool IsComplite => First != 0 && Second != 0;
        public IEnumerable<ushort> Segments
        {
            get
            {
                if (First != 0)
                    yield return First;
                if (Second != 0)
                    yield return Second;
            }
        }
        public MainRoad() { }
        public MainRoad(ushort first, ushort second)
        {
            First = first;
            Second = second;
        }
        public bool IsMain(ushort id) => id != 0 && (id == First || id == Second);
        public void Replace(ushort from, ushort to)
        {
            if (First == from)
                First = to;
            else if (Second == from)
                Second = to;
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr("F", First);
            config.AddAttr("S", Second);

            return config;
        }

        public void FromXml(XElement config)
        {
            First = config.GetAttrValue<ushort>("F", 0);
            Second = config.GetAttrValue<ushort>("S", 0);
        }

        public override string ToString() => $"{First}-{Second}";
    }

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeStyleType, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 150f;
        protected override bool IsEqual(NodeStyleType first, NodeStyleType second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeStyleType> { }
        protected override string GetDescription(NodeStyleType value) => value.Description();
    }
}
