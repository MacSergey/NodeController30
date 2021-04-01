namespace NodeController.Patches.TMPE
{
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using KianCommons.Patches;
    using KianCommons;
    using NodeController;
    using HarmonyLib;
    using ColossalFramework;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class IsEnteringBlockedJunctionAllowedConfigurable
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();
        public static MethodBase TargetMethod() => typeof(JunctionRestrictionsManager).GetMethod(nameof(JunctionRestrictionsManager.IsEnteringBlockedJunctionAllowedConfigurable));

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeManager.Instance.buffer[nodeID];
            if (data == null)
            {
                var flags = nodeID.ToNode().m_flags;
                bool oneway = flags.IsFlagSet(NetNode.Flags.OneWayIn) & flags.IsFlagSet(NetNode.Flags.OneWayOut);
                if (oneway & !segmentId.ToSegment().Info.m_hasPedestrianLanes)
                {
                    __result = false;
                    return false;
                }
            }


            return PrefixUtils.HandleTernaryBool(data?.IsEnteringBlockedJunctionAllowedConfigurable(), ref __result);

        }
    }
}