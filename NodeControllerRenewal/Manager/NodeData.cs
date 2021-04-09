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
    public enum NodeTypeT
    {
        Middle,
        Bend,
        Stretch,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
        Custom,
        End,
    }

    [Serializable]
    public class NodeData : ISerializable, INetworkData, INetworkData<NodeData>
    {
        #region PROPERTIES

        public string Title => $"Node #{NodeId}";

        public ushort NodeId { get; set; }
        public NetNode Node => NodeId.GetNode();
        public NetInfo Info => Node.Info;
        public NodeTypeT NodeType { get; set; }
        public IEnumerable<SegmentEndData> SegmentEndDatas => Node.SegmentsId().Select(s => SegmentEndManager.Instance[s, NodeId]);
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
        public bool IsStraight => IsMain && MainDot < -0.999f;
        public bool Is180 => IsMain && MainDot > 0.999f;
        public bool IsEqualWidth => IsMain && Math.Abs(FirstSegment.Info.m_halfWidth - SecondSegment.Info.m_halfWidth) < 0.001f;

        public bool FirstTimeTrafficLight { get; set; }

        public bool IsFlatJunctions
        {
            set
            {
                var count = 0;
                foreach (ushort segmentId in SegmentIdsList)
                {
                    var segEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    if (value)
                    {
                        segEnd.FlatJunctions = true;
                        segEnd.Twist = false;
                    }
                    else
                    {
                        segEnd.FlatJunctions = count >= 2;
                        segEnd.Twist = count >= 2;
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
                    var segEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segEnd.Offset = value;
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
                    var segEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segEnd.Shift = value;
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
                    var segEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segEnd.RotateAngle = value;
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
            get => Node.SegmentsId().Any(s => SegmentEndManager.Instance[s, NodeId, true].NoMarkings);
            set
            {
                foreach (var segmentId in Node.SegmentsId())
                {
                    var segEnd = SegmentEndManager.Instance[segmentId, NodeId, true];
                    segEnd.NoMarkings = value;
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
                NodeType = DefaultNodeType = NodeTypeT.End;
            else
                throw new NotImplementedException($"Unsupported node flags: {DefaultFlags}");

            SegmentIdsList = node.SegmentsId().ToList();
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
            NodeType = DefaultNodeType;

            foreach (var segEnd in SegmentEndDatas)
                segEnd.ResetToDefault();

            Update();
        }

        #endregion

        #region UTILITIES

        bool IsUniform<T>(Func<SegmentEndData, T> predicate)
        {
            var count = Node.SegmentsId().Select(s => predicate(SegmentEndManager.Instance[s, NodeId, true])).Distinct().Count();
            return count <= 1;
        }
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
            if (IsEnd)
                return newNodeType == NodeTypeT.End;

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
                NodeTypeT.End => false,
                _ => throw new Exception("Unreachable code"),
            };
        }
        public static bool IsSupported(ushort nodeId)
        {
            if (!NetUtil.IsNodeValid(nodeId))
                return false;

            var node = nodeId.GetNode();
            var segmentIds = node.SegmentsId().ToArray();
            foreach (ushort segmentId in segmentIds)
            {
                if (!NetUtil.IsSegmentValid(segmentId))
                    return false;
            }

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

        public List<EditorItem> GetUIComponents(UIComponent parent, Action refresh)
        {
            var properties = new List<EditorItem>();

            properties.Add(GetNodeTypeProperty(parent, refresh));
            if (CanModifyFlatJunctions)
                properties.Add(GetActionButtons(parent));

            if (!IsMiddleNode)
            {
                GetUIComponents(properties, parent, GetOffsetProperty, GetSegmentOffsetProperty, (data) => data.Offset, (data, value) => data.Offset = value);
                GetUIComponents(properties, parent, GetShiftProperty, GetSegmentShiftProperty, (data) => data.Shift, (data, value) => data.Shift = value);
                GetUIComponents(properties, parent, GetRotateProperty, GetSegmentRotateProperty, (data) => data.RotateAngle, (data, value) => data.RotateAngle = value);
            }
            if (IsMiddleNode)
            {
                GetUIComponents(properties, parent, GetSlopeProperty, null, (data) => data.SlopeAngle, (data, value) => data.SlopeAngle = value);
                GetUIComponents(properties, parent, GetTwistProperty, null, (data) => data.TwistAngle, (data, value) => data.TwistAngle = value);
            }

            properties.Add(GetHideMarkingProperty(parent));

            return properties;

        }
        private void GetUIComponents(List<EditorItem> properties, UIComponent parent, Func<UIComponent, FloatPropertyPanel> getNodeProperty, Func<UIComponent, SegmentEndData, FloatPropertyPanel> getSegmentProperty, Func<INetworkData, float> getValue, Action<INetworkData, float> setValue)
        {
            var nodeProperty = getNodeProperty(parent);
            nodeProperty.Value = getValue(this);
            properties.Add(nodeProperty);

            var segmentProperties = new List<FloatPropertyPanel>();
            if (getSegmentProperty != null)
            {
                foreach (var segmentId in Node.SegmentsId())
                {
                    var segmentData = SegmentEndManager.Instance[segmentId, NodeId, true];
                    var segmentProperty = getSegmentProperty(parent, segmentData);
                    segmentProperty.Value = getValue(segmentData);
                    segmentProperty.OnValueChanged += (newValue) =>
                        {
                            var segmentData = SegmentEndManager.Instance[segmentId, NodeId, true];
                            setValue(segmentData, newValue);
                            nodeProperty.Value = getValue(this);
                            Update();
                        };
                    segmentProperties.Add(segmentProperty);
                    properties.Add(segmentProperty);
                }
            }

            nodeProperty.OnValueChanged += (float newValue) =>
            {
                setValue(this, newValue);
                foreach (var segmentProperty in segmentProperties)
                    segmentProperty.Value = newValue;
            };
        }

        private FloatPropertyPanel GetOffsetProperty(UIComponent parent)
        {
            var offsetProperty = GetNodeProperty(parent, "Offset");
            offsetProperty.MinValue = 0;
            offsetProperty.MaxValue = 100;

            return offsetProperty;
        }
        private FloatPropertyPanel GetShiftProperty(UIComponent parent)
        {
            var offsetProperty = GetNodeProperty(parent, "Shift");
            offsetProperty.MinValue = -32;
            offsetProperty.MaxValue = 32;

            return offsetProperty;
        }
        private FloatPropertyPanel GetRotateProperty(UIComponent parent)
        {
            var rotateProperty = GetNodeProperty(parent, "Rotate");
            rotateProperty.MinValue = -60;
            rotateProperty.MaxValue = 60;

            return rotateProperty;
        }
        private FloatPropertyPanel GetSlopeProperty(UIComponent parent)
        {
            var slopeProperty = GetNodeProperty(parent, "Slope");
            slopeProperty.MinValue = -60;
            slopeProperty.MaxValue = 60;

            return slopeProperty;
        }
        private FloatPropertyPanel GetTwistProperty(UIComponent parent)
        {
            var twistProperty = GetNodeProperty(parent, "Twist");
            twistProperty.MinValue = -60;
            twistProperty.MaxValue = 60;

            return twistProperty;
        }

        private FloatPropertyPanel GetNodeProperty(UIComponent parent, string name)
        {
            var property = ComponentPool.Get<FloatPropertyPanel>(parent, name);
            property.Text = name;
            property.CheckMin = true;
            property.CheckMax = true;
            property.UseWheel = true;
            property.WheelStep = 1f;
            property.Init();

            return property;
        }

        private FloatPropertyPanel GetSegmentOffsetProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} offset");
            offsetProperty.MinValue = 0;
            offsetProperty.MaxValue = 100;

            return offsetProperty;
        }
        private FloatPropertyPanel GetSegmentShiftProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} shift");
            offsetProperty.MinValue = -32;
            offsetProperty.MaxValue = 32;

            return offsetProperty;
        }
        private FloatPropertyPanel GetSegmentRotateProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var rotateProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} rotate");
            rotateProperty.MinValue = -60;
            rotateProperty.MaxValue = 60;

            return rotateProperty;
        }
        private FloatPropertyPanel GetSegmentSlopeProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var slopeProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} slope");
            slopeProperty.MinValue = -60;
            slopeProperty.MaxValue = 60;

            return slopeProperty;
        }
        private FloatPropertyPanel GetSegmentTwistProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var twistProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} twist");
            twistProperty.MinValue = -60;
            twistProperty.MaxValue = 60;

            return twistProperty;
        }
        private FloatPropertyPanel GetSegmentProperty(UIComponent parent, string name)
        {
            var property = ComponentPool.Get<FloatPropertyPanel>(parent, name);
            property.Text = name;
            property.CheckMin = true;
            property.CheckMax = true;
            property.UseWheel = true;
            property.WheelStep = 1f;
            property.Init();

            return property;
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
                Update();
                refresh();
            };

            return typeProperty;
        }
        private ButtonsPanel GetActionButtons(UIComponent parent)
        {
            var actionButtons = ComponentPool.Get<ButtonsPanel>(parent);
            var slopeIndex = actionButtons.AddButton("Make slope");
            var flatIndex = actionButtons.AddButton("Make flat");
            actionButtons.Init();
            actionButtons.OnButtonClick += OnButtonClick;

            return actionButtons;

            void OnButtonClick(int index)
            {
                if (index == slopeIndex)
                    IsFlatJunctions = false;
                else if (index == flatIndex)
                    IsFlatJunctions = true;
            }
        }
        private BoolListPropertyPanel GetHideMarkingProperty(UIComponent parent)
        {
            var hideMarkingProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            hideMarkingProperty.Text = "Hide crosswalk marking";
            hideMarkingProperty.Init("No", "Yes");
            hideMarkingProperty.SelectedObject = NoMarkings;
            hideMarkingProperty.OnSelectObjectChanged += (value) => NoMarkings = value;

            return hideMarkingProperty;
        }

        //private ButtonPanel GetResetButton(UIComponent parent)
        //{
        //    var resetButton = ComponentPool.Get<ButtonPanel>(parent);
        //    resetButton.Text = "Reset to default";
        //    resetButton.Init();
        //    resetButton.OnButtonClick += ResetToDefault;

        //    return resetButton;
        //}

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
