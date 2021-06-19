using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeController
{
    public class NodeControllerTool : BaseTool<Mod, NodeControllerTool, ToolModeType>
    {
        public static NodeControllerShortcut ActivationShortcut { get; } = new NodeControllerShortcut(nameof(ActivationShortcut), nameof(CommonLocalize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.N, true, false, false));

        protected override bool ShowToolTip => (Settings.ShowToolTip || Mode.Type == ToolModeType.Select) && !Panel.IsHover;
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override Shortcut Activation => ActivationShortcut;
        public NodeControllerPanel Panel => SingletonItem<NodeControllerPanel>.Instance;

        public NodeData Data { get; private set; }
        public bool IsUnderground => Data?.IsUnderground ?? false;

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
        protected override void OnToolUpdate()
        {
            if(Data is NodeData data)
            {
                Singleton<InfoManager>.instance.SetCurrentMode(data.IsUnderground ? InfoManager.InfoMode.Underground : InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            }

            base.OnToolUpdate();
        }

        protected override bool CheckInfoMode(InfoManager.InfoMode mode, InfoManager.SubInfoMode subInfo) => (mode == InfoManager.InfoMode.None || mode == InfoManager.InfoMode.Underground) && subInfo == InfoManager.SubInfoMode.Default;

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
        protected bool IsUnderground => Tool.IsUnderground;
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
    public class NodeControllerShortcut : ModShortcut<Mod>
    {
        public NodeControllerShortcut(string name, string labelKey, InputKey key, Action action = null) : base(name, labelKey, key, action) { }
    }
    public class NodeControllerToolThreadingExtension : BaseThreadingExtension<NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseToolLoadingExtension<NodeControllerTool> { }
}
