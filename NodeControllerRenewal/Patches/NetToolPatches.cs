using System;
using NodeController.LifeCycle;
using ModsCommon.Utilities;
using System.Linq;

namespace NodeController.Patches
{
    public static class NetToolPatch
    {
        internal static MoveItSegmentData MoveSegmentData { get; private set; }
        internal static bool MoveCopyData => MoveSegmentData != null;
        internal static ushort NodeId { get; set; }
        /// <summary>
        /// scenario 1: no change - returns the input node.
        /// scenario 2: move node : segment is released and a smaller segment is created - returns the moved node.
        /// scenario 3: merge node: segment is released and the other node is returned.
        ///
        /// How to handle:
        /// 1: skip (DONE)
        /// 2: copy segment end for the node that didn't move (moved node cannot have customisations) (DONE)
        /// 3: when split-segment creates a new segment, that copy segment end to it.

        public static void MoveMiddleNodePrefix(ushort node)
        {
            var segmentId = node.GetNode().SegmentIds().First();
            MoveSegmentData = LifeCycle.MoveItIntegration.CopySegment(segmentId);
            NodeId = segmentId.GetSegment().GetOtherNode(node);
        }

        public static void MoveMiddleNodePostfix(ushort node)
        {
            if (MoveSegmentData?.Start != null || MoveSegmentData?.End != null)
            {
                // scenario 3.
                if (node == NodeId)
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
            SplitSegmentData = LifeCycle.MoveItIntegration.CopySegment(segment);
        }

        public static void SplitSegmentPostfix()
        {
            SplitSegmentData = SplitSegmentData2 = SplitSegmentData3 = null;
        }
    }
}
