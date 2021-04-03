using HarmonyLib;
using KianCommons;
using System;
using NodeController.LifeCycle;
using static KianCommons.HelpersExtensions;
using NodeController;

namespace NodeController.Patches
{
    public static class NetToolPatch
    {
        internal static MoveItSegmentData MoveSegmentData { get; private set; }
        internal static bool MoveCopyData => MoveSegmentData != null;
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


        public static void MoveMiddleNodePrefix(ref ushort node) // TODO remove ref when in lates harmony.
        {
            if (!InSimulationThread())
                return;

            NodeID = node;
            ushort segmentID = NetUtil.GetFirstSegment(NodeID);
            MoveSegmentData = LifeCycle.MoveItIntegration.CopySegment(segmentID);
            NodeID2 = segmentID.ToSegment().GetOtherNode(NodeID);
        }

        /// <param name="node">output node</param>
        public static void MoveMiddleNodePostfix(ref ushort node)
        {
            if (!InSimulationThread())
                return;

            if (MoveSegmentData?.Start != null || MoveSegmentData?.End != null)
            {
                // scenario 3.
                if (node == NodeID2)
                {
                    if (SplitSegmentData2 == null)
                        SplitSegmentData2 = MoveSegmentData;
                    else
                        SplitSegmentData3 = MoveSegmentData;
                }
            }
            MoveSegmentData = null;
        }

        internal static MoveItSegmentData SplitSegmentData3 { get; set; }
        internal static MoveItSegmentData SplitSegmentData2 { get; set; }
        internal static MoveItSegmentData SplitSegmentData { get; private set; }
        internal static bool SplitCopyData => MoveSegmentData != null || SplitSegmentData2 != null || SplitSegmentData3 != null;

        public static void SplitSegmentPrefix(ushort segment)
        {
            if (!InSimulationThread())
                return;

            SplitSegmentData = LifeCycle.MoveItIntegration.CopySegment(segment);
        }

        public static void SplitSegmentPostfix()
        {
            if (!InSimulationThread())
                return;

            SplitSegmentData = SplitSegmentData2 = SplitSegmentData3 = null;
        }
    }
}
