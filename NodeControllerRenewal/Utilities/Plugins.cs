using ModsCommon.Utilities;
using System;
using System.Linq;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController.Utilities
{
    public static class DependencyUtilities
    {
        public static PluginInfo CSUR => PluginUtilities.GetPlugin("CSUR ToolBox", 1959342332ul);
        public static PluginInfo HideCrossings => PluginUtilities.GetPlugin("HideCrosswalks"/*, 1939169189ul*/);
        public static PluginInfo TrafficManager => PluginUtilities.GetPlugin("TrafficManager"/*, 1637663252ul*/);

        //public const string HARMONY_ID = "csur.toolbox";

        private static bool CSUREnabled { get; } = CSUR?.isEnabled == true;
        public static float GetMinCornerOffset(ushort segmentID, ushort nodeID)
        {
            var info = nodeID.GetNode().Info;

            if (CSUREnabled && info.m_netAI is RoadBaseAI && info.name.Contains("CSUR"))
                return GetMinCornerOffset(info.m_minCornerOffset, nodeID);
            else
                return segmentID.GetSegment().Info.m_minCornerOffset;
        }
        private static float GetMinCornerOffset(float cornerOffset, ushort nodeId) => CSURToolBox.Util.CSURUtil.GetMinCornerOffset(cornerOffset, nodeId);

        public static bool IsCSUR(this NetInfo info)
        {
            if (info == null || (info.m_netAI.GetType() != typeof(RoadAI) && info.m_netAI.GetType() != typeof(RoadBridgeAI) && info.m_netAI.GetType() != typeof(RoadTunnelAI)))
                return false;
            return info.name.Contains(".CSUR ");
        }
    }
}
