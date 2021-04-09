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

namespace NodeController
{
    [Serializable]
    public class NodeData : ISerializable, INetworkData, INetworkData<NodeData>
    {
        #region PROPERTIES

        public string Title => $"Node #{NodeId}";

        public ushort NodeId { get; set; }
        public NetNode Node => NodeId.GetNode();
        public NetInfo Info => Node.Info;
        private NodeType Type { get; set; }
        public NodeTypeT NodeType
        {
            get => Type.Type;
            set
            {
                NodeType newType = value switch
                {
                    NodeTypeT.Middle => new MiddleNode(this),
                    NodeTypeT.Bend => new BendNode(this),
                    NodeTypeT.Stretch => new StretchNode(this),
                    NodeTypeT.Crossing => new CrossingNode(this),
                    NodeTypeT.UTurn => new UTurnNode(this),
                    NodeTypeT.Custom => new CustomNode(this),
                    NodeTypeT.End => new EndNode(this),
                    _ => throw new NotImplementedException(),
                };
                Type = newType;
                Update();
            }
        }
        public IEnumerable<SegmentEndData> SegmentEndDatas => Node.SegmentIds().Select(s => SegmentEndManager.Instance[s, NodeId]);
        public bool IsDefault => NodeType == DefaultNodeType && !SegmentEndDatas.Any(s => s?.IsDefault != true);

        public NetNode.Flags DefaultFlags { get; set; }
        public NodeTypeT DefaultNodeType { get; set; }

        private List<ushort> SegmentIdsList { get; set; }
        public bool IsEnd => SegmentIdsList.Count == 1;
        public bool IsMain => SegmentIdsList.Count == 2;
        public bool IsJunction => SegmentIdsList.Count > 2;
        public IEnumerable<ushort> SegmentIds => SegmentIdsList;
        public int SegmentCount => SegmentIdsList.Count;

        ushort FirstSegmentId => IsMain ? SegmentIdsList[0] : 0;
        ushort SecondSegmentId => IsMain ? SegmentIdsList[1] : 0;
        NetSegment FirstSegment => FirstSegmentId.GetSegment();
        NetSegment SecondSegment => SecondSegmentId.GetSegment();
        public SegmentEndData FirstSegmentEnd => SegmentEndManager.Instance[FirstSegmentId, NodeId];
        public SegmentEndData SecondSegmentEnd => SegmentEndManager.Instance[SecondSegmentId, NodeId];

        public bool HasPedestrianLanes => SegmentIdsList.Any(s => s.GetSegment().Info.m_hasPedestrianLanes);
        private int PedestrianLaneCount => SegmentIdsList.Max(s => s.GetSegment().Info.CountPedestrianLanes());
        private float MainDot => DotXZ(FirstSegment.GetDirection(NodeId).XZ(), SecondSegment.GetDirection(NodeId).XZ());
        public bool IsStraight => IsMain && MainDot < -0.99f;
        public bool Is180 => IsMain && MainDot > 0.99f;
        public bool IsEqualWidth => IsMain && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;

        public bool FirstTimeTrafficLight { get; set; }

