namespace NodeController.Patches._NetTool
{
    using HarmonyLib;
    using KianCommons;
    using System;
    using NodeController.LifeCycle;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.Assertion;
    using NodeController;

    [HarmonyPatch(typeof(global::NetTool), "MoveMiddleNode")]
    public static class MoveMiddleNodePatch
    {
        internal static MoveItSegmentData SegmentData { get; private set; }
        internal static bool CopyData => SegmentData != null;
        internal static ushort NodeID, NodeID2;
        /// <summary>
        /// scenario 1: no change - returns the input node.
        /// scenario 2: move node : segment is released and a smaller segment is created - returns the moved node.
        /// scenario 3: merge node: segment is released and the other node is returned.
        ///
        /// How to handle:
        /// 1: skip (DONE)
        /// 2: copy segment end for the node that didn't move (moved node cannot have customisations) (DONE)
        /// 3: when split-segment creates a new segment, that copy segment end to it.
        /// </summary>
        /// <param name="node">input node</param>


        public static void Prefix(ref ushort node) // TODO remove ref when in lates harmony.
        {
            if (!InSimulationThread())
                return;

            NodeID = node;
            AssertEqual(NodeID.ToNode().CountSegments(), 1, "CountSegments");
            ushort segmentID = NetUtil.GetFirstSegment(NodeID);
            Mod.Logger.Debug($"MoveMiddleNode.Prefix() node:{NodeID} segment:{segmentID}");
            SegmentData = MoveItIntegration.CopySegment(segmentID);
            NodeID2 = segmentID.ToSegment().GetOtherNode(NodeID);
        }

        /// <param name="node">output node</param>
        public static void Postfix(ref ushort node)
        {
            if (!InSimulationThread()) return;
            if (SegmentData?.Start != null || SegmentData?.End != null)
            {
                Mod.Logger.Debug($"MoveMiddleNode.Postfix()");

                // scenario 3.
                if (node == NodeID2)
                {
                    if (SplitSegmentPatch.SegmentData2 == null)
                    {
                        SplitSegmentPatch.SegmentData2 = SegmentData;
                    }
                    else
                    {
                        SplitSegmentPatch.SegmentData3 = SegmentData;
                    }
                }
            }
            SegmentData = null;
        }
    }
}
