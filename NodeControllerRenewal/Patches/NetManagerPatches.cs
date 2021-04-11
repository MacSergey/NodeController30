using NodeController.LifeCycle;
using static KianCommons.HelpersExtensions;
using ColossalFramework;
using KianCommons;
using HarmonyLib;
using ModsCommon;

namespace NodeController.Patches
{
    public static class NetManagerPatches
    {
        public static void ReleaseNodeImplementationPrefix(ushort node) => Manager.Instance.RemoveNode(node);
        public static void NetManagerUpdateNodePostfix(ushort node) => Manager.Instance.AddToUpdate(node);
        public static void NetManagerSimulationStepImplPostfix() => Manager.Instance.Update();
    }
}
