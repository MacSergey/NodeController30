using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class DragSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.DragEnd;
        public SegmentEndData SegmentEnd { get; private set; } = null;
        private float CachedRotate { get; set; }
        private float RoundTo => InputExtension.OnlyShiftIsPressed ? 1f : 0.1f;

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditNodeToolMode editMode ? editMode.HoverSegmentEndCenter : null;
            CachedRotate = SegmentEnd.IsMinBorderT ? 0f : SegmentEnd.RotateAngle;
        }
        public override void OnMouseDrag(Event e)
        {
            SegmentEnd.RawSegmentBezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out _);
            SegmentEnd.Offset = SegmentEnd.RawSegmentBezier.Distance(0f, t).RoundToNearest(RoundTo);
            SegmentEnd.SetRotate(CachedRotate, true);
            SegmentEnd.UpdateNode();

            Tool.Panel.RefreshPanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd.RenderSides(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });
            SegmentEnd.SegmentBezier.Render(new OverlayData(cameraInfo) { Width = SegmentEndData.CenterDotRadius * 2 + 1 });

            var defaultColor = new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            SegmentEnd.Render(defaultColor, defaultColor, yellow);
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
