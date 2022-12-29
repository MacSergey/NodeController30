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
        private static InitialState InitialUpdateState = InitialState.NotRunning;
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
            InitialUpdateState = InitialState.NotRunning;
        }

        private void Clear()
        {
            SingletonMod<Mod>.Logger.Debug("Clear manager");
            Buffer = new NodeData[NetManager.MAX_NODE_COUNT];
            InitialUpdateState = InitialState.NotRunning;
        }
        public void RemoveAll()
        {
            var nodeIds = Buffer.Where(d => d != null).Select(d => d.Id).ToArray();
            SimulationManager.instance.AddAction(() =>
            {
                foreach (var nodeId in nodeIds)
                    NetManager.instance.UpdateNode(nodeId);
            });
            Clear();
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
        public bool TryGetFinalNodeData(ushort nodeId, out NodeData data)
        {
            if (!TryGetNodeData(nodeId, out data) || (data.State & State.Fail) != 0)
            {
                data = null;
                return false;
            }
            else
                return true;
        }

        public bool TryGetFinalSegmentData(ushort nodeId, ushort segmentId, out SegmentEndData segmentData)
        {
            if (TryGetFinalNodeData(nodeId, out var data))
                data.TryGetSegment(segmentId, out segmentData);
            else
                segmentData = null;

            return segmentData != null;
        }

        public void GetSegmentData(ushort segmentId, out SegmentEndData startData, out SegmentEndData endData)
        {
            ref var segment = ref segmentId.GetSegment();

            if (TryGetNodeData(segment.m_startNode, out var startNodeData))
                startNodeData.TryGetSegment(segmentId, out startData);
            else
                startData = null;

            if (TryGetNodeData(segment.m_endNode, out var endNodeData))
                endNodeData.TryGetSegment(segmentId, out endData);
            else
                endData = null;
        }
        public void GetFinalSegmentData(ushort segmentId, out SegmentEndData startData, out SegmentEndData endData)
        {
            ref var segment = ref segmentId.GetSegment();

            if (TryGetFinalNodeData(segment.m_startNode, out var startNodeData))
                startNodeData.TryGetSegment(segmentId, out startData);
            else
                startData = null;

            if (TryGetFinalNodeData(segment.m_endNode, out var endNodeData))
                endNodeData.TryGetSegment(segmentId, out endData);
            else
                endData = null;
        }

        public bool TryGetSegmentData(ushort segmentId, bool isStart, out SegmentEndData data)
        {
            if (TryGetNodeData(segmentId.GetSegment().GetNode(isStart), out NodeData nodeData))
                nodeData.TryGetSegment(segmentId, out data);
            else
                data = null;

            return data != null;
        }
        public bool TryGetFinalSegmentData(ushort segmentId, bool isStart, out SegmentEndData data)
        {
            if (TryGetFinalNodeData(segmentId.GetSegment().GetNode(isStart), out NodeData nodeData))
                nodeData.TryGetSegment(segmentId, out data);
            else
                data = null;

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

        public void StartInitialUpdate(ushort[] toUpdateIds, bool initial)
        {
            SingletonMod<Mod>.Logger.Debug("Start initial update");
            InitialUpdateState = initial ? InitialState.InProgress : InitialState.NotRunning;
            Update(Options.UpdateThisNow | Options.UpdateThisLater, toUpdateIds);
        }
        public void FinishInitialUpdate()
        {
            SingletonMod<Mod>.Logger.Debug("Finish initial update");
            InitialUpdateState = InitialState.Finished;
        }

        public void Update(ushort nodeId) => Update(Options.UpdateAll, nodeId);
        private void Update(Options options, params ushort[] toUpdateIds)
        {
            if ((options & Options.UpdateThisNow) != 0)
            {
                GetUpdateList(toUpdateIds, options & ~Options.UpdateAllLater, out var nodeIds, out var segmentIds);
                UpdateImpl(nodeIds.ToArray(), segmentIds.ToArray());
            }

            if ((options & Options.UpdateAllLater) != 0)
            {
                GetUpdateList(toUpdateIds, options & ~Options.UpdateThisNow, out var nodeIds, out _);

                if (InitialUpdateState == InitialState.InProgress)
                {
                    foreach (var nodeId in nodeIds)
                        NetManager.instance.UpdateNode(nodeId);
                }
                else
                {
                    SimulationManager.instance.AddAction(() =>
                    {
                        foreach (var nodeId in nodeIds)
                            NetManager.instance.UpdateNode(nodeId);
                    });
                }
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

                if ((options & Options.UpdateNearbyLater) != 0)
                {
                    foreach (var segmentIs in nodeSegmentIds)
                    {
                        var otherNodeId = segmentIs.GetSegment().GetOtherNode(nodeId);
                        if (GetOrCreateNodeData(otherNodeId, options & Options.CreateThis | Options.UpdateThisNow) != null)
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
#if EXTRALOG
            SingletonMod<Mod>.Logger.Debug($"Update #{id} start\nNodes:{string.Join(", ", nodeIds.Select(i => i.ToString()).ToArray())}\nSegments:{string.Join(", ", segmentIds.Select(i => i.ToString()).ToArray())}");
#else
            SingletonMod<Mod>.Logger.Debug($"Update #{id} start\tNodes:{nodeIds.Length}\tSegments:{segmentIds.Length}");
#endif
#endif
            var manager = SingletonManager<Manager>.Instance;

            foreach (var nodeId in nodeIds)
            {
                if (TryGetNodeData(nodeId, out var nodeData))
                    nodeData.EarlyUpdate();
            }

#if DEBUG && EXTRALOG
            var updateDone = sw.ElapsedTicks;
#endif

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateBeziers(segmentId);

#if DEBUG && EXTRALOG
            var bezierDone = sw.ElapsedTicks;
#endif

            foreach (var nodeId in nodeIds)
            {
                if (TryGetNodeData(nodeId, out var nodeData))
                    SegmentEndData.UpdateMinLimits(nodeData);
            }

#if DEBUG && EXTRALOG
            var minDone = sw.ElapsedTicks;
#endif

            foreach (var segmentId in segmentIds)
                SegmentEndData.UpdateMaxLimits(segmentId);

#if DEBUG && EXTRALOG
            var maxDone = sw.ElapsedTicks;
#endif

            foreach (var nodeId in nodeIds)
            {
                if (TryGetNodeData(nodeId, out var nodeData))
                    nodeData.LateUpdate();
            }

#if DEBUG
#if EXTRALOG
            var lateUpdateDone = sw.ElapsedTicks;

            SingletonMod<Mod>.Logger.Debug($"Update #{id} finished after {sw.ElapsedTicks / 10000f}ms; Early={updateDone / 10000f}ms Bezier={(bezierDone - updateDone) / 10000f}ms Min={(minDone - bezierDone) / 10000f}ms Max={(maxDone - minDone) / 10000f}ms Late={(lateUpdateDone - maxDone) / 10000f}ms");
#else
            SingletonMod<Mod>.Logger.Debug($"Update #{id} finished after {sw.ElapsedTicks / 10000f}ms");
#endif
#endif
        }

        public static void SimulationStep()
        {
            if (InitialUpdateState == InitialState.InProgress)
            {
                SingletonMod<Mod>.Logger.Debug($"Initial update is in progress, Simulation step skipped");
                return;
            }
            else if (InitialUpdateState == InitialState.Finished)
            {
                SingletonMod<Mod>.Logger.Debug($"Initial update finished, the first Simulation step skipped");
                InitialUpdateState = InitialState.NotRunning;
                return;
            }
            else
            {
                try
                {
                    var manager = SingletonManager<Manager>.Instance;
                    var nodesToUpdate = NetManager.instance.GetUpdateNodes().Where(s => manager.ContainsNode(s)).ToArray();
                    var segmentsToUpdate = NetManager.instance.GetUpdateSegments().Where(s => manager.ContainsSegment(s)).ToArray();
                    manager.UpdateImpl(nodesToUpdate, segmentsToUpdate);
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Simulation step error", error);
                }
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
        public void FromXml(XElement config, NetObjectsMap map, bool inital)
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

                        if (inital || !ContainsNode(id))
                        {
                            var data = new NodeData(id, type);
                            data.FromXml(nodeConfig, map);
                            Buffer[data.Id] = data;

                            toUpdate.Add(data.Id);
                        }
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

            StartInitialUpdate(toUpdate.ToArray(), inital);
        }
        public void Import(XElement config)
        {
            RemoveAll();
            FromXml(config, new NetObjectsMap(), true);
        }

        [Flags]
        public enum Options
        {
            None = 0,

            CreateThis = 1,

            UpdateThisNow = 2,
            UpdateThisLater = 4,
            UpdateNearbyLater = 8,

            UpdateAllLater = UpdateThisLater | UpdateNearbyLater,
            UpdateAll = UpdateAllLater | UpdateThisNow,

            Default = CreateThis | UpdateAll,
        }

        private enum InitialState
        {
            NotRunning,
            InProgress,
            Finished,
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
