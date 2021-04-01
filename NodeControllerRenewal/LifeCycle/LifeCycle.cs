namespace NodeController.LifeCycle
{
    using System;
    using CitiesHarmony.API;
    using ICities;
    using KianCommons;
    using NodeController.GUI;
    using System.Diagnostics;
    using UnityEngine.SceneManagement;
    using NodeController;
    using NodeController.Util;

    public static class LifeCycle
    {
        public static string HARMONY_ID = "CS.Kian.NodeController";
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
        }

        public static void Disable()
        {
            LoadingManager.instance.m_simulationDataReady -= SimulationDataReady;
        }
        public static void HotReload()
        {
            SimulationDataReady();
        }

        public static void SimulationDataReady()
        {
            try
            {
                if (Scene == "ThemeEditor")
                    return;

                CSURUtil.Init();
                if (Settings.GameConfig == null)
                {
                    Settings.GameConfig = Mode switch
                    {
                        LoadMode.NewGameFromScenario or LoadMode.LoadScenario or LoadMode.LoadMap => GameConfigT.LoadGameDefault,// no NC or old NC
                        _ => GameConfigT.NewGameDefault,
                    };
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
    }
}
