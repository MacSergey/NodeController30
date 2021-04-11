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
    public class SegmentEndData : INetworkData
    {
        #region PROPERTIES

        public string Title => $"Segment #{Id}";

        public ushort NodeId { get; set; }
        public ushort Id { get; set; }

        public NetSegment Segment => Id.GetSegment();
        public NetInfo Info => Segment.Info;
        public NetNode Node => NodeId.GetNode();
        public NodeData NodeData => Manager.Instance[NodeId];
        public NodeStyleType NodeType => NodeData.Type;
        public bool IsStartNode => Segment.IsStartNode(NodeId);
        public Vector3 Direction => IsStartNode ? Segment.m_startDirection : Segment.m_endDirection;

        public float DefaultOffset => CSURUtil.GetMinCornerOffset(Id, NodeId);
        public bool DefaultIsFlat => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultTwist => DefaultIsFlat && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool IsFlat { get; set; }
        public bool Twist { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= TwistAngle == 0;
                ret &= IsFlat == DefaultIsFlat;
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
        public float TwistAngle { get; set; }

        public bool CanModifyOffset => NodeData?.CanModifyOffset ?? false;
        public bool CanModifyCorners => NodeData != null && (CanModifyOffset || NodeType == NodeStyleType.End || NodeType == NodeStyleType.Middle);
        public bool CanModifyFlatJunctions => NodeData?.CanModifyFlatJunctions ?? false;
        public bool CanModifyTwist => CanTwist(Id, NodeId);
        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData != null && NodeData.Type == NodeStyleType.Stretch)
                    return false; // always ignore.
                else if (NoMarkings)
                    return true; // always hide
                else
                    return null; // default.
            }
        }

        #endregion

        #region BASIC

        public SegmentEndData(ushort segmentID, ushort nodeID)
        {
            NodeId = nodeID;
            Id = segmentID;

            Calculate();
            IsFlat = DefaultIsFlat;
            Twist = DefaultTwist;
        }

        public void Calculate()
        {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();
        }
        public void ResetToDefault()
        {
            Twist = DefaultTwist;
            NoCrossings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
        }

        public void OnAfterCalculate()
        {
            Segment.CalculateCorner(Id, true, IsStartNode, leftSide: true, cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(Id, true, IsStartNode, leftSide: false, cornerPos: out var rpos, cornerDirection: out var rdir, out _);

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
            var firstSegmentId = segment.GetLeftSegment(nodeId);
            var secondSegmentId = segment.GetRightSegment(nodeId);
            var nodeData = Manager.Instance[nodeId];
            var segmentEnd1 = nodeData[firstSegmentId];
            var segmentEnd2 = nodeData[secondSegmentId];

            bool flat1 = segmentEnd1?.IsFlat ?? firstSegmentId.GetSegment().Info.m_flatJunctions;
            bool flat2 = segmentEnd2?.IsFlat ?? secondSegmentId.GetSegment().Info.m_flatJunctions;
            if (flat1 && flat2)
                return false;

            if (segmentIds.Length == 2)
            {
                var dir1 = firstSegmentId.GetSegment().GetDirection(nodeId);
                var dir = segmentId.GetSegment().GetDirection(nodeId);
                if (Mathf.Abs(VectorUtils.DotXZ(dir, dir1)) > 0.999f)
                    return false;
            }

            return true;
        }

        public override string ToString() => $"{GetType().Name} (segment:{Id} node:{NodeId})";

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
