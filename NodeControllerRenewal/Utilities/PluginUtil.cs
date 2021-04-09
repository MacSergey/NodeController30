namespace KianCommons.Plugins
{
    using System;
    using ColossalFramework.Plugins;
    using ICities;
    using System.Reflection;
    using ColossalFramework;
    using static ColossalFramework.Plugins.PluginManager;
    using ColossalFramework.PlatformServices;
    using UnityEngine.Assertions;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using NodeController;
    using ModsCommon;

    public static class PluginExtensions
    {
        public static ulong GetWorkshopID(this PluginInfo plugin) => plugin.publishedFileID.AsUInt64;
        public static bool IsActive(this PluginInfo plugin) => plugin?.isEnabled ?? false;
        public static Assembly GetMainAssembly(this PluginInfo plugin) => plugin?.userModInstance?.GetType()?.Assembly;
    }

    public static class PluginUtil
    {
        public static PluginInfo GetCSUR() => GetPlugin("CSUR ToolBox", 1959342332ul);
        public static PluginInfo GetAdaptiveRoads() => GetPlugin("AdaptiveRoads");
        public static PluginInfo GetHideCrossings() => GetPlugin("HideCrosswalks", searchOptions: AssemblyEquals);
        public static PluginInfo GetTrafficManager() => GetPlugin("TrafficManager", searchOptions: AssemblyEquals);
        public static PluginInfo GetNetworkDetective() => GetPlugin("NetworkDetective", searchOptions: AssemblyEquals);
        public static PluginInfo GetNetworkSkins() => GetPlugin("NetworkSkins", searchOptions: AssemblyEquals);

        [Flags]
        public enum SearchOptionT
        {
            None = 0,

            Contains = 1 << 0,

            StartsWidth = 1 << 1,

            [Obsolete("always active")]
            Equals = 1 << 2,

            AllModes = Contains | StartsWidth,

            /// <summary></summary>
            CaseInsensetive = 1 << 3,

            /// <summary></summary>
            IgnoreWhiteSpace = 1 << 4,

            AllOptions = CaseInsensetive | IgnoreWhiteSpace,

            /// <summary>search for IUserMod.Name</summary>
            UserModName = 1 << 5,

            /// <summary>search for the type of user mod instance excluding name space</summary>
            UserModType = 1 << 6,

            /// <summary>search for the root name space of user mod type</summary>
            RootNameSpace = 1 << 7,

            /// <summary>search for the PluginInfo.name</summary>
            PluginName = 1 << 8,

            /// <summary>search for the name of the main assembly</summary>
            AssemblyName = 1 << 9,

            AllTargets = UserModName | UserModType | RootNameSpace | PluginName | AssemblyName,
        }


        public const SearchOptionT DefaultsearchOptions = SearchOptionT.Contains | SearchOptionT.AllOptions | SearchOptionT.UserModName;

        public const SearchOptionT AssemblyEquals = SearchOptionT.AllOptions | SearchOptionT.AssemblyName;

        public static PluginInfo GetPlugin(string searchName, ulong searchId, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            return GetPlugin(searchName, new[] { searchId }, searchOptions);
        }

        public static PluginInfo GetPlugin(string searchName, ulong[] searchIds = null, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            foreach (PluginInfo current in PluginManager.instance.GetPluginsInfo())
            {
                if (current == null)
                    continue;

                bool match = Matches(current, searchIds);

                IUserMod userModInstance = current.userModInstance as IUserMod;
                if (userModInstance == null)
                    continue;

                if (searchOptions.IsFlagSet(SearchOptionT.UserModName))
                    match = match || Match(userModInstance.Name, searchName, searchOptions);

                Type userModType = userModInstance.GetType();
                if (searchOptions.IsFlagSet(SearchOptionT.UserModType))
                    match = match || Match(userModType.Name, searchName, searchOptions);

                if (searchOptions.IsFlagSet(SearchOptionT.RootNameSpace))
                {
                    string ns = userModType.Namespace;
                    string rootNameSpace = ns.Split('.')[0];
                    match = match || Match(rootNameSpace, searchName, searchOptions);
                }

                if (searchOptions.IsFlagSet(SearchOptionT.PluginName))
                    match = match || Match(current.name, searchName, searchOptions);

                if (searchOptions.IsFlagSet(SearchOptionT.AssemblyName))
                {
                    Assembly asm = current.GetMainAssembly();
                    match = match || Match(asm?.Name(), searchName, searchOptions);
                }

                if (match)
                    return current;
            }
            return null;
        }

        public static bool Match(string name1, string name2, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            if (string.IsNullOrEmpty(name1))
                return false;

            if (searchOptions.IsFlagSet(SearchOptionT.CaseInsensetive))
            {
                name1 = name1.ToLower();
                name2 = name2.ToLower();
            }
            if (searchOptions.IsFlagSet(SearchOptionT.IgnoreWhiteSpace))
            {
                name1 = name1.Replace(" ", "");
                name2 = name2.Replace(" ", "");
            }

            if (name1 == name2)
                return true;
            if (searchOptions.IsFlagSet(SearchOptionT.Contains))
            {
                if (name1.Contains(name2))
                    return true;
            }
            if (searchOptions.IsFlagSet(SearchOptionT.StartsWidth))
            {
                if (name1.StartsWith(name2))
                    return true;
            }
            return false;
        }

        public static bool Matches(PluginInfo plugin, ulong[] searchIds)
        {
            if (searchIds == null)
                return false;

            foreach (var id in searchIds)
            {
                if (id == 0)
                {
                    SingletonMod<Mod>.Logger.Error("unexpected 0 as mod search id");
                    continue;
                }
                if (id == plugin.GetWorkshopID())
                    return true;
            }
            return false;
        }
    }
}
