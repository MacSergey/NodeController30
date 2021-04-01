namespace NodeController.LifeCycle
{
    using System;
    using CitiesHarmony.API;
    using ICities;
    using KianCommons;
    using NodeController.GUI;
    using System.Diagnostics;
    using UnityEngine.SceneManagement;
    using NodeController30;
    using NodeController.Util;

    public static class LifeCycle
    {
        public static string HARMONY_ID = "CS.Kian.NodeController";
        public static bool bHotReload = false;

        public static SimulationManager.UpdateMode UpdateMode => SimulationManager.instance.m_metaData.m_updateMode;
        public static LoadMode Mode => (LoadMode)UpdateMode;
        public static string Scene => SceneManager.GetActiveScene().name;

        public static bool Loaded;

        public static void Enable()
        {
            Loaded = false;

            HarmonyHelper.EnsureHarmonyInstalled();
            LoadingManager.instance.m_simulationDataReady += SimulationDataReady; // load/update data
            if (HelpersExtensions.InGameOrEditor)
                HotReload();

            if (fastTestHarmony) HarmonyUtil.InstallHarmony(HARMONY_ID);
        }

        const bool fastTestHarmony = false;

        public static void Disable()
        {
            LoadingManager.instance.m_simulationDataReady -= SimulationDataReady;
            Unload(); // in case of hot unload

            if (fastTestHarmony) HarmonyUtil.UninstallHarmony(HARMONY_ID);
        }

        public static void OnLevelUnloading()
        {
            bHotReload = false;
            Unload(); // called when loading new game or exiting to main menu.
        }

        public static void HotReload()
        {
            bHotReload = true;
            SimulationDataReady();
            OnLevelLoaded(Mode);
        }

        public static void SimulationDataReady()
        {
            try
            {
                //Log.Info($"LifeCycle.SimulationDataReady() called. mode={Mode} updateMode={UpdateMode}, scene={Scene}");
                //System.Threading.Thread.Sleep(1000 * 50); //50 sec
                //Log.Info($"LifeCycle.SimulationDataReady() after sleep");

                if (Scene == "ThemeEditor")
                    return;
                CSURUtil.Init();
                if (Settings.GameConfig == null)
                {
                    switch (Mode)
                    {
                        case LoadMode.NewGameFromScenario:
                        case LoadMode.LoadScenario:
                        case LoadMode.LoadMap:
                            // no NC or old NC
                            Settings.GameConfig = GameConfigT.LoadGameDefault;
                            break;
                        default:
                            Settings.GameConfig = GameConfigT.NewGameDefault;
                            break;
                    }
                }

                HarmonyUtil.InstallHarmony(HARMONY_ID); // game config is checked in patch.

                NodeManager.Instance.OnLoad();
                SegmentEndManager.Instance.OnLoad();
                NodeManager.ValidateAndHeal(true);
                Loaded = true;
                Mod.Logger.Debug("LifeCycle.SimulationDataReady() sucessful");
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
            }
        }

        public static void OnLevelLoaded(LoadMode mode)
        {
            // after level has been loaded.
            if (Loaded)
            {
                NodeControllerTool.Create();
            }
        }

        public static void Unload()
        {
            if (!Loaded) return; //protect against disabling from main menu.
            Mod.Logger.Debug("LifeCycle.Unload() called");
            HarmonyUtil.UninstallHarmony(HARMONY_ID);
            Settings.GameConfig = null;
            NodeControllerTool.Remove();
            Loaded = false;
        }
    }
}
