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

        internal static bool InGameOrEditor => SceneManager.GetActiveScene().name != "IntroScreen" && SceneManager.GetActiveScene().name != "Startup";

        [Obsolete("use Helpers.InStartupMenu instead")]
        internal static bool InStartup => InStartupMenu;
        internal static bool InStartupMenu => SceneManager.GetActiveScene().name == "IntroScreen" || SceneManager.GetActiveScene().name == "Startup";

        public static bool HandleNullBool(this bool? res, ref bool __result)
        {
            if (res.HasValue)
            {
                __result = res.Value;
                return false;
            }
            else
                return true;
        }
    }
}
