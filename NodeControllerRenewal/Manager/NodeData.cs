using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using static ColossalFramework.Math.VectorUtils;
using TrafficManager.API.Traffic.Enums;
using KianCommons;
using static KianCommons.ReflectionHelpers;
using KianCommons.Serialization;

using System.Diagnostics;
using System.Linq;
using ModsCommon.Utilities;
using KianCommons.Math;
using ModsCommon;
using ModsCommon.UI;
using ColossalFramework.UI;
using System.Collections;

namespace NodeController
{
    [Serializable]
    public class NodeData : INetworkData
    {
        #region PROPERTIES

        public string Title => $"Node #{Id}";
        private int UpdateProcess { get; set; } = 0;
        private bool InUpdate => UpdateProcess != 0;

        public ushort Id { get; set; }
        public NetNode Node => Id.GetNode();
        public NetInfo Info => Node.Info;
        private NodeStyle Style { get; set; }
        public NodeStyleType Type
        {
            get => Style.Type;
            set
            {
                NodeStyle newType = value switch
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
                Style = newType;
                Refresh();
            }
        }
        private Dictionary<ushort, SegmentEndData> SegmentEnds { get; set; } = new Dictionary<ushort, SegmentEndData>();
        public IEnumerable<SegmentEndData> SegmentEndDatas => SegmentEnds.Values;
        public SegmentEndData this[ushort segmentId] => SegmentEnds.TryGetValue(segmentId, out var data) ? data : null;
        private MainRoad MainRoad { get; set; } = new MainRoad();

        public bool IsDefault => Type == DefaultType && !SegmentEndDatas.Any(s => s?.IsDefault != true);

        public NetNode.Flags DefaultFlags { get; set; }
        public NodeStyleType DefaultType { get; set; }

        public bool IsEnd => SegmentEnds.Count == 1;
        public bool IsMain => SegmentEnds.Count == 2;
        public bool IsJunction => SegmentEnds.Count > 2;
        public IEnumerable<ushort> SegmentIds => SegmentEnds.Keys;
        public int SegmentCount => SegmentEnds.Count;

        NetSegment FirstSegment => MainRoad.First.GetSegment();
        NetSegment SecondSegment => MainRoad.Second.GetSegment();
        public SegmentEndData FirstMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.First, out var data) ? data : null;
        public SegmentEndData SecondMainSegmentEnd => SegmentEnds.TryGetValue(MainRoad.Second, out var data) ? data : null;

        public bool HasPedestrianLanes => SegmentEnds.Keys.Any(s => s.GetSegment().Info.m_hasPedestrianLanes);
        private int PedestrianLaneCount => SegmentEnds.Keys.Max(s => s.GetSegment().Info.CountPedestrianLanes());
        private float MainDot => DotXZ(FirstSegment.GetDirection(Id).XZ(), SecondSegment.GetDirection(Id).XZ());
        public bool IsStraight => IsMain && MainDot < -0.99f;
        public bool Is180 => IsMain && MainDot > 0.99f;
        public bool IsEqualWidth => IsMain && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;

        public bool IsFlatJunctions
        {
            get => MainRoad.Segments.All(s => SegmentEnds[s].IsFlat);
            set
            {
                foreach (var data in SegmentEnds.Values)
                {
                    if (value)
                    {
                        data.IsFlat = true;
                        data.Twist = false;
                    }
                    else
                    {
                        var isMain = MainRoad.IsMain(data.Id);
                        data.IsFlat = !isMain;
                        data.Twist = !isMain;
                    }
                }

                UpdateNode();
            }
        }
        public float Offset
        {
            get => SegmentEnds.Values.Average(s => s.Offset);
            set
            {
                foreach (var data in SegmentEnds.Values)
                    data.Offset = value;

                UpdateNode();
            }
        }
        public float Shift
        {
            get => SegmentEnds.Values.Average(s => s.Shift);
            set
            {
                foreach (var data in SegmentEnds.Values)
                    data.Shift = value;

                UpdateNode();
            }
        }
        public float RotateAngle
        {
            get => SegmentEnds.Values.Average(s => s.RotateAngle);
            set
            {
                foreach (var data in SegmentEnds.Values)
                    data.RotateAngle = value;

                UpdateNode();
            }
        }
        public float SlopeAngle
        {
            get => IsMain ? (FirstMainSegmentEnd.SlopeAngle - SecondMainSegmentEnd.SlopeAngle) / 2 : 0f;
            set
            {
                if (IsMain)
                {
                    FirstMainSegmentEnd.SlopeAngle = value;
                    SecondMainSegmentEnd.SlopeAngle = -value;
                    UpdateNode();
                }
            }
        }
        public float TwistAngle
        {
            get => IsMain ? (FirstMainSegmentEnd.TwistAngle - SecondMainSegmentEnd.TwistAngle) / 2 : 0f;
            set
            {
                if (IsMain)
                {
                    FirstMainSegmentEnd.TwistAngle = value;
                    SecondMainSegmentEnd.TwistAngle = -value;
                    UpdateNode();
                }
            }
        }

        public bool NoMarkings
        {
            get => SegmentEnds.Values.Any(s => s.NoMarkings);
            set
            {
                foreach (var data in SegmentEnds.Values)
                    data.NoMarkings = value;

                UpdateNode();
            }
        }
        public float Stretch
        {
            get => (FirstMainSegmentEnd.Stretch + SecondMainSegmentEnd.Stretch) / 2;
            set
            {
                FirstMainSegmentEnd.Stretch = value;
                SecondMainSegmentEnd.Stretch = value;
                UpdateNode();
            }
        }

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public bool IsRoad => Info.m_netAI is RoadBaseAI;

