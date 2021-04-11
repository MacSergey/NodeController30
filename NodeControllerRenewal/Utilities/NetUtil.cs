using ColossalFramework;
using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KianCommons
{
    internal static class NetUtil
    {
        internal static int CountPedestrianLanes(this NetInfo info) => info.m_lanes.Count(lane => lane.m_laneType == NetInfo.LaneType.Pedestrian);

        static bool CheckID(this ref NetNode node1, ushort nodeId2)
        {
            var node2 = nodeId2.GetNode();
            return node1.m_buildIndex == node2.m_buildIndex && node1.m_position == node2.m_position;
        }
        internal static ushort GetID(this ref NetNode node)
        {
            var segment = node.Segments().First();
            return node.CheckID(segment.m_startNode) ? segment.m_startNode : segment.m_endNode;
        }

        #region copied from TMPE
        internal static NetInfo.Direction Invert(this NetInfo.Direction direction, bool invert = true) => invert ? NetInfo.InvertDirection(direction) : direction;

        internal static bool IsGoingForward(this NetInfo.Direction direction) => (direction & NetInfo.Direction.Both) == NetInfo.Direction.Forward || (direction & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidBackward;

        /// <summary>
        /// checks if vehicles move backward or bypass backward (considers LHT)
        /// </summary>
        /// <returns>true if vehicles move backward,
        /// false if vehilces going ward, bi-directional, or non-directional</returns>
        internal static bool IsGoingBackward(this NetInfo.Lane laneInfo, bool invertDirection = false) => laneInfo.m_finalDirection.Invert(invertDirection).IsGoingForward();
        #endregion

        public static IEnumerable<LaneData> IterateSegmentLanes(ushort segmentId)
        {
            int idx = 0;
            if (segmentId.GetSegment().Info == null)
            {
                SingletonMod<Mod>.Logger.Error("null info: potentially caused by missing assets. segmentId=" + segmentId);
                yield break;
            }
            int n = segmentId.GetSegment().Info.m_lanes.Length;
            bool inverted = segmentId.GetSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            for (uint laneID = segmentId.GetSegment().m_lanes;
                laneID != 0 && idx < n;
                laneID = laneID.GetLane().m_nextLane, idx++)
            {
                var laneInfo = segmentId.GetSegment().Info.m_lanes[idx];
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
            ushort segmentId = laneID.GetLane().m_segment;
            var id = segmentId.GetSegment().m_lanes;

            for (int i = 0; i < segmentId.GetSegment().Info.m_lanes.Length && id != 0; i++)
            {
                if (id == laneID)
                    return i;
                id = id.GetLane().m_nextLane;
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

            ushort segmentID = LaneID.GetLane().m_segment;
            laneInfo_ = segmentID.GetSegment().Info.m_lanes[LaneIndex];
            bool backward = laneInfo_.IsGoingBackward();
            bool inverted = segmentID.GetSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            StartNode = backward == inverted; //xnor
        }

        public readonly ushort SegmentID => Lane.m_segment;
        public readonly ref NetSegment Segment => ref SegmentID.GetSegmentRef();
        public readonly ref NetLane Lane => ref LaneID.GetLaneRef();
        public readonly ushort NodeID => StartNode ? Segment.m_startNode : Segment.m_endNode;
        public readonly NetLane.Flags Flags
        {
            get => (NetLane.Flags)Lane.m_flags;
            set => LaneID.GetLaneRef().m_flags = (ushort)value;
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

