using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using KianCommons;
using System;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModsCommon.Utilities;
using NodeController.Utilities;

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
        public bool IsStartNode => Segment.IsStartNode(NodeId);

        public float DefaultOffset => CSURUtilities.GetMinCornerOffset(Id, NodeId);
        public bool DefaultIsFlat => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => DefaultIsFlat && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool IsSlope { get; set; }
        public bool IsTwist { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= TwistAngle == 0;
                ret &= IsSlope == !DefaultIsFlat;
                ret &= IsTwist == DefaultIsTwist;

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

        public SegmentEndData(ushort segmentId, ushort nodeId)
        {
            Id = segmentId;
            NodeId = nodeId;

            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            ResetToDefault();
        }

        public void ResetToDefault()
        {
            Offset = DefaultOffset;

            IsSlope = !DefaultIsFlat;
            IsTwist = DefaultIsTwist;
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

            bool flat1 = !segmentEnd1?.IsSlope ?? firstSegmentId.GetSegment().Info.m_flatJunctions;
            bool flat2 = !segmentEnd2?.IsSlope ?? secondSegmentId.GetSegment().Info.m_flatJunctions;
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
