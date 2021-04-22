using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.UI;
using System.Collections.Generic;
using UnityEngine;

namespace NodeController
{
    public class NodeControllerTool : BaseTool<Mod, NodeControllerTool, ToolModeType>
    {
        public static Shortcut ActivationShortcut { get; } = new Shortcut(Settings.SettingsFile, nameof(ActivationShortcut), "Activation", SavedInputKey.Encode(KeyCode.N, false, false, true));

        protected override bool ShowToolTip => !Panel.IsHover;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override Shortcut Activation => ActivationShortcut;
        public NodeControllerPanel Panel => SingletonItem<NodeControllerPanel>.Instance;

        public NodeData Data { get; private set; }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectNodeToolMode>();
            yield return CreateToolMode<EditNodeToolMode>();
            yield return CreateToolMode<DragSegmentEndToolMode>();
            yield return CreateToolMode<DragCornerToolMode>();
            yield return CreateToolMode<RotateSegmentEndToolMode>();
            yield return CreateToolMode<ChangeMainRoadToolMode>();
            yield return CreateToolMode<AlignSegmentEndsToolMode>();
        }

        protected override void InitProcess()
        {
            base.InitProcess();
            NodeControllerPanel.CreatePanel();
        }

        public void SetDefaultMode() => SetMode(ToolModeType.Edit);
        protected override void SetModeNow(IToolMode mode)
        {
            base.SetModeNow(mode);
            Panel.Active = (Mode as NodeControllerToolMode)?.ShowPanel == true;
        }
        public void SetData(NodeData data)
        {
            Data?.UpdateNode();
            Data = data;
            Panel.SetData(data);
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
        Edit = 2,
        DragEnd = 4,
        DragCorner = 8,
        Rotate = 16,
        ChangeMain = 32,
        Aling = 64,
    }
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension<Mod, NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension<Mod, NodeControllerTool> { }
}
