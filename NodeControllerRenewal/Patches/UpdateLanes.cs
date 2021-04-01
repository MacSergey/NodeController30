using ColossalFramework;
using HarmonyLib;
using KianCommons;

namespace NodeController
{
    [HarmonyPatch(typeof(RoadBaseAI))]
    [HarmonyPatch(nameof(RoadBaseAI.UpdateLanes))]
    [HarmonyBefore("de.viathinksoft.tmpe")]
    class UpdateLanes
    {
        public static bool AllFlagsAreForward(ushort segmentID, bool startNode)
        {
            NetLane.Flags flags = 0;
            foreach (var lane in NetUtil.IterateLanes(segmentID, startNode: startNode))
                flags |= lane.Flags;

            return (flags & NetLane.Flags.LeftForwardRight) == NetLane.Flags.Forward;
        }

        static void Postfix(ref RoadBaseAI __instance, ushort segmentID)
        {
            if (!NetUtil.IsSegmentValid(segmentID))
                return;

            foreach (bool startNode in HelpersExtensions.ALL_BOOL)
            {
                if (AllFlagsAreForward(segmentID, startNode))
                {
                    foreach (var lane in NetUtil.IterateLanes(segmentID, startNode: startNode))
                        lane.Flags &= ~NetLane.Flags.LeftForwardRight;
                }
            }
        }
    }
}
