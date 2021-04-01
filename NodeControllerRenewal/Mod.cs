
using ICities;
using KianCommons;
using ModsCommon;
using NodeController.GUI;
using NodeController.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

namespace NodeController
{
    public class Mod : BasePatcherMod
    {
        public override string WorkshopUrl => throw new NotImplementedException();
        protected override string ModName => "Node Controller Renewal";
        protected override string ModDescription => string.Empty;
        protected override List<Version> ModVersions => new List<Version>();

        protected override string ModId => nameof(NodeController);
        protected override bool ModIsBeta => true;
        protected override string ModLocale => string.Empty;

        public static SimulationManager.UpdateMode UpdateMode => SimulationManager.instance.m_metaData.m_updateMode;
        public static LoadMode Mode => (LoadMode)UpdateMode;
        public static bool Loaded { get; private set; }
        public static string Scene => SceneManager.GetActiveScene().name;


        protected override BasePatcher CreatePatcher() => new Patcher(this);

        public override void OnEnabled()
        {
            base.OnEnabled();

            Loaded = false;

            LoadingManager.instance.m_simulationDataReady += SimulationDataReady; // load/update data
            if (HelpersExtensions.InGameOrEditor)
                SimulationDataReady();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            LoadingManager.instance.m_simulationDataReady -= SimulationDataReady;
        }
        public static void SimulationDataReady()
        {
            try
            {
                if (Scene == "ThemeEditor")
                    return;

                CSURUtil.Init();
                if (GUI.Settings.GameConfig == null)
                {
                    GUI.Settings.GameConfig = Mode switch
                    {
                        LoadMode.NewGameFromScenario or LoadMode.LoadScenario or LoadMode.LoadMap => GameConfigT.LoadGameDefault,// no NC or old NC
                        _ => GameConfigT.NewGameDefault,
                    };
                }


                NodeManager.Instance.OnLoad();
                SegmentEndManager.Instance.OnLoad();
                NodeManager.ValidateAndHeal(true);
                Loaded = true;
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
            }
        }

        protected override void GetSettings(UIHelperBase helper)
        {
            GUI.Settings.OnSettingsUI(helper);
        }
    }
}
