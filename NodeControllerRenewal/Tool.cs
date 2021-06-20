using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
        protected override void OnReset()
        {
            Data = null;
            SetInfoMode();
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
            SetInfoMode();
        }
        private void SetInfoMode() => Singleton<InfoManager>.instance.SetCurrentMode(Data?.IsUnderground == true ? InfoManager.InfoMode.Underground : InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);

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
        public void CalculateShiftByNearby()
        {
            CalculateShift(1);
            Panel.RefreshPanel();
        }
        public void CalculateShiftByIntersections()
        {
            CalculateShift();
            Panel.RefreshPanel();
        }
        public void SetShiftBetweenIntersections()
        {
            if (!Data.IsTwoRoads)
                return;

            if (GetDatas(ushort.MaxValue, out var datas, out var segments))
            {
                var shift = -Data.Shift;
                SetShift(datas, segments, shift, shift, true);
            }

            Panel.RefreshPanel();
        }

        private void CalculateShift(ushort maxCount = ushort.MaxValue)
        {
            if (!Data.IsTwoRoads)
                return;

            if (GetDatas(maxCount, out var datas, out var segments))
            {
                var startShift = datas.First()[segments.First()].Shift;
                var endShift = -datas.Last()[segments.Last()].Shift;
                SetShift(datas, segments, startShift, endShift, false);
            }
        }
        private bool GetDatas(ushort maxCount, out NodeData[] datas, out ushort[] segments)
        {
            var nodeIds = new List<ushort>();

            nodeIds.AddRange(Data.Id.NextNodes(Data.MainRoad.First, true, maxCount).Reverse());
            nodeIds.Add(Data.Id);
            nodeIds.AddRange(Data.Id.NextNodes(Data.MainRoad.Second, true, maxCount));

            var manager = SingletonManager<Manager>.Instance;
            datas = nodeIds.Select(i => manager[i, true]).ToArray();
            segments = new ushort[nodeIds.Count - 1];

            if (datas.Any(d => d == null))
                return false;

            for (var i = 0; i < datas.Length - 1; i += 1)
            {
                var dataA = datas[i];
                var dataB = datas[i + 1];

                if (dataA.MainRoad.First == dataB.MainRoad.First || dataA.MainRoad.First == dataB.MainRoad.Second)
                    segments[i] = dataA.MainRoad.First;
                else if (dataA.MainRoad.Second == dataB.MainRoad.First || dataA.MainRoad.Second == dataB.MainRoad.Second)
                    segments[i] = dataA.MainRoad.Second;
                else
                    return false;
            }

            return true;
        }
        private void SetShift(NodeData[] datas, ushort[] segments, float startShift, float endShift, bool includeEnds)
        {
            var lengths = segments.Select(i => new BezierTrajectory(ref i.GetSegment()).Length).ToArray();
            var fullLength = lengths.Sum();
            var currentLength = 0f;

            for (var i = 0; i < datas.Length; i += 1)
            {
                if (i == 0)
                {
                    if (includeEnds)
                        datas[i][segments[i]].Shift = startShift;
                }
                else if (i == datas.Length - 1)
                {
                    if (includeEnds)
                        datas[i][segments[i - 1]].Shift = -endShift;
                }
                else
                {
                    currentLength += lengths[i - 1];
                    var shift = Mathf.Lerp(startShift, endShift, currentLength / fullLength);
                    datas[i][segments[i - 1]].Shift = -shift;
                    datas[i][segments[i]].Shift = shift;
                    datas[i].UpdateNode(false);
                }
            }
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
