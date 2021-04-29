using ModsCommon.Utilities;
using System;
using System.Linq;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController.Utilities
{
    public static class DependencyUtilities
    {
        private static IPluginSearcher CSURSearcher { get; } = PluginUtilities.GetSearcher("CSUR ToolBox", BaseSearcher.Option.DefaultSearch, 1959342332ul);
        private static IPluginSearcher HideCrossingsSearcher { get; } = PluginUtilities.GetSearcher("RM Crossings", BaseSearcher.Option.DefaultSearch, 1939169189ul, 1934023593ul);
        private static IPluginSearcher TMPESearcher { get; } = PluginUtilities.GetSearcher("TM:PE", BaseSearcher.Option.DefaultSearch, 1637663252ul, 1806963141ul);
        private static IPluginSearcher NC2Searcher { get; } = PluginUtilities.GetSearcher("Node controller", BaseSearcher.Option.AllOptions, 2085403475ul);


        public static PluginInfo CSUR { get; } = PluginUtilities.GetPlugin(CSURSearcher);
        public static PluginInfo HideCrossings { get; } = PluginUtilities.GetPlugin(HideCrossingsSearcher);
        public static PluginInfo TrafficManager { get; } = PluginUtilities.GetPlugin(TMPESearcher);
        public static PluginInfo NC2 { get; } = PluginUtilities.GetPlugin(NC2Searcher);

        static DependencyUtilities()
        {
            if (NC2 is PluginInfo plugin)
                NC2StateWatcher = new PlaginStateWatcher(plugin);
        }
        public static PlaginStateWatcher NC2StateWatcher { get;}

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
