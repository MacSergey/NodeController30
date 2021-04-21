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

        private float RoundTo => InputExtension.OnlyShiftIsPressed ? 1f : 0.1f;

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
            SegmentEnd.SetCornerOffset(SegmentEnd[Corner].RawBezier.Distance(0f, t).RoundToNearest(RoundTo), Corner);
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd.RenderСontour(new OverlayData(cameraInfo) { Color = Colors.Green });
            SegmentEnd.RenderEnd(new OverlayData(cameraInfo) { Color = Colors.Green });
            SegmentEnd[Corner].Render(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });
            SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = $"{SegmentEnd[Corner].Offset:0.0}";
            color = SegmentEnd[Corner].IsMinBorderT || SegmentEnd[Corner].IsMaxBorderT ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
