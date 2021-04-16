using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.UI;
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
        public static readonly SavedBool SnapToMiddleNode = new SavedBool("SnapToMiddleNode", GUI.Settings.FileName, def: false, true);

        public static readonly SavedBool Hide_TMPE_Overlay = new SavedBool("Hide_TMPE_Overlay", GUI.Settings.FileName, def: false, true);

        protected override bool ShowToolTip => true;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override Shortcut Activation => ActivationShortcut;
        public NodeControllerPanel Panel => SingletonItem<NodeControllerPanel>.Instance;

        public NodeData Data { get; private set; }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
            yield return CreateToolMode<EditToolMode>();
            yield return CreateToolMode<DragSegmentEndToolMode>();
            yield return CreateToolMode<RotateSegmentEndToolMode>();
            yield return CreateToolMode<ChangeMainRoadToolMode>();
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
        Drag = 4,
        Rotate = 8,
        ChangeMain = 16,
    }
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension<Mod, NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension<Mod, NodeControllerTool> { }
}
