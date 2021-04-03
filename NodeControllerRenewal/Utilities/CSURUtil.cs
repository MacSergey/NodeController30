using KianCommons;
using System;
using System.Runtime.CompilerServices;
using KianCommons.Plugins;

namespace NodeController.Util
{
    public static class CSURUtil
    {
        public const string HARMONY_ID = "csur.toolbox";
        internal static bool CSUREnabled;
        public static void Init() => CSUREnabled = PluginUtil.GetCSUR().IsActive();

        public static float GetMinCornerOffset(ushort segmentID, ushort nodeID)
        {
            NetInfo info = nodeID.ToNode().Info;
            if (CSUREnabled && info.m_netAI is RoadBaseAI && info.name.Contains("CSUR"))
                return GetMinCornerOffset_(info.m_minCornerOffset, nodeID);
            else
                return segmentID.ToSegment().Info.m_minCornerOffset;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float GetMinCornerOffset_(float cornerOffset0, ushort nodeID) => CSURToolBox.Util.CSURUtil.GetMinCornerOffset(cornerOffset0, nodeID);
    }

}
