namespace KianCommons
{
    using ICities;
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class HelpersExtensions
    {
        internal static bool InSimulationThread() => System.Threading.Thread.CurrentThread == SimulationManager.instance.m_simulationThread;

        internal static bool[] ALL_BOOL = new bool[] { false, true };

        internal static AppMode currentMode => SimulationManager.instance.m_ManagersWrapper.loading.currentMode;
        internal static bool CheckGameMode(AppMode mode)
        {
            try
            {
                if (currentMode == mode)
                    return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// determines if simulation is inside game/editor. useful to detect hot-reload.
        /// </summary>
        internal static bool InGameOrEditor => SceneManager.GetActiveScene().name != "IntroScreen" && SceneManager.GetActiveScene().name != "Startup";


        /// <summary>
        /// checks if game is loaded in and user is playing a city. (returns false early in the loading process)
        /// </summary>
        internal static bool InGame => CheckGameMode(AppMode.Game);

        /// <summary>
        /// checks if game is loaded in asset editor mod. (returns false early in the loading process)
        /// </summary>
        internal static bool InAssetEditor => CheckGameMode(AppMode.AssetEditor);

        [Obsolete]
        internal static bool IsActive => InGameOrEditor;

        [Obsolete("use Helpers.InStartupMenu instead")]
        internal static bool InStartup => Helpers.InStartupMenu;
    }

    internal static class Helpers
    {
        internal static void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        internal static bool InStartupMenu => SceneManager.GetActiveScene().name == "IntroScreen" || SceneManager.GetActiveScene().name == "Startup";
    }


}
