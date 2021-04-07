using NodeController.LifeCycle;
using static KianCommons.HelpersExtensions;
using ColossalFramework;
using KianCommons;
using HarmonyLib;
using ModsCommon;

namespace NodeController.Patches
{
    public static class NetManagerPatches
    {
        public static MoveItSegmentData UpgradingSegmentData;
        public static bool Upgrading => (bool)AccessTools.Field(typeof(Singleton<NetTool>), "m_upgrading").GetValue(Singleton<NetTool>.instance);

        public static void CreateSegmentPostfix(ref ushort segment, ushort startNode, ushort endNode, bool __result)
        {
            if (!__result || !InSimulationThread())
                return;

            if (NetToolPatch.MoveCopyData)
            {
                var segmentData = NetToolPatch.MoveSegmentData;
                PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
            }
            else if (NetToolPatch.SplitCopyData)
            {
                var segmentData = NetToolPatch.SplitSegmentData;
                var segmentData2 = NetToolPatch.SplitSegmentData2;
                var segmentData3 = NetToolPatch.SplitSegmentData3;

                PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
                PasteSegment(segmentData2, startNode, endNode, targetSegmentID: segment);
                PasteSegment(segmentData3, startNode, endNode, targetSegmentID: segment);
            }
            else if (UpgradingSegmentData != null)
            {
                if (Upgrading)
                {
                    var segmentData = UpgradingSegmentData;
                    PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
                }
                UpgradingSegmentData = null; // consume
            }
            else
            {
                SegmentEndManager.Instance.SetAt(segment, true, null);
                SegmentEndManager.Instance.SetAt(segment, false, null);
            }
        }

        static void PasteSegment(MoveItSegmentData segmentData, ushort nodeID1, ushort nodeID2, ushort targetSegmentID)
        {
            if (segmentData != null)
            {
                PasteSegmentEnd(segmentData.Start, nodeID1, nodeID2, targetSegmentID);
                PasteSegmentEnd(segmentData.End, nodeID1, nodeID2, targetSegmentID);
            }
        }
        static void PasteSegmentEnd(SegmentEndData data, ushort nodeID1, ushort nodeID2, ushort targetSegmentID)
        {
            if (data != null && data.NodeId == nodeID1 || data.NodeId == nodeID2)
                LifeCycle.MoveItIntegration.PasteSegmentEnd(data, data.NodeId, targetSegmentID);
        }

        public static void ReleaseSegmentImplementationPrefix(ushort segment)
        {
            if (UpgradingSegmentData != null)
            {
                SingletonMod<Mod>.Logger.Error("Unexpected UpgradingSegmentData != null");
                UpgradingSegmentData = null;
            }
            if (Upgrading)
                UpgradingSegmentData = LifeCycle.MoveItIntegration.CopySegment(segment);

            SegmentEndManager.Instance.SetAt(segmentID: segment, true, value: null);
            SegmentEndManager.Instance.SetAt(segmentID: segment, false, value: null);
        }
        public static void ReleaseNodeImplementationPrefix(ushort node) => NodeManager.Instance.SetNullNodeAndSegmentEnds(node);
    }
}
