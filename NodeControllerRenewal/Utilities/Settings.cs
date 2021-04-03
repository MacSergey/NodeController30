namespace NodeController.GUI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using ICities;
    using KianCommons;
    using ModsCommon;
    using NodeController;
    using System;
    using static KianCommons.HelpersExtensions;

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
            // Creating setting file - from SamsamTS
            if (GameSettings.FindSettingsFileByName(FileName) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FileName } });
            }
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            Mod.Logger.Debug("Make settings was called");
            MakeGlobalSettings(helper);
            if (!InStartup)
                MakeGameSettings(helper);
        }

        public static void FixTooltipAlignment(UIComponent component)
        {
            component.eventTooltipShow += (c, _) =>
            {
                if (c.tooltipBox is UILabel label)
                    label.textAlignment = UIHorizontalAlignment.Left;
            };
        }

        public static void MakeGlobalSettings(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Global settings") as UIHelper;
            UIPanel panel = group.self as UIPanel;

            var keymappings = panel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping("Activation Shortcut", NodeControllerTool.ActivationShortcut.InputKey);

            UICheckBox snapToggle = group.AddCheckbox(
                "Snap to middle node",
                NodeControllerTool.SnapToMiddleNode.value,
                val => NodeControllerTool.SnapToMiddleNode.value = val) as UICheckBox;
            snapToggle.tooltip = "when you click near a middle node:\n" +
                " - [checked] => Node controller modifies the node\n" +
                " - [unchceked] => Node controller moves the node to hovered position.";
            FixTooltipAlignment(snapToggle);

            UICheckBox TMPE_Overlay = group.AddCheckbox(
                "Hide TMPE overlay on the selected node",
                NodeControllerTool.Hide_TMPE_Overlay.value,
                val => NodeControllerTool.Hide_TMPE_Overlay.value = val) as UICheckBox;
            TMPE_Overlay.tooltip = "Holding control hides all TMPE overlay.\n" +
                "but if this is checked, you don't have to (excluding Corssings/Uturn)";
            FixTooltipAlignment(TMPE_Overlay);

        }

        static UICheckBox universalFixes_;
        public static void MakeGameSettings(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Game settings") as UIHelper;

            UIPanel panel = group.self as UIPanel;

            object val = GameConfig?.UnviversalSlopeFixes; val = val ?? "null";
            Mod.Logger.Debug($"MakeGameSettings: UnviversalSlopeFixes =" + val);
            universalFixes_ = group.AddCheckbox("apply universal slope fixes(flat junctions, curvature of extreme slopes)", defaultValue: GameConfig?.UnviversalSlopeFixes ?? GameConfigT.NewGameDefault.UnviversalSlopeFixes, ApplyUniversalSlopeFixes) as UICheckBox;
            universalFixes_.tooltip = "changing this may influence existing custom nodes.";
        }

        public static void UpdateGameSettings()
        {
            if (GameConfig == null)
            {
                Mod.Logger.Error("GameConfig==null");
                return;
            }
            Mod.Logger.Debug($"UpdateGameSettings: UnviversalSlopeFixes =" + GameConfig.UnviversalSlopeFixes);
            if (universalFixes_)
                universalFixes_.isChecked = GameConfig.UnviversalSlopeFixes;
        }

        static void ApplyUniversalSlopeFixes(bool value)
        {
            GameConfig.UnviversalSlopeFixes = value;
            for (ushort segmentID = 0; segmentID < NetManager.MAX_SEGMENT_COUNT; ++segmentID)
            {
                if (NetUtil.IsSegmentValid(segmentID))
                {
                    // update only those that have flat junctions and not customized (custom nodes use enforced flat junctions).
                    if (segmentID.ToSegment().Info.m_flatJunctions == false &&
                        !segmentID.ToSegment().m_startNode.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle) &&
                        !segmentID.ToSegment().m_endNode.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle) &&
                        SegmentEndManager.Instance.GetAt(segmentID, true) == null &&
                        SegmentEndManager.Instance.GetAt(segmentID, false) == null)
                    {
                        NetManager.instance.UpdateSegment(segmentID);
                    }

                    // also update segments with extreme slopes.
                    if (segmentID.ToSegment().m_startDirection.y > 2 ||
                        segmentID.ToSegment().m_endDirection.y > 2)
                    {
                        NetManager.instance.UpdateSegment(segmentID);
                    }
                }
            }
        }
    }
}

