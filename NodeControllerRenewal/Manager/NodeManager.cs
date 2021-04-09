namespace NodeController
{
    using KianCommons;
    using System;
    using KianCommons.Serialization;
    using NodeController;
    using ModsCommon;
    using ModsCommon.Utilities;

    [Serializable]
    public class NodeManager
    {
        #region LIFECYCLE
        public static NodeManager Instance { get; private set; } = new NodeManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data, Version version)
        {
            if (data == null)
            {
                Instance = new NodeManager();
                SingletonMod<Mod>.Logger.Debug($"NodeBlendManager.Deserialize(data=null)");

            }
            else
            {
                SingletonMod<Mod>.Logger.Debug($"NodeBlendManager.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data, version) as NodeManager;
            }
        }

        public void OnLoad()
        {
            UpdateAll();
        }

        #endregion LifeCycle

        public NodeData[] buffer = new NodeData[NetManager.MAX_NODE_COUNT];

        #region MOVEIT BACKWARD COMPATIBLITY

        [Obsolete("delete when moveit is updated")]
        public static byte[] CopyNodeData(ushort nodeID) => SerializationUtil.Serialize(Instance.buffer[nodeID]);

        public static ushort TargetNodeId = 0;

        [Obsolete("kept here for backward compatibility with MoveIT")]
        public static void PasteNodeData(ushort nodeID, byte[] data) => Instance.PasteNodeDataImp(nodeID, data);

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeId">target nodeID</param>
        private void PasteNodeDataImp(ushort nodeId, byte[] data)
        {
            SingletonMod<Mod>.Logger.Debug($"NodeManager.PasteNodeDataImp(nodeID={nodeId}, data={data})");
            if (data == null)
            {
                // for backward compatibality reasons its not a good idea to do this:
                // ResetNodeToDefault(nodeID); 
            }
            else
            {
                foreach (var segmentId in nodeId.GetNode().SegmentsId())
                    _ = SegmentEndManager.Instance[segmentId, nodeId, true];

                TargetNodeId = nodeId; // must be done before deserialization.
                buffer[nodeId] = SerializationUtil.Deserialize(data, this.VersionOf()) as NodeData;
                buffer[nodeId].NodeId = nodeId;
                UpdateData(nodeId);
                TargetNodeId = 0;
            }
        }
        #endregion

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeTypeT nodeType = NodeTypeT.Crossing)
        {
            if (ToolBase.ToolErrors.None != NetUtil.InsertNode(controlPoint, out ushort nodeId))
                return null;

            foreach (var segmentId in nodeId.GetNode().SegmentsId())
            {
                var segEnd = new SegmentEndData(segmentId, nodeId);
                SegmentEndManager.Instance[segmentId, nodeId] = segEnd;
            }

            var info = controlPoint.m_segment.ToSegment().Info;
            if (nodeType == NodeTypeT.Crossing && (info.CountPedestrianLanes() < 2 || info.m_netAI is not RoadBaseAI))
                buffer[nodeId] = new NodeData(nodeId);
            else
                buffer[nodeId] = new NodeData(nodeId, nodeType);

            return buffer[nodeId];
        }

        public NodeData GetOrCreate(ushort nodeId)
        {
            if (Instance.buffer[nodeId] is not NodeData data)
            {
                data = new NodeData(nodeId);
                buffer[nodeId] = data;
            }

            foreach (var segmentId in nodeId.GetNode().SegmentsId())
                _ = SegmentEndManager.Instance[segmentId, nodeId, true];

            return data;
        }

        public void UpdateData(ushort nodeId)
        {
            if (nodeId == 0 || buffer[nodeId] == null)
                return;

            bool selected = SingletonTool<NodeControllerTool>.Instance.Data is NodeData nodeData && nodeData.NodeId == nodeId;
            if (buffer[nodeId].IsDefault && !selected)
                ResetNodeToDefault(nodeId);
            else
            {
                foreach (var segmentId in nodeId.GetNode().SegmentsId())
                {
                    var segEnd = SegmentEndManager.Instance[segmentId, nodeId];
                    segEnd.Update();
                }
                buffer[nodeId].Update();
            }
        }

        public void ResetNodeToDefault(ushort nodeID)
        {
            if (buffer[nodeID] != null)
                SingletonMod<Mod>.Logger.Debug($"node:{nodeID} reset to defualt");
            else
                SingletonMod<Mod>.Logger.Debug($"node:{nodeID} is alreadey null. no need to reset to default");

            SetNullNodeAndSegmentEnds(nodeID);

            NetManager.instance.UpdateNode(nodeID);
        }

        public void UpdateAll()
        {
            foreach (var nodeData in buffer)
            {
                if (nodeData == null)
                    continue;
                if (NetUtil.IsNodeValid(nodeData.NodeId))
                    nodeData.Update();
                else
                    ResetNodeToDefault(nodeData.NodeId);
            }
        }
        public void OnBeforeCalculateNodePatch(ushort nodeId)
        {
            if (buffer[nodeId] == null) return;

            if (!NetUtil.IsNodeValid(nodeId) || !NodeData.IsSupported(nodeId))
            {
                SetNullNodeAndSegmentEnds(nodeId);
                return;
            }

            foreach (var segmentId in nodeId.GetNode().SegmentsId())
            {
                var segEnd = SegmentEndManager.Instance[segmentId, nodeId, true];
                segEnd.Calculate();
            }

            buffer[nodeId].Calculate();

            if (!buffer[nodeId].CanChangeTo(buffer[nodeId].NodeType))
                ResetNodeToDefault(nodeId);
        }

        public void SetNullNodeAndSegmentEnds(ushort nodeId)
        {
            foreach (var segmentID in nodeId.GetNode().SegmentsId())
                SegmentEndManager.Instance[segmentID, nodeId] = null;

            buffer[nodeId] = null;
        }

        public void Heal()
        {
            SingletonMod<Mod>.Logger.Debug("NodeManager.Validate() heal");
            buffer[0] = null;
            for (ushort nodeId = 1; nodeId < buffer.Length; ++nodeId)
            {
                if (buffer[nodeId] is not NodeData)
                    continue;

                if (!NetUtil.IsNodeValid(nodeId))
                {
                    SetNullNodeAndSegmentEnds(nodeId);
                    continue;
                }

                if (buffer[nodeId].NodeId != nodeId)
                    buffer[nodeId].NodeId = nodeId;
            }
        }

        public static void ValidateAndHeal()
        {
            Instance.Heal();
            SegmentEndManager.Instance.Heal();
        }
    }
}
