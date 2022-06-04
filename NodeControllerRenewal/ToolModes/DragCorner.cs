using ColossalFramework.Math;
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

        private float RoundTo => Utility.OnlyShiftIsPressed ? 1f : 0.1f;

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
            var delta = SegmentEnd[Corner].Position - SegmentEnd[Corner].MarkerPosition;
            var ray = new Segment3(Tool.Ray.a + delta, Tool.Ray.b + delta);

            SegmentEnd[Corner].RawTrajectory.GetHitPosition(ray, out _, out var t, out _);
            var offset = SegmentEnd[Corner].RawTrajectory.Distance(0f, t);
            offset -= SegmentEnd[Corner].AdditionalLength;
            offset = offset.RoundToNearest(RoundTo);
            offset += SegmentEnd[Corner].AdditionalLength;

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
            var underground = IsUnderground;
            var allow = new OverlayData(cameraInfo) { RenderLimit = underground };
            var forbidden = new OverlayData(cameraInfo) { Color = Colors.Red, RenderLimit = underground };

            SegmentEnd[Corner].Render(allow, forbidden, allow);
            SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
            SegmentEnd.RenderEnd(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
            SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            var side = SegmentEnd[Corner];

            var value = side.RawTrajectory.Cut(0f, side.CurrentT).GetLength(1f, 7) - side.AdditionalLength;
            text = $"{value:0.0}";
            color = side.IsMinBorderT || side.IsMaxBorderT ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
