using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeController
{
    public class Manager : IManager
    {
        private NodeData[] Buffer { get; } = new NodeData[NetManager.MAX_NODE_COUNT];

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeStyleType nodeType = NodeStyleType.Crossing)
        {
            if (NetTool.CreateNode(controlPoint.m_segment.GetSegment().Info, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out var nodeId, out _, out _, out _) != ToolBase.ToolErrors.None)
                return null;
            else
                nodeId.GetNodeRef().m_flags |= NetNode.Flags.Middle | NetNode.Flags.Moveable;

            var info = controlPoint.m_segment.GetSegment().Info;
            var data = (nodeType == NodeStyleType.Crossing && info.m_netAI is RoadBaseAI && info.PedestrianLanes() >= 2) ? Create(nodeId, nodeType: nodeType) : Create(nodeId);
            return data;
        }
        public NodeData this[ushort nodeId, bool create = false] => this[nodeId, create ? Options.All : Options.NotCreate];
        private NodeData this[ushort nodeId, Options options = Options.NotCreate]
        {
            get
            {
                if (Buffer[nodeId] is not NodeData data)
                    data = (options & Options.Create) != 0 ? Create(nodeId, options) : null;

                return data;
            }
        }
        public SegmentEndData this[ushort nodeId, ushort segmentId, bool create = false] => this[nodeId, create] is NodeData data ? data[segmentId] : null;
        private NodeData Create(ushort nodeId, Options options = Options.Create, NodeStyleType? nodeType = null)
        {
            try
            {
                var data = new NodeData(nodeId, nodeType);
                Buffer[nodeId] = data;
                Update(nodeId, options);
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
        public bool ContainsNode(ushort nodeId) => Buffer[nodeId] != null;
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


        public void Update(ushort nodeId, bool now = false) => Update(nodeId, Options.IncludeNearby | (now ? Options.UpdateNow : Options.Update));
        private void Update(ushort nodeId, Options options)
        {
            if ((options & Options.Update) == Options.Update)
            {
                GetUpdateList(nodeId, (options & Options.IncludeNearby) == Options.IncludeNearby, out var nodeIds, out var segmentIds);

                AddToUpdate(nodeIds, segmentIds);
                if ((options & Options.UpdateNow) == Options.UpdateNow)
                    UpdateNow(nodeIds, segmentIds, false);
            }
        }
        private void AddToUpdate(List<ushort> nodeIds, List<ushort> segmentIds)
        {
            foreach (var nodeId in nodeIds)
                NetManager.instance.UpdateNode(nodeId, 0, 2);
            foreach (var segmentId in segmentIds)
                NetManager.instance.UpdateSegment(segmentId);
        }

        private void GetUpdateList(ushort nodeId, bool includeNearby, out List<ushort> nodeIds, out List<ushort> segmentIds)
        {
            nodeIds = new List<ushort>();
            segmentIds = new List<ushort>();

            if (Buffer[nodeId] == null)
                return;

            nodeIds.Add(nodeId);
            segmentIds.AddRange(nodeId.GetNode().SegmentIds());

            if (includeNearby)
            {
                foreach (var segmentIs in segmentIds)
                {
                    var otherNodeId = segmentIs.GetSegment().GetOtherNode(nodeId);
                    if (this[otherNodeId, Options.Create] != null)
                        nodeIds.Add(otherNodeId);
                }
            }
        }


        public static void SimulationStep()
        {
            var nodeIds = NetManager.instance.GetUpdateNodes().Where(n => SingletonManager<Manager>.Instance.Buffer[n] != null).ToList();
            var segmentIds = NetManager.instance.GetUpdateSegments().Where(s => SingletonManager<Manager>.Instance.ContainsSegment(s)).ToList();

            UpdateNow(nodeIds, segmentIds, true);
        }
        private static void UpdateNow(List<ushort> nodeIds, List<ushort> segmentIds, bool flags)
        {
            foreach (var nodeId in nodeIds)
                SingletonManager<Manager>.Instance.Buffer[nodeId].Update(flags);

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateBeziers(segmentId);

            foreach (var nodeId in nodeIds)
                SegmentEndData.UpdateMinLimits(SingletonManager<Manager>.Instance.Buffer[nodeId]);

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateMaxLimits(segmentId);

            foreach (var nodeId in nodeIds)
                SingletonManager<Manager>.Instance.Buffer[nodeId].LateUpdate();
        }

        public static void ReleaseNodeImplementationPrefix(ushort node) => SingletonManager<Manager>.Instance.Buffer[node] = null;

        public XElement ToXml()
        {
            var config = new XElement(nameof(NodeController));

            config.AddAttr("V", SingletonMod<Mod>.Version);

            foreach (var data in Buffer)
            {
                if (data != null)
                    config.Add(data.ToXml());
            }

            return config;
        }
        public void FromXml(XElement config)
        {
            foreach (var nodeConfig in config.Elements(NodeData.XmlName))
            {
                if (NodeData.FromXml(nodeConfig, out NodeData data))
                    Buffer[data.Id] = data;
            }
            foreach (var data in Buffer)
            {
                if (data != null)
                    Update(data.Id);
            }
        }

        private enum Options
        {
            NotCreate = 0,
            Create = 1,
            IncludeNearby = Create | 2,
            Update = Create | 4,
            UpdateNow = Update | 8,

            All = IncludeNearby | UpdateNow,
        }
    }
}
