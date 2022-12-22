using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        private static Dictionary<Options, SavedInt> OptionsVisibility { get; } = GetOptionVisibilitySaved();
        private static Dictionary<Options, SavedInt> GetOptionVisibilitySaved()
        {
            var savedDic = new Dictionary<Options, SavedInt>();
            foreach (var option in EnumExtension.GetEnumValues<Options>(i => true))
            {
                var saved = new SavedInt($"{option}Visible", SettingsFile, (int)GetDefaultOptionVisibility(option), true);
                savedDic[option] = saved;
            }
            return savedDic;
        }
        private static OptionVisibility GetDefaultOptionVisibility(Options option) => option switch
        {
            Options.MainRoad => OptionVisibility.Visible,
            Options.Offset => OptionVisibility.Visible,
            Options.Rotate => OptionVisibility.Visible,
            Options.Shift => OptionVisibility.Visible,
            Options.Slope => OptionVisibility.Visible,
            Options.Twist => OptionVisibility.Hidden,
            Options.Stretch => OptionVisibility.Hidden,
            Options.Marking => OptionVisibility.Visible,
            Options.Collision => OptionVisibility.Hidden,
            Options.Nodeless => OptionVisibility.Hidden,
            _ => OptionVisibility.Hidden,
        };

        public static OptionVisibility GetOptionVisibility(Options option) => (OptionVisibility)OptionsVisibility[option].value;
        public static void SetOptionVisibility(Options option, OptionVisibility visibility) => OptionsVisibility[option].value = (int)visibility;

        protected UIAdvancedHelper ShortcutsTab => GetTab(nameof(ShortcutsTab));
        protected UIAdvancedHelper BackupTab => GetTab(nameof(BackupTab));

        protected override IEnumerable<KeyValuePair<string, string>> AdditionalTabs
        {
            get
            {
                yield return new KeyValuePair<string, string>(nameof(ShortcutsTab), CommonLocalize.Settings_Shortcuts);
                if (Utility.InGame)
                    yield return new KeyValuePair<string, string>(nameof(BackupTab), Localize.Settings_BackupTab);
            }
        }


        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);
            AddGeneral(GeneralTab, out var undergroundOptions);
            AddOptionVisible(GeneralTab);
            AddNotifications(GeneralTab);
            AddKeyMapping(ShortcutsTab, undergroundOptions);

            if (Utility.InGame)
                AddBackupData(BackupTab);
#if DEBUG
            AddDebug(DebugTab);
#endif
        }

        private void AddGeneral(UIAdvancedHelper helper, out OptionPanelWithLabelData undergroundOptions)
        {
            var generalGroup = helper.AddGroup(CommonLocalize.Settings_General);
            AddCheckBox(generalGroup, Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            AddLabel(generalGroup, Localize.Settings_SelectMiddleNodesDiscription, 0.8f, padding: 25);
            AddCheckBox(generalGroup, Localize.Settings_RenderNearNode, RenderNearNode);
            AddCheckBox(generalGroup, Localize.Settings_NodeIsSlopedByDefault, NodeIsSlopedByDefault);
            AddCheckboxPanel(generalGroup, Localize.Settings_InsertNode, InsertNode, new string[] { Localize.Settings_InsertNodeEnabled, string.Format(Localize.Settings_InsertNodeWithModifier, InsertModifier), Localize.Settings_InsertNodeDisabled });
            undergroundOptions = AddCheckboxPanel(generalGroup, Localize.Settings_ToggleUnderground, ToggleUndergroundMode, new string[] { string.Format(Localize.Settings_ToggleUndergroundHold, UndergroundModifier), string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut) });
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddToolButton<NodeControllerTool, NodeControllerButton>(generalGroup);
            AddCheckBox(generalGroup, Localize.Settings_LongIntersectionFix, LongIntersectionFix);
            AddLabel(generalGroup, Localize.Settings_LongIntersectionFixWarning, 0.8f, new Color32(255, 68, 68, 255), 25);
            AddLabel(generalGroup, Localize.Settings_ApplyAfterRestart, 0.8f, new Color32(255, 215, 81, 255), 25);
        }

        private void AddOptionVisible(UIAdvancedHelper helper)
        {
            var panel = helper.AddGroup(Localize.Settings_OptionsVisibility).self as UIPanel;
            panel.gameObject.AddComponent<OptionVisiblePanel>();
        }

        private void AddKeyMapping(UIAdvancedHelper helper, OptionPanelWithLabelData undergroundOptions)
        {
            var keymappingsGroup = helper.AddGroup(CommonLocalize.Settings_Shortcuts);
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

            keymappings.BindingChanged += OnBindingChanged;
            void OnBindingChanged(Shortcut shortcut)
            {
                if (shortcut == SelectNodeToolMode.EnterUndergroundShortcut || shortcut == SelectNodeToolMode.ExitUndergroundShortcut)
                    undergroundOptions.checkBoxes[1].label.text = string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut);
            }
        }

        private void AddBackupData(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup();

            AddDeleteAll(group);
            AddDump(group);
            AddRestore(group);
        }
        private void AddDeleteAll(UIHelper group)
        {
            var button = AddButton(group, Localize.Settings_DeleteDataButton, Click, 600);
            button.color = new Color32(255, 40, 40, 255);
            button.hoveredColor = new Color32(224, 40, 40, 255);
            button.pressedColor = new Color32(192, 40, 40, 255);
            button.focusedColor = button.color;

            void Click()
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Settings_DeleteDataCaption;
                messageBox.MessageText = $"{Localize.Settings_DeleteDataMessage}\n{Localize.MessageBox_CantUndone}";
                messageBox.OnButton1Click = Ñonfirmed;
            }
            bool Ñonfirmed()
            {
                SingletonManager<Manager>.Instance.RemoveAll();
                return true;
            }
        }
        private void AddDump(UIHelper group)
        {
            AddButton(group, Localize.Settings_DumpDataButton, Click, 600);

            void Click()
            {
                var result = Loader.DumpData(out string path);

                if (result)
                {
                    var messageBox = MessageBox.Show<TwoButtonMessageBox>();
                    messageBox.CaptionText = Localize.Settings_DumpDataCaption;
                    messageBox.MessageText = Localize.Settings_DumpMessageSuccess;
                    messageBox.Button1Text = Localize.Settings_CopyPathToClipboard;
                    messageBox.Button2Text = CommonLocalize.MessageBox_OK;
                    messageBox.OnButton1Click = CopyToClipboard;
                    messageBox.SetButtonsRatio(2, 1);

                    bool CopyToClipboard()
                    {
                        Clipboard.text = path;
                        return false;
                    }
                }
                else
                {
                    var messageBox = MessageBox.Show<OkMessageBox>();
                    messageBox.CaptionText = Localize.Settings_DumpDataCaption;
                    messageBox.MessageText = Localize.Settings_DumpMessageFailed;
                }
            }
        }
        private void AddRestore(UIHelper group)
        {
            AddButton(group, Localize.Settings_RestoreDataButton, Click, 600);

            void Click()
            {
                var messageBox = MessageBox.Show<ImportDataMessageBox>();
                messageBox.CaptionText = Localize.Settings_RestoreDataCaption;
                messageBox.MessageText = $"{Localize.Settings_RestoreDataMessage}\n{Localize.MessageBox_CantUndone}";

            }
        }
