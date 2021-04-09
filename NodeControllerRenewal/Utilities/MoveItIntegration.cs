using KianCommons;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using KianCommons.Serialization;
using NodeController;
using ModsCommon;

namespace NodeController.LifeCycle
{
    [Serializable]
    public class MoveItSegmentData
    {
        public MoveItSegmentData Clone() => new MoveItSegmentData { Start = Start, End = End };
        public SegmentEndData Start;
        public SegmentEndData End;
        public override string ToString() => $"MoveItSegmentData(Start={Start} End={End})";
    }

    public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    {
        public MoveItIntegrationBase GetInstance() => new MoveItIntegration();
    }

    public class MoveItIntegration : MoveItIntegrationBase
    {
        static NodeManager NodeManager => NodeManager.Instance;
        static SegmentEndManager SegmentEndManager => SegmentEndManager.Instance;

        public override string ID => "CS.Kian.NodeController";

        public override Version DataVersion => new Version(2, 1, 1);

        public override object Decode64(string base64Data, Version dataVersion)
        {
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Decode64({base64Data},{dataVersion}) was called");
            if (base64Data == null || base64Data.Length == 0)
                return null;
            else
            {
                byte[] data = Convert.FromBase64String(base64Data);
                return SerializationUtil.Deserialize(data, dataVersion);
            }
        }

        public override string Encode64(object record)
        {
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Encode64({record}) was called");
            var data = SerializationUtil.Serialize(record);
            if (data == null || data.Length == 0)
                return null;
            else
                return Convert.ToBase64String(data);
        }

        public override object Copy(InstanceID sourceInstanceID)
        {
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Copy({sourceInstanceID.ToSTR()}) called");
            switch (sourceInstanceID.Type)
            {
                case InstanceType.NetNode:
                    return CopyNode(sourceInstanceID.NetNode);
                case InstanceType.NetSegment:
                    return CopySegment(sourceInstanceID.NetSegment);
                default:
                    SingletonMod<Mod>.Logger.Debug("Unsupported integration");
                    return null;
            }
        }

        public override void Paste(InstanceID targetrInstanceID, object record, Dictionary<InstanceID, InstanceID> map)
        {
            string strRecord = record == null ? "null" : record.ToString();
            string strInstanceID = targetrInstanceID.ToSTR();
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Paste({strInstanceID}, record:{strRecord}, map) was called");
            switch (targetrInstanceID.Type)
            {
                case InstanceType.NetNode:
                    PasteNode(targetrInstanceID.NetNode, (NodeData)record, map);
                    break;
                case InstanceType.NetSegment:
                    PasteSegment(targetrInstanceID.NetSegment, (MoveItSegmentData)record, map);
                    break;
                default:
                    SingletonMod<Mod>.Logger.Debug("Unsupported integration");
                    break;
            }
        }

        public static NodeData CopyNode(ushort sourceNodeID) => NodeManager.Instance.buffer[sourceNodeID]?.Clone();

        public static MoveItSegmentData CopySegment(ushort sourceSegmentID)
        {
            var ret = new MoveItSegmentData
            {
                Start = SegmentEndManager[sourceSegmentID, true]?.Clone(),
                End = SegmentEndManager[sourceSegmentID, false]?.Clone()
            };
            if (ret.Start == null && ret.End == null)
                return null;

            return ret;
        }

        public static void PasteNode(ushort targetNodeID, NodeData record, Dictionary<InstanceID, InstanceID> map)
        {
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.PasteNode({targetNodeID}) called with record = {record}");
            if (record == null)
            {
                //nodeMan.ResetNodeToDefault(nodeID); // doing this is not backward comaptible
            }
            else
            {
                record = record.Clone();
                NodeManager.buffer[targetNodeID] = record;
                NodeManager.buffer[targetNodeID].NodeId = targetNodeID;

                // Do not call refresh here as it might restart node to 0 even though corner offsets from
                // segments may come in later.
                // after cloning is complete, everything will be updated.
                //nodeMan.RefreshData(targetNodeID);
            }
        }

        public static void PasteSegment(
            ushort targetSegmentID, MoveItSegmentData data, Dictionary<InstanceID, InstanceID> map)
        {
            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.PasteSegment({targetSegmentID}) called with record = " + data);
            if (data == null)
            {
                // doing this is not backward comatible:
                //segEndMan.ResetSegmentEndToDefault(segmentId, true); 
                //segEndMan.ResetSegmentEndToDefault(segmentId, false);
            }
            else
            {
                PasteSegmentEnd(segmentEndData: data.Start, targetSegmentID: targetSegmentID, map: map);
                PasteSegmentEnd(segmentEndData: data.End, targetSegmentID: targetSegmentID, map: map);
            }
        }

        public static void Paste(object record, Dictionary<InstanceID, InstanceID> map)
        {
            if (record is NodeData nodeData)
            {
                ushort mappedNodeID = MappedNodeID(map, nodeData.NodeId);
                PasteNode(mappedNodeID, nodeData, map);
            }
            else if (record is MoveItSegmentData moveItSegmentData)
            {
                ushort segmentID;
                if (moveItSegmentData.Start != null)
                    segmentID = moveItSegmentData.Start.SegmentId;
                else if (moveItSegmentData.End != null)
                    segmentID = moveItSegmentData.End.SegmentId;
                else
                    return;

                ushort mappedSegmentID = MappedSegmentID(map, segmentID);
                PasteSegment(mappedSegmentID, moveItSegmentData, map);
            }
        }

        public static ushort MappedNodeID(Dictionary<InstanceID, InstanceID> map, ushort nodeID)
        {
            InstanceID instanceID = new InstanceID { NetNode = nodeID };
            if (map.TryGetValue(instanceID, out InstanceID mappedInstanceID))
                return mappedInstanceID.NetNode;
            else
                throw new Exception($"map does not contian node:{nodeID} map = {map.ToSTR()}");
        }
        public static ushort MappedSegmentID(Dictionary<InstanceID, InstanceID> map, ushort segmentID)
        {
            InstanceID instanceID = new InstanceID { NetSegment = segmentID };
            if (map.TryGetValue(instanceID, out InstanceID mappedInstanceID))
                return mappedInstanceID.NetSegment;
            else
                throw new Exception($"map does not contian segment:{segmentID} map = {map.ToSTR()}");
        }

        public static void PasteSegmentEnd(SegmentEndData segmentEndData, ushort targetSegmentID, Dictionary<InstanceID, InstanceID> map)
        {
            if (segmentEndData != null)
            {
                ushort nodeID = MappedNodeID(map, segmentEndData.NodeId);
                PasteSegmentEnd(segmentEndData, nodeID, targetSegmentID);
            }
        }

        public static void PasteSegmentEnd(SegmentEndData segmentEndData, ushort targetNodeID, ushort targetSegmentID)
        {
            SingletonMod<Mod>.Logger.Debug($"PasteSegmentEnd({segmentEndData}, targetNodeID:{targetNodeID}, targetSegmentID:{targetNodeID})");
            if (segmentEndData != null)
            {
                segmentEndData = segmentEndData.Clone();
                segmentEndData.SegmentId = targetSegmentID;
                segmentEndData.NodeId = targetNodeID;
            }
            SegmentEndManager[targetSegmentID, targetNodeID] = segmentEndData;
        }

    }
}
