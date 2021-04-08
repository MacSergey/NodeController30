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

        private SegmentEndData[] Buffer { get; } = new SegmentEndData[NetManager.MAX_SEGMENT_COUNT * 2];

        private int GetIndex(ushort segmentID, bool startNode) => segmentID * 2 + (startNode ? 0 : 1);
        public SegmentEndData GetAt(ushort segmentID, ushort nodeID)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return GetAt(segmentID, startNode);
        }
        public SegmentEndData GetAt(ushort segmentID, bool startNode) => Buffer[GetIndex(segmentID, startNode)];

        public void SetAt(ushort segmentID, ushort nodeID, SegmentEndData data)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            SetAt(segmentID, startNode, data);
        }

        public void SetAt(ushort segmentID, bool startNode, SegmentEndData data) => Buffer[GetIndex(segmentID, startNode)] = data;

        public SegmentEndData GetOrCreate(ushort segmentID, ushort nodeID)
        {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return GetOrCreate(segmentID, startNode);
        }

        public SegmentEndData GetOrCreate(ushort segmentID, bool startNode)
        {
            if(GetAt(segmentID, startNode) is not SegmentEndData data)
            {
                ushort nodeID = NetUtil.GetSegmentNode(segmentID, startNode);
                data = new SegmentEndData(segmentID, nodeID);
                SetAt(segmentID, startNode, data);
            }
            return data;
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
            foreach (var segmentEndData in Buffer)
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
            Buffer[0] = Buffer[1] = null;
            for (int i = 1; i < Buffer.Length; ++i)
            {
                if(Buffer[i] is not SegmentEndData data)
                    continue;

                bool startNode = i % 2 == 0;
                ushort segmentID = (ushort)Mathf.FloorToInt(i / 2);
                ushort nodeID = segmentID.ToSegment().GetNode(startNode);

                if (!NetUtil.IsNodeValid(nodeID) || !NetUtil.IsSegmentValid(segmentID))
                {
                    Buffer[i] = null;
                    continue;
                }
                if (data.NodeId != nodeID)
                    data.NodeId = nodeID;
                if (data.SegmentId != segmentID)
                    data.SegmentId = segmentID;

                if (data.IsStartNode != startNode)
                    Buffer[i] = null;
            }
        }
    }
}