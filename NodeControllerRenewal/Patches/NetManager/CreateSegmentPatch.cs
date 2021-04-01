namespace NodeController.Patches._NetManager
{
    using HarmonyLib;
    using NodeController;
    using NodeController.LifeCycle;
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using NodeController.Patches._NetTool;
    using System;
    using NodeController30;

    [HarmonyPatch(typeof(global::NetManager), nameof(NetManager.CreateSegment))]
    public static class CreateSegmentPatch
    {
        // pastes segment ends that:
        // 1- not nullnot null and
        // 2- its nodeID matches input start/end nodeID.
        static void PasteSegment(
            MoveItSegmentData segmentData, ushort nodeID1, ushort nodeID2, ushort targetSegmentID)
        {
            if (segmentData == null) return;
            PasteSegmentEnd(segmentData.Start, nodeID1, nodeID2, targetSegmentID);
            PasteSegmentEnd(segmentData.End, nodeID1, nodeID2, targetSegmentID);
        }

        static void PasteSegmentEnd(
            SegmentEndData data, ushort nodeID1, ushort nodeID2, ushort targetSegmentID)
        {
            if (data != null)
            {
                ushort nodeID = data.NodeID;
                if (nodeID == nodeID1 || nodeID == nodeID2)
                {
                    MoveItIntegration.PasteSegmentEnd(data, targetNodeID: nodeID, targetSegmentID: targetSegmentID);
                }
            }
        }


        public static void Postfix(ref ushort segment, ushort startNode, ushort endNode, bool __result)
        {
            if (!__result || !InSimulationThread()) return;
            Mod.Logger.Debug($"CreateSegment.Postfix( {startNode}.-{segment}-.{endNode} )");

            if (MoveMiddleNodePatch.CopyData)
            {
                var segmentData = MoveMiddleNodePatch.SegmentData;
                Mod.Logger.Debug($"Moving middle node: copying data to newly created segment. newSegmentID={segment} data={segmentData}");
                PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
            }
            else if (SplitSegmentPatch.CopyData)
            {
                var segmentData = SplitSegmentPatch.SegmentData;
                var segmentData2 = SplitSegmentPatch.SegmentData2;
                var segmentData3 = SplitSegmentPatch.SegmentData3;
                Mod.Logger.Debug($"Spliting segment: copying data to newly created segment. newSegmentID={segment} data={segmentData} dat2={segmentData2} dat3={segmentData3}");

                PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
                PasteSegment(segmentData2, startNode, endNode, targetSegmentID: segment);
                PasteSegment(segmentData3, startNode, endNode, targetSegmentID: segment);
            }
            else if (ReleaseSegmentImplementationPatch.UpgradingSegmentData != null)
            {
                if (!ReleaseSegmentImplementationPatch.m_upgrading)
                    Mod.Logger.Error("Unexpected UpgradingSegmentData != null but m_upgrading == false ");
                else
                {
                    var segmentData = ReleaseSegmentImplementationPatch.UpgradingSegmentData;
                    PasteSegment(segmentData, startNode, endNode, targetSegmentID: segment);
                }
                ReleaseSegmentImplementationPatch.UpgradingSegmentData = null; // consume
            }
            else
            {
                SegmentEndManager.Instance.SetAt(segment, true, null);
                SegmentEndManager.Instance.SetAt(segment, false, null);
            }
        }
    }
}
