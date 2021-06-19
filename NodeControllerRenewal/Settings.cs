using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ModsCommon.SettingsHelper;

namespace NodeController
{
    public class Settings : BaseSettings<Mod>
    {
        public static SavedBool SelectMiddleNodes { get; } = new SavedBool(nameof(SelectMiddleNodes), SettingsFile, true, true);
        public static SavedBool RenderNearNode { get; } = new SavedBool(nameof(RenderNearNode), SettingsFile, true, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);

        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);

            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);

            var keymappings = AddKeyMappingPanel(generalGroup);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);

            AddCheckBox(generalGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            AddLabel(generalGroup, Localize.Settings_SelectMiddleNodesDiscription, 0.8f, padding: 25);
            AddCheckBox(generalGroup, Localize.Settings_RenderNearNode, RenderNearNode);
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);

            AddNotifications(GeneralTab);
#if DEBUG
            AddDebug(DebugTab);
#endif
        }

#if DEBUG
        public static SavedFloat SegmentId { get; } = new SavedFloat(nameof(SegmentId), SettingsFile, 0f, false);
        public static SavedFloat NodeId { get; } = new SavedFloat(nameof(NodeId), SettingsFile, 0f, false);

        private void AddDebug(UIAdvancedHelper helper)
        {
            var overlayGroup = helper.AddGroup("Selection overlay");

            AddCheckBox(overlayGroup, "Alpha blend overlay", Selection.AlphaBlendOverlay);
            AddCheckBox(overlayGroup, "Render overlay center", Selection.RenderOverlayCentre);
            AddCheckBox(overlayGroup, "Render overlay borders", Selection.RenderOverlayBorders);
            AddFloatField(overlayGroup, "Overlay width", Selection.OverlayWidth, 3f, 1f);


            var groupOther = helper.AddGroup("Other");

            AddFloatField(groupOther, "SegmentId", SegmentId, 0f);
            AddFloatField(groupOther, "NodeId", NodeId, 0f);
            AddButton(groupOther, "Add all nodes", AddAllNodes, 200f);
            AddButton(groupOther, "Clear", SingletonManager<Manager>.Destroy, 200f);

            static void AddAllNodes()
            {
                var netManaget = Singleton<NetManager>.instance;
                var manager = SingletonManager<Manager>.Instance;

                for (ushort i = 0; i < NetManager.MAX_NODE_COUNT; i+=1)
                {
                    var node = i.GetNode();
                    var created = node.m_flags.IsSet(NetNode.Flags.Created);
                    var isRoad = node.Segments().Any(s => s.Info.m_netAI is RoadBaseAI);
                    if (created && isRoad)
                    {
                        _ = manager[i, Manager.Options.Create];
                        SingletonMod<Mod>.Logger.Debug($"Added node #{i}");
                    }
                }
                manager.UpdateAll();
            }
        }
#endif
    }
}

