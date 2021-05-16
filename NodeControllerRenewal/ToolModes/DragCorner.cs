using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController
{
    public class DragCornerToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.DragCorner;

        public SegmentEndData SegmentEnd { get; private set; } = null;
        public SideType Corner { get; private set; }

        private float RoundTo => Utilites.OnlyShiftIsPressed ? 1f : 0.1f;

        protected override void Reset(IToolMode prevMode)
        {
            if (prevMode is EditNodeToolMode editMode)
            {
                SegmentEnd = editMode.HoverSegmentEndCorner;
                Corner = editMode.HoverCorner;
            }
            else
                SegmentEnd = null;
        }
        public override void OnMouseDrag(Event e)
        {
            SegmentEnd[Corner].RawBezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out _);
            var offset = SegmentEnd[Corner].RawBezier.Distance(0f, t).RoundToNearest(RoundTo);
            if (Corner == SideType.Left)
                SegmentEnd.LeftOffset = offset;
            else
                SegmentEnd.RightOffset = offset;

            SegmentEnd.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd[Corner].Render(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });
            SegmentEnd.RenderСontour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor });
            SegmentEnd.RenderEnd(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor });
            SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            var side = SegmentEnd[Corner];

            text = $"{side.RawBezier.Trajectory.Cut(0f, side.CurrentT).Length(1f, 7):0.0}";
            color = side.IsMinBorderT || side.IsMaxBorderT ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
