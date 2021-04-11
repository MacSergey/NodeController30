using ColossalFramework;
using KianCommons;
using KianCommons.Patches;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.API.Traffic.Enums;

namespace NodeController.Patches
{
    public static class ExternalModPatches
    {
        public static bool CanToggleTrafficLightPrefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason)
        {
            var nodeData = Manager.Instance[nodeId];
            return HandleNullBool(nodeData?.CanHaveTrafficLights(out reason), ref __result);
        }
        public static bool GetDefaultEnteringBlockedJunctionAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(data?.IsDefaultEnteringBlockedJunctionAllowed, ref __result);
        }
        public static bool GetDefaultPedestrianCrossingAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            NodeData data = Manager.Instance[nodeID];
            return HandleNullBool(data?.IsDefaultPedestrianCrossingAllowed, ref __result);
        }
        public static bool GetDefaultUturnAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(data?.IsDefaultUturnAllowed, ref __result);
        }
        public static bool IsEnteringBlockedJunctionAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            if (data == null)
            {
                var flags = nodeID.GetNode().m_flags;
                bool oneway = flags.IsFlagSet(NetNode.Flags.OneWayIn) & flags.IsFlagSet(NetNode.Flags.OneWayOut);
                if (oneway & !segmentId.GetSegment().Info.m_hasPedestrianLanes)
                {
                    __result = false;
                    return false;
                }
            }

            return HandleNullBool(data?.IsEnteringBlockedJunctionAllowedConfigurable, ref __result);
        }
        public static bool IsPedestrianCrossingAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(data?.IsPedestrianCrossingAllowedConfigurable, ref __result);
        }
        public static bool IsUturnAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(data?.IsUturnAllowedConfigurable, ref __result);
        }

        public static bool ShouldHideCrossingPrefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            var data = Manager.Instance[nodeID, segmentID];
            return HandleNullBool(data?.ShouldHideCrossingTexture, ref __result);
        }

        private static bool HandleNullBool(this bool? res, ref bool __result)
        {
            if (res.HasValue)
            {
                __result = res.Value;
                return false;
            }
            else
                return true;
        }
    }
}
