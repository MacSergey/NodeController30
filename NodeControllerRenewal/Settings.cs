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
        public static SavedBool SnapToggle { get; } = new SavedBool(nameof(SnapToggle), SettingsFile, true, true);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        protected override void OnSettingsUI()
        {
            var languageGroup = GeneralTab.AddGroup(Localize.Settings_Language) as UIHelper;
            AddLanguageList(languageGroup);

            var mainGroup = GeneralTab.AddGroup();

            var keymappings = AddKeyMappingPanel(mainGroup);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);
            AddCheckBox(mainGroup, Localize.Settings_SnapToMiddleNode, SnapToggle);

            var notificationsGroup = GeneralTab.AddGroup(Localize.Settings_Notifications) as UIHelper;

            AddCheckBox(notificationsGroup, Localize.Settings_ShowWhatsNew, ShowWhatsNew);
            AddCheckBox(notificationsGroup, Localize.Settings_ShowOnlyMajor, ShowOnlyMajor);
        }
    }
}

