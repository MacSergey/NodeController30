using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Settings;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModsCommon.Settings.Helper;

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
        public static SavedInt OverlayOpacity { get; } = new SavedInt(nameof(OverlayOpacity), SettingsFile, 100, true);
        public static bool IsInsertEnable => InsertNode != 2;
        public static bool IsInsertWithModifier => InsertNode == 1;
        public static bool IsUndegroundWithModifier => ToggleUndergroundMode == 0;
        public static string InsertModifier => LocalizeExtension.Ctrl;
        public static string UndergroundModifier => LocalizeExtension.Shift;

        private static Dictionary<Options, SavedInt> OptionsVisibility { get; } = GetOptionVisibilitySaved();
        private static Dictionary<Options, SavedInt> GetOptionVisibilitySaved()
        {
            var savedDic = new Dictionary<Options, SavedInt>();
            foreach (var option in EnumExtension.GetEnumValues<Options>(i => true).Order())
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
            Options.LeftCornerPos => OptionVisibility.Visible,
            Options.RightCornerPos => OptionVisibility.Visible,
            Options.LeftCornerDir => OptionVisibility.Visible,
            Options.RightCornerDir => OptionVisibility.Visible,
            Options.LeftCornerFlatEnd => OptionVisibility.Visible,
            Options.RightCornerFlatEnd => OptionVisibility.Visible,
            _ => OptionVisibility.Hidden,
        };

        public static OptionVisibility GetOptionVisibility(Options option)
        {
            var visible = (OptionVisibility)OptionsVisibility[option].value;
            if (option != Options.Marking || DependencyUtilities.HideCrossings?.isEnabled == true || visible != OptionVisibility.Visible)
                return visible;
            else
                return OptionVisibility.Hidden;

        }
        public static void SetOptionVisibility(Options option, OptionVisibility visibility) => OptionsVisibility[option].value = (int)visibility;

        protected UIComponent ShortcutsTab => GetTabContent(nameof(ShortcutsTab));
        protected UIComponent BackupTab => GetTabContent(nameof(BackupTab));

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

        private void AddGeneral(UIComponent helper, out OptionPanelData undergroundOptions)
        {
            var generalSection = helper.AddOptionsSection(CommonLocalize.Settings_General);
            var middleNodeGroup = generalSection.AddItemsGroup();
            var toggle = middleNodeGroup.AddToggle(Localize.Settings_SelectMiddleNodes, SelectMiddleNodes);
            toggle.PaddingBottom = 0;
            middleNodeGroup.AddInfoLabel(Localize.Settings_SelectMiddleNodesDiscription, 0.8f, new Color32(255, 215, 81, 255));

            generalSection.AddToggle(Localize.Settings_RenderNearNode, RenderNearNode);
            generalSection.AddToggle(Localize.Settings_NodeIsSlopedByDefault, NodeIsSlopedByDefault);
            generalSection.AddTogglePanel(Localize.Settings_InsertNode, InsertNode, new string[] { Localize.Settings_InsertNodeEnabled, string.Format(Localize.Settings_InsertNodeWithModifier, InsertModifier), Localize.Settings_InsertNodeDisabled });
            undergroundOptions = generalSection.AddTogglePanel(Localize.Settings_ToggleUnderground, ToggleUndergroundMode, new string[] { string.Format(Localize.Settings_ToggleUndergroundHold, UndergroundModifier), string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut) });
            generalSection.AddToggle(CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddToolButton<NodeControllerTool, NodeControllerButton>(generalSection);
            var overlayField = generalSection.AddIntField(Localize.Settings_OverlayOpacity, OverlayOpacity, 10, 100);
            overlayField.Control.width = 60f;
            overlayField.Control.Format = "{0}%";

            var longFixGroup = generalSection.AddItemsGroup();
            toggle = longFixGroup.AddToggle(Localize.Settings_LongIntersectionFix, LongIntersectionFix);
            toggle.PaddingBottom = 0;
            longFixGroup.AddInfoLabel(Localize.Settings_LongIntersectionFixWarning, 0.8f, new Color32(255, 68, 68, 255));
            longFixGroup.AddInfoLabel(Localize.Settings_ApplyAfterRestart, 0.8f, new Color32(255, 215, 81, 255));
        }

        private void AddOptionVisible(UIComponent helper)
        {
            var section = helper.AddOptionsSection(Localize.Settings_OptionsVisibility);
            foreach (var option in EnumExtension.GetEnumValues<Options>().Order())
            {
                var item = section.AddUIComponent<OptionVisibilitySettingsItem>();
                item.Label = option.Description();
                item.Option = option;
            }
        }

        private void AddKeyMapping(UIComponent helper, OptionPanelData undergroundOptions)
        {
            var keymappings = helper.AddOptionsSection(CommonLocalize.Settings_Shortcuts);
            keymappings.AddKeyMappingButton(NodeControllerTool.ActivationShortcut);

            keymappings.AddKeyMappingButton(SelectNodeToolMode.SelectionStepOverShortcut);
            keymappings.AddKeyMappingButton(SelectNodeToolMode.EnterUndergroundShortcut, OnBindingChanged);
            keymappings.AddKeyMappingButton(SelectNodeToolMode.ExitUndergroundShortcut, OnBindingChanged);

            keymappings.AddKeyMappingButton(EditNodeToolMode.ResetOffsetShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.ResetToDefaultShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.MakeStraightEndsShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.CalculateShiftByNearbyShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.CalculateShiftByIntersectionsShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.SetShiftBetweenIntersectionsShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.CalculateTwistByNearbyShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.CalculateTwistByIntersectionsShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.SetTwistBetweenIntersectionsShortcut);
            //keymappings.AddKeymapping(EditNodeToolMode.ChangeNodeStyleShortcut);
            keymappings.AddKeyMappingButton(EditNodeToolMode.ChangeMainRoadModeShortcut);

            void OnBindingChanged(Shortcut shortcut)
            {
                undergroundOptions.checkBoxes.SetLabel(1, string.Format(Localize.Settings_ToggleUndergroundButtons, SelectNodeToolMode.EnterUndergroundShortcut, SelectNodeToolMode.ExitUndergroundShortcut));
            }
        }

        private void AddBackupData(UIComponent helper)
        {
            var section = helper.AddOptionsSection();

            AddDeleteAll(section);
            AddDump(section);
            AddRestore(section);
        }
        private void AddDeleteAll(CustomUIPanel group)
        {
            var buttonPanel = group.AddButtonPanel();
            var button = buttonPanel.AddButton(Localize.Settings_DeleteDataButton, Click, 600);
            button.BgColors = new ColorSet(new Color32(255, 40, 40, 255), new Color32(224, 40, 40, 255), new Color32(192, 40, 40, 255), new Color32(255, 40, 40, 255), default);

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
        private void AddDump(CustomUIPanel group)
        {
            var buttonPanel = group.AddButtonPanel();
            buttonPanel.AddButton(Localize.Settings_DumpDataButton, Click, 600);

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
        private void AddRestore(CustomUIPanel group)
        {
            var buttonPanel = group.AddButtonPanel();
            buttonPanel.AddButton(Localize.Settings_RestoreDataButton, Click, 600);

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

        private void AddDebug(UIComponent helper)
        {
            var overlaySection = helper.AddOptionsSection("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlaySection);
            Selection.AddRenderOverlayCentre(overlaySection);
            Selection.AddRenderOverlayBorders(overlaySection);
            Selection.AddBorderOverlayWidth(overlaySection);


            var otherSection = helper.AddOptionsSection("Other");

            otherSection.AddFloatField("SegmentId", SegmentId, 0f);
            otherSection.AddFloatField("NodeId", NodeId, 0f);

            var buttonPanel = otherSection.AddButtonPanel();
            buttonPanel.AddButton("Add all nodes", AddAllNodes, 200f);
            buttonPanel.AddButton("Clear", SingletonManager<Manager>.Destroy, 200f);
            otherSection.AddToggle("Show extra debug", ExtraDebug);

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
        protected StringDropDown DropDown { get; set; }
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
            DropDown = Content.AddUIComponent<StringDropDown>();
            ComponentStyle.DropDownMessageBoxStyle(DropDown, new Vector2(DefaultWidth - 2 * DefaultPadding, 38));
            DropDown.EntityTextScale = 1f;

            DropDown.textScale = 1.25f;
            DropDown.OnSelectObject += DropDownValueChanged;

            var files = Loader.GetDataRestoreList();
            foreach (var file in files)
                DropDown.AddItem(file.Key, file.Value);

            DropDown.SelectedObject = files.FirstOrDefault().Key;
            DropDown.OnSetPopupStyle += SetPopupStyle;
            DropDown.OnSetEntityStyle += SetEntityStyle;

            DropDownValueChanged(DropDown.SelectedObject);
        }
        private void SetPopupStyle(StringDropDown.StringPopup popup, ref bool overridden)
        {
            popup.PopupSettingsStyle<DropDownItem<string>, StringDropDown.StringEntity, StringDropDown.StringPopup>();
            overridden = true;
        }
        private void SetEntityStyle(StringDropDown.StringEntity entity, ref bool overridden)
        {
            entity.EntitySettingsStyle<DropDownItem<string>, StringDropDown.StringEntity>();
            overridden = true;
        }

        private void DropDownValueChanged(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
                ImportButton.Enable();
            else
                ImportButton.Disable();
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
    }
}

