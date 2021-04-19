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
    public class DragSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Drag;
        public SegmentEndData SegmentEnd { get; private set; } = null;
        private float CachedRotate { get; set; }
        private float RoundTo => InputExtension.OnlyShiftIsPressed ? 1f : 0.1f;

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditToolMode editMode ? editMode.HoverSegmentEndCenter : null;
            CachedRotate = SegmentEnd.IsBorderT ? 0f : SegmentEnd.RotateAngle;
        }
        public override void OnMouseDrag(Event e)
        {
            SegmentEnd.RawSegmentBezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out _);
            SegmentEnd.Offset = SegmentEnd.RawSegmentBezier.Cut(0f, t).Length.RoundToNearest(RoundTo);
            SegmentEnd.SetRotate(CachedRotate);
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd.RenderSides(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });
            SegmentEnd.SegmentBezier.Render(new OverlayData(cameraInfo) { Width = SegmentEndData.DotRadius * 2 + 1 });

            var green = new OverlayData(cameraInfo) { Color = Colors.Green };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            SegmentEnd.Render(green, green, yellow);
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = SegmentEnd.Offset.ToString("0.0");
            color = SegmentEnd.IsStartBorderOffset || SegmentEnd.IsEndBorderOffset ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
