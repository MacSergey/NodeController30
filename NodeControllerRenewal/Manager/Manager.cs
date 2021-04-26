using HarmonyLib;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

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
                nodeId.GetNode().m_flags |= NetNode.Flags.Middle | NetNode.Flags.Moveable;

            var info = controlPoint.m_segment.GetSegment().Info;
            var newNodeType = nodeType == NodeStyleType.Crossing && (info.m_netAI is not RoadBaseAI || info.PedestrianLanes() < 2) ? null : (NodeStyleType?)nodeType;
            var data = Create(nodeId, Options.Default, newNodeType);
            return data;
        }
        public NodeData this[ushort nodeId, bool create = false] => this[nodeId, create ? Options.Default : Options.None];
        private NodeData this[ushort nodeId, Options options = Options.None]
        {
            get
            {
                if (Buffer[nodeId] is not NodeData data)
                    data = options.IsSet(Options.CreateThis) ? Create(nodeId, options) : null;

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
            ref var segment = ref segmentId.GetSegment();
            start = Buffer[segment.m_startNode]?[segmentId];
            end = Buffer[segment.m_endNode]?[segmentId];
        }

        public bool ContainsNode(ushort nodeId) => Buffer[nodeId] != null;
        public bool ContainsSegment(ushort segmentId)
        {
            ref var segment = ref segmentId.GetSegment();
            return Buffer[segment.m_startNode] != null || Buffer[segment.m_endNode] != null;
        }
        public SegmentEndData GetSegmentData(ushort id, bool isStart)
        {
            ref var segment = ref id.GetSegment();
            return Buffer[segment.GetNode(isStart)]?[id];
        }


        public void Update(ushort nodeId, bool now = false) => Update(nodeId, Options.This | Options.Nearby | (now ? Options.UpdateNow : Options.Update));
        private void Update(ushort nodeId, Options options)
        {
            if (options.IsSet(Options.Update))
            {
                if (options.IsSet(Options.UpdateNow))
                {
                    GetUpdateList(nodeId, options & ~Options.Update, out var nodeIds, out var segmentIds);
                    UpdateNow(nodeIds.ToArray(), segmentIds.ToArray(), false);
                }
                if(options.IsSet(Options.Update))
                {
                    GetUpdateList(nodeId, options & ~Options.UpdateNow, out var nodeIds, out _);
                    AddToUpdate(nodeIds);
                }
            }
        }
        private void AddToUpdate(HashSet<ushort> nodeIds)
        {
            foreach (var nodeId in nodeIds)
                NetManager.instance.UpdateNode(nodeId);
        }

        private void GetUpdateList(ushort nodeId, Options options, out HashSet<ushort> nodeIds, out HashSet<ushort> segmentIds)
        {
            nodeIds = new HashSet<ushort>();
            segmentIds = new HashSet<ushort>();

            if (Buffer[nodeId] == null)
                return;

            nodeIds.Add(nodeId);
            var nodeSegmentIds = nodeId.GetNode().SegmentIds().ToArray();
            segmentIds.AddRange(nodeSegmentIds);

            if ((options & (Options.UpdateNearby | Options.UpdateNearbyNow)) != 0)
            {
                foreach (var segmentIs in nodeSegmentIds)
                {
                    var otherNodeId = segmentIs.GetSegment().GetOtherNode(nodeId);
                    if (this[otherNodeId, options & Options.CreateNearby] != null)
                        nodeIds.Add(otherNodeId);
                }
            }
        }

        public static void SimulationStep()
        {
            var nodeIds = NetManager.instance.GetUpdateNodes().Where(s => SingletonManager<Manager>.Instance.ContainsNode(s)).ToArray();
            var segmentIds = NetManager.instance.GetUpdateSegments().Where(s => SingletonManager<Manager>.Instance.ContainsSegment(s)).ToArray();

            UpdateNow(nodeIds, segmentIds, true);
        }
        private static void UpdateNow(ushort[] nodeIds, ushort[] segmentIds, bool flags)
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
        public void FromXml(XElement config, ObjectsMap map)
        {
            foreach (var nodeConfig in config.Elements(NodeData.XmlName))
            {
                if (NodeData.FromXml(nodeConfig, map, out NodeData data))
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
            None = 0,

            This = 1,
            Nearby = 2,

            Create = 4,
            Update = 8,
            UpdateNow = 16,
            UpdateAll = Update | UpdateNow,

            CreateThis = This | Create,
            UpdateThis = This | Update,
            UpdateThisNow = This | UpdateNow,

            CreateNearby = Nearby | Create,
            UpdateNearby = Nearby | Update,
            UpdateNearbyNow = Nearby | UpdateNow,

            Default = CreateThis | UpdateThis | UpdateThisNow | CreateNearby | UpdateNearby,
        }
    }
}
