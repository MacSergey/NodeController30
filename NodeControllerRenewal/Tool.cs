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
    public class NodeControllerTool : BaseTool<NodeControllerTool, ToolModeType>
    {
        public static void Create() => Create<NodeControllerTool>();

        public static string SettingsFile => $"{nameof(NodeController30)}{nameof(SettingsFile)}";
        public static Shortcut ActivationShortcut { get; } = new Shortcut(SettingsFile, nameof(ActivationShortcut), "Activation", SavedInputKey.Encode(KeyCode.N, false, false, true));

        protected override bool ShowToolTip => true;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override bool IsActivationPressed => ActivationShortcut.InputKey.IsKeyUp();

        public new static NodeControllerTool Instance
        {
            get => BaseTool.Instance as NodeControllerTool;
            set => BaseTool.Instance = value;
        }
        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
        }
        public override void Enable() => Enable<NodeControllerTool>();

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
    public class NodeControllerThreadingExtension : BaseThreadingExtension { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension { }
}
