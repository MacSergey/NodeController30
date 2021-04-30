using ModsCommon.Utilities;
using System;
using System.Linq;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController.Utilities
{
    public static class DependencyUtilities
    {
        private static IPluginSearcher HideCrossingsSearcher { get; } = PluginUtilities.GetSearcher("RM Crossings", BaseSearcher.Option.DefaultSearch, 1939169189ul, 1934023593ul);
        private static IPluginSearcher TMPESearcher { get; } = PluginUtilities.GetSearcher("TM:PE", BaseSearcher.Option.DefaultSearch, 1637663252ul, 1806963141ul);
        private static IPluginSearcher NC2Searcher { get; } = PluginUtilities.GetSearcher("Node controller", BaseSearcher.Option.AllOptions, 2085403475ul);


        public static PluginInfo HideCrossings { get; } = PluginUtilities.GetPlugin(HideCrossingsSearcher);
        public static PluginInfo TrafficManager { get; } = PluginUtilities.GetPlugin(TMPESearcher);
        public static PluginInfo NC2 { get; } = PluginUtilities.GetPlugin(NC2Searcher);

        static DependencyUtilities()
        {
            if (NC2 is PluginInfo plugin)
                NC2StateWatcher = new PlaginStateWatcher(plugin);
        }
        public static PlaginStateWatcher NC2StateWatcher { get;}
    }
}