        public bool IsFlatJunctions
        {
            set
            {
                var count = 0;
                foreach (ushort segmentId in SegmentIdsList)
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    if (value)
                    {
                        segmentEnd.FlatJunctions = true;
                        segmentEnd.Twist = false;
                    }
                    else
                    {
                        segmentEnd.FlatJunctions = count >= 2;
                        segmentEnd.Twist = count >= 2;
                    }
                    count += 1;
                }
                Update();
            }
        }
        public float Offset
        {
            get => SegmentIdsList.Average(s => SegmentEndManager.Instance[s, NodeId, true].Offset);
            set
            {
                foreach (var segmentId in SegmentIdsList)
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segmentEnd.Offset = value;
                }
                Update();
            }
        }
        public float Shift
        {
            get => SegmentIdsList.Average(s => SegmentEndManager.Instance[s, NodeId, true].Shift);
            set
            {
                foreach (var segmentId in SegmentIdsList)
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segmentEnd.Shift = value;
                }
                Update();
            }
        }
        public float RotateAngle
        {
            get => SegmentIdsList.Average(s => SegmentEndManager.Instance[s, NodeId, true].RotateAngle);
            set
            {
                foreach (var segmentId in SegmentIdsList)
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segmentEnd.RotateAngle = value;
                }
                Update();
            }
        }
        public float SlopeAngle
        {
            get => IsMain ? (FirstSegmentEnd.SlopeAngle - SecondSegmentEnd.SlopeAngle) / 2 : 0f;
            set
            {
                if (IsMain)
                {
                    FirstSegmentEnd.SlopeAngle = value;
                    SecondSegmentEnd.SlopeAngle = -value;
                    Update();
                }
            }
        }
        public float TwistAngle
        {
            get => IsMain ? (FirstSegmentEnd.TwistAngle - SecondSegmentEnd.TwistAngle) / 2 : 0f;
            set
            {
                if (IsMain)
                {
                    FirstSegmentEnd.TwistAngle = value;
                    SecondSegmentEnd.TwistAngle = -value;
                    Update();
                }
            }
        }

        public bool NoMarkings
        {
            get => Node.SegmentIds().Any(s => SegmentEndManager.Instance[s, NodeId, true].NoMarkings);
            set
            {
                foreach (var segmentId in Node.SegmentIds())
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segmentEnd.NoMarkings = value;
                }
                Update();
            }
        }
        public float Stretch
        {
            get => (FirstSegmentEnd.Stretch + SecondSegmentEnd.Stretch) / 2;
            set
            {
                FirstSegmentEnd.Stretch = value;
                SecondSegmentEnd.Stretch = value;
                Update();
            }
        }

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public bool IsRoad => Info.m_netAI is RoadBaseAI;

        public bool IsEndNode => NodeType == NodeTypeT.End;
        public bool IsMiddleNode => NodeType == NodeTypeT.Middle;
        public bool IsBendNode => NodeType == NodeTypeT.Bend;
        public bool IsJunctionNode => !IsMiddleNode && !IsBendNode && !IsEndNode;

        public bool WantsTrafficLight => NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset => NodeType == NodeTypeT.Bend || NodeType == NodeTypeT.Stretch || NodeType == NodeTypeT.Custom;
        public bool CanMassEditNodeCorners => IsMain;
        public bool CanModifyFlatJunctions => !IsMiddleNode;
        public bool IsAsymRevert => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);
        public bool CanModifyTextures => IsRoad && !IsCSUR;
        public bool ShowNoMarkingsToggle => CanModifyTextures && NodeType == NodeTypeT.Custom;
        public bool NeedsTransitionFlag => IsMain && (NodeType == NodeTypeT.Custom || NodeType == NodeTypeT.Crossing || NodeType == NodeTypeT.UTurn);
        public bool ShouldRenderCenteralCrossingTexture => NodeType == NodeTypeT.Crossing && CrossingIsRemoved(FirstSegmentId) && CrossingIsRemoved(SecondSegmentId);


        public bool? IsUturnAllowedConfigurable => NodeType switch
        {
            NodeTypeT.Crossing or NodeTypeT.Stretch or NodeTypeT.Middle or NodeTypeT.Bend => false,// always off
            NodeTypeT.UTurn or NodeTypeT.Custom or NodeTypeT.End => null,// default
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultUturnAllowed => NodeType switch
        {
            NodeTypeT.UTurn => true,
            NodeTypeT.Crossing or NodeTypeT.Stretch => false,
            NodeTypeT.Middle or NodeTypeT.Bend or NodeTypeT.Custom or NodeTypeT.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsPedestrianCrossingAllowedConfigurable => NodeType switch
        {
            NodeTypeT.Crossing or NodeTypeT.UTurn or NodeTypeT.Stretch or NodeTypeT.Middle or NodeTypeT.Bend => false,
            NodeTypeT.Custom => (IsMain && !HasPedestrianLanes) ? false : null,
            NodeTypeT.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultPedestrianCrossingAllowed => NodeType switch
        {
            NodeTypeT.Crossing => true,
            NodeTypeT.UTurn or NodeTypeT.Stretch or NodeTypeT.Middle or NodeTypeT.Bend => false,
            NodeTypeT.Custom when IsMain && FirstSegment.Info.m_netAI.GetType() != SecondSegment.Info.m_netAI.GetType() => false,
            NodeTypeT.Custom or NodeTypeT.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? CanHaveTrafficLights(out ToggleTrafficLightError reason)
        {
            reason = ToggleTrafficLightError.None;
            switch (NodeType)
            {
                case NodeTypeT.Crossing:
                case NodeTypeT.UTurn:
                case NodeTypeT.End:
                case NodeTypeT.Custom:
                    return null;
                case NodeTypeT.Stretch:
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    reason = ToggleTrafficLightError.NoJunction;
                    return false;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        public bool? IsEnteringBlockedJunctionAllowedConfigurable => NodeType switch
        {
            NodeTypeT.Custom when IsJunction => null,
            NodeTypeT.Custom when DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut) && !HasPedestrianLanes => false,//
            NodeTypeT.Crossing or NodeTypeT.UTurn or NodeTypeT.Custom or NodeTypeT.End => null,// default off
            NodeTypeT.Stretch or NodeTypeT.Middle or NodeTypeT.Bend => false,// always on
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultEnteringBlockedJunctionAllowed => NodeType switch
        {
            NodeTypeT.Stretch => true,// always on
            NodeTypeT.Crossing => false,// default off
            NodeTypeT.UTurn or NodeTypeT.Middle or NodeTypeT.Bend or NodeTypeT.End => null,// default
            NodeTypeT.Custom => IsJunction ? null : true,
            _ => throw new Exception("Unreachable code"),
        };

        #endregion

        #region BASIC

        public NodeData() { }
        public NodeData(ushort nodeId)
        {
            NodeId = nodeId;
            Calculate();
            NodeType = DefaultNodeType;
            FirstTimeTrafficLight = false;
            Update();
        }

        public NodeData(ushort nodeId, NodeTypeT nodeType) : this(nodeId)
        {
            NodeType = nodeType;
            FirstTimeTrafficLight = nodeType == NodeTypeT.Crossing;
        }
        private NodeData(NodeData template) => CopyProperties(this, template);
        public NodeData(SerializationInfo info, StreamingContext context)
        {
            SerializationUtil.SetObjectFields(info, this);

            if (NodeManager.TargetNodeId != 0)
                NodeId = NodeManager.TargetNodeId;

            SerializationUtil.SetObjectProperties(info, this);
            Update();
        }
        public NodeData Clone() => new NodeData(this);
        public void GetObjectData(SerializationInfo info, StreamingContext context) => SerializationUtil.GetObjectFields(info, this);

        public void Calculate()
        {
            var node = Node;
            DefaultFlags = node.m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultNodeType = NodeTypeT.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Bend))
                DefaultNodeType = NodeTypeT.Bend;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultNodeType = NodeTypeT.Custom;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.End))
                DefaultNodeType = NodeTypeT.End;
            else
                throw new NotImplementedException($"Unsupported node flags: {DefaultFlags}");

            NodeType = DefaultNodeType;

            SegmentIdsList = node.SegmentIds().ToList();
            SegmentIdsList.Sort(CompareSegments);
            SegmentIdsList.Reverse();

            Refresh();
        }
        public void RefreshAndUpdate()
        {
            Refresh();
            Update();
        }
        private void Refresh()
        {
            if (NodeType != NodeTypeT.Custom)
                NoMarkings = false;

            if (!CanModifyOffset)
            {
                if (NodeType == NodeTypeT.UTurn)
                    Offset = 8f;
                else if (NodeType == NodeTypeT.Crossing)
                    Offset = 0f;
            }
        }

        public void Update() => NetManager.instance.UpdateNode(NodeId);
        public void ResetToDefault()
        {
            foreach (var segmentEnd in SegmentEndDatas)
                segmentEnd.ResetToDefault();

            Update();
        }

        #endregion

        #region UTILITIES

        static int CompareSegments(ushort firstSegmentId, ushort secondSegmentId)
        {
            var firstInfo = firstSegmentId.GetSegment().Info;
            var secondInfo = secondSegmentId.GetSegment().Info;

            int result;

            if ((result = firstInfo.m_flatJunctions.CompareTo(secondInfo.m_flatJunctions)) == 0)
                if ((result = firstInfo.m_forwardVehicleLaneCount.CompareTo(secondInfo.m_forwardVehicleLaneCount)) == 0)
                    if ((result = firstInfo.m_halfWidth.CompareTo(secondInfo.m_halfWidth)) == 0)
                        result = ((firstInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false).CompareTo((secondInfo.m_netAI as RoadBaseAI)?.m_highwayRules ?? false);

            return result;
        }
        public bool CanChangeTo(NodeTypeT newNodeType)
        {
            if (newNodeType == NodeType)
                return true;

            if (IsJunction || IsCSUR)
                return newNodeType == NodeTypeT.Custom;

            bool middle = DefaultFlags.IsFlagSet(NetNode.Flags.Middle);
            return newNodeType switch
            {
                NodeTypeT.Crossing => IsEqualWidth && IsStraight && PedestrianLaneCount >= 2,
                NodeTypeT.UTurn => IsMain && IsRoad && Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0,
                NodeTypeT.Stretch => CanModifyTextures && !middle && IsStraight,
                NodeTypeT.Bend => !middle,
                NodeTypeT.Middle => IsStraight || Is180,
                NodeTypeT.Custom => true,
                NodeTypeT.End => IsEnd,
                _ => throw new Exception("Unreachable code"),
            };
        }
        public static bool IsSupported(ushort nodeId)
        {
            var node = nodeId.GetNode();
            if (!node.IsValid())
                return false;

            var segmentIds = node.SegmentIds().ToArray();
            if(segmentIds.Any(id => !id.GetSegment().IsValid()))
                    return false;

            if (!node.m_flags.CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.LevelCrossing | NetNode.Flags.Outside | NetNode.Flags.Deleted))
                return false;

            if (segmentIds.Length != 2)
                return true;

            return !NetUtil.IsCSUR(node.Info);
        }

        bool CrossingIsRemoved(ushort segmentId) => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(NodeId, segmentId);

        public override string ToString() => $"NodeData(id:{NodeId} type:{NodeType})";

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetNodeTypeProperty(parent, refresh);
            Type.GetUIComponents(parent, refresh);
        }
       
        private NodeTypePropertyPanel GetNodeTypeProperty(UIComponent parent, Action refresh)
        {
            var typeProperty = ComponentPool.Get<NodeTypePropertyPanel>(parent);
            typeProperty.Text = "Node type";
            typeProperty.Init(CanChangeTo);
            typeProperty.SelectedObject = NodeType;
            typeProperty.OnSelectObjectChanged += (value) =>
            {
                NodeType = value;
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

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeTypeT, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 100f;
        protected override bool IsEqual(NodeTypeT first, NodeTypeT second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeTypeT> { }
        protected override string GetDescription(NodeTypeT value) => value.ToString();
    }
}
