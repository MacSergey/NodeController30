namespace NodeController.Patches._NetManager
{
    using System;
    using System.Reflection;
    using HarmonyLib;
    using ColossalFramework;
    using NodeController.LifeCycle;
    using KianCommons;
    using static KianCommons.ReflectionHelpers;
    using NodeController30;

    [HarmonyPatch]
    public static class ReleaseSegmentImplementationPatch
    {
        //private void ReleaseSegmentImplementation(ushort segment, ref NetSegment data, bool keepNodes)
        public static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(NetManager), "ReleaseSegmentImplementation", new[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) }, null);
        }

        public static MoveItSegmentData UpgradingSegmentData;
        public static bool m_upgrading =>
            (bool)GetFieldValue(Singleton<NetTool>.instance, "m_upgrading");

        public static void Prefix(ushort segment)
        {
            if (UpgradingSegmentData != null)
            {
                Mod.Logger.Error("Unexpected UpgradingSegmentData != null");
                UpgradingSegmentData = null;
            }
            if (m_upgrading)
                UpgradingSegmentData = MoveItIntegration.CopySegment(segment);

            Mod.Logger.Debug($"ReleaseSegment.Prefix({segment})\n" + Environment.StackTrace);
            SegmentEndManager.Instance.SetAt(segmentID: segment, true, value: null);
            SegmentEndManager.Instance.SetAt(segmentID: segment, false, value: null);
        }
    }
}