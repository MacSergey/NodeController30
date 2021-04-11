using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using KianCommons;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using static KianCommons.HelpersExtensions;

namespace NodeController.GUI
{
    [Serializable]
    public class GameConfigT
    {
        public bool UnviversalSlopeFixes;

        public static GameConfigT NewGameDefault => new GameConfigT
        {
            UnviversalSlopeFixes = true,
        };

        public static GameConfigT LoadGameDefault => new GameConfigT
        {
            UnviversalSlopeFixes = false,
        };
    }

    public static class Settings
    {
        public const string FileName = nameof(NodeController);

        public static GameConfigT GameConfig { get; set; } = GameConfigT.LoadGameDefault;

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(FileName) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FileName } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            SingletonMod<Mod>.Logger.Debug("Make settings was called");
            MakeGlobalSettings(helper);
            if (!InStartup)
                MakeGameSettings(helper);
        }
        public static void MakeGlobalSettings(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Global settings") as UIHelper;
            UIPanel panel = group.self as UIPanel;

            var keymappings = panel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping(NodeControllerTool.ActivationShortcut);

            UICheckBox snapToggle = group.AddCheckbox("Snap to middle node", NodeControllerTool.SnapToMiddleNode.value, val => NodeControllerTool.SnapToMiddleNode.value = val) as UICheckBox;
            snapToggle.tooltip = "When you click near a middle node:\n - [checked] => Node controller modifies the node\n - [unchceked] => Node controller moves the node to hovered position.";
            snapToggle.eventTooltipShow += OnTooltipShow;

            UICheckBox TMPE_Overlay = group.AddCheckbox("Hide TMPE overlay on the selected node", NodeControllerTool.Hide_TMPE_Overlay.value, val => NodeControllerTool.Hide_TMPE_Overlay.value = val) as UICheckBox;
            TMPE_Overlay.tooltip = "Holding control hides all TMPE overlay.\nbut if this is checked, you don't have to (excluding Corssings/Uturn)";
            TMPE_Overlay.eventTooltipShow += OnTooltipShow;
        }

        private static void OnTooltipShow(UIComponent component, UITooltipEventParameter eventParam)
        {
            if (component.tooltipBox is UILabel label)
                label.textAlignment = UIHorizontalAlignment.Left;
        }

        static UICheckBox universalFixes_;
        public static void MakeGameSettings(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Game settings") as UIHelper;
            UIPanel panel = group.self as UIPanel;

            universalFixes_ = group.AddCheckbox("apply universal slope fixes(flat junctions, curvature of extreme slopes)", defaultValue: GameConfig?.UnviversalSlopeFixes ?? GameConfigT.NewGameDefault.UnviversalSlopeFixes, ApplyUniversalSlopeFixes) as UICheckBox;
            universalFixes_.tooltip = "changing this may influence existing custom nodes.";
        }

        public static void UpdateGameSettings()
        {
            if (GameConfig == null)
            {
                SingletonMod<Mod>.Logger.Error("GameConfig==null");
                return;
            }
            SingletonMod<Mod>.Logger.Debug($"UpdateGameSettings: UnviversalSlopeFixes ={GameConfig.UnviversalSlopeFixes}");
            if (universalFixes_)
                universalFixes_.isChecked = GameConfig.UnviversalSlopeFixes;
        }

        static void ApplyUniversalSlopeFixes(bool value)
        {
            GameConfig.UnviversalSlopeFixes = value;
            for (ushort segmentId = 0; segmentId < NetManager.MAX_SEGMENT_COUNT; ++segmentId)
            {
                if (segmentId.GetSegment().IsValid())
                {
                    // update only those that have flat junctions and not customized (custom nodes use enforced flat junctions).
                    var segment = segmentId.GetSegment();
                    Manager.GetSegmentData(segmentId, out var start, out var end);
                    if (segment.Info.m_flatJunctions == false && !segment.m_startNode.GetNode().m_flags.IsFlagSet(NetNode.Flags.Middle) && !segment.m_endNode.GetNode().m_flags.IsFlagSet(NetNode.Flags.Middle) && start == null && end == null)
                    {
                        NetManager.instance.UpdateSegment(segmentId);
                    }

                    // also update segments with extreme slopes.
                    if (segment.m_startDirection.y > 2 || segment.m_endDirection.y > 2)
                        NetManager.instance.UpdateSegment(segmentId);
                }
            }
        }
    }
}

