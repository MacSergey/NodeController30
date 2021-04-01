namespace NodeController.Patches.TMPE
{
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using KianCommons.Patches;
    using KianCommons;
    using NodeController;
    using HarmonyLib;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class GetDefaultUturnAllowed
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();
        public static MethodBase TargetMethod() => typeof(JunctionRestrictionsManager).GetMethod(nameof(JunctionRestrictionsManager.GetDefaultUturnAllowed));

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeManager.Instance.buffer[nodeID];
            return PrefixUtils.HandleTernaryBool(data?.GetDefaultUturnAllowed(), ref __result);
        }
    }
}