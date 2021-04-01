namespace NodeController.Patches.TMPE
{
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using NodeController;
    using HarmonyLib;
    using TrafficManager.API.Traffic.Enums;
    using KianCommons.Patches;
    using KianCommons;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class CanToggleTrafficLight
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        public static MethodBase TargetMethod() => typeof(TrafficLightManager).GetMethod(nameof(TrafficLightManager.CanToggleTrafficLight));

        public static bool Prefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason)
        {
            var nodeData = NodeManager.Instance.buffer[nodeId];
            return PrefixUtils.HandleTernaryBool(nodeData?.CanHaveTrafficLights(out reason), ref __result);
        }
    }
}