        public bool IsEndNode => Type == NodeStyleType.End;
        public bool IsMiddleNode => Type == NodeStyleType.Middle;
        public bool IsBendNode => Type == NodeStyleType.Bend;
        public bool IsJunctionNode => !IsMiddleNode && !IsBendNode && !IsEndNode;
        public bool IsMoveableNode => IsMiddleNode && Style.IsDefault;


        public bool WantsTrafficLight => Type == NodeStyleType.Crossing;
        public bool CanModifyOffset => Type == NodeStyleType.Bend || Type == NodeStyleType.Stretch || Type == NodeStyleType.Custom;
        public bool CanMassEditNodeCorners => IsMain;
        public bool CanModifyFlatJunctions => !IsMiddleNode;
        public bool IsAsymRevert => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);
        public bool CanModifyTextures => IsRoad && !IsCSUR;
        public bool ShowNoMarkingsToggle => CanModifyTextures && Type == NodeStyleType.Custom;
        public bool NeedsTransitionFlag => IsMain && (Type == NodeStyleType.Custom || Type == NodeStyleType.Crossing || Type == NodeStyleType.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => Type == NodeStyleType.Crossing && CrossingIsRemoved(MainRoad.First) && CrossingIsRemoved(MainRoad.Second);


        public bool? IsUturnAllowedConfigurable => Type switch
        {
            NodeStyleType.Crossing or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,// always off
            NodeStyleType.UTurn or NodeStyleType.Custom or NodeStyleType.End => null,// default
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultUturnAllowed => Type switch
        {
            NodeStyleType.UTurn => true,
            NodeStyleType.Crossing or NodeStyleType.Stretch => false,
            NodeStyleType.Middle or NodeStyleType.Bend or NodeStyleType.Custom or NodeStyleType.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsPedestrianCrossingAllowedConfigurable => Type switch
        {
            NodeStyleType.Crossing or NodeStyleType.UTurn or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            NodeStyleType.Custom => (IsMain && !HasPedestrianLanes) ? false : null,
            NodeStyleType.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultPedestrianCrossingAllowed => Type switch
        {
            NodeStyleType.Crossing => true,
            NodeStyleType.UTurn or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            NodeStyleType.Custom when IsMain && FirstSegment.Info.m_netAI.GetType() != SecondSegment.Info.m_netAI.GetType() => false,
            NodeStyleType.Custom or NodeStyleType.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? CanHaveTrafficLights(out ToggleTrafficLightError reason)
        {
            reason = ToggleTrafficLightError.None;
            switch (Type)
            {
                case NodeStyleType.Crossing:
                case NodeStyleType.UTurn:
                case NodeStyleType.End:
                case NodeStyleType.Custom:
                    return null;
                case NodeStyleType.Stretch:
                case NodeStyleType.Middle:
                case NodeStyleType.Bend:
                    reason = ToggleTrafficLightError.NoJunction;
                    return false;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        public bool? IsEnteringBlockedJunctionAllowedConfigurable => Type switch
        {
            NodeStyleType.Custom when IsJunction => null,
            NodeStyleType.Custom when DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut) && !HasPedestrianLanes => false,//
            NodeStyleType.Crossing or NodeStyleType.UTurn or NodeStyleType.Custom or NodeStyleType.End => null,// default off
            NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,// always on
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultEnteringBlockedJunctionAllowed => Type switch
        {
            NodeStyleType.Stretch => true,// always on
            NodeStyleType.Crossing => false,// default off
            NodeStyleType.UTurn or NodeStyleType.Middle or NodeStyleType.Bend or NodeStyleType.End => null,// default
            NodeStyleType.Custom => IsJunction ? null : true,
            _ => throw new Exception("Unreachable code"),
        };

        #endregion

        #region BASIC

        public NodeData(ushort nodeId, NodeStyleType? nodeType = null)
        {
            Id = nodeId;
            Init(nodeType);
            Update();
        }
        private void Init(NodeStyleType? nodeType)
        {
            StartUpdate();

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

            Type = nodeType != null && IsPossibleType(nodeType.Value) ? nodeType.Value : DefaultType;

            StopUpdate();
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
        public void UpdateNode()
        {
            if (!InUpdate)
                Manager.Instance.Update(Id);
        }
        public void Refresh()
        {
            ResetToDefault();
            UpdateNode();
        }
        private void ResetToDefault()
        {
            StartUpdate();

            if (Style.ResetOffset)
                Offset = Style.DefaultOffset;
            if (Style.ResetShift)
                Shift = Style.DefaultShift;
            if (Style.ResetRotate)
                RotateAngle = Style.DefaultRotate;
            if (Style.ResetSlope)
                SlopeAngle = Style.DefaultSlope;
            if (Style.ResetTwist)
                TwistAngle = Style.DefaultTwist;
            if (Style.ResetNoMarking)
                NoMarkings = Style.DefaultNoMarking;
            if (Style.ResetFlatJunction)
                IsFlatJunctions = Style.DefaultFlatJunction;

            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault();

            StopUpdate();
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
        public static bool IsSupported(ushort nodeId)
        {
            var node = nodeId.GetNode();
            if (!node.IsValid())
                return false;

            var segmentIds = node.SegmentIds().ToArray();
            if (segmentIds.Any(id => !id.GetSegment().IsValid()))
                return false;

            if (!node.m_flags.CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.LevelCrossing | NetNode.Flags.Outside | NetNode.Flags.Deleted))
                return false;

            if (segmentIds.Length != 2)
                return true;

            return !NetUtil.IsCSUR(node.Info);
        }
        bool CrossingIsRemoved(ushort segmentId) => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(Id, segmentId);
        private void StartUpdate() => UpdateProcess += 1;
        private void StopUpdate() => UpdateProcess = Math.Max(UpdateProcess - 1, 0);

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
