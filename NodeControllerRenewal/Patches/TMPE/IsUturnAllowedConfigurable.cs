namespace NodeController.Patches.TMPE
{
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using NodeController;
    using KianCommons;
    using HarmonyLib;
    using KianCommons.Patches;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class IsUturnAllowedConfigurable
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        public static MethodBase TargetMethod() => typeof(JunctionRestrictionsManager).GetMethod(nameof(JunctionRestrictionsManager.IsUturnAllowedConfigurable));

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeManager.Instance.buffer[nodeID];
            return PrefixUtils.HandleTernaryBool(data?.IsUturnAllowedConfigurable(), ref __result);
        }
    }
}