using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System.Linq;

namespace NodeController.Patches
{
    public static class RoadBaseAIPatches
    {
        public static void UpdateLanesPostfix(ushort segmentID)
        {
            ref var segment = ref segmentID.GetSegment();
            if (!segment.IsValid())
                return;

            foreach (bool startNode in new bool[] { false, true })
            {
                var laneIds = segment.GetLaneIds(startNode).ToArray();

                foreach (var lineId in laneIds)
                {
                    if (((NetLane.Flags)lineId.GetLane().m_flags & NetLane.Flags.LeftForwardRight) != NetLane.Flags.Forward)
                        return;
                }

                foreach (var lineId in laneIds)
                {
                    var lane = lineId.GetLane();
                    lane.m_flags = (ushort)(((NetLane.Flags)lane.m_flags) & ~NetLane.Flags.LeftForwardRight);
                }
            }
        }
        public static void UpdateNodeFlagsPostfix(ushort nodeID, ref NetNode data)
        {
            if (data.CountSegments() == 2 && SingletonManager<Manager>.Instance[nodeID] is NodeData nodeData)
            {
                //if (nodeData.FirstTimeTrafficLight && TrafficLightManager.Instance.CanEnableTrafficLight(nodeID, ref data, out var res))
                //{
                //    TrafficLightManager.Instance.SetTrafficLight(nodeID, true, ref data);
                //    nodeData.FirstTimeTrafficLight = false;
                //}
                //else 
                if (ExternalModPatches.CanHaveTrafficLights(nodeData, out _) == false)
                    data.m_flags &= ~NetNode.Flags.TrafficLights;
            }
        }
    }
}
