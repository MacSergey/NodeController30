using ColossalFramework;
using ColossalFramework.UI;
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

        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                if (Mode is IShortcutMode mode)
                {
                    foreach (var shortcut in mode.Shortcuts)
                        yield return shortcut;
                }
            }
        }

        protected override bool ShowToolTip => base.ShowToolTip && (Settings.ShowToolTip || Mode.Type == ToolModeType.Select);
        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        public override Shortcut Activation => ActivationShortcut;
        public NodeControllerPanel Panel => SingletonItem<NodeControllerPanel>.Instance;

        protected override UITextureAtlas UUIAtlas => NodeControllerTextures.Atlas;
        protected override string UUINormalSprite => NodeControllerTextures.UUIButtonNormal;
        protected override string UUIHoveredSprite => NodeControllerTextures.UUIButtonHovered;
        protected override string UUIPressedSprite => NodeControllerTextures.UUIButtonPressed;
        protected override string UUIDisabledSprite => /*NodeControllerTextures.UUIDisabled;*/string.Empty;

        public NodeData Data { get; private set; }
        public bool IsUnderground => Data?.IsUnderground ?? false;

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectNodeToolMode>();
            yield return CreateToolMode<EditNodeToolMode>();
            yield return CreateToolMode<DragSegmentEndToolMode>();
            yield return CreateToolMode<DragCornerToolMode>();
            yield return CreateToolMode<RotateSegmentEndToolMode>();
            yield return CreateToolMode<RotateCornerToolMode>();
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
            CalculateValue(s => s.Shift, (s, v) => s.Shift = v, 1);
            Panel.RefreshPanel();
        }
        public void CalculateShiftByIntersections()
        {
            CalculateValue(s => s.Shift, (s, v) => s.Shift = v);
            Panel.RefreshPanel();
        }
        public void SetShiftBetweenIntersections() => SetBetweenIntersections(n => n.Shift, (s, v) => s.Shift = v);

        public void CalculateTwistByNearby()
        {
            CalculateValue(s => s.TwistAngle, (s, v) => s.TwistAngle = v, 1);
            Panel.RefreshPanel();
        }
        public void CalculateTwistByIntersections()
        {
            CalculateValue(s => s.TwistAngle, (s, v) => s.TwistAngle = v);
            Panel.RefreshPanel();
        }
        public void SetTwistBetweenIntersections() => SetBetweenIntersections(n => n.TwistAngle, (s, v) => s.TwistAngle = v);

        public void SetBetweenIntersections(Func<NodeData, float> dataGetter, Action<SegmentEndData, float> dataSetter)
        {
            if (!Data.IsTwoRoads)
                return;

            if (GetDatas(ushort.MaxValue, out var datas, out var segments))
            {
                var value = -dataGetter(Data);
                SetValue(datas, segments, value, value, true, dataSetter);
            }

            Panel.RefreshPanel();
        }

        //public void ChangeNodeStyle()
        //{
        //    if (Data.Style.SupportMode != SupportOption.None)
        //    {
        //        Data.IsSlopeJunctions = !Data.IsSlopeJunctions;
        //        Data.UpdateNode();
        //        Panel.SetPanel();
        //    }
        //}
        public void ChangeMainRoadMode()
        {
            if (Data.IsJunction && Data.Style.SupportMode != SupportOption.None)
            {
                Data.MainRoad.Auto = !Data.MainRoad.Auto;
                Data.UpdateNode();
                Panel.SetPanel();
            }
        }

        private void CalculateValue(Func<SegmentEndData, float> dataGetter, Action<SegmentEndData, float> dataSetter, ushort maxCount = ushort.MaxValue)
        {
            if (!Data.IsTwoRoads)
                return;

            if (GetDatas(maxCount, out var datas, out var segments))
            {
                var startValue = 0f;
                var endValue = 0f;
                if(datas.First().TryGetSegment(segments.First(), out var firstSegmentData))
                    startValue = dataGetter(firstSegmentData);
                if (datas.Last().TryGetSegment(segments.Last(), out var lastSegmentData))
                    endValue = -dataGetter(lastSegmentData);

                SetValue(datas, segments, startValue, endValue, false, dataSetter);
            }
        }
        private bool GetDatas(ushort maxCount, out NodeData[] datas, out ushort[] segments)
        {
            var nodeIds = new List<ushort>();

            nodeIds.AddRange(Data.Id.NextNodes(Data.MainRoad.First, true, maxCount).Reverse());
            nodeIds.Add(Data.Id);
            nodeIds.AddRange(Data.Id.NextNodes(Data.MainRoad.Second, true, maxCount));

            var manager = SingletonManager<Manager>.Instance;
            datas = nodeIds.Select(id => manager.GetOrCreateNodeData(id)).ToArray();
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
        private void SetValue(NodeData[] datas, ushort[] segments, float startValue, float endValue, bool includeEnds, Action<SegmentEndData, float> dataSetter)
        {
            var lengths = segments.Select(i => new BezierTrajectory(ref i.GetSegment()).Length).ToArray();
            var fullLength = lengths.Sum();
            var currentLength = 0f;

            for (var i = 0; i < datas.Length; i += 1)
            {
                if (i == 0)
                {
                    if (includeEnds)
                    {
                        if(datas[i].TryGetSegment(segments[i], out var segmentData))
                            dataSetter(segmentData, startValue);
                    }      
                }
                else if (i == datas.Length - 1)
                {
                    if (includeEnds)
                    {
                        if (datas[i].TryGetSegment(segments[i - 1], out var segmentData))
                            dataSetter(segmentData, -endValue);
                    }
                }
                else
                {
                    currentLength += lengths[i - 1];
                    var value = Mathf.Lerp(startValue, endValue, currentLength / fullLength);
                    if (datas[i].TryGetSegment(segments[i - 1], out var segmentData1))
                        dataSetter(segmentData1, -value);
                    if (datas[i].TryGetSegment(segments[i], out var segmentData2))
                        dataSetter(segmentData2, value);
                    datas[i].UpdateNode();
                }
            }
        }
    }
    public abstract class NodeControllerToolMode : BaseToolMode<NodeControllerTool>, IToolMode<ToolModeType>, IToolModePanel, IShortcutMode
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;
        protected bool IsUnderground => Tool.IsUnderground;

        public virtual IEnumerable<Shortcut> Shortcuts { get { yield break; } }

        public static Color32 Green => CommonColors.Green.SetOpacity(Settings.OverlayOpacity);
        public static Color32 Red => CommonColors.Red.SetOpacity(Settings.OverlayOpacity);
        public static Color32 Yellow => CommonColors.Yellow.SetOpacity(Settings.OverlayOpacity);
        public static Color32 Purple => CommonColors.Purple.SetOpacity(Settings.OverlayOpacity);
    }
    public interface IShortcutMode
    {
        public IEnumerable<Shortcut> Shortcuts { get; }
    }
    public enum ToolModeType
    {
        None = 0,
        Select = 1,
        Edit = 2,
        DragEnd = 4,
        DragCorner = 8,
        RotateEnd = 16,
        RotateCorner = 32,
        ChangeMain = 64,
        Aling = 128,
    }
    public class NodeControllerShortcut : ToolShortcut<Mod, NodeControllerTool, ToolModeType>
    {
        public NodeControllerShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.Edit) : base(name, labelKey, key, action, modeType) { }
    }
    public class NodeControllerToolThreadingExtension : BaseUUIThreadingExtension<NodeControllerTool> { }
    public class NodeControllerToolLoadingExtension : BaseUUIToolLoadingExtension<NodeControllerTool> { }
}
