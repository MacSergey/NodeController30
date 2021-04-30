using ModsCommon.Utilities;
using System;
using System.Linq;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController.Utilities
{
    public static class DependencyUtilities
    {
        private static IPluginSearcher HideCrossingsSearcher { get; } = PluginUtilities.GetSearcher("RM Crossings", 1939169189ul, 1934023593ul);
        private static IPluginSearcher TMPESearcher { get; } = PluginUtilities.GetSearcher("TM:PE", 1637663252ul, 1806963141ul);
        private static IPluginSearcher NC2Searcher { get; } = new AnySearcher
            (
            new AllSearcher(new IdSearcher(2085403475ul), PathSearcher.Workshop),
            new AllSearcher(new UserModNameSearcher("Node Controller"), new NotSearcher(new UserModNameSearcher("Node Controller Renewal")), PathSearcher.Local)
            );

        public static PluginInfo HideCrossings => PluginUtilities.GetPlugin(HideCrossingsSearcher);
        public static PluginInfo TrafficManager => PluginUtilities.GetPlugin(TMPESearcher);
        public static PluginInfo NC2 => PluginUtilities.GetPlugin(NC2Searcher);
    }
}
