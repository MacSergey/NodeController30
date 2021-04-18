using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;
using ColossalFramework;
using ModsCommon;
using NodeController.Patches;

namespace NodeController
{
    public class AlignSegmentEndsToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Aling;
        public override bool ShowPanel => false;

        private SegmentEndData HoverSegmentEnd { get; set; }
        private SideType HoverSide { get; set; }
        private bool IsHoverSegmentEnd => HoverSegmentEnd != null;

        private SegmentEndData SelectedSegmentEnd { get; set; }
        private SideType SelectedSide { get; set; }
        private bool IsSelectedSegmentEnd => SelectedSegmentEnd != null;
        private bool IsLeftSelected => IsSelectedSegmentEnd && SelectedSide == SideType.Left;
        private bool IsRightSelected => IsSelectedSegmentEnd && SelectedSide == SideType.Right;

        private IEnumerable<SideType> Sides
        {
            get
            {
                if (!IsLeftSelected)
                    yield return SideType.Left;
                if (!IsRightSelected)
                    yield return SideType.Right;
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            HoverSegmentEnd = null;
            SelectedSegmentEnd = null;
        }
        public override void OnToolUpdate()
        {
            if (!IsSelectedSegmentEnd && !InputExtension.ShiftIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                var sides = Sides.ToArray();

                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    if (segmentData == SelectedSegmentEnd)
                        continue;

                    foreach (var side in sides)
                    {
                        if (IsHover(segmentData, side))
                        {
                            HoverSegmentEnd = segmentData;
                            HoverSide = side;
                            return;
                        }
                    }
                }
            }

            HoverSegmentEnd = null;
        }
        private bool IsHover(SegmentEndData segmentEnd, SideType side)
        {
            segmentEnd.GetCorner(side == SideType.Left, out var position, out _);
            var hitPos = Tool.Ray.GetRayPosition(position.y, out _);
            return (position - hitPos).sqrMagnitude < SegmentEndData.DotRadius * SegmentEndData.DotRadius;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsSelectedSegmentEnd)
            {
                if (IsHoverSegmentEnd)
                {
                    SelectedSegmentEnd = HoverSegmentEnd;
                    SelectedSide = HoverSide;
                }
            }
            else if (IsHoverSegmentEnd)
            {
                Tool.Data.Align(SelectedSegmentEnd, HoverSegmentEnd, SelectedSide);
                Tool.Panel.UpdatePanel();
                Tool.SetDefaultMode();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSelectedSegmentEnd)
                SelectedSegmentEnd = null;
            else
                Tool.SetDefaultMode();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var green = new OverlayData(cameraInfo) { Color = Colors.Green };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            var purple = new OverlayData(cameraInfo) { Color = Colors.Purple };
            var white = new OverlayData(cameraInfo);

            foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
            {
                if (segmentEnd == SelectedSegmentEnd)
                    segmentEnd.RenderAlign(green, SelectedSide == SideType.Left ? purple : null, SelectedSide == SideType.Right ? purple : null);
                else if (segmentEnd == HoverSegmentEnd)
                {
                    var leftData = IsLeftSelected ? default(OverlayData?) : (HoverSide == SideType.Left ? white : yellow);
                    var rightData = IsRightSelected ? default(OverlayData?) : (HoverSide == SideType.Right ? white : yellow);
                    segmentEnd.RenderAlign(green, leftData, rightData);
                }
                else
                {
                    var leftData = IsLeftSelected ? default(OverlayData?) : yellow;
                    var rightData = IsRightSelected ? default(OverlayData?) : yellow;
                    segmentEnd.RenderAlign(green, leftData, rightData);
                }
            }
        }
    }
}
