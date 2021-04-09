using KianCommons;
using System;
using System.Runtime.CompilerServices;
using KianCommons.Plugins;
using ModsCommon.Utilities;

namespace NodeController.Util
{
    public static class CSURUtil
    {
        public const string HARMONY_ID = "csur.toolbox";
        internal static bool CSUREnabled { get; } = PluginUtil.GetCSUR().IsActive();

        public static float GetMinCornerOffset(ushort segmentID, ushort nodeID)
        {
            NetInfo info = nodeID.GetNode().Info;
            if (CSUREnabled && info.m_netAI is RoadBaseAI && info.name.Contains("CSUR"))
                return CSURToolBox.Util.CSURUtil.GetMinCornerOffset(info.m_minCornerOffset, nodeID);
            else
                return segmentID.GetSegment().Info.m_minCornerOffset;
        }
    }

}
