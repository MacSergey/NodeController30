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
            AddLanguage(GeneralTab);


            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);

            var keymappings = AddKeyMappingPanel(generalGroup);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);

            AddCheckBox(generalGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            AddLabel(generalGroup, Localize.Settings_SelectMiddleNodesDiscription, 0.8f, padding: 25);
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);

            AddNotifications(GeneralTab);

#if DEBUG
            var debugTab = CreateTab("Debug");
            AddDebug(debugTab);
#endif
        }
#if DEBUG

        public static SavedFloat SegmentId { get; } = new SavedFloat(nameof(SegmentId), SettingsFile, 0f, false);
        public static SavedFloat NodeId { get; } = new SavedFloat(nameof(NodeId), SettingsFile, 0f, false);

        private void AddDebug(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup("Debug");

            AddCheckBox(group, "Alpha blend overlay", Selection.AlphaBlendOverlay);
            AddCheckBox(group, "Render overlay center", Selection.RenderOverlayCentre);
            AddCheckBox(group, "Render overlay borders", Selection.RenderOverlayBorders);
            AddFloatField(group, "Overlay width", Selection.OverlayWidth, 3f, 1f);
            AddFloatField(group, "SegmentId", SegmentId, 0f);
            AddFloatField(group, "NodeId", NodeId, 0f);
            AddButton(group, "Add all nodes", AddAllNodes, 200f);
            AddButton(group, "Clear", SingletonManager<Manager>.Destroy, 200f);

            AddHarmonyReport(group);

            static void AddAllNodes()
            {
                var netManaget = Singleton<NetManager>.instance;
                for(ushort i = 0; i < NetManager.MAX_NODE_COUNT; i+=1)
                {
                    var node = i.GetNode();
                    if(node.m_flags.IsSet(NetNode.Flags.Created))
                        _ = SingletonManager<Manager>.Instance[i, true];
                }
            }
        }
#endif
    }
}

