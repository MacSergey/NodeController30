using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using static ColossalFramework.Math.VectorUtils;
using KianCommons;

using System.Diagnostics;
using System.Linq;
using ModsCommon.Utilities;
using ModsCommon.UI;
using ColossalFramework.UI;
using System.Collections;
using NodeController.Utilities;

namespace NodeController
{
    [Serializable]
    public class NodeData : INetworkData
    {
        #region PROPERTIES

        public string Title => $"Node #{Id}";

        public ushort Id { get; set; }
        public NetNode Node => Id.GetNode();
        public NetInfo Info => Node.Info;
        private NodeStyle Style { get; set; }
        public NodeStyleType Type
        {
            get => Style.Type;
            set
            {
                SetType(value);
                UpdateNode();
            }
        }
        private Dictionary<ushort, SegmentEndData> SegmentEnds { get; set; } = new Dictionary<ushort, SegmentEndData>();
        public IEnumerable<SegmentEndData> SegmentEndDatas => SegmentEnds.Values;
        public SegmentEndData this[ushort segmentId] => SegmentEnds.TryGetValue(segmentId, out var data) ? data : null;
        private MainRoad MainRoad { get; set; } = new MainRoad();

        public bool IsDefault => Type == DefaultType && !SegmentEndDatas.Any(s => s?.IsDefault != true);

        public NetNode.Flags DefaultFlags { get; private set; }
        public NodeStyleType DefaultType { get; private set; }

        public bool IsEnd => SegmentEnds.Count == 1;
        public bool IsMain => SegmentEnds.Count == 2;
        public bool IsJunction => SegmentEnds.Count > 2;
        public IEnumerable<ushort> SegmentIds => SegmentEnds.Keys;
        public int SegmentCount => SegmentEnds.Count;

