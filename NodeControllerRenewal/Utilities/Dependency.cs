using ModsCommon.Utilities;
using System;
using System.Linq;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController.Utilities
{
    public static class DependencyUtilities
    {
        public static ulong HideCrosswalksBetaId => 1939169189ul;
        public static ulong HideCrosswalksId => 1934023593ul;
        public static PluginSearcher HideCrossingsSearcher { get; } = PluginUtilities.GetSearcher("RM Crossings", HideCrosswalksBetaId, HideCrosswalksId);
        public static PluginSearcher TMPESearcher { get; } = PluginUtilities.GetSearcher("TM:PE", 1637663252ul, 1806963141ul, 2489276785ul);
        public static PluginSearcher NC2Searcher { get; } = new IdSearcher(2085403475ul) | (IdSearcher.Invalid & new UserModNameSearcher("Node Controller") & !new UserModNameSearcher("Node Controller Renewal"));

        public static PluginInfo HideCrossings => PluginUtilities.GetPlugin(HideCrossingsSearcher);
        public static PluginInfo TrafficManager => PluginUtilities.GetPlugin(TMPESearcher);
        public static PluginInfo NC2 => PluginUtilities.GetPlugin(NC2Searcher);
    }
}
