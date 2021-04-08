namespace NodeController
{
    using KianCommons;
    using System;
    using KianCommons.Serialization;
    using NodeController;
    using ModsCommon;

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
        /// <param name="nodeID">target nodeID</param>
        public static void PasteNodeData(ushort nodeID, byte[] data) => Instance.PasteNodeDataImp(nodeID, data);

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeID">target nodeID</param>
        private void PasteNodeDataImp(ushort nodeID, byte[] data)
        {
            SingletonMod<Mod>.Logger.Debug($"NodeManager.PasteNodeDataImp(nodeID={nodeID}, data={data})");
            if (data == null)
            {
                // for backward compatibality reasons its not a good idea to do this:
                // ResetNodeToDefault(nodeID); 
            }
            else
            {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                    SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: nodeID);

                TargetNodeId = nodeID; // must be done before deserialization.
                buffer[nodeID] = SerializationUtil.Deserialize(data, this.VersionOf()) as NodeData;
                buffer[nodeID].NodeId = nodeID;
                UpdateData(nodeID);
                TargetNodeId = 0;
            }
        }
        #endregion

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeTypeT nodeType = NodeTypeT.Crossing)
        {
            if (ToolBase.ToolErrors.None != NetUtil.InsertNode(controlPoint, out ushort nodeID))
                return null;

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
            {
                var segEnd = new SegmentEndData(segmentID, nodeID);
                SegmentEndManager.Instance.SetAt(segmentID, nodeID, segEnd);
            }

            var info = controlPoint.m_segment.ToSegment().Info;
            int nPedLanes = info.CountPedestrianLanes();
            bool isRoad = info.m_netAI is RoadBaseAI;
            if (nodeType == NodeTypeT.Crossing && (nPedLanes < 2 || !isRoad))
                buffer[nodeID] = new NodeData(nodeID);
            else
                buffer[nodeID] = new NodeData(nodeID, nodeType);

            return buffer[nodeID];
        }

        public NodeData GetOrCreate(ushort nodeID)
        {
            if (Instance.buffer[nodeID] is not NodeData data)
            {
                data = new NodeData(nodeID);
                buffer[nodeID] = data;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                SegmentEndManager.Instance.GetOrCreate(segmentID, nodeID);

            return data;
        }

        /// <summary>
        /// Calls update node. releases data for <paramref name="nodeID"/> if uncessary. 
        /// </summary>
        /// <param name="nodeID"></param>
        public void UpdateData(ushort nodeID)
        {
            if (nodeID == 0 || buffer[nodeID] == null)
                return;

            bool selected = SingletonTool<NodeControllerTool>.Instance.Data is NodeData nodeData && nodeData.NodeId == nodeID;
            if (buffer[nodeID].IsDefault && !selected)
                ResetNodeToDefault(nodeID);
            else
            {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                {
                    var segEnd = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
                    segEnd.Update();
                }
                buffer[nodeID].Update();
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

        /// <summary>
        /// Called after stock code and before postfix code.
        /// if node is invalid or otherwise unsupported, it will be set to null.
        /// </summary>
        public void OnBeforeCalculateNodePatch(ushort nodeID)
        {
            // nodeID.ToNode still has default flags.
            if (buffer[nodeID] == null) return;

            if (!NetUtil.IsNodeValid(nodeID) || !NodeData.IsSupported(nodeID))
            {
                SetNullNodeAndSegmentEnds(nodeID);
                return;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
            {
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: nodeID);
                segEnd.Calculate();
            }

            buffer[nodeID].Calculate();

            if (!buffer[nodeID].CanChangeTo(buffer[nodeID].NodeType))
                ResetNodeToDefault(nodeID);
        }

        public void SetNullNodeAndSegmentEnds(ushort nodeID)
        {
            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                SegmentEndManager.Instance.SetAt(segmentID, nodeID, null);

            buffer[nodeID] = null;
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

        /// <param name="showError1">set true to display error panel before healing</param>
        public static void ValidateAndHeal(bool showError1)
        {
            Instance.Heal();
            SegmentEndManager.Instance.Heal();
        }
    }
}
