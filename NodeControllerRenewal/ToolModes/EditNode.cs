using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class EditNodeToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Edit;

        public SegmentEndData HoverSegmentEndCenter { get; private set; }
        private bool IsHoverSegmentEndCenter => HoverSegmentEndCenter != null;

        public SegmentEndData HoverSegmentEndCircle { get; private set; }
        private bool IsHoverSegmentEndCircle => HoverSegmentEndCircle != null;

        public SegmentEndData HoverSegmentEndCorner { get; private set; }
        public SideType HoverCorner { get; private set; }
        private bool IsHoverSegmentEndCorner => HoverSegmentEndCorner != null;

        public override void OnToolUpdate()
        {
            if (Tool.Data.IsJunction && !Tool.Panel.IsHover && InputExtension.OnlyAltIsPressed)
                Tool.SetMode(ToolModeType.ChangeMain);
            else if (!Tool.Panel.IsHover && InputExtension.OnlyShiftIsPressed)
                Tool.SetMode(ToolModeType.Aling);
            else if (Tool.MouseRayValid && Tool.Data.IsMoveableEnds)
            {
                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    if (!segmentData.IsMoveable)
                        continue;

                    var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                    var magnitude = (segmentData.Position - hitPos).magnitude;
                    if (magnitude < SegmentEndData.CenterDotRadius)
                    {
                        HoverSegmentEndCenter = segmentData;
                        HoverSegmentEndCircle = null;
                        HoverSegmentEndCorner = null;
                        return;
                    }                   
                    else if (magnitude < SegmentEndData.CircleRadius + 1f && magnitude > SegmentEndData.CircleRadius - 0.5f)
                    {
                        HoverSegmentEndCenter = null;
                        HoverSegmentEndCircle = segmentData;
                        HoverSegmentEndCorner = null;
                        return;
                    }                   
                    else if (CheckCorner(segmentData, SideType.Left) || CheckCorner(segmentData, SideType.Right))
                        return;
                }
            }

            HoverSegmentEndCenter = null;
            HoverSegmentEndCircle = null;
            HoverSegmentEndCorner = null;
        }
        private bool CheckCorner(SegmentEndData segmentData, SideType side)
        {
            var hitPos = Tool.Ray.GetRayPosition(segmentData[side].Position.y, out _);

            if ((segmentData[side].Position - hitPos).magnitude < SegmentEndData.CornerDotRadius)
            {
                HoverSegmentEndCenter = null;
                HoverSegmentEndCircle = null;
                HoverSegmentEndCorner = segmentData;
                HoverCorner = side;
                return true;
            }
            else
                return false;
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetData(null);
            Tool.SetMode(ToolModeType.Select);
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSegmentEndCenter)
                Tool.SetMode(ToolModeType.DragEnd);
            else if (IsHoverSegmentEndCircle)
                Tool.SetMode(ToolModeType.Rotate);
            else if (IsHoverSegmentEndCorner)
                Tool.SetMode(ToolModeType.DragCorner);
        }
        public override string GetToolInfo() => $"Hold Shift to alignment roads\nHold Alt to change main road";

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var hover = new OverlayData(cameraInfo);
            var green = new OverlayData(cameraInfo) { Color = Colors.Green };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var outter = segmentData == HoverSegmentEndCircle ? hover : yellow;
                var inner = segmentData == HoverSegmentEndCenter ? hover : yellow;
                var left = segmentData == HoverSegmentEndCorner && HoverCorner == SideType.Left ? hover : yellow;
                var right = segmentData == HoverSegmentEndCorner && HoverCorner == SideType.Right ? hover : yellow;
                segmentData.Render(green, outter, inner, left, right);
            }
        }
    }
}
