using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NodeController
{
    public class NodeControllerTool : BaseTool<Mod, NodeControllerTool, ToolModeType>
    {
        public static NodeControllerShortcut ActivationShortcut { get; } = new NodeControllerShortcut(nameof(ActivationShortcut), nameof(Localize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.N, true, false, false));

        protected override bool ShowToolTip => (Settings.ShowToolTip || Mode.Type == ToolModeType.Select) && !Panel.IsHover;
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
            yield return CreateToolMode<ChangeMainSlopeDirectionToolMode>();
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
            Data = data;
            Data?.UpdateNode();
            Panel.SetData(Data);
        }
        public void SetKeepDefaults()
        {
            Data.SetKeepDefaults();
            Panel.RefreshPanel();
        }
        public void ResetToDefault()
        {
            Data.ResetToDefault();
            Panel.SetPanel();
        }
        public void MakeStraightEnds()
        {
            Data.MakeStraightEnds();
            Panel.RefreshPanel();
        }
    }
    public abstract class NodeControllerToolMode : BaseToolMode<NodeControllerTool>, IToolMode<ToolModeType>, IToolModePanel
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
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension<NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension<NodeControllerTool> { }
}
