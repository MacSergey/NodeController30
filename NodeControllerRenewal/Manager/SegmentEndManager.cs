namespace NodeController
{
    using System;
    using System.Collections.Generic;
    using KianCommons;
    using UnityEngine.Assertions;
    using UnityEngine;
    using KianCommons.Serialization;
    using NodeController;
    using ModsCommon;

    [Serializable]
    public class SegmentEndManager
    {
        #region LIFECYCLE
        public static SegmentEndManager Instance { get; private set; } = new SegmentEndManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data, Version version)
        {
            if (data == null)
            {
                Instance = new SegmentEndManager();
                SingletonMod<Mod>.Logger.Debug($"SegmentEndManager.Deserialize(data=null)");

            }
            else
            {
                SingletonMod<Mod>.Logger.Debug($"SegmentEndManager.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data, version) as SegmentEndManager;
            }
        }

        public void OnLoad()
        {
            UpdateAll();
        }

        #endregion LifeCycle

        public SegmentEndData[] buffer = new SegmentEndData[NetManager.MAX_SEGMENT_COUNT * 2];

        public ref SegmentEndData GetAt(ushort segmentID, ushort nodeID)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetAt(segmentID, startNode);
        }
        public ref SegmentEndData GetAt(ushort segmentID, bool startNode)
        {
            if (startNode)
                return ref buffer[segmentID * 2];
            else
                return ref buffer[segmentID * 2 + 1];
        }

        public void SetAt(ushort segmentID, ushort nodeID, SegmentEndData value)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            SetAt(segmentID, startNode, value);
        }

        public void SetAt(ushort segmentID, bool startNode, SegmentEndData value)
        {
            GetAt(segmentID, startNode) = value;
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, ushort nodeID)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetOrCreate(segmentID, startNode);
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, bool startNode)
        {
            ref SegmentEndData data = ref GetAt(segmentID, startNode);
            if (data == null)
            {
                ushort nodeID = NetUtil.GetSegmentNode(segmentID, startNode);
                data = new SegmentEndData(segmentID: segmentID, nodeID: nodeID);
                SetAt(segmentID: segmentID, startNode: startNode, data);
            }
            return ref data;
        }

        public void ResetSegmentEndToDefault(ushort segmentID, bool startNode)
        {
            SegmentEndData segEnd = GetAt(segmentID, startNode);
            if (segEnd != null)
                SingletonMod<Mod>.Logger.Debug($"segment End:({segmentID},{startNode}) reset to defualt");
            else
                SingletonMod<Mod>.Logger.Debug($"segment End:({segmentID},{startNode}) is already null.");
            SetAt(segmentID, startNode, null);
            NetManager.instance.UpdateSegment(segmentID);
        }

        public void UpdateAll()
        {
            foreach (var segmentEndData in buffer)
            {
                if (segmentEndData == null) 
                    continue;
                if (NetUtil.IsSegmentValid(segmentEndData.SegmentId))
                    segmentEndData.Update();
                else
                {
                    ResetSegmentEndToDefault(segmentEndData.SegmentId, true);
                    ResetSegmentEndToDefault(segmentEndData.SegmentId, false);
                }
            }
        }

        public void Heal()
        {
            SingletonMod<Mod>.Logger.Debug("SegmentEndManager.Heal() called");
            buffer[0] = buffer[1] = null;
            for (int i = 1; i < buffer.Length; ++i)
            {
                ref SegmentEndData data = ref buffer[i];
                if (data == null) 
                    continue;

                bool startNode = i % 2 == 0;
                ushort segmentID = (ushort)Mathf.FloorToInt(i / 2);
                ushort nodeID = segmentID.ToSegment().GetNode(startNode);

                if (!NetUtil.IsNodeValid(nodeID) || !NetUtil.IsSegmentValid(segmentID))
                {
                    buffer[i] = null;
                    continue;
                }
                if (data.NodeId != nodeID)
                    data.NodeId = nodeID;
                if (data.SegmentId != segmentID)
                    data.SegmentId = segmentID;

                if (data.IsStartNode != startNode)
                    buffer[i] = null;
            }
        }
    }
}