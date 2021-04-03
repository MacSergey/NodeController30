using ColossalFramework;
using ColossalFramework.Math;
using KianCommons.Math;
using NodeController;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KianCommons
{
    internal class NetServiceException : Exception
    {
        public NetServiceException(string m) : base(m) { }
        public NetServiceException() : base() { }
        public NetServiceException(string m, Exception e) : base(m, e) { }
    }

    internal static class NetUtil
    {
        public const float SAFETY_NET = 0.02f;

        public static NetManager netMan = NetManager.instance;

        public const float MPU = 8f; // meter per unit

        private static NetNode[] nodeBuffer_ = netMan.m_nodes.m_buffer;
        private static NetSegment[] segmentBuffer_ = netMan.m_segments.m_buffer;
        private static NetLane[] laneBuffer_ = netMan.m_lanes.m_buffer;
        internal static ref NetNode ToNode(this ushort id) => ref nodeBuffer_[id];
        internal static ref NetSegment ToSegment(this ushort id) => ref segmentBuffer_[id];
        internal static ref NetLane ToLane(this uint id) => ref laneBuffer_[id];

        public static bool IsCSUR(this NetInfo info)
        {
            if (info == null || (info.m_netAI.GetType() != typeof(RoadAI) && info.m_netAI.GetType() != typeof(RoadBridgeAI) && info.m_netAI.GetType() != typeof(RoadTunnelAI)))
                return false;
            return info.name.Contains(".CSUR ");
        }


        public static ToolBase.ToolErrors InsertNode(NetTool.ControlPoint controlPoint, out ushort nodeId, bool test = false)
        {
            var ret = NetTool.CreateNode(
                controlPoint.m_segment.ToSegment().Info,
                controlPoint, controlPoint, controlPoint,
                NetTool.m_nodePositionsSimulation,
                maxSegments: 0,
                test: test, visualize: false, autoFix: true, needMoney: false,
                invert: false, switchDir: false,
                relocateBuildingID: 0,
                out nodeId, out var newSegment, out var cost, out var productionRate);

            if (!test)
                nodeId.ToNode().m_flags |= NetNode.Flags.Middle | NetNode.Flags.Moveable;

            return ret;
        }

        internal static int CountPedestrianLanes(this NetInfo info) => info.m_lanes.Count(lane => lane.m_laneType == NetInfo.LaneType.Pedestrian);

        static bool CheckID(this ref NetNode node1, ushort nodeId2)
        {
            ref NetNode node2 = ref nodeId2.ToNode();
            return node1.m_buildIndex == node2.m_buildIndex && node1.m_position == node2.m_position;
        }
        internal static ushort GetID(this ref NetNode node)
        {
            ref NetSegment seg = ref node.GetFirstSegment().ToSegment();
            bool startNode = node.CheckID(seg.m_startNode);
            return startNode ? seg.m_startNode : seg.m_endNode;
        }
        public static ushort GetFirstSegment(ushort nodeID) => nodeID.ToNode().GetFirstSegment();
        public static ushort GetFirstSegment(this ref NetNode node)
        {
            ushort segmentID = 0;
            int i;
            for (i = 0; i < 8; ++i)
            {
                segmentID = node.GetSegment(i);
                if (segmentID != 0)
                    break;
            }
            return segmentID;
        }

        #region Math

        #endregion MATH

        #region copied from TMPE
        internal static NetInfo.Direction Invert(this NetInfo.Direction direction, bool invert = true) => invert ? NetInfo.InvertDirection(direction) : direction;

        internal static bool IsGoingForward(this NetInfo.Direction direction) => (direction & NetInfo.Direction.Both) == NetInfo.Direction.Forward || (direction & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidBackward;

        /// <summary>
        /// checks if vehicles move backward or bypass backward (considers LHT)
        /// </summary>
        /// <returns>true if vehicles move backward,
        /// false if vehilces going ward, bi-directional, or non-directional</returns>
        internal static bool IsGoingBackward(this NetInfo.Lane laneInfo, bool invertDirection = false) => laneInfo.m_finalDirection.Invert(invertDirection).IsGoingForward();
        public static bool IsStartNode(ushort segmentId, ushort nodeId) => segmentId.ToSegment().m_startNode == nodeId;
        public static ushort GetSegmentNode(ushort segmentID, bool startNode) => segmentID.ToSegment().GetNode(startNode);
        public static ushort GetNode(this ref NetSegment segment, bool startNode) => startNode ? segment.m_startNode : segment.m_endNode;
        public static bool IsSegmentValid(ushort segmentId) => segmentId != 0 && segmentId.ToSegment().IsValid();
        public static bool IsValid(this ref NetSegment segment) => segment.Info != null ? segment.m_flags.CheckFlags(required: NetSegment.Flags.Created, forbidden: NetSegment.Flags.Deleted) : false;

        public static bool IsNodeValid(ushort nodeId) => nodeId != 0 && nodeId.ToNode().IsValid();
        public static bool IsValid(this ref NetNode node) => node.Info == null ? false : node.m_flags.CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.Deleted);

        #endregion

        public static IEnumerable<ushort> IterateNodeSegments(ushort nodeID)
        {
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentID = nodeID.ToNode().GetSegment(i);
                if (segmentID != 0)
                    yield return segmentID;
            }
        }

        public static IEnumerable<LaneData> IterateSegmentLanes(ushort segmentId)
        {
            int idx = 0;
            if (segmentId.ToSegment().Info == null)
            {
                Mod.Logger.Error("null info: potentially caused by missing assets. segmentId=" + segmentId);
                yield break;
            }
            int n = segmentId.ToSegment().Info.m_lanes.Length;
            bool inverted = segmentId.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            for (uint laneID = segmentId.ToSegment().m_lanes;
                laneID != 0 && idx < n;
                laneID = laneID.ToLane().m_nextLane, idx++)
            {
                var laneInfo = segmentId.ToSegment().Info.m_lanes[idx];
                bool forward = laneInfo.m_finalDirection == NetInfo.Direction.Forward;
                yield return new LaneData
                {
                    LaneID = laneID,
                    LaneIndex = idx,
                    LaneInfo = laneInfo,
                    StartNode = forward ^ !inverted,
                };
            }
        }

        public static IEnumerable<LaneData> IterateLanes(ushort segmentId, bool? startNode = null, NetInfo.LaneType laneType = NetInfo.LaneType.All, VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All)
        {
            foreach (LaneData laneData in IterateSegmentLanes(segmentId))
            {
                if (startNode != null && startNode != laneData.StartNode)
                    continue;
                if (!laneData.LaneInfo.m_laneType.IsFlagSet(laneType))
                    continue;
                if (!laneData.LaneInfo.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                yield return laneData;
            }
        }
        public static int GetLaneIndex(uint laneID)
        {
            ushort segmentId = laneID.ToLane().m_segment;
            var id = segmentId.ToSegment().m_lanes;

            for (int i = 0; i < segmentId.ToSegment().Info.m_lanes.Length && id != 0; i++)
            {
                if (id == laneID)
                    return i;
                id = id.ToLane().m_nextLane;
            }
            return -1;
        }
    }

    [Serializable]
    public struct LaneData
    {
        public uint LaneID;
        public int LaneIndex;
        public bool StartNode;

        [NonSerialized] private NetInfo.Lane laneInfo_;
        public NetInfo.Lane LaneInfo
        {
            get => laneInfo_ ??= Segment.Info.m_lanes[LaneIndex];
            set => laneInfo_ = value;
        }

        public LaneData(uint laneID, int laneIndex = -1)
        {
            LaneID = laneID;
            if (laneIndex < 0)
                laneIndex = NetUtil.GetLaneIndex(laneID);
            LaneIndex = laneIndex;

            ushort segmentID = LaneID.ToLane().m_segment;
            laneInfo_ = segmentID.ToSegment().Info.m_lanes[LaneIndex];
            bool backward = laneInfo_.IsGoingBackward();
            bool inverted = segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            StartNode = backward == inverted; //xnor
        }

        public readonly ushort SegmentID => Lane.m_segment;
        public readonly ref NetSegment Segment => ref SegmentID.ToSegment();
        public readonly ref NetLane Lane => ref LaneID.ToLane();
        public readonly ushort NodeID => StartNode ? Segment.m_startNode : Segment.m_endNode;
        public readonly NetLane.Flags Flags
        {
            get => (NetLane.Flags)Lane.m_flags;
            set => LaneID.ToLane().m_flags = (ushort)value;
        }

        public bool LeftSide => LaneInfo.m_position < 0 != Segment.m_flags.IsFlagSet(NetSegment.Flags.Invert);
        public bool RightSide => !LeftSide;

        public override string ToString()
        {
            try
            {
                return $"LaneData:[segment:{SegmentID} segmentInfo:{Segment.Info} node:{NodeID} laneID:{LaneID} Index={LaneIndex} {LaneInfo?.m_laneType} { LaneInfo?.m_vehicleType}]";
            }
            catch (NullReferenceException)
            {
                return $"LaneData:[segment:{SegmentID} segmentInfo:{Segment.Info} node:{NodeID} lane ID:{LaneID} null";
            }
        }
    }
}

