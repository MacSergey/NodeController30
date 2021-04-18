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
    public class ChangeMainRoadToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.ChangeMain;
        public override bool ShowPanel => false;

        private SegmentEndData HoverSegmentEnd { get; set; }
        private bool IsHoverSegmentEnd => HoverSegmentEnd != null;
        private SegmentEndData SelectedSegmentEnd { get; set; }
        private bool IsSelectedSegmentEnd => SelectedSegmentEnd != null;
        private float Radius => SegmentEndData.DotRadius + 0.5f;

        protected override void Reset(IToolMode prevMode)
        {
            HoverSegmentEnd = null;
            SelectedSegmentEnd = null;
        }
        public override void OnToolUpdate()
        {
            if (!IsSelectedSegmentEnd && !InputExtension.AltIsPressed)
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
                Tool.Data.SetMain(SelectedSegmentEnd.Id, HoverSegmentEnd.Id);

            Tool.SetDefaultMode();
        }
        public override void OnSecondaryMouseClicked()
        {
            Tool.SetDefaultMode();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var width = Radius * 2;

            if (!IsSelectedSegmentEnd)
            {
                Tool.Data.MainBezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });

                foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                    segmentEnd.RenderOutterCircle(new OverlayData(cameraInfo) { Color = Colors.Green });

                foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                    segmentData.Position.RenderCircle(new OverlayData(cameraInfo) { Color = segmentData == HoverSegmentEnd ? Color.white : Colors.Yellow }, width, 0f);
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
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, HoverSegmentEnd.Position, -HoverSegmentEnd.Direction);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });
                }
                else
                {
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, endPosition);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });
                }

                foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                {
                    if (segmentEnd != SelectedSegmentEnd)
                        segmentEnd.RenderOutterCircle(new OverlayData(cameraInfo) { Color = segmentEnd == HoverSegmentEnd ? Color.white : Colors.Green });
                }

                SelectedSegmentEnd.Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow }, width, 0f);

                if (IsHoverSegmentEnd)
                    HoverSegmentEnd.Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow }, width, 0f);
                else
                    endPosition.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow }, width, 0f);
            }
        }
    }
}
