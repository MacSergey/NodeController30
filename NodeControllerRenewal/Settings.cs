using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ModsCommon.SettingsHelper;

namespace NodeController
{
    public class Settings : BaseSettings<Mod>
    {
        public static SavedBool SelectMiddleNodes { get; } = new SavedBool(nameof(SelectMiddleNodes), SettingsFile, true, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);

        protected override void OnSettingsUI()
        {
            var languageGroup = GeneralTab.AddGroup(Localize.Settings_Language) as UIHelper;
            AddLanguageList(languageGroup);


            var generalGroup = GeneralTab.AddGroup(Localize.Settings_General) as UIHelper;

            var keymappings = AddKeyMappingPanel(generalGroup);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);

            AddCheckBox(generalGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            AddLabel(generalGroup, Localize.Settings_SelectMiddleNodesDiscription, 0.8f, padding: 25);
            AddCheckBox(generalGroup, Localize.Settings_ShowTooltips, ShowToolTip);


            var notificationsGroup = GeneralTab.AddGroup(Localize.Settings_Notifications) as UIHelper;

            AddCheckBox(notificationsGroup, Localize.Settings_ShowWhatsNew, ShowWhatsNew);
            AddCheckBox(notificationsGroup, Localize.Settings_ShowOnlyMajor, ShowOnlyMajor);

#if DEBUG
            var debugTab = CreateTab("Debug");
            AddDebug(debugTab);
#endif
        }
#if DEBUG

        public static SavedFloat SegmentId { get; } = new SavedFloat(nameof(SegmentId), SettingsFile, 0f, false);
        public static SavedFloat NodeId { get; } = new SavedFloat(nameof(NodeId), SettingsFile, 0f, false);
        private void AddDebug(UIHelperBase helper)
        {
            var group = helper.AddGroup("Debug") as UIHelper;

            AddCheckBox(group, "Alpha blend overlay", Selection.AlphaBlendOverlay);
            AddCheckBox(group, "Render overlay center", Selection.RenderOverlayCentre);
            AddCheckBox(group, "Render overlay borders", Selection.RenderOverlayBorders);
            AddFloatField(group, "Overlay width", Selection.OverlayWidth, 3f, 1f);
            AddFloatField(group, "SegmentId", SegmentId, 0f);
            AddFloatField(group, "NodeId", NodeId, 0f);
        }
#endif
    }
}

