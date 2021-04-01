using ColossalFramework;
using JetBrains.Annotations;
using KianCommons;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController.Patches
{
    [UsedImplicitly]
    public static class CalculateCornerPatch2
    {
        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(ushort segmentID, bool start, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection)
        {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            Assertion.AssertNotNull(GUI.Settings.GameConfig, "Settings.GameConfig");
            if (data == null && !GUI.Settings.GameConfig.UnviversalSlopeFixes)
                return;

            ushort nodeID = segmentID.ToSegment().GetNode(start);
            bool middle = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle);
            bool untouchable = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
            if (!middle)
            {
                bool flatJunctions = data?.FlatJunctions ?? untouchable || segmentID.ToSegment().Info.m_flatJunctions;
                bool slope = !flatJunctions;
                if (slope)
                    FixCornerPos(nodeID.ToNode().m_position, segmentID.ToSegment().GetDirection(nodeID), ref cornerPos);

                else
                {
                    // left segment going away from the node is right segment going toward the node.
                    ushort neighbourSegmentID = leftSide ? segmentID.ToSegment().GetRightSegment(nodeID) : segmentID.ToSegment().GetLeftSegment(nodeID);

                    bool twist;
                    if (data != null)
                        twist = data.CanModifyTwist() && data.Twist;
                    else
                    {
                        twist = !untouchable && segmentID.ToSegment().Info.m_flatJunctions;
                        twist = twist && SegmentEndData.CanTwist(segmentID: segmentID, nodeID: nodeID);
                    }

                    if (twist)
                    {
                        Vector3 nodePos = nodeID.ToNode().m_position;
                        Vector3 neighbourEndDir = neighbourSegmentID.ToSegment().GetDirection(nodeID);

                        FixCornerPosMinor(nodePos, neighbourEndDir, ref cornerDirection, ref cornerPos);
                    }
                }
            }
            if (data != null)
                data.ApplyCornerAdjustments(ref cornerPos, ref cornerDirection, leftSide);
            else
            {
                // if vector dir is not limitted inside ApplyCornerAdjustments then do it here.
                // this must NOT be done before ApplyCornerAdjustments().
                float absY = Mathf.Abs(cornerDirection.y);
                if (absY > 2)
                    cornerDirection *= 2 / absY;
            }
        }
        /// <summary>
        /// give slope to junction
        /// </summary>
        public static void FixCornerPos(Vector3 nodePos, Vector3 segmentEndDir, ref Vector3 cornerPos)
        {
            float d = DotXZ(cornerPos - nodePos, segmentEndDir);
            cornerPos.y = nodePos.y + d * segmentEndDir.y;
        }
        /// <summary>
        /// embank segment end to match slope of the junction.
        /// TODO: also give slope if segment comes at an angle.
        /// </summary>
        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir, ref Vector3 cornerDir, ref Vector3 cornerPos)
        {
            float d = DotXZ(cornerPos - nodePos, neighbourEndDir);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            float acos = DotXZ(cornerDir, neighbourEndDir);
            cornerDir.y = neighbourEndDir.y * acos;
        }
    }
}
