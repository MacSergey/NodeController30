using KianCommons;
using System;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModsCommon;

namespace NodeController
{
    [Serializable]
    public class Manager
    {
        public static Manager Instance { get; private set; } = new Manager();

        private NodeData[] Buffer { get; } = new NodeData[NetManager.MAX_NODE_COUNT];

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeStyleType nodeType = NodeStyleType.Crossing)
        {
            if (NetTool.CreateNode(controlPoint.m_segment.GetSegment().Info, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out var nodeId, out _, out _, out _) != ToolBase.ToolErrors.None)
                return null;
            else
                nodeId.GetNodeRef().m_flags |= NetNode.Flags.Middle | NetNode.Flags.Moveable;

            var info = controlPoint.m_segment.GetSegment().Info;
            var data = (nodeType == NodeStyleType.Crossing && info.m_netAI is RoadBaseAI && info.CountPedestrianLanes() >= 2) ? Create(nodeId, nodeType) : Create(nodeId);
            return data;
        }
        public NodeData this[ushort nodeId, bool create = false]
        {
            get
            {
                if (Buffer[nodeId] is not NodeData data)
                    data = create ? Create(nodeId) : null;

                return data;
            }
        }
        public SegmentEndData this[ushort nodeId, ushort segmentId, bool create = false] => this[nodeId, create] is NodeData data ? data[segmentId] : null;
        private NodeData Create(ushort nodeId, NodeStyleType? nodeType = null)
        {
            try
            {
                var data = new NodeData(nodeId, nodeType);
                Buffer[nodeId] = data;
                Update(nodeId, false);
                return data;
            }
            catch (NotImplementedException)
            {
                return null;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error(error);
                return null;
            }
        }
        public void GetSegmentData(ushort segmentId, out SegmentEndData start, out SegmentEndData end)
        {
            var segment = segmentId.GetSegment();
            start = Buffer[segment.m_startNode]?[segmentId];
            end = Buffer[segment.m_endNode]?[segmentId];
        }
        public bool ContainsSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            return Buffer[segment.m_startNode] != null || Buffer[segment.m_endNode] != null;
        }
        public SegmentEndData GetSegmentData(ushort id, bool isStart)
        {
            var segment = id.GetSegment();
            return Buffer[segment.GetNode(isStart)]?[id];
        }

        public void Update(ushort nodeId) => Update(nodeId, true);
        private void Update(ushort nodeId, bool updateNearby)
        {
            NetManager.instance.UpdateNode(nodeId);
            if (updateNearby)
            {
                foreach (var segment in nodeId.GetNode().Segments())
                {
                    var otherNodeId = segment.GetOtherNode(nodeId);
                    _ = this[otherNodeId, true];
                    NetManager.instance.UpdateNode(otherNodeId);
                }
            }
        }

        public static void ReleaseNodeImplementationPrefix(ushort node) => Instance.Buffer[node] = null;
        public static void CalculateNodePostfix(ushort nodeID)
        {
            if (Instance.Buffer[nodeID] is NodeData data)
                data.Update();
        }
        public static void CalculateSegmentPostfix(ushort segmentID)
        {
            if (Instance.ContainsSegment(segmentID))
                SegmentEndData.Update(segmentID);
        }
        public static void UpdateNodePostfix(ushort nodeID)
        {
            if (Instance.Buffer[nodeID] is NodeData data)
            {
                SegmentEndData.Update(data);
                data.UpdatePosition();
            }
        }
    }
}
