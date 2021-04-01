namespace NodeController.Patches.TMPE
{
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using NodeController;
    using HarmonyLib;
    using KianCommons.Patches;
    using KianCommons;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class GetDefaultEnteringBlockedJunctionAllowed
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        public static MethodBase TargetMethod() => typeof(JunctionRestrictionsManager).GetMethod(nameof(JunctionRestrictionsManager.GetDefaultEnteringBlockedJunctionAllowed));

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result)
        {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeManager.Instance.buffer[nodeID];
            return PrefixUtils.HandleTernaryBool(
                data?.GetDefaultEnteringBlockedJunctionAllowed(),
                ref __result);
        }
    }
}