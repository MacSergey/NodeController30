namespace KianCommons
{
    using ICities;
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class HelpersExtensions
    {
        internal static bool InGameOrEditor => SceneManager.GetActiveScene().name != "IntroScreen" && SceneManager.GetActiveScene().name != "Startup";
        internal static bool InStartupMenu => SceneManager.GetActiveScene().name == "IntroScreen" || SceneManager.GetActiveScene().name == "Startup";
    }
}
