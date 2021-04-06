using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController
{
    public class NodeControllerTool : BaseTool<Mod, NodeControllerTool, ToolModeType>
    {
        public static string SettingsFile => $"{nameof(NodeController)}{nameof(SettingsFile)}";
        public static Shortcut ActivationShortcut { get; } = new Shortcut(SettingsFile, nameof(ActivationShortcut), "Activation", SavedInputKey.Encode(KeyCode.N, false, false, true));
        public static readonly SavedBool SnapToMiddleNode = new SavedBool("SnapToMiddleNode", NodeController.GUI.Settings.FileName, def: false, true);

        public static readonly SavedBool Hide_TMPE_Overlay = new SavedBool("Hide_TMPE_Overlay", NodeController.GUI.Settings.FileName, def: false, true);

        protected override bool ShowToolTip => true;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override Shortcut Activation => ActivationShortcut;

        public ushort SelectedSegmentID { get; set; }
        public ushort SelectedNodeID { get; set; }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
        }
    }
    public abstract class NodeControllerToolMode : BaseToolMode<Mod, NodeControllerTool>, IToolMode<ToolModeType>, IToolModePanel
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;
    }
    public enum ToolModeType
    {
        None = 0,
        Select = 1,
    }
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension<Mod, NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension<Mod, NodeControllerTool> { }
}
