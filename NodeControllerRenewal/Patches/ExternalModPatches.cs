using ColossalFramework;
using ModsCommon.Utilities;
using TrafficManager.API.Traffic.Enums;

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
            NodeStyleType.Custom => (node.IsMain && !node.HasPedestrianLanes) ? false : null,
            NodeStyleType.End => null,
            _ => null,
        };
        private static bool? IsDefaultPedestrianCrossingAllowed(NodeData node) => node?.Type switch
        {
            NodeStyleType.Crossing => true,
            NodeStyleType.UTurn or NodeStyleType.Stretch or NodeStyleType.Middle or NodeStyleType.Bend => false,
            NodeStyleType.Custom when node.IsMain && node.FirstSegment.Info.m_netAI.GetType() != node.SecondSegment.Info.m_netAI.GetType() => false,
            NodeStyleType.Custom or NodeStyleType.End => null,
            _ => null,
        };
        public static bool? CanHaveTrafficLights(NodeData node, out ToggleTrafficLightError reason)
        {
            reason = ToggleTrafficLightError.None;
            switch (node?.Type)
            {
                case NodeStyleType.Crossing:
                case NodeStyleType.UTurn:
                case NodeStyleType.End:
                case NodeStyleType.Custom:
                    return null;
                case NodeStyleType.Stretch:
                case NodeStyleType.Middle:
                case NodeStyleType.Bend:
                    reason = ToggleTrafficLightError.NoJunction;
                    return false;
                default:
                    return null;
            }
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
            NodeStyleType.Stretch => true,// always on
            NodeStyleType.Crossing => false,// default off
            NodeStyleType.UTurn or NodeStyleType.Middle or NodeStyleType.Bend or NodeStyleType.End => null,// default
            NodeStyleType.Custom => node.IsJunction ? null : true,
            _ => null,
        };


        public static bool CanToggleTrafficLightPrefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason)
        {
            var nodeData = Manager.Instance[nodeId];
            return HandleNullBool(CanHaveTrafficLights(nodeData, out reason), ref __result);
        }
        public static bool GetDefaultEnteringBlockedJunctionAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(IsDefaultEnteringBlockedJunctionAllowed(data), ref __result);
        }
        public static bool GetDefaultPedestrianCrossingAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            NodeData data = Manager.Instance[nodeID];
            return HandleNullBool(IsDefaultPedestrianCrossingAllowed(data), ref __result);
        }
        public static bool GetDefaultUturnAllowedPrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(IsDefaultUturnAllowed(data), ref __result);
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

            return HandleNullBool(IsEnteringBlockedJunctionAllowedConfigurable(data), ref __result);
        }
        public static bool IsPedestrianCrossingAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(IsPedestrianCrossingAllowedConfigurable(data), ref __result);
        }
        public static bool IsUturnAllowedConfigurablePrefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.GetSegment().m_startNode : segmentId.GetSegment().m_endNode;
            var data = Manager.Instance[nodeID];
            return HandleNullBool(IsUturnAllowedConfigurable(data), ref __result);
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
