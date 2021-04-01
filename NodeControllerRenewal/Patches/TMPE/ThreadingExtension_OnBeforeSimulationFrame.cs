namespace NodeController.Patches.TMPE
{
    using HarmonyLib;
    using KianCommons;
    using System.Reflection;
    using TrafficManager;
    using KianCommons.Plugins;

    // TODO: remove this when TMPE is updated.
    [HarmonyPatch(typeof(ThreadingExtension), nameof(ThreadingExtension.OnBeforeSimulationFrame))]
    static class ThreadingExtension_OnBeforeSimulationFrame
    {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        static FieldInfo field_firstFrame { get; } = AccessTools.Field(typeof(ThreadingExtension), "firstFrame");
        public static void Prefix(ThreadingExtension __instance)
        {
            field_firstFrame?.SetValue(__instance, false);
        }
    }
}