#if DEBUG
        public static SavedFloat SegmentId { get; } = new SavedFloat(nameof(SegmentId), SettingsFile, 0f, false);
        public static SavedFloat NodeId { get; } = new SavedFloat(nameof(NodeId), SettingsFile, 0f, false);
        public static SavedBool ExtraDebug { get; } = new SavedBool(nameof(ExtraDebug), SettingsFile, false, true);

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
            AddCheckBox(groupOther, "Show extra debug", ExtraDebug);

            static void AddAllNodes()
            {
                var netManaget = Singleton<NetManager>.instance;
                var manager = SingletonManager<Manager>.Instance;

                for (ushort i = 0; i < NetManager.MAX_NODE_COUNT; i += 1)
                {
                    var node = i.GetNode();
                    var created = node.m_flags.IsSet(NetNode.Flags.Created);
                    var isRoad = node.SegmentIds().Any(s => s.GetSegment().Info.m_netAI is RoadBaseAI);
                    if (created && isRoad)
                    {
                        _ = manager.GetOrCreateNodeData(i);
                        SingletonMod<Mod>.Logger.Debug($"Added node #{i}");
                    }
                }
            }
        }
#endif
    }

    public class ImportDataMessageBox : SimpleMessageBox
    {
        private CustomUIButton ImportButton { get; set; }
        private CustomUIButton CancelButton { get; set; }
        protected FileDropDown DropDown { get; set; }
        public ImportDataMessageBox()
        {
            ImportButton = AddButton(ImportClick);
            ImportButton.text = NodeController.Localize.Settings_Restore;
            ImportButton.Disable();
            CancelButton = AddButton(CancelClick);
            CancelButton.text = CommonLocalize.Settings_Cancel;

            AddFileList();
        }
        private void AddFileList()
        {
            DropDown = Panel.Content.AddUIComponent<FileDropDown>();
            ComponentStyle.CustomSettingsStyle(DropDown, new Vector2(DefaultWidth - 2 * Padding, 38));

            DropDown.listWidth = (int)DropDown.width;
            DropDown.listHeight = 200;
            DropDown.itemPadding = new RectOffset(14, 14, 0, 0);
            DropDown.textScale = 1.25f;
            DropDown.clampListToScreen = true;
            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;

            AddData();
            DropDown.selectedIndex = 0;
        }

        private void DropDownIndexChanged(UIComponent component, int value)
        {
            if (DropDown.SelectedObject != null)
                ImportButton.Enable();
            else
                ImportButton.Disable();
        }

        private void AddData()
        {
            foreach (var file in Loader.GetDataRestoreList())
                DropDown.AddItem(file.Key, file.Value);
        }
        private void ImportClick()
        {
            var result = Loader.ImportData(DropDown.SelectedObject);

            var resultMessageBox = MessageBox.Show<OkMessageBox>();
            resultMessageBox.CaptionText = NodeController.Localize.Settings_RestoreDataCaption;
            resultMessageBox.MessageText = result ? NodeController.Localize.Settings_RestoreDataMessageSuccess : NodeController.Localize.Settings_RestoreDataMessageFailed;

            Close();
        }

        protected virtual void CancelClick() => Close();

        public class FileDropDown : UIDropDown<string> { }
    }
}

