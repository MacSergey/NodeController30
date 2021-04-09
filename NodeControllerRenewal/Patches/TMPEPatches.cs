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
    public static class TMPEPatches
    {
        public static bool CanToggleTrafficLightPrefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason)
        {
            var nodeData = NodeManager.Instance[nodeId];
            return HelpersExtensions.HandleNullBool(nodeData?.CanHaveTrafficLights(out reason), ref __result);
        }
        public static bool GetDefaultEnteringBlockedJunctionAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = NodeManager.Instance[nodeID];
            return HelpersExtensions.HandleNullBool(data?.IsDefaultEnteringBlockedJunctionAllowed, ref __result);
        }
        public static bool GetDefaultPedestrianCrossingAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            NodeData data = NodeManager.Instance[nodeID];
            return HelpersExtensions.HandleNullBool(data?.IsDefaultPedestrianCrossingAllowed, ref __result);
        }
        public static bool GetDefaultUturnAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = NodeManager.Instance[nodeID];
            return HelpersExtensions.HandleNullBool(data?.IsDefaultUturnAllowed, ref __result);
        }
        public static bool IsEnteringBlockedJunctionAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = NodeManager.Instance[nodeID];
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

            return HelpersExtensions.HandleNullBool(data?.IsEnteringBlockedJunctionAllowedConfigurable, ref __result);
        }
        public static bool IsPedestrianCrossingAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = NodeManager.Instance[nodeID];
            return HelpersExtensions.HandleNullBool(data?.IsPedestrianCrossingAllowedConfigurable, ref __result);
        }
        public static bool IsUturnAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = NodeManager.Instance[nodeID];
            return HelpersExtensions.HandleNullBool(data?.IsUturnAllowedConfigurable, ref __result);
        }
    }
}
