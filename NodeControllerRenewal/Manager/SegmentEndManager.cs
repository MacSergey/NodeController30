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

        #endregion

        private SegmentEndData[] Buffer { get; } = new SegmentEndData[NetManager.MAX_SEGMENT_COUNT * 2];

        private int GetIndex(ushort segmentId, bool startNode) => segmentId * 2 + (startNode ? 0 : 1);
        public SegmentEndData this[ushort segmentId, ushort nodeId, bool create = false]
        {
            get
            {
                if (this[segmentId, NetUtil.IsStartNode(segmentId, nodeId)] is not SegmentEndData data)
                {
                    if (create)
                    {
                        data = new SegmentEndData(segmentId, nodeId);
                        this[segmentId, NetUtil.IsStartNode(segmentId, nodeId)] = data;
                    }
                    else
                        data = null;
                }
                return data;
            }
            set => this[segmentId, NetUtil.IsStartNode(segmentId, nodeId)] = value;
        }
        public SegmentEndData this[ushort segmentId, bool startNode]
        {
            get => Buffer[GetIndex(segmentId, startNode)];
            set => Buffer[GetIndex(segmentId, startNode)] = value;
        }
        public void ResetToDefault(ushort segmentId, bool startNode)
        {
            this[segmentId, startNode] = null;
            NetManager.instance.UpdateSegment(segmentId);
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
                    ResetToDefault(segmentEndData.SegmentId, true);
                    ResetToDefault(segmentEndData.SegmentId, false);
                }
            }
        }

        public void Heal()
        {
            SingletonMod<Mod>.Logger.Debug("SegmentEndManager.Heal() called");
            Buffer[0] = Buffer[1] = null;
            for (int i = 1; i < Buffer.Length; ++i)
            {
                if (Buffer[i] is not SegmentEndData data)
                    continue;

                var startNode = i % 2 == 0;
                var segmentID = (ushort)Mathf.FloorToInt(i / 2);
                var nodeID = segmentID.ToSegment().GetNode(startNode);

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