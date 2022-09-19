using ColossalFramework;
using HarmonyLib;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeController
{
    public class Manager : IManager
    {
        private static bool InitialUpdateInProgress = false;
        public static int Errors { get; set; } = 0;
        public static bool HasErrors => Errors != 0;
        public static void SetFailed()
        {
            Errors = -1;
        }

        private NodeData[] Buffer { get; set; }

        public Manager()
        {
            SingletonMod<Mod>.Logger.Debug("Create manager");
            Buffer = new NodeData[NetManager.MAX_NODE_COUNT];
            InitialUpdateInProgress = false;
        }

        private void Clear()
        {
            SingletonMod<Mod>.Logger.Debug("Clear manager");
            Buffer = new NodeData[NetManager.MAX_NODE_COUNT];
            InitialUpdateInProgress = false;
        }
        private NodeData Create(ushort nodeId, Options options, NodeStyleType? nodeType = null)
        {
            try
            {
                var data = new NodeData(nodeId, nodeType);
                Buffer[nodeId] = data;
                Update(options, nodeId);
                return data;
            }
            catch (NodeNotCreatedException)
            {
                return null;
            }
            catch (NodeStyleNotImplementedException)
            {
                return null;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Cant create Node data #{nodeId}", error);
                return null;
            }
        }

        public NodeData GetNodeData(ushort nodeId) => Buffer[nodeId];
        public NodeData GetOrCreateNodeData(ushort nodeId, Options options = Options.Default)
        {
            if (Buffer[nodeId] is not NodeData data)
            {
                if ((options & Options.CreateThis) != 0)
                    data = Create(nodeId, options);
                else
                    data = null;
            }

            return data;
        }

        public bool TryGetNodeData(ushort nodeId, out NodeData data)
        {
            data = Buffer[nodeId];
            return data != null;
        }

        public SegmentEndData GetSegmentData(ushort nodeId, ushort segmentId) => GetNodeData(nodeId)?[segmentId];
        public void GetSegmentData(ushort segmentId, out SegmentEndData start, out SegmentEndData end)
        {
            ref var segment = ref segmentId.GetSegment();
            start = Buffer[segment.m_startNode]?[segmentId];
            end = Buffer[segment.m_endNode]?[segmentId];
        }

        public bool TryGetSegmentData(ushort segmentId, bool isStart, out SegmentEndData data)
        {
            data = Buffer[segmentId.GetSegment().GetNode(isStart)]?[segmentId];
            return data != null;
        }
        public bool TryGetSegmentData(ushort nodeId, ushort segmentId, out SegmentEndData data)
        {
            data = Buffer[nodeId]?[segmentId];
            return data != null;
        }

        public bool ContainsNode(ushort nodeId) => Buffer[nodeId] != null;
        public bool ContainsSegment(ushort segmentId)
        {
            ref var segment = ref segmentId.GetSegment();
            return Buffer[segment.m_startNode] != null || Buffer[segment.m_endNode] != null;
        }

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

        public void InitialUpdate(ushort[] toUpdateIds)
        {
            SingletonMod<Mod>.Logger.Debug("Start initial update");
            InitialUpdateInProgress = true;

            GetUpdateList(toUpdateIds, Options.UpdateThis, out var nodeIds, out var segmentIds);
            UpdateImpl(nodeIds.ToArray(), segmentIds.ToArray());

            SimulationManager.instance.AddAction(() =>
            {
                foreach (var nodeId in nodeIds)
                    NetManager.instance.UpdateNode(nodeId);
            });
        }

        public void Update(ushort nodeId) => Update(Options.UpdateAll, nodeId);
        private void Update(Options options, params ushort[] toUpdateIds)
        {
            if ((options & Options.UpdateAll) != 0)
            {
                GetUpdateList(toUpdateIds, options, out var nodeIds, out _);

                SimulationManager.instance.AddAction(() =>
                {
                    foreach (var nodeId in nodeIds)
                        NetManager.instance.UpdateNode(nodeId);
                });
            }
        }
        private void GetUpdateList(ushort[] toUpdateIds, Options options, out HashSet<ushort> nodeIds, out HashSet<ushort> segmentIds)
        {
            nodeIds = new HashSet<ushort>();
            segmentIds = new HashSet<ushort>();

            foreach (var nodeId in toUpdateIds)
            {
                if (Buffer[nodeId] == null)
                    continue;

                nodeIds.Add(nodeId);
                var nodeSegmentIds = nodeId.GetNode().SegmentIds().ToArray();
                segmentIds.AddRange(nodeSegmentIds);

                if ((options & Options.UpdateNearby) != 0)
                {
                    foreach (var segmentIs in nodeSegmentIds)
                    {
                        var otherNodeId = segmentIs.GetSegment().GetOtherNode(nodeId);
                        if (GetOrCreateNodeData(otherNodeId, options) != null)
                            nodeIds.Add(otherNodeId);
                    }
                }
            }
        }

        private void UpdateImpl(ushort[] nodeIds, ushort[] segmentIds)
        {
            if (nodeIds.Length == 0)
                return;

#if DEBUG
            var id = DateTime.Now.Millisecond;
            var sw = Stopwatch.StartNew();

            SingletonMod<Mod>.Logger.Debug($"Update {id}\nNodes:{string.Join(", ", nodeIds.Select(i => i.ToString()).ToArray())}\nSegments:{string.Join(", ", segmentIds.Select(i => i.ToString()).ToArray())}");
#endif
            var manager = SingletonManager<Manager>.Instance;

            foreach (var nodeId in nodeIds)
                manager.Buffer[nodeId].EarlyUpdate();

#if DEBUG
            var updateDone = sw.ElapsedTicks;
#endif

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateBeziers(segmentId);

#if DEBUG
            var bezierDone = sw.ElapsedTicks;
#endif

            foreach (var nodeId in nodeIds)
                SegmentEndData.UpdateMinLimits(manager.Buffer[nodeId]);

#if DEBUG
            var minDone = sw.ElapsedTicks;
#endif

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateMaxLimits(segmentId);

#if DEBUG
            var maxDone = sw.ElapsedTicks;
#endif

            foreach (var nodeId in nodeIds)
                manager.Buffer[nodeId].LateUpdate();

#if DEBUG
            var lateUpdateDone = sw.ElapsedTicks;

            SingletonMod<Mod>.Logger.Debug($"Update {id} finish in {sw.ElapsedTicks}; Update={updateDone} Bezier={bezierDone - updateDone} Min={minDone - bezierDone} Max={maxDone - minDone} Late={lateUpdateDone - maxDone}");
#endif
        }

        public static void SimulationStep()
        {
            if (InitialUpdateInProgress)
            {
                InitialUpdateInProgress = false;
                SingletonMod<Mod>.Logger.Debug("Finish initial update");
            }
            else
            {
                var manager = SingletonManager<Manager>.Instance;
                var nodeIds = NetManager.instance.GetUpdateNodes().Where(s => manager.ContainsNode(s)).ToArray();
                var segmentIds = NetManager.instance.GetUpdateSegments().Where(s => manager.ContainsSegment(s)).ToArray();

                SingletonManager<Manager>.Instance.UpdateImpl(nodeIds, segmentIds);
            }
        }
        public static void ReleaseNodeImplementationPrefix(ushort node) => SingletonManager<Manager>.Instance.Buffer[node] = null;

        public XElement ToXml()
        {
            var config = new XElement(nameof(NodeController));
            config.AddAttr("V", SingletonMod<Mod>.Version);

            Errors = 0;

            foreach (var data in Buffer)
            {
                if (data != null)
                    config.Add(data.ToXml());
            }

            return config;
        }
        public void FromXml(XElement config, NetObjectsMap map)
        {
            Errors = 0;

            var toUpdate = new List<ushort>();

            foreach (var nodeConfig in config.Elements(NodeData.XmlName))
            {
                var id = nodeConfig.GetAttrValue(nameof(NodeData.Id), (ushort)0);

                if (map.TryGetNode(id, out var targetId))
                    id = targetId;

                if (id != 0 && id <= NetManager.MAX_NODE_COUNT)
                {
                    try
                    {
                        if ((id.GetNode().flags & (NetNode.FlagsLong.Created | NetNode.FlagsLong.Deleted)) != NetNode.FlagsLong.Created)
                        {
                            SingletonMod<Mod>.Logger.Error($"Can't load Node data #{id}: Node is not created");
                            Errors += 1;
                            continue;
                        }

                        var type = (NodeStyleType)nodeConfig.GetAttrValue("T", (int)NodeStyleType.Custom);
                        var data = new NodeData(id, type);
                        data.FromXml(nodeConfig, map);
                        Buffer[data.Id] = data;

                        toUpdate.Add(data.Id);
                    }
                    catch (NodeNotCreatedException error)
                    {
                        SingletonMod<Mod>.Logger.Error($"Can't load Node data #{id}: {error.Message}");
                        Errors += 1;
                    }
                    catch (NodeStyleNotImplementedException error)
                    {
                        SingletonMod<Mod>.Logger.Error($"Can't load Node data #{id}: {error.Message}");
                        Errors += 1;
                    }
                    catch (Exception error)
                    {
                        SingletonMod<Mod>.Logger.Error($"Can't load Node data #{id}", error);
                        Errors += 1;
                    }
                }
            }

            InitialUpdate(toUpdate.ToArray());
        }

        [Flags]
        public enum Options
        {
            None = 0,

            CreateThis = 1,
            UpdateThis = 2,
            UpdateNearby = 4,

            UpdateAll = UpdateThis | UpdateNearby,
            Default = CreateThis | UpdateAll,
        }
    }

    public class NodeNotCreatedException : Exception
    {
        public ushort Id { get; }

        public NodeNotCreatedException(ushort id) : base($"Node #{id} not created")
        {
            Id = id;
        }
    }
    public class NodeStyleNotImplementedException : NotImplementedException
    {
        public ushort Id { get; }
        public NetNode.Flags Flags { get; }

        public NodeStyleNotImplementedException(ushort id, NetNode.Flags flags) : base($"Node #{id} style {flags} not implemented")
        {
            Id = id;
            Flags = flags;
        }
    }
}
