using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using CSUtil.Commons;
using KianCommons;
using System;
using System.Runtime.Serialization;
using UnityEngine;
using CSURUtil = NodeController.Util.CSURUtil;
using static KianCommons.ReflectionHelpers;
using System.Linq;
using KianCommons.Serialization;
using ModsCommon;
using System.Collections.Generic;
using ModsCommon.UI;
using ModsCommon.Utilities;

namespace NodeController
{
    [Serializable]
    public class SegmentEndData : INetworkData, INetworkData<SegmentEndData>, ISerializable
    {
        #region PROPERTIES

        public string Title => $"Segment #{SegmentId}";

        public ushort NodeId { get; set; }
        public ushort SegmentId { get; set; }

        public NetSegment Segment => SegmentId.GetSegment();
        public NetInfo Info => Segment.Info;
        public NetNode Node => NodeId.GetNode();
        public NodeData NodeData => NodeManager.Instance[NodeId];
        public NodeTypeT NodeType => NodeData.NodeType;
        public bool IsStartNode => Segment.IsStartNode(NodeId);
        public Vector3 Direction => IsStartNode ? Segment.m_startDirection : Segment.m_endDirection;

        public float DefaultOffset => CSURUtil.GetMinCornerOffset(SegmentId, NodeId);
        public bool DefaultFlatJunctions => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultTwist => DefaultFlatJunctions && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool FlatJunctions { get; set; }
        public bool Twist { get; set; }

        public float Stretch { get; set; }
        public float TwistAngle { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= Stretch == 0;
                ret &= TwistAngle == 0;
                ret &= FlatJunctions == DefaultFlatJunctions;
                ret &= Twist == DefaultTwist;

                ret &= NoCrossings == false;
                ret &= NoMarkings == false;
                ret &= NoJunctionTexture == false;
                ret &= NoJunctionProps == false;
                ret &= NoTLProps == false;
                return ret;
            }
        }
        public float Offset { get; set; }
        public float Shift { get; set; }
        public float RotateAngle { get; set; }
        public float SlopeAngle { get; set; }

        bool CrossingIsRemoved => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(NodeId, SegmentId);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public bool CanModifyOffset => NodeData?.CanModifyOffset ?? false;
        public bool CanModifyCorners => NodeData != null && (CanModifyOffset || NodeType == NodeTypeT.End || NodeType == NodeTypeT.Middle);
        public bool CanModifyFlatJunctions => NodeData?.CanModifyFlatJunctions ?? false;
        public bool CanModifyTwist => CanTwist(SegmentId, NodeId);
        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData != null && NodeData.NodeType == NodeTypeT.Stretch)
                    return false; // always ignore.
                else if (NoMarkings)
                    return true; // always hide
                else
                    return null; // default.
            }
        }

        #endregion

        #region BASIC

        public SegmentEndData() { }
        private SegmentEndData(SegmentEndData template) => CopyProperties(this, template);
        public SegmentEndData(SerializationInfo info, StreamingContext context)
        {
            SerializationUtil.SetObjectFields(info, this);

            // corner offset and slope angle deg
            SerializationUtil.SetObjectProperties(info, this);
            Update();
        }
        public SegmentEndData(ushort segmentID, ushort nodeID)
        {
            NodeId = nodeID;
            SegmentId = segmentID;

            Calculate();
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;

            Update();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) => SerializationUtil.GetObjectFields(info, this);
        public SegmentEndData Clone() => new SegmentEndData(this);

        public void Calculate()
        {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            //Refresh();
        }
        //private void Refresh()
        //{
        //    if (!CanModifyOffset)
        //        Offset = DefaultOffset;

        //    if (!CanModifyFlatJunctions)
        //        FlatJunctions = DefaultFlatJunctions;

        //    if (!CanModifyTwist)
        //        Twist = DefaultTwist;

        //    if (!CanModifyCorners)
        //    {
        //        SlopeAngle = 0f;
        //        Stretch = TwistAngle = 0;
        //    }

        //    if (!FlatJunctions)
        //        Twist = false;
        //}
        public void Update()
        {
            NetManager.instance.UpdateNode(NodeId);
        }
        public void ResetToDefault()
        {
            //Offset = DefaultOffset;
            //Shift = 0f;
            //RotateAngle = 0f;
            //SlopeAngle = 0f;
            //TwistAngle = 0f;

            //FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;
            NoCrossings = false;
            //NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            Stretch = TwistAngle = 0;
            //RefreshAndUpdate();
        }

        public void OnAfterCalculate()
        {
            Segment.CalculateCorner(SegmentId, true, IsStartNode, leftSide: true, cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(SegmentId, true, IsStartNode, leftSide: false, cornerPos: out var rpos, cornerDirection: out var rdir, out _);

            var diff = rpos - lpos;
            var se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public static bool CanTwist(ushort segmentId, ushort nodeId)
        {
            var segmentIds = nodeId.GetNode().SegmentIds().ToArray();

            if (segmentIds.Length == 1)
                return false;

            var segment = segmentId.GetSegment();
            var segment1Id = segment.GetLeftSegment(nodeId);
            var segment2Id = segment.GetRightSegment(nodeId);
            var segEnd1 = SegmentEndManager.Instance[segment1Id, nodeId];
            var segEnd2 = SegmentEndManager.Instance[segment2Id, nodeId];

            bool flat1 = segEnd1?.FlatJunctions ?? segment1Id.GetSegment().Info.m_flatJunctions;
            bool flat2 = segEnd2?.FlatJunctions ?? segment2Id.GetSegment().Info.m_flatJunctions;
            if (flat1 && flat2)
                return false;

            if (segmentIds.Length == 2)
            {
                var dir1 = segment1Id.GetSegment().GetDirection(nodeId);
                var dir = segmentId.GetSegment().GetDirection(nodeId);
                if (Mathf.Abs(VectorUtils.DotXZ(dir, dir1)) > 0.999f)
                    return false;
            }

            return true;
        }

        public override string ToString() => $"{GetType().Name} (segment:{SegmentId} node:{NodeId})";

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
