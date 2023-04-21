using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController
{
    public class AlignSegmentEndsToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Aling;
        public override bool ShowPanel => false;

        private List<SegmentSide> Targets { get; set; } = new List<SegmentSide>();

        private SegmentSide HoverSide { get; set; }
        private bool IsHoverSide => HoverSide != null;

        private SegmentSide SelectedSide { get; set; }
        private bool IsSelectedSide => SelectedSide != null;

        protected override void Reset(IToolMode prevMode)
        {
            HoverSide = null;
            SelectedSide = null;

            SelectTargets();
        }
        public override void OnToolUpdate()
        {
            if (!IsSelectedSide && !Utility.OnlyShiftIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                foreach (var target in Targets)
                {
                    var hitPos = Tool.Ray.GetRayPosition(target.MarkerPos.y, out _);
                    if ((target.MarkerPos - hitPos).sqrMagnitude < SegmentEndData.CenterRadius * SegmentEndData.CenterRadius)
                    {
                        HoverSide = target;
                        return;
                    }
                }
            }

            HoverSide = null;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsSelectedSide)
            {
                if (IsHoverSide)
                {
                    SelectedSide = HoverSide;
                    SelectTargets();
                }
            }
            else if (IsHoverSide)
            {
                SetAlign();
                Tool.SetDefaultMode();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSelectedSide)
            {
                SelectedSide = null;
                SelectTargets();
            }
            else
                Tool.SetDefaultMode();
        }
        private void SetAlign()
        {
            var segmentSide = GetSideLane(SelectedSide.SegmentData, SelectedSide.Type);
            var alignSide = GetSideLane(HoverSide.SegmentData, HoverSide.Type);
            var newShift = (SelectedSide.Type != HoverSide.Type ? -1 : 1) * (HoverSide.SegmentData.Shift + segmentSide) - alignSide;
            newShift = Mathf.Clamp(newShift, NodeStyle.MinShift, NodeStyle.MaxShift);

            if (Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                SelectedSide.SegmentData.Shift = newShift;
            else
                Tool.Data.Shift = Tool.Data.FirstMainSegmentEnd == SelectedSide.SegmentData ? newShift : -newShift;

            Tool.Data.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        private void SelectTargets()
        {
            Targets.Clear();

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                if (segmentData.IsUntouchable)
                    continue;

                if (!IsSelectedSide)
                {
                    Targets.Add(segmentData[SideType.Left]);
                    Targets.Add(segmentData[SideType.Right]);
                }
                else if (segmentData != SelectedSide.SegmentData && Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                    Targets.Add(segmentData[SelectedSide.Type.Invert()]);
            }

            if (IsSelectedSide)
            {
                if (Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                {
                    if (SingletonManager<Manager>.Instance.TryGetSegmentData(SelectedSide.SegmentData.Id, !SelectedSide.SegmentData.IsStartNode, out var data))
                        Targets.Add(data[SelectedSide.Type.Invert()]);
                }
                else if (!Tool.Data.IsJunction)
                {
                    foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                    {
                        if (SingletonManager<Manager>.Instance.TryGetSegmentData(segmentEnd.Id, !segmentEnd.IsStartNode, out var otherData))
                            Targets.Add(otherData[segmentEnd == SelectedSide.SegmentData ? SelectedSide.Type.Invert() : SelectedSide.Type]);
                    }
                }
            }
        }
        private float GetSideLane(SegmentEndData segmentEnd, SideType side)
        {
            var segment = segmentEnd.Id.GetSegment();
            var isStart = segmentEnd.IsStartNode;
            var isInvert = segment.IsInvert();
            var isLeft = side == SideType.Left;

            var isLaneInvert = isStart ^ isInvert;
            var info = segment.Info;

            foreach (var i in isLaneInvert ^ !isLeft ? info.m_sortedLanes : info.m_sortedLanes.Reverse())
            {
                var lane = info.m_lanes[i];
                if ((lane.m_vehicleType & (VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Bicycle | VehicleInfo.VehicleType.Tram | VehicleInfo.VehicleType.Trolleybus | VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Metro | VehicleInfo.VehicleType.Monorail)) != 0)
                    return ((isLaneInvert ? -1 : 1) * lane.m_position + (isLeft ? 0.5f : -0.5f) * lane.m_width) * segmentEnd.Stretch;
            }

            return 0f;
        }
        public override string GetToolInfo()
        {
            if (!IsSelectedSide)
            {
                if (!IsHoverSide)
                    return Localize.Tool_InfoSelectToAlign;
                else
                    return Localize.Tool_InfoClickToSelectFirstAlign.AddActionColor();
            }
            else
            {
                if (!IsHoverSide)
                    return Localize.Tool_InfoSelectAlignRelative;
                else
                    return Localize.Tool_InfoApplyAlign.AddActionColor();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var width = SegmentEndData.CenterRadius * 2;
            var underground = IsUnderground;

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var defaultColor = new OverlayData(cameraInfo) { Color = segmentData.OverlayColor, RenderLimit = underground };
                segmentData.RenderContour(defaultColor);
                segmentData.RenderStart(defaultColor);
            }

            foreach (var target in Targets)
            {
                if (target != HoverSide)
                    target.RenderCenter(new OverlayData(cameraInfo) { Color = Yellow, Width = width, RenderLimit = underground });
            }

            if (IsHoverSide)
                HoverSide.RenderCenter(new OverlayData(cameraInfo) { Width = width, RenderLimit = underground });

            if (IsSelectedSide)
                SelectedSide.RenderCenter(new OverlayData(cameraInfo) { Color = Purple, Width = width, RenderLimit = underground });
        }
    }
}
