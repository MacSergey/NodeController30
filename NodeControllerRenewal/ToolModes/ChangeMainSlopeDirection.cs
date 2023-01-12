﻿using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class ChangeMainSlopeDirectionToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.ChangeMain;
        public override bool ShowPanel => false;

        private SegmentEndData HoverSegmentEnd { get; set; }
        private bool IsHoverSegmentEnd => HoverSegmentEnd != null;
        private SegmentEndData SelectedSegmentEnd { get; set; }
        private bool IsSelectedSegmentEnd => SelectedSegmentEnd != null;
        private float Radius => SegmentEndData.CenterRadius + 0.5f;

        protected override void Reset(IToolMode prevMode)
        {
            HoverSegmentEnd = null;
            SelectedSegmentEnd = null;
        }
        public override void OnToolUpdate()
        {
            if (!IsSelectedSegmentEnd && !Utility.OnlyAltIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                if (!IsSelectedSegmentEnd)
                {
                    foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                    {
                        var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                        if ((segmentData.Position - hitPos).sqrMagnitude < Radius * Radius)
                        {
                            HoverSegmentEnd = segmentData;
                            return;
                        }
                    }
                }
                else
                {
                    foreach (var segmentData in Tool.Data.SegmentEndDatas)
                    {
                        if (segmentData == SelectedSegmentEnd)
                            continue;

                        var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);
                        var radius = SegmentEndData.CircleRadius - 0.5f;
                        if ((segmentData.Position - hitPos).sqrMagnitude < radius * radius)
                        {
                            HoverSegmentEnd = segmentData;
                            return;
                        }
                    }
                }
            }

            HoverSegmentEnd = null;
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSegmentEnd)
                SelectedSegmentEnd = HoverSegmentEnd == Tool.Data.FirstMainSegmentEnd ? Tool.Data.SecondMainSegmentEnd : Tool.Data.FirstMainSegmentEnd;
        }
        public override void OnMouseUp(Event e)
        {
            if (IsHoverSegmentEnd)
            {
                Tool.Data.SetMain(SelectedSegmentEnd.Id, HoverSegmentEnd.Id);
                Tool.Panel.SetPanel();
            }

            Tool.SetDefaultMode();
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsSelectedSegmentEnd)
                SelectedSegmentEnd = null;
        }
        public override void OnSecondaryMouseClicked()
        {
            Tool.SetDefaultMode();
        }

        public override string GetToolInfo()
        {
            if (!IsSelectedSegmentEnd)
            {
                if (!IsHoverSegmentEnd)
                    return Localize.Tool_InfoSelectMainSlopeDirection;
                else
                    return Localize.Tool_InfoDragMainSlopeDirectionEnd;
            }
            else
            {
                if (!IsHoverSegmentEnd)
                    return Localize.Tool_InfoSelectNewMainSlopeDirectionEnd;
                else
                    return Localize.Tool_InfoDropMainSlopeDirectionEnd;
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var width = Radius * 2;
            var underground = IsUnderground;

            if (!IsSelectedSegmentEnd)
            {
                Tool.Data.MainBezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow, RenderLimit = underground });

                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                    segmentData.RenderCircle(new OverlayData(cameraInfo) { Color = segmentData.OverlayColor, RenderLimit = underground });

                foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                    segmentData.Position.RenderCircle(new OverlayData(cameraInfo) { Color = segmentData == HoverSegmentEnd ? Color.white : Colors.Yellow, RenderLimit = underground }, width, 0f);
            }
            else
            {
                var middlePos = Tool.Data.MainBezier.Position(0.5f);
                var middleDir = Tool.Data.MainBezier.Tangent(0.5f);
                var plane = new Plane(middlePos, middlePos + middleDir, middlePos + middleDir.MakeFlat().Turn90(true));
                plane.Raycast(Tool.MouseRay, out var t);
                var endPosition = Tool.MouseRay.origin + Tool.MouseRay.direction * t;

                if (IsHoverSegmentEnd)
                {
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, HoverSegmentEnd.Position, -HoverSegmentEnd.Direction, true, true, true);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow, RenderLimit = underground });
                }
                else
                {
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, endPosition);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow, RenderLimit = underground });
                }

                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    if (segmentData != SelectedSegmentEnd)
                        segmentData.RenderCircle(new OverlayData(cameraInfo) { Color = segmentData == HoverSegmentEnd ? Color.white : segmentData.OverlayColor, RenderLimit = underground });
                }

                SelectedSegmentEnd.Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground }, width, 0f);

                if (IsHoverSegmentEnd)
                    HoverSegmentEnd.Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground }, width, 0f);
                else
                    endPosition.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground }, width, 0f);
            }
        }
    }
}
