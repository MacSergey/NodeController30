using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
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
        public static SavedBool NodeIsSlopedByDefault { get; } = new SavedBool(nameof(NodeIsSlopedByDefault), SettingsFile, false, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedInt InsertNode { get; } = new SavedInt(nameof(InsertNode), SettingsFile, 0, true);
        public static SavedInt ToggleUndergroundMode { get; } = new SavedInt(nameof(ToggleUndergroundMode), SettingsFile, 0, true);
        public static SavedBool LongIntersectionFix { get; } = new SavedBool(nameof(LongIntersectionFix), SettingsFile, false, true);
        public static bool IsInsertEnable => InsertNode != 2;
        public static bool IsInsertWithModifier => InsertNode == 1;
        public static bool IsUndegroundWithModifier => ToggleUndergroundMode == 0;
        public static string InsertModifier => LocalizeExtension.Ctrl;
        public static string UndergroundModifier => LocalizeExtension.Shift;

        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);


            var keymappingsGroup = GeneralTab.AddGroup(CommonLocalize.Settings_Shortcuts);
            var keymappings = AddKeyMappingPanel(keymappingsGroup);
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);

            keymappings.AddKeymapping(SelectNodeToolMode.SelectionStepOverShortcut);
            keymappings.AddKeymapping(SelectNodeToolMode.EnterUndergroundShortcut);
            keymappings.AddKeymapping(SelectNodeToolMode.ExitUndergroundShortcut);

            keymappings.AddKeymapping(EditNodeToolMode.ResetOffsetShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.ResetToDefaultShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.MakeStraightEndsShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.CalculateShiftByNearbyShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.CalculateShiftByIntersectionsShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.SetShiftBetweenIntersectionsShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.CalculateTwistByNearbyShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.CalculateTwistByIntersectionsShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.SetTwistBetweenIntersectionsShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.ChangeNodeStyleShortcut);
            keymappings.AddKeymapping(EditNodeToolMode.ChangeMainRoadModeShortcut);


            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);
            AddCheckBox(generalGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            AddLabel(generalGroup, Localize.Settings_SelectMiddleNodesDiscription, 0.8f, padding: 25);
            AddCheckBox(generalGroup, Localize.Settings_RenderNearNode, RenderNearNode);
            AddCheckBox(generalGroup, Localize.Settings_NodeIsSlopedByDefault, NodeIsSlopedByDefault);
            AddCheckboxPanel(generalGroup, Localize.Settings_InsertNode, InsertNode, new string[] { Localize.Settings_InsertNodeEnabled, string.Format(Localize.Settings_InsertNodeWithModifier, InsertModifier), Localize.Settings_InsertNodeDisabled });
            var undergroundOptions = AddCheckboxPanel(generalGroup, Localize.Settings_ToggleUnderground, ToggleUndergroundMode, new string[] { string.Format(Localize.Settings_ToggleUndergroundHold, UndergroundModifier), string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut) });
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddToolButton<NodeControllerTool, NodeControllerButton>(generalGroup);
            AddCheckBox(generalGroup, Localize.Settings_LongIntersectionFix, LongIntersectionFix);
            //AddLabel(generalGroup, Localize.Settings_LongIntersectionFixWarning, 0.8f, Color.red, 25);
            AddLabel(generalGroup, Localize.Settings_ApplyAfterRestart, 0.8f, Color.yellow, 25);

            AddNotifications(GeneralTab);
#if DEBUG
            AddDebug(DebugTab);
#endif

            keymappings.BindingChanged += OnBindingChanged;
            void OnBindingChanged(Shortcut shortcut)
            {
                if (shortcut == SelectNodeToolMode.EnterUndergroundShortcut || shortcut == SelectNodeToolMode.ExitUndergroundShortcut)
                    undergroundOptions.checkBoxes[1].label.text = string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut);
            }
        }

#if DEBUG
        public static SavedFloat SegmentId { get; } = new SavedFloat(nameof(SegmentId), SettingsFile, 0f, false);
        public static SavedFloat NodeId { get; } = new SavedFloat(nameof(NodeId), SettingsFile, 0f, false);

        private void AddDebug(UIAdvancedHelper helper)
        {
            var overlayGroup = helper.AddGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);


            var groupOther = helper.AddGroup("Other");

            AddFloatField(groupOther, "SegmentId", SegmentId, 0f);
            AddFloatField(groupOther, "NodeId", NodeId, 0f);
            AddButton(groupOther, "Add all nodes", AddAllNodes, 200f);
            AddButton(groupOther, "Clear", SingletonManager<Manager>.Destroy, 200f);

            static void AddAllNodes()
            {
                var netManaget = Singleton<NetManager>.instance;
                var manager = SingletonManager<Manager>.Instance;

                for (ushort i = 0; i < NetManager.MAX_NODE_COUNT; i += 1)
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