        public NetSegment FirstSegment => MainRoad.First.GetSegment();
        public NetSegment SecondSegment => MainRoad.Second.GetSegment();
        public SegmentEndData FirstMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.First, out var data) ? data : null;
        public SegmentEndData SecondMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.Second, out var data) ? data : null;

        public bool HasPedestrianLanes => SegmentEnds.Keys.Any(s => s.GetSegment().Info.m_hasPedestrianLanes);
        private int PedestrianLaneCount => SegmentEnds.Keys.Max(s => s.GetSegment().Info.CountPedestrianLanes());
        private float MainDot => DotXZ(FirstSegment.GetDirection(Id).XZ(), SecondSegment.GetDirection(Id).XZ());
        public bool IsStraight => IsMain && MainDot < -0.99f;
        public bool Is180 => IsMain && MainDot > 0.99f;
        public bool IsEqualWidth => IsMain && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;

        public float Offset
        {
            get => SegmentEnds.Values.Average(s => s.Offset);
            set
            {
                SetOffset(value);
                UpdateNode();
            }
        }
        public float Shift
        {
            get => SegmentEnds.Values.Average(s => s.Shift);
            set
            {
                SetShift(value);
                UpdateNode();
            }
        }
        public float RotateAngle
        {
            get => SegmentEnds.Values.Average(s => s.RotateAngle);
            set
            {
                SetRotate(value);
                UpdateNode();
            }
        }
        public float SlopeAngle
        {
            get => IsMain ? (FirstMainSegmentEnd.SlopeAngle - SecondMainSegmentEnd.SlopeAngle) / 2 : 0f;
            set
            {
                SetSlope(value);
                UpdateNode();
            }
        }
        public float TwistAngle
        {
            get => IsMain ? (FirstMainSegmentEnd.TwistAngle - SecondMainSegmentEnd.TwistAngle) / 2 : 0f;
            set
            {
                SetTwist(value);
                UpdateNode();
            }
        }
        public bool NoMarkings
        {
            get => SegmentEnds.Values.Any(s => s.NoMarkings);
            set
            {
                SetNoMarking(value);
                UpdateNode();
            }
        }
        public bool IsSlopeJunctions
        {
            get => MainRoad.Segments.Any(s => SegmentEnds[s].IsSlope);
            set
            {
                SetIsSlopeJunctions(value);
                UpdateNode();
            }
        }

        public bool IsCSUR => Info.IsCSUR();
        public bool IsRoad => Info.m_netAI is RoadBaseAI;

        public bool IsEndNode => Type == NodeStyleType.End;
        public bool IsMiddleNode => Type == NodeStyleType.Middle;
        public bool IsBendNode => Type == NodeStyleType.Bend;
        public bool IsJunctionNode => !IsMiddleNode && !IsBendNode && !IsEndNode;
        public bool IsMoveableNode => IsMiddleNode && Style.IsDefault;

        public bool IsMoveableEnds => Style.IsMoveable;

        public bool CanModifyTextures => IsRoad && !IsCSUR;
        public bool NeedsTransitionFlag => IsMain && (Type == NodeStyleType.Custom || Type == NodeStyleType.Crossing || Type == NodeStyleType.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => Type == NodeStyleType.Crossing && CrossingIsRemoved(MainRoad.First) && CrossingIsRemoved(MainRoad.Second);

        #endregion

        #region BASIC

        public NodeData(ushort nodeId, NodeStyleType? nodeType = null)
        {
            Id = nodeId;

            Update();

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

            SetType(nodeType != null && IsPossibleType(nodeType.Value) ? nodeType.Value : DefaultType);
        }
        public void Update()
        {
            UpdateSegmentEnds();
            UpdateMainRoad();
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
                    newSegmentsEnd.Add(segmentId, new SegmentEndData(segmentId, Id));
            }

            SegmentEnds = newSegmentsEnd;

            SegmentEndData.CalculateLimits(this);
        }
        private void UpdateMainRoad()
        {
            if (!ContainsSegment(MainRoad.First))
                MainRoad.First = FindMain(MainRoad.Second);
            if (!ContainsSegment(MainRoad.Second))
                MainRoad.Second = FindMain(MainRoad.First);
        }
        private ushort FindMain(ushort ignore)
        {
            var main = SegmentEnds.Values.Aggregate(default(SegmentEndData), (i, j) => Compare(i, j, ignore));
            return main?.Id ?? 0;
        }
        SegmentEndData Compare(SegmentEndData first, SegmentEndData second, ushort ignore)
        {
            if (first == null || first.Id == ignore)
                return second;
            else if (second == null || second.Id == ignore)
                return first;

            var firstInfo = first.Id.GetSegment().Info;
            var secondInfo = second.Id.GetSegment().Info;

            int result;

            if ((result = firstInfo.m_flatJunctions.CompareTo(secondInfo.m_flatJunctions)) == 0)
                if ((result = firstInfo.m_forwardVehicleLaneCount.CompareTo(secondInfo.m_forwardVehicleLaneCount)) == 0)
                    if ((result = firstInfo.m_halfWidth.CompareTo(secondInfo.m_halfWidth)) == 0)
                        result = ((firstInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false).CompareTo((secondInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false);

            if (result >= 0)
                return first;
            else
                return second;
        }
        public void UpdateNode() => Manager.Instance.Update(Id);
        public void ResetToDefault()
        {
            SetOffset(Style.DefaultOffset);
            SetShift(Style.DefaultShift);
            SetRotate(Style.DefaultRotate);
            SetSlope(Style.DefaultSlope);
            SetTwist(Style.DefaultTwist);
            SetNoMarking(Style.DefaultNoMarking);
            SetIsSlopeJunctions(Style.DefaultSlopeJunction);

            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault();

            UpdateNode();
        }

        private void SetType(NodeStyleType type)
        {
            NodeStyle newStyle = type switch
            {
                NodeStyleType.Middle => new MiddleNode(this),
                NodeStyleType.Bend => new BendNode(this),
                NodeStyleType.Stretch => new StretchNode(this),
                NodeStyleType.Crossing => new CrossingNode(this),
                NodeStyleType.UTurn => new UTurnNode(this),
                NodeStyleType.Custom => new CustomNode(this),
                NodeStyleType.End => new EndNode(this),
                _ => throw new NotImplementedException(),
            };
            Style = newStyle;

            if (!Style.SupportOffset)
                SetOffset(Style.DefaultOffset);
            if (!Style.SupportShift)
                SetShift(Style.DefaultShift);
            if (!Style.SupportRotate)
                SetRotate(Style.DefaultRotate);
            if (!Style.SupportSlope)
                SetSlope(Style.DefaultSlope);
            if (!Style.SupportTwist)
                SetTwist(Style.DefaultTwist);
            if (!Style.SupportNoMarking)
                SetNoMarking(Style.DefaultNoMarking);
            if (!Style.SupportSlopeJunction)
                SetIsSlopeJunctions(Style.DefaultSlopeJunction);

            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault();
        }
        private void SetOffset(float value)
        {
            foreach (var data in SegmentEnds.Values)
                data.Offset = value;
        }
        private void SetShift(float value)
        {
            foreach (var data in SegmentEnds.Values)
                data.Shift = value;
        }
        private void SetRotate(float value)
        {
            foreach (var data in SegmentEnds.Values)
                data.RotateAngle = value;
        }
        private void SetSlope(float value)
        {
            if (IsMain)
            {
                FirstMainSegmentEnd.SlopeAngle = value;
                SecondMainSegmentEnd.SlopeAngle = -value;
            }
        }
        private void SetTwist(float value)
        {
            if (IsMain)
            {
                FirstMainSegmentEnd.TwistAngle = value;
                SecondMainSegmentEnd.TwistAngle = -value;
            }
        }
        private void SetNoMarking(bool value)
        {
            foreach (var data in SegmentEnds.Values)
                data.NoMarkings = value;
        }
        private void SetIsSlopeJunctions(bool value)
        {
            foreach (var data in SegmentEnds.Values)
            {
                if (value)
                {
                    data.IsSlope = true;
                    data.IsTwist = false;
                }
                else
                {
                    var isMain = MainRoad.IsMain(data.Id);
                    data.IsSlope = !isMain;
                    data.IsTwist = !isMain;
                }
            }
        }

        #endregion

        #region UTILITIES

        public bool ContainsSegment(ushort segmentId) => SegmentEnds.ContainsKey(segmentId);
        public bool IsPossibleType(NodeStyleType newNodeType)
        {
            if (IsJunction || IsCSUR)
                return newNodeType == NodeStyleType.Custom;

            bool middle = DefaultFlags.IsFlagSet(NetNode.Flags.Middle);
            return newNodeType switch
            {
                NodeStyleType.Crossing => IsEqualWidth && IsStraight && PedestrianLaneCount >= 2,
                NodeStyleType.UTurn => IsMain && IsRoad && Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0,
                NodeStyleType.Stretch => CanModifyTextures && !middle && IsStraight,
                NodeStyleType.Bend => !middle,
                NodeStyleType.Middle => IsStraight || Is180,
                NodeStyleType.Custom => true,
                NodeStyleType.End => IsEnd,
                _ => throw new Exception("Unreachable code"),
            };
        }
        bool CrossingIsRemoved(ushort segmentId) => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(Id, segmentId);

        public override string ToString() => $"NodeData(id:{Id} type:{Type})";

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
            typeProperty.Text = "Node type";
            typeProperty.Init(IsPossibleType);
            typeProperty.SelectedObject = Type;
            typeProperty.OnSelectObjectChanged += (value) =>
            {
                Type = value;
                refresh();
            };

            return typeProperty;
        }

        //public string ToolTip(NodeTypeT nodeType) => nodeType switch
        //{
        //    NodeTypeT.Crossing => "Crossing node.",
        //    NodeTypeT.Middle => "Middle: No node.",
        //    NodeTypeT.Bend => IsAsymRevert ? "Bend: Asymmetrical road changes direction." : (HalfWidthDelta > 0.05f ? "Bend: Linearly match segment widths." : "Bend: Simple road corner."),
        //    NodeTypeT.Stretch => "Stretch: Match both pavement and road.",
        //    NodeTypeT.UTurn => "U-Turn: node with enough space for U-Turn.",
        //    NodeTypeT.Custom => "Custom: transition size and traffic rules are configrable.",
        //    NodeTypeT.End => "when there is only one segment at the node.",
        //    _ => null,
        //};

        #endregion
    }
    public class MainRoad
    {
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
    }

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeStyleType, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 100f;
        protected override bool IsEqual(NodeStyleType first, NodeStyleType second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeStyleType> { }
        protected override string GetDescription(NodeStyleType value) => value.ToString();
    }
}
