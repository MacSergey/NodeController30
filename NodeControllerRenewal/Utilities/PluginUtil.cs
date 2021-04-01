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
    using NodeController30;

    public static class PluginExtensions
    {
        public static IUserMod GetUserModInstance(this PluginInfo plugin) => plugin.userModInstance as IUserMod;

        public static string GetModName(this PluginInfo plugin) => GetUserModInstance(plugin).Name;

        public static ulong GetWorkshopID(this PluginInfo plugin) => plugin.publishedFileID.AsUInt64;

        /// <summary>
        /// shortcut for plugin?.isEnabled ?? false
        /// </summary>
        public static bool IsActive(this PluginInfo plugin) => plugin?.isEnabled ?? false;

        public static Assembly GetMainAssembly(this PluginInfo plugin) => plugin?.userModInstance?.GetType()?.Assembly;

        public static bool IsLocal(this PluginInfo plugin) =>
            plugin.GetWorkshopID() == 0 || plugin.publishedFileID == PublishedFileId.invalid;
    }

    public static class PluginUtil
    {
        static PluginManager man => PluginManager.instance;

        public static PluginInfo GetCurrentAssemblyPlugin() => GetPlugin(Assembly.GetExecutingAssembly());

        public static void LogPlugins(bool detailed = false)
        {
            string PluginToString(PluginInfo p)
            {
                string enabled = p.isEnabled ? "*" : " ";
                string id = p.IsLocal() ? "(local)" : p.GetWorkshopID().ToString();
                id.PadRight(12);
                if (!detailed)
                    return $"\t{enabled} {id} {p.GetModName()}";
#pragma warning disable
                return $"\t{enabled} " +
                    $"{id} " +
                    $"mod-name:{p.GetModName()} " +
                    $"asm-name:{p.GetMainAssembly()?.Name()} " +
                    $"user-mod-type:{p?.userModInstance?.GetType().Name}";
#pragma warning restore
            }

            var plugins = man.GetPluginsInfo().ToList();
            plugins.Sort((a, b) => b.isEnabled.CompareTo(a.isEnabled)); // enabled first
            var m = plugins.Select(p => PluginToString(p)).JoinLines();
            Mod.Logger.Debug("Installed mods are:\n" + m);
        }


        public static void ReportIncomaptibleMods(IEnumerable<PluginInfo> plugins)
        {
            // TODO complete:
        }

        public static PluginInfo GetCSUR() => GetPlugin("CSUR ToolBox", 1959342332ul);
        public static PluginInfo GetAdaptiveRoads() => GetPlugin("AdaptiveRoads");
        public static PluginInfo GetHideCrossings() => GetPlugin("HideCrosswalks", searchOptions: AssemblyEquals);
        public static PluginInfo GetTrafficManager() => GetPlugin("TrafficManager", searchOptions: AssemblyEquals);
        public static PluginInfo GetNetworkDetective() => GetPlugin("NetworkDetective", searchOptions: AssemblyEquals);
        public static PluginInfo GetNetworkSkins() => GetPlugin("NetworkSkins", searchOptions: AssemblyEquals);


        [Obsolete]
        internal static bool CSUREnabled;
        [Obsolete]
        static bool IsCSUR(PluginInfo current) =>
            current.name.Contains("CSUR ToolBox") || 1959342332 == (uint)current.publishedFileID.AsUInt64;
        [Obsolete]
        public static void Init()
        {
            CSUREnabled = false;
            foreach (PluginInfo current in man.GetPluginsInfo())
            {
                if (!current.isEnabled) continue;
                if (IsCSUR(current))
                {
                    CSUREnabled = true;
                    Mod.Logger.Debug(current.name + "detected");
                    return;
                }
            }
        }

        public static PluginInfo GetPlugin(IUserMod userMod)
        {
            foreach (PluginInfo current in man.GetPluginsInfo())
            {
                if (userMod == current.userModInstance)
                    return current;
            }
            return null;
        }

        public static PluginInfo GetPlugin(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();
            foreach (PluginInfo current in man.GetPluginsInfo())
            {
                if (current.ContainsAssembly(assembly))
                    return current;
            }
            return null;
        }

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


        public const SearchOptionT DefaultsearchOptions =
            SearchOptionT.Contains | SearchOptionT.AllOptions | SearchOptionT.UserModName;

        public const SearchOptionT AssemblyEquals =
            SearchOptionT.AllOptions | SearchOptionT.AssemblyName;

        public static PluginInfo GetPlugin(
            string searchName, ulong searchId, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            return GetPlugin(searchName, new[] { searchId }, searchOptions);
        }

        public static PluginInfo GetPlugin(
            string searchName, ulong[] searchIds = null, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            foreach (PluginInfo current in PluginManager.instance.GetPluginsInfo())
            {
                if (current == null) continue;

                bool match = Matches(current, searchIds);

                IUserMod userModInstance = current.userModInstance as IUserMod;
                if (userModInstance == null) continue;

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
                {
                    Mod.Logger.Debug("Found plugin:" + current.GetModName());
                    return current;
                }
            }
            Mod.Logger.Debug($"plugin not found: keyword={searchName} options={searchOptions}");
            return null;
        }

        public static bool Match(string name1, string name2, SearchOptionT searchOptions = DefaultsearchOptions)
        {
            if (string.IsNullOrEmpty(name1)) return false;
            Assertion.Assert((searchOptions & SearchOptionT.AllTargets) != 0);

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

            Mod.Logger.Debug($"[MATCHING] : {name1} =? {name2} " + searchOptions);

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
            Assertion.AssertNotNull(plugin);
            if (searchIds == null)
                return false;
            foreach (var id in searchIds)
            {
                if (id == 0)
                {
                    Mod.Logger.Error("unexpected 0 as mod search id");
                    continue;
                }
                if (id == plugin.GetWorkshopID())
                    return true;
            }
            return false;
        }
    }
}
