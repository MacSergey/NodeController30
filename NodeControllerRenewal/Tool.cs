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

namespace NodeController30
{
    public class NodeControllerTool : BaseTool<ToolModeType>
    {
        public static string SettingsFile => $"{nameof(NodeController30)}{nameof(SettingsFile)}";
        public static Shortcut ActivationShortcut { get; } = new Shortcut(SettingsFile, nameof(ActivationShortcut), "Activation", SavedInputKey.Encode(KeyCode.N, false, false, true));

        protected override bool ShowToolTip => true;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override bool IsActivationPressed => ActivationShortcut.InputKey.IsKeyUp();

        public static void Create() => Create<NodeControllerTool>();
        public new static NodeControllerTool Instance => BaseTool.Instance as NodeControllerTool;
        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
        }
    }
    public abstract class NodeControllerToolMode : BaseToolMode, IToolMode<ToolModeType>, IToolModePanel
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;
        protected new NodeControllerTool Tool => NodeControllerTool.Instance;
        protected NodeControllerTool Panel => NodeControllerTool.Instance;
    }
    public enum ToolModeType
    {
        None = 0,
        Select = 1,
    }
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension { }
}
