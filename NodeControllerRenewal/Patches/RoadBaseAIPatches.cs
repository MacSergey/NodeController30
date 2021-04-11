using ColossalFramework;
using CSUtil.Commons;
using HarmonyLib;
using KianCommons;
using ModsCommon.Utilities;
using TrafficManager.Manager.Impl;

namespace NodeController
{
    public static class RoadBaseAIPatches
    {
        public static void UpdateLanesPostfix(ushort segmentID)
        {
            if (!segmentID.GetSegment().IsValid())
                return;

            foreach (bool startNode in new bool[] { false, true })
            {
                if (AllFlagsAreForward(segmentID, startNode))
                {
                    foreach (var lane in NetUtil.IterateLanes(segmentID, startNode: startNode))
                        lane.Flags &= ~NetLane.Flags.LeftForwardRight;
                }
            }
        }
        public static bool AllFlagsAreForward(ushort segmentID, bool startNode)
        {
            NetLane.Flags flags = 0;
            foreach (var lane in NetUtil.IterateLanes(segmentID, startNode))
                flags |= lane.Flags;

            return (flags & NetLane.Flags.LeftForwardRight) == NetLane.Flags.Forward;
        }

        public static void UpdateNodeFlagsPostfix(ref NetNode data)
        {
            if (data.CountSegments() != 2)
                return;

            ushort nodeID = data.GetID();
            NodeData nodeData = Manager.Instance[nodeID];

            if (nodeData == null)
                return;

            if (nodeData.FirstTimeTrafficLight && TrafficLightManager.Instance.CanEnableTrafficLight(nodeID, ref data, out var res))
            {
                TrafficLightManager.Instance.SetTrafficLight(nodeID, true, ref data);
                nodeData.FirstTimeTrafficLight = false;
            }
            else if (nodeData.CanHaveTrafficLights(out _) == false)
                data.m_flags &= ~NetNode.Flags.TrafficLights;
        }
    }
}
