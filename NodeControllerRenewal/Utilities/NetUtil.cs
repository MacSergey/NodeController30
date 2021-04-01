using ColossalFramework;
using ColossalFramework.Math;
using KianCommons.Math;
using NodeController30;
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
        public static NetTool netTool => Singleton<NetTool>.instance;
        public static SimulationManager simMan => Singleton<SimulationManager>.instance;
        public static TerrainManager terrainMan => TerrainManager.instance;

        public const float MPU = 8f; // meter per unit

        private static NetNode[] nodeBuffer_ = netMan.m_nodes.m_buffer;
        private static NetSegment[] segmentBuffer_ = netMan.m_segments.m_buffer;
        private static NetLane[] laneBuffer_ = netMan.m_lanes.m_buffer;
        internal static ref NetNode ToNode(this ushort id) => ref nodeBuffer_[id];
        internal static ref NetSegment ToSegment(this ushort id) => ref segmentBuffer_[id];
        internal static ref NetLane ToLane(this uint id) => ref laneBuffer_[id];
        internal static NetLane.Flags Flags(this ref NetLane lane) => (NetLane.Flags)lane.m_flags;

        /// <summary>
        /// returns lane data of the given lane ID.
        /// throws exception if unsuccessful.
        /// </summary>
        internal static LaneData GetLaneData(uint laneId)
        {
            Assertion.Assert(laneId != 0, "laneId!=0");
            var flags = laneId.ToLane().Flags();
            bool valid = (flags & NetLane.Flags.Created | NetLane.Flags.Deleted) != NetLane.Flags.Created;
            Assertion.Assert(valid, "valid");
            ushort segmentId = laneId.ToLane().m_segment;
            foreach (var laneData in IterateSegmentLanes(segmentId))
            {
                if (laneData.LaneID == laneId)
                    return laneData;
            }
            throw new Exception($"Unreachable code. lane:{laneId} segment:{segmentId} info:{segmentId.ToSegment().Info}");
        }

        public static bool IsCSUR(this NetInfo info)
        {
            if (info == null ||
                (info.m_netAI.GetType() != typeof(RoadAI) &&
                info.m_netAI.GetType() != typeof(RoadBridgeAI) &&
                info.m_netAI.GetType() != typeof(RoadTunnelAI)))
            {
                return false;
            }
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

        static bool CheckID(this ref NetSegment segment1, ushort segmentId2)
        {
            ref NetSegment segment2 = ref segmentId2.ToSegment();
            return (segment1.m_startNode == segment2.m_startNode) & (segment1.m_endNode == segment2.m_endNode);
        }

        internal static ushort GetID(this ref NetNode node)
        {
            ref NetSegment seg = ref node.GetFirstSegment().ToSegment();
            bool startNode = node.CheckID(seg.m_startNode);
            return startNode ? seg.m_startNode : seg.m_endNode;
        }

        internal static ushort GetID(this ref NetSegment segment)
        {
            ref var node = ref segment.m_startNode.ToNode();
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentId = node.GetSegment(i);
                if (segment.CheckID(segmentId))
                    return segmentId;
            }
            return 0;
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

        public static Vector3 GetSegmentDir(ushort segmentID, ushort nodeID)
        {
            bool startNode = IsStartNode(segmentID, nodeID);
            ref NetSegment segment = ref segmentID.ToSegment();
            return startNode ? segment.m_startDirection : segment.m_endDirection;
        }

        internal static float MaxNodeHW(ushort nodeId)
        {
            float ret = 0;
            foreach (var segmentId in IterateNodeSegments(nodeId))
            {
                float hw = segmentId.ToSegment().Info.m_halfWidth;
                if (hw > ret)
                    ret = hw;
            }
            return ret;
        }

        #region Math

        /// Note: inverted flag or LHT does not influce the beizer.
        internal static Bezier3 CalculateSegmentBezier3(this ref NetSegment seg, bool bStartNode = true)
        {
            ref NetNode startNode = ref seg.m_startNode.ToNode();
            ref NetNode endNode = ref seg.m_endNode.ToNode();
            Bezier3 bezier = new Bezier3
            {
                a = startNode.m_position,
                d = endNode.m_position,
            };
            NetSegment.CalculateMiddlePoints(bezier.a, seg.m_startDirection, bezier.d, seg.m_endDirection, startNode.m_flags.IsFlagSet(NetNode.Flags.Middle), endNode.m_flags.IsFlagSet(NetNode.Flags.Middle), out bezier.b, out bezier.c);

            return bStartNode ? bezier : bezier.Invert();
        }

        /// <param name="startNode"> if true the bezier is inverted so that it will be facing start node</param>
        /// Note: inverted flag or LHT does not influce the beizer.
        internal static Bezier2 CalculateSegmentBezier2(ushort segmentId, bool startNode)
        {
            Bezier3 bezier3 = segmentId.ToSegment().CalculateSegmentBezier3(startNode);
            Bezier2 bezier2 = bezier3.ToCSBezier2();
            return bezier2;
        }

        /// <param name="endNodeID">bezier will be facing endNodeID</param>
        internal static Bezier2 CalculateSegmentBezier2(ushort segmentId, ushort endNodeID)
        {
            bool startNode = !IsStartNode(segmentId, endNodeID);
            return CalculateSegmentBezier2(segmentId, startNode);
        }

        internal static float GetClosestT(this ref NetSegment segment, Vector3 position)
        {
            Bezier3 bezier = segment.CalculateSegmentBezier3();
            return bezier.GetClosestT(position);
        }

        /// <param name="bLeft2">if other segment is to the left side of segmentID.</param>
        /// <param name="cornerDir">is normalized</param>
        internal static void CalculateCorner(
            ushort segmentID, ushort nodeID, bool bLeft2,
            out Vector2 cornerPoint, out Vector2 cornerDir)
        {
            segmentID.ToSegment().CalculateCorner(
                segmentID,
                true,
                IsStartNode(segmentID, nodeID),
                !bLeft2, // leftSide = if this segment is to the left of the other segment = !bLeft2
                out Vector3 cornerPos,
                out Vector3 cornerDirection,
                out bool smooth);
            cornerPoint = cornerPos.ToCS2D();
            cornerDir = cornerDirection.ToCS2D().normalized;
        }

        /// <param name="bLeft2">if other segment is to the left side of segmentID.</param>
        internal static void CalculateOtherCorner(
            ushort segmentID, ushort nodeID, bool bLeft2,
            out Vector2 cornerPoint, out Vector2 cornerDir)
        {
            ushort otherSegmentID = bLeft2 ?
                segmentID.ToSegment().GetLeftSegment(nodeID) :
                segmentID.ToSegment().GetRightSegment(nodeID);
            CalculateCorner(otherSegmentID, nodeID, !bLeft2,
                            out cornerPoint, out cornerDir);
        }

        /// <param name="bLeft2">if other segment is to the left side of segmentID.</param>
        internal static void CalculateCorner(ushort segmentID, ushort nodeID, bool bLeft2, out Vector3 cornerPos, out Vector3 cornerDirection)
            => segmentID.ToSegment().CalculateCorner(segmentID, true, IsStartNode(segmentID, nodeID), !bLeft2, out cornerPos, out cornerDirection, out _);

        internal static void CalculateSegEndCenter(ushort segmentID, ushort nodeID, out Vector3 pos, out Vector3 dir)
        {
            CalculateCorner(segmentID, nodeID, false, out Vector3 pos1, out Vector3 dir1);
            CalculateCorner(segmentID, nodeID, true, out Vector3 pos2, out Vector3 dir2);
            pos = (pos1 + pos2) * 0.5f;
            dir = (dir1 + dir2) * 0.5f;
        }

        #endregion MATH

        public static float SampleHeight(Vector2 point) => terrainMan.SampleDetailHeightSmooth(point.ToCS3D(0));
        public static Vector3 Get3DPos(Vector2 point) => point.ToCS3D(SampleHeight(point));

        #region copied from TMPE
        public static bool LHT => TrafficDrivesOnLeft;
        public static bool RHT => !LHT;
        public static bool TrafficDrivesOnLeft => Singleton<SimulationManager>.instance?.m_metaData?.m_invertTraffic == SimulationMetaData.MetaBool.True;

        public static bool CanConnectPathToSegment(ushort segmentID) => segmentID.ToSegment().CanConnectPath();

        public static bool CanConnectPath(this ref NetSegment segment) => segment.Info.m_netAI is RoadAI & segment.Info.m_hasPedestrianLanes;

        public static bool CanConnectPath(this NetInfo info) => info.m_netAI is RoadAI & info.m_hasPedestrianLanes;

        internal static bool IsInvert(this ref NetSegment segment) => segment.m_flags.IsFlagSet(NetSegment.Flags.Invert);

        internal static bool IsJunction(this ref NetNode node) => node.m_flags.IsFlagSet(NetNode.Flags.Junction);


        internal static NetInfo.Direction Invert(this NetInfo.Direction direction, bool invert = true) => invert ? NetInfo.InvertDirection(direction) : direction;

        /// <summary>
        /// checks if vehicles move backward or bypass backward (considers LHT)
        /// </summary>
        /// <returns>true if vehicles move backward,
        /// false if vehilces going ward, bi-directional, or non-directional</returns>
        internal static bool IsGoingBackward(this NetInfo.Direction direction) =>
            (direction & NetInfo.Direction.Both) == NetInfo.Direction.Backward ||
            (direction & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;

        internal static bool IsGoingForward(this NetInfo.Direction direction) =>
            (direction & NetInfo.Direction.Both) == NetInfo.Direction.Forward ||
            (direction & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidBackward;

        /// <summary>
        /// checks if vehicles move backward or bypass backward (considers LHT)
        /// </summary>
        /// <returns>true if vehicles move backward,
        /// false if vehilces going ward, bi-directional, or non-directional</returns>
        internal static bool IsGoingBackward(this NetInfo.Lane laneInfo, bool invertDirection = false) => laneInfo.m_finalDirection.Invert(invertDirection).IsGoingForward();

        internal static bool IsGoingForward(this NetInfo.Lane laneInfo, bool invertDirection = false) => laneInfo.m_finalDirection.Invert(invertDirection).IsGoingBackward();

        public static bool IsStartNode(ushort segmentId, ushort nodeId) => segmentId.ToSegment().m_startNode == nodeId;
        public static bool IsStartNode(this ref NetSegment segment, ushort nodeId) => segment.m_startNode == nodeId;
        public static ushort GetSegmentNode(ushort segmentID, bool startNode) => segmentID.ToSegment().GetNode(startNode);
        public static ushort GetNode(this ref NetSegment segment, bool startNode) => startNode ? segment.m_startNode : segment.m_endNode;
        public static bool HasNode(ushort segmentId, ushort nodeId) => segmentId.ToSegment().m_startNode == nodeId || segmentId.ToSegment().m_endNode == nodeId;
        public static ushort GetSharedNode(ushort segmentID1, ushort segmentID2) => segmentID1.ToSegment().GetSharedNode(segmentID2);
        public static bool IsSegmentValid(ushort segmentId) => segmentId != 0 && segmentId.ToSegment().IsValid();
        public static bool IsValid(this ref NetSegment segment) => segment.Info != null ? segment.m_flags.CheckFlags(required: NetSegment.Flags.Created, forbidden: NetSegment.Flags.Deleted) : false;

        public static bool IsValid(this InstanceID instance)
        {
            if (instance.IsEmpty)
                return false;
            if (instance.Type == InstanceType.NetNode)
                return IsNodeValid(instance.NetNode);
            if (instance.Type == InstanceType.NetSegment)
                return IsSegmentValid(instance.NetSegment);
            if (instance.Type == InstanceType.NetLane)
                return IsLaneValid(instance.NetLane);
            return true;
        }


        public static void AssertSegmentValid(ushort segmentId)
        {
            Assertion.AssertNeq(segmentId, 0, "segmentId");
            Assertion.AssertNotNull(segmentId.ToSegment().Info, $"segment:{segmentId} info");
            var flags = segmentId.ToSegment().m_flags;
            var goodFlags = flags.CheckFlags(required: NetSegment.Flags.Created, forbidden: NetSegment.Flags.Deleted);
            Assertion.Assert(goodFlags, $"segment {segmentId} {segmentId.ToSegment().Info} has bad flags: {flags}");
        }


        public static bool IsNodeValid(ushort nodeId)
        {
            if (nodeId == 0)
                return false;
            return nodeId.ToNode().IsValid();
        }

        public static bool IsValid(this ref NetNode node) => node.Info == null ? false : node.m_flags.CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.Deleted);

        public static bool IsLaneValid(uint laneId) => laneId != 0 ? laneId.ToLane().Flags().CheckFlags(required: NetLane.Flags.Created, forbidden: NetLane.Flags.Deleted) : false;

        public static ushort GetHeadNode(ref NetSegment segment)
        {
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            return invert ^ LHT ? segment.m_startNode : segment.m_endNode;
        }

        public static ushort GetHeadNode(ushort segmentId) =>
            GetHeadNode(ref segmentId.ToSegment());

        public static ushort GetTailNode(ref NetSegment segment)
        {
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            return invert ^ LHT ? segment.m_startNode : segment.m_endNode;
        }

        public static ushort GetTailNode(ushort segmentId) =>
            GetTailNode(ref segmentId.ToSegment());

        public static bool CalculateIsOneWay(ushort segmentId)
        {
            int forward = 0;
            int backward = 0;
            segmentId.ToSegment().CountLanes(
                segmentId,
                NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle,
                VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Train |
                VehicleInfo.VehicleType.Tram | VehicleInfo.VehicleType.Metro |
                VehicleInfo.VehicleType.Monorail,
                ref forward,
                ref backward);
            return (forward == 0) ^ (backward == 0);
        }

        #endregion

        public struct NodeSegments
        {
            public ushort[] segments;
            public int count;
            void Add(ushort segmentID) => segments[count++] = segmentID;

            public NodeSegments(ushort nodeID)
            {
                segments = new ushort[8];
                count = 0;

                ushort segmentID = GetFirstSegment(nodeID);
                Add(segmentID);
                while (true)
                {
                    segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                    if (segmentID == segments[0])
                        break;
                    else
                        Add(segmentID);
                }
            }
        }

        /// <summary>
        /// returns a counter-clockwise list of segments of the given node ID.
        /// </summary>
        public static IEnumerable<ushort> GetCCSegList(ushort nodeID)
        {
            ushort segmentID0 = GetFirstSegment(nodeID);
            Assertion.Assert(segmentID0 != 0, "GetFirstSegment!=0");
            yield return segmentID0;
            ushort segmentID = segmentID0;

            // add the rest of the segments.
            while (true)
            {
                segmentID = segmentID.ToSegment().GetRightSegment(nodeID);
                if ((segmentID == 0) | (segmentID == segmentID0))
                    yield break;
                else
                    yield return segmentID;
            }
        }

        /// <summary>
        /// returns a clock-wise list of segments of the given node ID.
        /// </summary>
        public static IEnumerable<ushort> GetCWSegList(ushort nodeID)
        {
            ushort segmentID0 = GetFirstSegment(nodeID);
            Assertion.Assert(segmentID0 != 0, "GetFirstSegment!=0");
            yield return segmentID0;
            ushort segmentID = segmentID0;

            // add the rest of the segments.
            while (true)
            {
                segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                if ((segmentID == 0) | (segmentID == segmentID0))
                    yield break;
                else
                    yield return segmentID;
            }
        }

        public static IEnumerable<ushort> IterateNodeSegments(ushort nodeID)
        {
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentID = nodeID.ToNode().GetSegment(i);
                if (segmentID != 0)
                    yield return segmentID;
            }
        }

        public static NodeSegmentIterator IterateSegments(this ref NetNode node) => new NodeSegmentIterator(node.GetID());

        public static ushort GetAnotherSegment(this ref NetNode node, ushort segmentId0)
        {
            for (int i = 0; i < 8; ++i)
            {
                ushort segmentId = node.GetSegment(i);
                if (segmentId != segmentId0 && segmentId != 0)
                    return segmentId;
            }
            return 0;
        }

        public static IEnumerable<uint> IterateNodeLanes(ushort nodeId)
        {
            int idx = 0;
            if (nodeId.ToNode().Info == null)
            {
                Mod.Logger.Error("null info: potentially caused by missing assets");
                yield break;
            }
            for (uint laneID = nodeId.ToNode().m_lane;
                laneID != 0;
                laneID = laneID.ToLane().m_nextLane, idx++)
            {
                yield return laneID;
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

        public static IEnumerable<LaneData> IterateLanes(
         ushort segmentId,
         bool? startNode = null,
         NetInfo.LaneType laneType = NetInfo.LaneType.All,
         VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All)
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


        public static NetInfo.Lane SortedLane(this NetInfo info, int index)
        {
            int sortedIndex = info.m_sortedLanes[index];
            return info.m_lanes[sortedIndex];
        }

        public static IEnumerable<NetInfo.Lane> SortedLanes(this NetInfo info)
        {
            for (int i = 0; i < info.m_sortedLanes.Length; ++i)
            {
                int sortedIndex = info.m_sortedLanes[i];
                yield return info.m_lanes[sortedIndex];
            }
        }

        /// <summary>
        /// sorted from outer lane to inner lane when heading toward <paramref name="startNode"/>
        /// </summary>
        /// <param name="segmentId"></param>
        /// <param name="startNode"></param>
        /// <param name="laneType"></param>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        public static LaneData[] GetSortedLanes(
            ushort segmentId,
            bool? startNode,
            NetInfo.LaneType laneType = NetInfo.LaneType.All,
            VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All)
        {
            var lanes = IterateLanes(
                segmentId: segmentId,
                startNode: startNode,
                laneType: laneType,
                vehicleType: vehicleType).ToArray();

            LaneData[] ret = new LaneData[lanes.Length];
            for (int i = 0; i < lanes.Length; ++i)
            {
                int j = segmentId.ToSegment().Info.m_sortedLanes[i];
                ret[i] = lanes[j];
            }

            // make sure that the outmost lane is the first lane.
            bool reverse = ret[0].LaneInfo.m_direction == NetInfo.Direction.Backward;
            return reverse ? ret.Reverse().ToArray() : ret;
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
        public static uint GetlaneID(ushort segmentID, int laneIndex)
        {
            uint laneID = segmentID.ToSegment().m_lanes;
            int n = segmentID.ToSegment().Info.m_lanes.Length;
            for (int i = 0; i < n && laneID != 0; i++)
            {
                if (i == laneIndex)
                    return laneID;
                laneID = laneID.ToLane().m_nextLane;
            }
            return 0;
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

        public readonly Bezier3 Bezier => Lane.m_bezier;
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

    public struct LaneIDIterator : IEnumerable<uint>, IEnumerator<uint>
    {
        uint laneID_;
        ushort segmentID_;

        public LaneIDIterator(ushort segmentID)
        {
            segmentID_ = segmentID;
            laneID_ = 0;
        }

        public void Reset() => laneID_ = 0;
        public void Dispose() { }

        public uint Current => laneID_;

        public bool MoveNext()
        {
            if (laneID_ == 0)
            {
                laneID_ = segmentID_.ToSegment().m_lanes;
                return laneID_ != 0;
            }
            uint ret = laneID_.ToLane().m_nextLane;
            if (ret != 0) laneID_ = ret;
            return ret != 0;
        }

        public LaneIDIterator GetEnumerator() => this;
        IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        object IEnumerator.Current => Current;
    }

    public struct NodeSegmentIterator : IEnumerable<ushort>, IEnumerator<ushort>
    {
        int i_;
        ushort segmentId_;
        ushort nodeId_;

        public NodeSegmentIterator(ushort nodeId)
        {
            nodeId_ = nodeId;
            segmentId_ = 0;
            i_ = 0;
        }

        public bool MoveNext()
        {
            while (i_ < 8)
            {
                segmentId_ = nodeId_.ToNode().GetSegment(i_++);
                if (segmentId_ != 0)
                    return true;
            }
            segmentId_ = 0;
            return false;
        }

        public ushort Current => segmentId_;
        public NodeSegmentIterator GetEnumerator() => this;
        public void Reset() => i_ = segmentId_ = 0;
        public void Dispose() => Reset();
        object IEnumerator.Current => Current;
        IEnumerator<ushort> IEnumerable<ushort>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}

