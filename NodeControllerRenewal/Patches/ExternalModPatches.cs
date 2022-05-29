using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using TrafficManager.API.Traffic.Enums;
using static TrafficManager.API.Hook.IJunctionRestrictionsHook;

namespace NodeController.Patches
{
    public static class ExternalModPatches
    {
        public static void UpdateFlag(FlagsHookArgs args, JunctionRestrictionsFlags flag, Func<bool?> func)
        {
            if (args.Mask.IsFlagSet(flag))
            {
                var value = func();
                if (value.HasValue)
                    args.Result = args.Result.SetFlags(flag, value.Value);
            }
        }

        public static void GetConfigurableHook(FlagsHookArgs args)
        {
            ushort nodeID = args.StartNode ? args.SegmentId.GetSegment().m_startNode : args.SegmentId.GetSegment().m_endNode;
            var node = SingletonManager<Manager>.Instance[nodeID];

            if (node == null)
                UpdateFlag(args, JunctionRestrictionsFlags.AllowEnterWhenBlocked, () => IsEnteringBlockedJunctionAllowedConfigurable(args.SegmentId, nodeID));
            else
            {
                UpdateFlag(args, JunctionRestrictionsFlags.AllowUTurn, () => IsUturnAllowedConfigurable(node));
                UpdateFlag(args, JunctionRestrictionsFlags.AllowPedestrianCrossing, () => IsPedestrianCrossingAllowedConfigurable(node));
                UpdateFlag(args, JunctionRestrictionsFlags.AllowEnterWhenBlocked, () => IsEnteringBlockedJunctionAllowedConfigurable(node));
            }
        }

        public static void GetDefaultsHook(FlagsHookArgs args)
        {
            ushort nodeID = args.StartNode ? args.SegmentId.GetSegment().m_startNode : args.SegmentId.GetSegment().m_endNode;
            var node = SingletonManager<Manager>.Instance[nodeID];

            if (node != null)
            {
                UpdateFlag(args, JunctionRestrictionsFlags.AllowUTurn, () => IsDefaultUturnAllowed(node));
                UpdateFlag(args, JunctionRestrictionsFlags.AllowPedestrianCrossing, () => IsDefaultPedestrianCrossingAllowed(node));
                UpdateFlag(args, JunctionRestrictionsFlags.AllowEnterWhenBlocked, () => IsDefaultEnteringBlockedJunctionAllowed(node));
            }
        }

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
            var nodeData = SingletonManager<Manager>.Instance[nodeId];
            return HandleNullBool(CanHaveTrafficLights(nodeData, out reason), ref __result);
        }

        private static bool? IsEnteringBlockedJunctionAllowedConfigurable(ushort segmentId, ushort nodeID)
        {
            var flags = nodeID.GetNode().m_flags;
            bool oneway = flags.IsFlagSet(NetNode.Flags.OneWayIn) & flags.IsFlagSet(NetNode.Flags.OneWayOut);
            return oneway && !segmentId.GetSegment().Info.m_hasPedestrianLanes
                ? true
                : null;
        }

        public static bool ShouldHideCrossingPrefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            var data = SingletonManager<Manager>.Instance[nodeID, segmentID];
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
