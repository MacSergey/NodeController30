﻿using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using TrafficManager.API.Traffic.Enums;
using UnityEngine.Networking.Types;

namespace NodeController.Patches
{
    public static class ExternalModPatches
    {
        private static bool? IsUturnAllowedConfigurable(NodeData node) => node?.Type switch
        {
            NodeStyleType.Crossing or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,// always off
            NodeStyleType.UTurn or NodeStyleType.Custom or NodeStyleType.End => null,// default
            _ => null,
        };
        private static bool? IsDefaultUturnAllowed(NodeData node) => node?.Type switch
        {
            NodeStyleType.UTurn => true,
            NodeStyleType.Crossing or NodeStyleType.Stretch => false,
            NodeStyleType.Middle or NodeStyleType.Bend or NodeStyleType.Custom or NodeStyleType.End => null,
            _ => null,
        };
        private static bool? IsPedestrianCrossingAllowedConfigurable(NodeData node) => node?.Type switch
        {
            NodeStyleType.Crossing or NodeStyleType.UTurn or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            NodeStyleType.Custom => (node.IsTwoRoads && !node.HasPedestrianLanes) ? false : null,
            NodeStyleType.End => null,
            _ => null,
        };
        private static bool? IsDefaultPedestrianCrossingAllowed(NodeData node) => node?.Type switch
        {
            NodeStyleType.Crossing => true,
            NodeStyleType.UTurn or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            NodeStyleType.Custom when node.IsTwoRoads && node.FirstSegment.Info.m_netAI.GetType() != node.SecondSegment.Info.m_netAI.GetType() => false,
            NodeStyleType.Custom or NodeStyleType.End => null,
            _ => null,
        };
        public static bool? CanHaveTrafficLights(NodeData node, out ToggleTrafficLightError reason)
        {
            var result = node?.Style.SupportTrafficLights;
            reason = result == false ? ToggleTrafficLightError.NoJunction : ToggleTrafficLightError.None;
            return result;
        }
        private static bool? IsEnteringBlockedJunctionAllowedConfigurable(NodeData node) => node?.Type switch
        {
            NodeStyleType.Custom when node.IsJunction => null,
            NodeStyleType.Custom when node.DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & node.DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut) && !node.HasPedestrianLanes => false,
            NodeStyleType.Crossing or NodeStyleType.UTurn or NodeStyleType.Custom or NodeStyleType.End => null,
            NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            _ => null,
        };
        private static bool? IsDefaultEnteringBlockedJunctionAllowed(NodeData node) => node?.Type switch
        {
            NodeStyleType.Stretch => true,
            NodeStyleType.Crossing => false,
            NodeStyleType.UTurn or NodeStyleType.Middle or NodeStyleType.Bend or NodeStyleType.End => null,// default
            NodeStyleType.Custom => node.IsJunction ? null : true,
            _ => null,
        };


        public static bool CanToggleTrafficLightPrefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason)
        {
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(CanHaveTrafficLights(nodeData, out reason), ref __result);
        }
        public static bool GetDefaultEnteringBlockedJunctionAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(IsDefaultEnteringBlockedJunctionAllowed(nodeData), ref __result);
        }
        public static bool GetDefaultPedestrianCrossingAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(IsDefaultPedestrianCrossingAllowed(nodeData), ref __result);
        }
        public static bool GetDefaultUturnAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(IsDefaultUturnAllowed(nodeData), ref __result);
        }
        public static bool IsEnteringBlockedJunctionAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            if (nodeData == null)
            {
                var flags = nodeId.GetNode().m_flags;
                bool oneway = flags.IsFlagSet(NetNode.Flags.OneWayIn) & flags.IsFlagSet(NetNode.Flags.OneWayOut);
                if (oneway & !segmentId.GetSegment().Info.m_hasPedestrianLanes)
                {
                    __result = false;
                    return false;
                }
            }

            return HandleNullBool(IsEnteringBlockedJunctionAllowedConfigurable(nodeData), ref __result);
        }
        public static bool IsPedestrianCrossingAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(IsPedestrianCrossingAllowedConfigurable(nodeData), ref __result);
        }
        public static bool IsUturnAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeId = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            SingletonManager<Manager>.Instance.TryGetFinalNodeData(nodeId, out var nodeData);
            return HandleNullBool(IsUturnAllowedConfigurable(nodeData), ref __result);
        }

        public static bool ShouldHideCrossingPrefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            SingletonManager<Manager>.Instance.TryGetFinalSegmentData(nodeID, segmentID, out var nodeData);
            return HandleNullBool(nodeData?.ShouldHideCrossingTexture, ref __result);
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
