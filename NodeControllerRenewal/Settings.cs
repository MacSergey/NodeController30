using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine.SceneManagement;
using static ModsCommon.SettingsHelper;

namespace NodeController
{
    public class Settings : BaseSettings<Mod>
    {
        public static SavedBool SelectMiddleNodes { get; } = new SavedBool(nameof(SelectMiddleNodes), SettingsFile, true, true);

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
            AddCheckBox(mainGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);

            var notificationsGroup = GeneralTab.AddGroup(Localize.Settings_Notifications) as UIHelper;

            AddCheckBox(notificationsGroup, Localize.Settings_ShowWhatsNew, ShowWhatsNew);
            AddCheckBox(notificationsGroup, Localize.Settings_ShowOnlyMajor, ShowOnlyMajor);

#if DEBUG
            var debugTab = CreateTab("Debug");
            AddDebug(debugTab);
#endif
        }
#if DEBUG
        private void AddDebug(UIHelperBase helper)
        {
            var group = helper.AddGroup("Debug") as UIHelper;

            AddCheckBox(group, "Alpha blend overlay", Selection.AlphaBlendOverlay);
            AddCheckBox(group, "Render overlay center", Selection.RenderOverlayCentre);
            AddCheckBox(group, "Render overlay borders", Selection.RenderOverlayBorders);
            AddFloatField(group, "Overlay width", Selection.OverlayWidth, 3f, 1f);
        }
#endif
    }
}

