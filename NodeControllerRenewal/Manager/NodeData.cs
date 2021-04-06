using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using static ColossalFramework.Math.VectorUtils;
using TrafficManager.API.Traffic.Enums;
using TernaryBool = CSUtil.Commons.TernaryBool;
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
        public string Title => $"Node #{NodeID}";
        public override string ToString() => $"NodeData(id:{NodeID} type:{NodeType})";

        #region SERIALIZATION
        public NodeData() { } // so that the code compiles

        //serialization
        public void GetObjectData(SerializationInfo info, StreamingContext context) => SerializationUtil.GetObjectFields(info, this);

        // deserialization
        public NodeData(SerializationInfo info, StreamingContext context)
        {
            SerializationUtil.SetObjectFields(info, this);

            if (NodeManager.TargetNodeID != 0)// backward compatibility.
                NodeID = NodeManager.TargetNodeID;

            // corner offset and clear markings
            SerializationUtil.SetObjectProperties(info, this);
            Update();
        }

        private NodeData(NodeData template)
        {
            CopyProperties(this, template);
        }

        public NodeData Clone() => new NodeData(this);
        #endregion


        // intrinsic
        public ushort NodeID;

        // defaults
        public NetNode.Flags DefaultFlags;
        public NodeTypeT DefaultNodeType;

        // cache
        public bool HasPedestrianLanes;
        public int SegmentCount;
        public float CurveRaduis0;

        // cache only for segment count == 2
        public float HWDiff;
        public int PedestrianLaneCount;
        public bool IsStraight;
        public bool Is180;
        ushort segmentID1, segmentID2;
        public List<ushort> SortedSegmentIDs; //sorted by how big semgent is.
        public SegmentEndData SegmentEnd1 => SegmentEndManager.Instance.GetAt(segmentID1, NodeID);
        public SegmentEndData SegmentEnd2 => SegmentEndManager.Instance.GetAt(segmentID2, NodeID);

        // Configurable
        public NodeTypeT NodeType;

        #region BULK EDIT

        #region CORNER OFFSET
        public float CornerOffset
        {
            get => Node.SegmentsId().Average(s => SegmentEndManager.Instance.GetOrCreate(s, NodeID).CornerOffset);
            set
            {
                foreach (var segmentId in Node.SegmentsId())
                {
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentId, NodeID);
                    segEnd.CornerOffset = value;
                }
            }
        }

        public bool HasUniformCornerOffset()
        {
            float cornerOffset0 = -1;
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0)
                    continue;
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                if (!segEnd.HasUniformCornerOffset())
                    return false;
                if (cornerOffset0 == -1)
                    cornerOffset0 = segEnd.CornerOffset;
                else if (cornerOffset0 != segEnd.CornerOffset)
                    return false;
            }
            return true;
        }
        #endregion

        #region NO MARKINGS

        public bool NoMarkings
        {
            get => Node.SegmentsId().Any(s => SegmentEndManager.Instance.GetOrCreate(s, NodeID).NoMarkings);
            set
            {
                foreach (var segmentId in Node.SegmentsId())
                {
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentId, NodeID);
                    segEnd.NoMarkings = value;
                }
            }
        }

        public bool HasUniformNoMarkings()
        {
            bool? noMarkings0 = null;
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0)
                    continue;
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                if (noMarkings0 == null)
                    noMarkings0 = segEnd.NoMarkings;
                else if (noMarkings0 != segEnd.NoMarkings)
                    return false;
            }
            return true;
        }
        #endregion

        #region FLATTEN NODE

        static int CompareSegments(ushort seg1Id, ushort seg2Id)
        {
            NetInfo info1 = seg1Id.GetSegment().Info;
            NetInfo info2 = seg2Id.GetSegment().Info;

            int slope1 = info1.m_flatJunctions ? 0 : 1;
            int slope2 = info2.m_flatJunctions ? 0 : 1;
            int diff = slope1 - slope2;
            if (diff != 0)
                return diff;

            diff = info1.m_forwardVehicleLaneCount - info2.m_forwardVehicleLaneCount;
            if (diff != 0)
                return diff;

            diff = (int)Math.Ceiling(info2.m_halfWidth - info1.m_halfWidth);
            if (diff != 0)
                return diff;

            bool bHighway1 = (info1.m_netAI as RoadBaseAI)?.m_highwayRules ?? false;
            bool bHighway2 = (info1.m_netAI as RoadBaseAI)?.m_highwayRules ?? false;
            int iHighway1 = bHighway1 ? 1 : 0;
            int iHighway2 = bHighway2 ? 1 : 0;
            diff = iHighway1 - iHighway2;
            return diff;
        }

        public void Flatten()
        {
            SingletonMod<Mod>.Logger.Debug("NodeData.Flatten() called");
            foreach (ushort segmentID in SortedSegmentIDs)
            {
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                segEnd.FlatJunctions = true;
                segEnd.Twist = false;
            }
        }

        public void UnFlatten()
        {
            SingletonMod<Mod>.Logger.Debug("NodeData.UnFlatten() called");
            for (int i = 0; i < SortedSegmentIDs.Count; ++i)
            {
                ushort segmentID = SortedSegmentIDs[i];
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                bool sideSegment = i >= 2;
                segEnd.FlatJunctions = sideSegment;
                segEnd.Twist = sideSegment;
            }
        }

        #endregion

        #region EMBANKMENT ANGLE
        public float EmbankmentAngle
        {
            get => (SegmentEnd1.EmbankmentAngleDeg - SegmentEnd2.EmbankmentAngleDeg) / 2;
            set
            {
                SegmentEnd1.EmbankmentAngleDeg = value;
                SegmentEnd2.EmbankmentAngleDeg = -value;
            }
        }

        public float EmbankmentPercent
        {
            get => (SegmentEnd1.EmbankmentPercent - SegmentEnd2.EmbankmentPercent) / 2;
            set
            {
                SegmentEnd1.EmbankmentPercent = value;
                SegmentEnd2.EmbankmentPercent = -value;
            }
        }

        public bool HasUniformEmbankmentAngle() => SegmentEnd1.EmbankmentAngleDeg == -SegmentEnd2.EmbankmentAngleDeg;
        #endregion

        #region SLOPE ANGLE
        public float SlopeAngleDeg
        {
            get => (SegmentEnd1.SlopeAngleDeg - SegmentEnd2.SlopeAngleDeg) / 2;
            set
            {
                SegmentEnd1.SlopeAngleDeg = value;
                SegmentEnd2.SlopeAngleDeg = -value;
            }
        }

        public bool HasUniformSlopeAngle() => MathUtil.EqualAprox(SegmentEnd1.SlopeAngleDeg, -SegmentEnd2.SlopeAngleDeg, error: 1f);
        #endregion

        #region STRETCH
        public float Stretch
        {
            get => (SegmentEnd1.Stretch + SegmentEnd2.Stretch) / 2;
            set
            {
                SegmentEnd1.Stretch = value;
                SegmentEnd2.Stretch = value;
            }
        }

        public bool HasUniformStretch() => SegmentEnd1.Stretch == SegmentEnd2.Stretch;
        #endregion

        #endregion

        public bool FirstTimeTrafficLight; // turn on traffic light when inserting pedestrian node for the first time.

        public ref NetNode Node => ref NodeID.ToNode();

        public NodeData(ushort nodeID)
        {
            NodeID = nodeID;
            Calculate();
            NodeType = DefaultNodeType;
            FirstTimeTrafficLight = false;
            Update();
        }

        public NodeData(ushort nodeID, NodeTypeT nodeType) : this(nodeID)
        {
            NodeType = nodeType;
            FirstTimeTrafficLight = nodeType == NodeTypeT.Crossing;
        }

        public void Calculate()
        {
            CalculateDefaults();
            Refresh();
        }

        /// <summary>
        /// Capture the default values.
        /// </summary>
        private void CalculateDefaults()
        {
            SegmentCount = NodeID.GetNode().CountSegments();
            DefaultFlags = NodeID.GetNode().m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultNodeType = NodeTypeT.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Bend))
                DefaultNodeType = NodeTypeT.Bend;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultNodeType = NodeTypeT.Custom;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.End))
                NodeType = DefaultNodeType = NodeTypeT.End;
            else
                throw new NotImplementedException("unsupported node flags: " + DefaultFlags);

            if (SegmentCount == 2)
            {
                float hw0 = 0;
                Vector3 dir0 = default;
                foreach (ushort segmentID in NetUtil.IterateNodeSegments(NodeID))
                {
                    int nPedLanes = segmentID.GetSegment().Info.CountPedestrianLanes();
                    if (hw0 == 0)
                    {
                        segmentID1 = segmentID;
                        hw0 = segmentID.GetSegment().Info.m_halfWidth;
                        dir0 = NormalizeXZ(segmentID.GetSegment().GetDirection(NodeID));
                        PedestrianLaneCount = nPedLanes;
                    }
                    else
                    {
                        segmentID2 = segmentID;
                        HWDiff = Mathf.Abs(segmentID.GetSegment().Info.m_halfWidth - hw0);
                        var dir1 = NormalizeXZ(segmentID.GetSegment().GetDirection(NodeID));
                        float dot = DotXZ(dir0, dir1);
                        IsStraight = dot < -0.999f; // 180 degrees
                        Is180 = dot > 0.999f; // 0 degrees
                        PedestrianLaneCount = Math.Max(PedestrianLaneCount, nPedLanes);
                    }
                }
            }

            foreach (ushort segmetnID in NetUtil.IterateNodeSegments(NodeID))
                HasPedestrianLanes |= segmetnID.GetSegment().Info.m_hasPedestrianLanes;

            SortedSegmentIDs = new List<ushort>(Node.CountSegments());
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0)
                    continue;
                SortedSegmentIDs.Add(segmentID);
            }

            SortedSegmentIDs.Sort(CompareSegments);
            SortedSegmentIDs.Reverse();

            Refresh();
        }

        /// <summary>
        /// this is called to make necessary changes to the node to handle external changes
        /// </summary>
        private void Refresh()
        {
            if (NodeType != NodeTypeT.Custom)
                NoMarkings = false;

            if (!CanModifyOffset)
            {
                if (NodeType == NodeTypeT.UTurn)
                    CornerOffset = 8f;
                else if (NodeType == NodeTypeT.Crossing)
                    CornerOffset = 0f;
            }
        }

        public void Update()
        {
            NetManager.instance.UpdateNode(NodeID);
        }

        public void RefreshAndUpdate()
        {
            Refresh();
            Update();
        }

        public IEnumerable<SegmentEndData> SegmentEndDatas
        {
            get
            {
                for (int i = 0; i < 8; ++i)
                {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID != 0)
                        yield return SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: NodeID);
                }
            }
        }

        public bool IsDefault => NodeType == DefaultNodeType && !SegmentEndDatas.Any(s => s?.IsDefault != true);
        public void ResetToDefault()
        {
            NodeType = DefaultNodeType;

            foreach (var segEnd in SegmentEndDatas)
                segEnd?.ResetToDefault();

            Update();
        }
        public static bool IsSupported(ushort nodeID)
        {
            if (!NetUtil.IsNodeValid(nodeID)) // check info !=null (and maybe more checks in future)
                return false;

            foreach (ushort segmentID in NetUtil.IterateNodeSegments(nodeID))
            {
                if (!NetUtil.IsSegmentValid(segmentID))
                    return false;
            }

            var flags = nodeID.GetNode().m_flags;
            if (!flags.CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.LevelCrossing | NetNode.Flags.Outside | NetNode.Flags.Deleted))
                return false;

            if (nodeID.GetNode().CountSegments() != 2)
                return true;

            var info = nodeID.GetNode().Info;
            return !NetUtil.IsCSUR(info);
        }
        public bool CanChangeTo(NodeTypeT newNodeType)
        {
            if (SegmentCount == 1)
                return newNodeType == NodeTypeT.End;

            if (SegmentCount > 2 || IsCSUR)
                return newNodeType == NodeTypeT.Custom;

            bool middle = DefaultFlags.IsFlagSet(NetNode.Flags.Middle);
            return newNodeType switch
            {
                NodeTypeT.Crossing => PedestrianLaneCount >= 2 && HWDiff < 0.001f && IsStraight,
                NodeTypeT.UTurn => IsRoad && Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0,
                NodeTypeT.Stretch => CanModifyTextures && !middle && IsStraight,
                NodeTypeT.Bend => !middle,
                NodeTypeT.Middle => IsStraight || Is180,
                NodeTypeT.Custom => true,
                NodeTypeT.End => false,
                _ => throw new Exception("Unreachable code"),
            };
        }

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => NodeID.ToNode().Info;
        public bool IsRoad => Info.m_netAI is RoadBaseAI;
        public bool IsEndNode => NodeType == NodeTypeT.End;
        public bool NeedMiddleFlag => NodeType == NodeTypeT.Middle;
        public bool NeedBendFlag => NodeType == NodeTypeT.Bend;
        public bool NeedJunctionFlag => !NeedMiddleFlag && !NeedBendFlag && !IsEndNode;
        public bool WantsTrafficLight => NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset => NodeType == NodeTypeT.Bend || NodeType == NodeTypeT.Stretch || NodeType == NodeTypeT.Custom;
        public bool CanMassEditNodeCorners => SegmentCount == 2;
        public bool CanModifyFlatJunctions => !NeedMiddleFlag;
        public bool IsAsymRevert => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);
        public bool CanModifyTextures => IsRoad && !IsCSUR;
        public bool ShowNoMarkingsToggle => CanModifyTextures && NodeType == NodeTypeT.Custom;

        bool CrossingIsRemoved(ushort segmentId) => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(NodeID, segmentId);
        public bool NeedsTransitionFlag() => SegmentCount == 2 && (NodeType == NodeTypeT.Custom || NodeType == NodeTypeT.Crossing || NodeType == NodeTypeT.UTurn);
        public bool ShouldRenderCenteralCrossingTexture() => NodeType == NodeTypeT.Crossing && CrossingIsRemoved(segmentID1) && CrossingIsRemoved(segmentID2);

        public string ToolTip(NodeTypeT nodeType) => nodeType switch
        {
            NodeTypeT.Crossing => "Crossing node.",
            NodeTypeT.Middle => "Middle: No node.",
            NodeTypeT.Bend => IsAsymRevert ? "Bend: Asymmetrical road changes direction." : (HWDiff > 0.05f ? "Bend: Linearly match segment widths." : "Bend: Simple road corner."),
            NodeTypeT.Stretch => "Stretch: Match both pavement and road.",
            NodeTypeT.UTurn => "U-Turn: node with enough space for U-Turn.",
            NodeTypeT.Custom => "Custom: transition size and traffic rules are configrable.",
            NodeTypeT.End => "when there is only one segment at the node.",
            _ => null,
        };

        #region EXTERNAL MODS
        // undefined -> don't touch prev value
        // true -> force true
        // false -> force false.
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
            NodeTypeT.Custom => (SegmentCount == 2 && !HasPedestrianLanes) ? false : null,
            NodeTypeT.End => null,
            _ => throw new Exception("Unreachable code"),
        };
        public bool? IsDefaultPedestrianCrossingAllowed
        {
            get
            {
                switch (NodeType)
                {
                    case NodeTypeT.Crossing:
                        return true; // always on
                    case NodeTypeT.UTurn:
                    case NodeTypeT.Stretch:
                    case NodeTypeT.Middle:
                    case NodeTypeT.Bend:
                        return false; // always off
                    case NodeTypeT.Custom:
                        var netAI1 = segmentID1.ToSegment().Info.m_netAI;
                        var netAI2 = segmentID2.ToSegment().Info.m_netAI;
                        bool sameAIType = netAI1.GetType() == netAI2.GetType();
                        if (SegmentCount == 2 && !sameAIType) // eg: at bridge/tunnel entrances.
                            return false; // default off
                        return null; // don't care
                    case NodeTypeT.End:
                        return null;
                    default:
                        throw new Exception("Unreachable code");
                }
            }
        }
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
            NodeTypeT.Custom when SegmentCount > 2 => null,
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
            NodeTypeT.Custom => SegmentCount > 2 ? null : true,
            _ => throw new Exception("Unreachable code"),
        };

        #endregion

        public List<EditorItem> GetUIComponents(UIComponent parent)
        {
            var properties = new List<EditorItem>();

            properties.Add(GetNodeTypeProperty(parent));
            properties.Add(GetOffsetProperty(parent));
            if (SegmentCount == 2)
            {
                properties.Add(GetSlopeProperty(parent));
                properties.Add(GetEmbankmentProperty(parent));
            }
            properties.Add(GetHideMarkingProperty(parent));
            properties.Add(GetActionButtons(parent));
            properties.Add(GetResetButton(parent));

            return properties;
        }
        private NodeTypePropertyPanel GetNodeTypeProperty(UIComponent parent)
        {
            var typeProperty = ComponentPool.Get<NodeTypePropertyPanel>(parent);
            typeProperty.Text = "Node type";
            typeProperty.Init();
            typeProperty.SelectedObject = NodeType;
            typeProperty.OnSelectObjectChanged += (value) => NodeType = value;

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
                    UnFlatten();
                else if (index == flatIndex)
                    Flatten();
            }
        }

        private FloatPropertyPanel GetOffsetProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            offsetProperty.Text = "Corner offset";
            offsetProperty.MinValue = 0;
            offsetProperty.CheckMin = true;
            offsetProperty.MaxValue = 100;
            offsetProperty.CheckMax = true;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 1f;
            offsetProperty.Init();
            offsetProperty.Value = CornerOffset;
            offsetProperty.OnValueChanged += (value) => CornerOffset = value;

            return offsetProperty;
        }
        private FloatPropertyPanel GetSlopeProperty(UIComponent parent)
        {
            var slopeProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            slopeProperty.Text = "Slope";
            slopeProperty.MinValue = -60;
            slopeProperty.CheckMin = true;
            slopeProperty.MaxValue = 60;
            slopeProperty.CheckMax = true;
            slopeProperty.UseWheel = true;
            slopeProperty.WheelStep = 1f;
            slopeProperty.Init();
            slopeProperty.Value = SlopeAngleDeg;
            slopeProperty.OnValueChanged += (value) => SlopeAngleDeg = value;

            return slopeProperty;
        }
        private FloatPropertyPanel GetEmbankmentProperty(UIComponent parent)
        {
            var embankmentProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            embankmentProperty.Text = "Embankment";
            embankmentProperty.MinValue = -60;
            embankmentProperty.CheckMin = true;
            embankmentProperty.MaxValue = 60;
            embankmentProperty.CheckMax = true;
            embankmentProperty.UseWheel = true;
            embankmentProperty.WheelStep = 1f;
            embankmentProperty.Init();
            embankmentProperty.Value = EmbankmentAngle;
            embankmentProperty.OnValueChanged += (value) => EmbankmentAngle = value;

            return embankmentProperty;
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
        private ButtonPanel GetResetButton(UIComponent parent)
        {
            var resetButton = ComponentPool.Get<ButtonPanel>(parent);
            resetButton.Text = "Reset to default";
            resetButton.Init();
            resetButton.OnButtonClick += ResetToDefault;

            return resetButton;
        }
    }

    public class NodeTypePropertyPanel : EnumOncePropertyPanel<NodeTypeT, NodeTypePropertyPanel.NodeTypeDropDown>
    {
        protected override float DropDownWidth => 100f;
        protected override bool IsEqual(NodeTypeT first, NodeTypeT second) => first == second;
        public class NodeTypeDropDown : UIDropDown<NodeTypeT> { }
        protected override string GetDescription(NodeTypeT value) => value.ToString();
    }
}
