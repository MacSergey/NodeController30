using NodeController.LifeCycle;
using static KianCommons.HelpersExtensions;
using NodeController.Patches._NetTool;
using ColossalFramework;
using KianCommons;

namespace NodeController.Patches
{
    public static class NetManagerPatches
    {
        public static MoveItSegmentData UpgradingSegmentData;
        public static bool Upgrading => (bool)ReflectionHelpers.GetFieldValue(Singleton<NetTool>.instance, "m_upgrading");

        public static void CreateSegmentPostfix(ref ushort segment, ushort startNode, ushort endNode, bool __result)
        {
            if (!__result || !InSimulationThread())
                return;

            if (MoveMiddleNodePatch.CopyData)
            {
                var segmentData = MoveMiddleNodePatch.SegmentData;
                PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
            }
            else if (SplitSegmentPatch.CopyData)
            {
                var segmentData = SplitSegmentPatch.SegmentData;
                var segmentData2 = SplitSegmentPatch.SegmentData2;
                var segmentData3 = SplitSegmentPatch.SegmentData3;

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
            if (data != null && data.NodeID == nodeID1 || data.NodeID == nodeID2)
                LifeCycle.MoveItIntegration.PasteSegmentEnd(data, data.NodeID, targetSegmentID);
        }

        public static void ReleaseSegmentImplementationPrefix(ushort segment)
        {
            if (UpgradingSegmentData != null)
            {
                Mod.Logger.Error("Unexpected UpgradingSegmentData != null");
                UpgradingSegmentData = null;
            }
            if (Upgrading)
                UpgradingSegmentData = LifeCycle.MoveItIntegration.CopySegment(segment);

            SegmentEndManager.Instance.SetAt(segmentID: segment, true, value: null);
            SegmentEndManager.Instance.SetAt(segmentID: segment, false, value: null);
        }
        public static void ReleaseNodeImplementationPrefix(ushort node)
        {
            NodeManager.Instance.SetNullNodeAndSegmentEnds(node);
        }
    }
}
