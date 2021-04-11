using KianCommons;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using NodeController;
using ModsCommon;
using ModsCommon.Utilities;

namespace NodeController.LifeCycle
{
    //[Serializable]
    //public class MoveItSegmentData
    //{
    //    public MoveItSegmentData Clone() => new MoveItSegmentData { Start = Start, End = End };
    //    public SegmentEndData Start;
    //    public SegmentEndData End;
    //    public override string ToString() => $"MoveItSegmentData(Start={Start} End={End})";
    //}

    //public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    //{
    //    public MoveItIntegrationBase GetInstance() => new MoveItIntegration();
    //}

//    public class MoveItIntegration : MoveItIntegrationBase
//    {
//        static Manager NodeManager => Manager.Instance;

//        public override string ID => "CS.Kian.NodeController";

//        public override Version DataVersion => new Version(2, 1, 1);

//        public override object Decode64(string base64Data, Version dataVersion)
//        {
//            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Decode64({base64Data},{dataVersion}) was called");
//            if (base64Data == null || base64Data.Length == 0)
//                return null;
//            else
//            {
//                byte[] data = Convert.FromBase64String(base64Data);
//                return SerializationUtil.Deserialize(data, dataVersion);
//            }
//        }

//        public override string Encode64(object record)
//        {
//            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.Encode64({record}) was called");
//            var data = SerializationUtil.Serialize(record);
//            if (data == null || data.Length == 0)
//                return null;
//            else
//                return Convert.ToBase64String(data);
//        }

//        public override object Copy(InstanceID sourceInstanceId)
//        {
//            switch (sourceInstanceId.Type)
//            {
//                case InstanceType.NetNode:
//                    return CopyNode(sourceInstanceId.NetNode);
//                case InstanceType.NetSegment:
//                    return CopySegment(sourceInstanceId.NetSegment);
//                default:
//                    SingletonMod<Mod>.Logger.Debug("Unsupported integration");
//                    return null;
//            }
//        }

//        public override void Paste(InstanceID targetrInstanceId, object record, Dictionary<InstanceID, InstanceID> map)
//        {
//            switch (targetrInstanceId.Type)
//            {
//                case InstanceType.NetNode:
//                    PasteNode(targetrInstanceId.NetNode, (NodeData)record, map);
//                    break;
//                case InstanceType.NetSegment:
//                    PasteSegment(targetrInstanceId.NetSegment, (MoveItSegmentData)record, map);
//                    break;
//                default:
//                    SingletonMod<Mod>.Logger.Debug("Unsupported integration");
//                    break;
//            }
//        }

//        public static NodeData CopyNode(ushort sourceNodeId) => NodeManager[sourceNodeId]?.Clone();

//        public static MoveItSegmentData CopySegment(ushort sourceSegmentId)
//        {
//            Manager.GetSegmentData(sourceSegmentId, out var start, out var end);
//            var ret = new MoveItSegmentData
//            {
//                Start = start?.Clone(),
//                End = end?.Clone()
//            };
//            if (ret.Start == null && ret.End == null)
//                return null;

//            return ret;
//        }

//        public static void PasteNode(ushort targetNodeId, NodeData record, Dictionary<InstanceID, InstanceID> map)
//        {
//            SingletonMod<Mod>.Logger.Debug($"MoveItIntegration.PasteNode({targetNodeId}) called with record = {record}");
//            if (record == null)
//            {
//                //nodeMan.ResetNodeToDefault(nodeID); // doing this is not backward comaptible
//            }
//            else
//            {
//                record = record.Clone();
//                NodeManager[targetNodeId] = record;
//                NodeManager[targetNodeId].Id = targetNodeId;
//            }
//        }
//        public static void PasteSegment(ushort nodeId, MoveItSegmentData data)
//        {
//            if (data == null)
//                return;

//            foreach(var segmentId in nodeId.GetNode().SegmentIds())
//            {
//                var segment = segmentId.GetSegment();
//                var targetNodeId = segment.GetOtherNode(nodeId);

//                if (data.Start?.NodeId == targetNodeId)
//                    PasteSegmentEnd(data.Start, targetNodeId, segmentId);
//                else if (data.End?.NodeId == targetNodeId)
//                    PasteSegmentEnd(data.End, targetNodeId, segmentId);
//            }
//        }
//        public static void PasteSegment(ushort targetSegmentId, MoveItSegmentData data, Dictionary<InstanceID, InstanceID> map)
//        {
//            if (data == null)
//            {
//                // doing this is not backward comatible:
//                //segEndMan.ResetSegmentEndToDefault(segmentId, true); 
//                //segEndMan.ResetSegmentEndToDefault(segmentId, false);
//            }
//            else
//            {
//                PasteSegmentEnd(data.Start, targetSegmentId, map);
//                PasteSegmentEnd(data.End, targetSegmentId, map);
//            }
//        }

//        public static void Paste(object record, Dictionary<InstanceID, InstanceID> map)
//        {
//            if (record is NodeData nodeData)
//            {
//                ushort mappedNodeId = MappedNodeId(map, nodeData.Id);
//                PasteNode(mappedNodeId, nodeData, map);
//            }
//            else if (record is MoveItSegmentData moveItSegmentData)
//            {
//                ushort segmentId;
//                if (moveItSegmentData.Start != null)
//                    segmentId = moveItSegmentData.Start.Id;
//                else if (moveItSegmentData.End != null)
//                    segmentId = moveItSegmentData.End.Id;
//                else
//                    return;

//                ushort mappedSegmentId = MappedSegmentId(map, segmentId);
//                PasteSegment(mappedSegmentId, moveItSegmentData, map);
//            }
//        }

//        public static ushort MappedNodeId(Dictionary<InstanceID, InstanceID> map, ushort nodeId)
//        {
//            InstanceID instanceId = new InstanceID { NetNode = nodeId };
//            if (map.TryGetValue(instanceId, out InstanceID mappedInstanceId))
//                return mappedInstanceId.NetNode;
//            else
//                throw new Exception($"map does not contian node:{nodeId}");
//        }
//        public static ushort MappedSegmentId(Dictionary<InstanceID, InstanceID> map, ushort segmentId)
//        {
//            InstanceID instanceId = new InstanceID { NetSegment = segmentId };
//            if (map.TryGetValue(instanceId, out InstanceID mappedInstanceID))
//                return mappedInstanceID.NetSegment;
//            else
//                throw new Exception($"map does not contian segment:{segmentId}");
//        }

//        public static void PasteSegmentEnd(SegmentEndData segmentEndData, ushort targetSegmentId, Dictionary<InstanceID, InstanceID> map)
//        {
//            if (segmentEndData != null)
//            {
//                ushort nodeId = MappedNodeId(map, segmentEndData.NodeId);
//                PasteSegmentEnd(segmentEndData, nodeId, targetSegmentId);
//            }
//        }

//        public static void PasteSegmentEnd(SegmentEndData segmentEndData, ushort targetNodeId, ushort targetSegmentId)
//        {
//            if (segmentEndData != null)
//            {
//                segmentEndData = segmentEndData.Clone();
//                segmentEndData.Id = targetSegmentId;
//                segmentEndData.NodeId = targetNodeId;
//            }
//            //SegmentEndManager[targetSegmentId, targetNodeId] = segmentEndData;
//        }

//    }
}
