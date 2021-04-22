using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine.SceneManagement;

namespace NodeController
{
    public class Settings : BaseSettings<Mod>
    {
        public static string SettingsFile => $"{nameof(NodeController)}{nameof(SettingsFile)}";

        public static SavedBool SnapToggle { get; } = new SavedBool(nameof(SnapToggle), SettingsFile, true, true);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        protected override void OnSettingsUI()
        {
            var group = GeneralTab.AddGroup();

            var keymappings = AddKeyMappingPanel(group);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);
            AddCheckBox(group, "Snap toggle", SnapToggle);
        }
    }
}

