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

        private NodeData[] Buffer { get; } = new NodeData[NetManager.MAX_NODE_COUNT];

        #region MOVEIT BACKWARD COMPATIBLITY

        [Obsolete("delete when moveit is updated")]
        public static byte[] CopyNodeData(ushort nodeID) => SerializationUtil.Serialize(Instance.Buffer[nodeID]);

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
                foreach (var segmentId in nodeId.GetNode().SegmentIds())
                    _ = SegmentEndManager.Instance[segmentId, nodeId, true];

                TargetNodeId = nodeId; // must be done before deserialization.
                Buffer[nodeId] = SerializationUtil.Deserialize(data, this.VersionOf()) as NodeData;
                Buffer[nodeId].NodeId = nodeId;
                UpdateData(nodeId);
                TargetNodeId = 0;
            }
        }
        #endregion

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeStyleType nodeType = NodeStyleType.Crossing)
        {
            if (NetUtil.InsertNode(controlPoint, out ushort nodeId) != ToolBase.ToolErrors.None)
                return null;

            foreach (var segmentId in nodeId.GetNode().SegmentIds())
            {
                var segmentEnd = new SegmentEndData(segmentId, nodeId);
                SegmentEndManager.Instance[segmentId, nodeId] = segmentEnd;
            }

            NodeData data;

            var info = controlPoint.m_segment.GetSegment().Info;
            if (nodeType == NodeStyleType.Crossing && info.m_netAI is RoadBaseAI && info.CountPedestrianLanes() >= 2)
                data = new NodeData(nodeId, nodeType);
            else
                data = new NodeData(nodeId);

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

                        foreach (var segmentId in nodeId.GetNode().SegmentIds())
                            _ = SegmentEndManager.Instance[segmentId, nodeId, true];
                    }
                    else
                        data = null;
                }
                return data;
            }
            set => Instance.Buffer[nodeId] = value;
        }

        public void UpdateData(ushort nodeId)
        {
            if (nodeId == 0 || Buffer[nodeId] == null)
                return;

            bool selected = SingletonTool<NodeControllerTool>.Instance.Data is NodeData nodeData && nodeData.NodeId == nodeId;
            if (Buffer[nodeId].IsDefault && !selected)
                ResetNodeToDefault(nodeId);
            else
            {
                foreach (var segmentId in nodeId.GetNode().SegmentIds())
                {
                    var segmentEnd = SegmentEndManager.Instance[segmentId, nodeId];
                    segmentEnd.Update();
                }
                Buffer[nodeId].Update();
            }
        }

        public void ResetNodeToDefault(ushort nodeID)
        {
            if (Buffer[nodeID] != null)
                SingletonMod<Mod>.Logger.Debug($"node:{nodeID} reset to defualt");
            else
                SingletonMod<Mod>.Logger.Debug($"node:{nodeID} is alreadey null. no need to reset to default");

            SetNullNodeAndSegmentEnds(nodeID);

            NetManager.instance.UpdateNode(nodeID);
        }

        public void UpdateAll()
        {
            foreach (var nodeData in Buffer)
            {
                if (nodeData == null)
                    continue;
                if (nodeData.Node.IsValid())
                    nodeData.Update();
                else
                    ResetNodeToDefault(nodeData.NodeId);
            }
        }
        public void OnBeforeCalculateNodePatch(ushort nodeId)
        {
            if (Buffer[nodeId] == null)
                return;

            var node = nodeId.GetNode();

            if (!node.IsValid() || !NodeData.IsSupported(nodeId))
            {
                SetNullNodeAndSegmentEnds(nodeId);
                return;
            }

            foreach (var segmentId in node.SegmentIds())
            {
                var segmentEnd = SegmentEndManager.Instance[segmentId, nodeId, true];
                segmentEnd.Calculate();
            }

            Buffer[nodeId].Calculate();

            if (!Buffer[nodeId].IsPossibleType(Buffer[nodeId].Type))
                ResetNodeToDefault(nodeId);
        }

        public void SetNullNodeAndSegmentEnds(ushort nodeId)
        {
            foreach (var segmentID in nodeId.GetNode().SegmentIds())
                SegmentEndManager.Instance[segmentID, nodeId] = null;

            Buffer[nodeId] = null;
        }

        public void Heal()
        {
            SingletonMod<Mod>.Logger.Debug("NodeManager.Validate() heal");
            Buffer[0] = null;
            for (ushort nodeId = 1; nodeId < Buffer.Length; ++nodeId)
            {
                if (Buffer[nodeId] is not NodeData)
                    continue;

                if (!nodeId.GetNode().IsValid())
                {
                    SetNullNodeAndSegmentEnds(nodeId);
                    continue;
                }

                if (Buffer[nodeId].NodeId != nodeId)
                    Buffer[nodeId].NodeId = nodeId;
            }
        }

        public static void ValidateAndHeal()
        {
            Instance.Heal();
            SegmentEndManager.Instance.Heal();
        }
    }
}
