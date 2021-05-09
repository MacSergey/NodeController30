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
            if (!IsSelectedSide && !InputExtension.ShiftIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                foreach (var target in Targets)
                {
                    var hitPos = Tool.Ray.GetRayPosition(target.Position.y, out _);
                    if ((target.Position - hitPos).sqrMagnitude < SegmentEndData.CenterDotRadius * SegmentEndData.CenterDotRadius)
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
            var segmentSide = GetSideLane(SelectedSide.Data, SelectedSide.Type);
            var alignSide = GetSideLane(HoverSide.Data, HoverSide.Type);
            var newShift = (SelectedSide.Type != HoverSide.Type ? -1 : 1) * (HoverSide.Data.Shift + segmentSide) - alignSide;
            newShift = Mathf.Clamp(newShift, NodeStyle.MinShift, NodeStyle.MaxShift);

            if (Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                SelectedSide.Data.Shift = newShift;
            else
                Tool.Data.Shift = Tool.Data.FirstMainSegmentEnd == SelectedSide.Data ? newShift : -newShift;

            Tool.Data.UpdateNode();
            Tool.Panel.UpdatePanel();
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
                else if (segmentData != SelectedSide.Data && Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                    Targets.Add(segmentData[SelectedSide.Type.Invert()]);
            }

            if (IsSelectedSide)
            {
                if (Tool.Data.Style.SupportShift.IsSet(SupportOption.Individually))
                {
                    var data = SingletonManager<Manager>.Instance.GetSegmentData(SelectedSide.Data.Id, !SelectedSide.Data.IsStartNode);
                    Targets.Add(data[SelectedSide.Type.Invert()]);
                }
                else if (!Tool.Data.IsJunction)
                {
                    foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                    {
                        if (SingletonManager<Manager>.Instance.GetSegmentData(segmentEnd.Id, !segmentEnd.IsStartNode) is SegmentEndData otherData)
                            Targets.Add(otherData[segmentEnd == SelectedSide.Data ? SelectedSide.Type.Invert() : SelectedSide.Type]);
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
                if (lane.IsGroundLane())
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
                    return Localize.Tool_InfoClickToSelectFirstAlign;
            }
            else
            {
                if (!IsHoverSide)
                    return Localize.Tool_InfoSelectAlignRelative;
                else
                    return Localize.Tool_InfoApplyAlign;
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var width = SegmentEndData.CenterDotRadius * 2;

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var defaultColor = new OverlayData(cameraInfo) { Color = segmentData.OverlayColor };
                segmentData.RenderСontour(defaultColor);
                segmentData.RenderEnd(defaultColor);
            }

            foreach (var target in Targets)
            {
                if (target != HoverSide)
                    target.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, Width = width });
            }

            if (IsHoverSide)
                HoverSide.RenderCircle(new OverlayData(cameraInfo) { Width = width });

            if (IsSelectedSide)
                SelectedSide.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Purple, Width = width });
        }
    }
}
