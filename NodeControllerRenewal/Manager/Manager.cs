namespace NodeController
{
    using KianCommons;
    using System;
    using KianCommons.Serialization;
    using NodeController;
    using ModsCommon;
    using ModsCommon.Utilities;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class Manager
    {
        #region LIFECYCLE
        public static Manager Instance { get; private set; } = new Manager();

        #endregion LifeCycle

        private NodeData[] Buffer { get; } = new NodeData[NetManager.MAX_NODE_COUNT];
        protected HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeStyleType nodeType = NodeStyleType.Crossing)
        {
            if (NetUtil.InsertNode(controlPoint, out ushort nodeId) != ToolBase.ToolErrors.None)
                return null;

            var info = controlPoint.m_segment.GetSegment().Info;
            var data = (nodeType == NodeStyleType.Crossing && info.m_netAI is RoadBaseAI && info.CountPedestrianLanes() >= 2) ? new NodeData(nodeId, nodeType) : new NodeData(nodeId);
            Buffer[nodeId] = data;
            return data;
        }
        public NodeData this[ushort nodeId, bool create = false]
        {
            get
            {
                if (Instance.Buffer[nodeId] is not NodeData data)
                {
                    if (create)
                    {
                        data = new NodeData(nodeId);
                        Buffer[nodeId] = data;
                    }
                    else
                        data = null;
                }
                return data;
            }
        }
        public SegmentEndData this[ushort nodeId, ushort segmentId, bool create = false] => this[nodeId, create] is NodeData data ? data[segmentId] : null;

        public void RemoveNode(ushort nodeId) => Buffer[nodeId] = null;

        public static void GetSegmentData(ushort id, out SegmentEndData start, out SegmentEndData end)
        {
            var segment = id.GetSegment();
            start = Instance[segment.m_startNode]?[id];
            end = Instance[segment.m_endNode]?[id];
        }
        public static SegmentEndData GetSegmentData(ushort id, bool isStart)
        {
            var segment = id.GetSegment();
            return Instance[isStart ? segment.m_startNode : segment.m_endNode]?[id];
        }

        public void AddToUpdate(ushort nodeId)
        {
            if (Buffer[nodeId] != null)
                NeedUpdate.Add(nodeId);
        }
        public void Update()
        {
            var needUpdate = NeedUpdate.ToArray();
            NeedUpdate.Clear();
            foreach (var nodeId in needUpdate)
            {
                if (Buffer[nodeId] is NodeData data)
                    data.Update();
            }
        }
    }
}
