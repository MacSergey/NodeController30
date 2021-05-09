using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class RotateSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Rotate;
        public SegmentEndData SegmentEnd { get; private set; } = null;
        public Vector3 BeginDirection { get; private set; }
        public float BeginRotate { get; private set; }
        private float RoundTo
        {
            get
            {
                if (InputExtension.OnlyShiftIsPressed)
                    return 10f;
                else if (InputExtension.OnlyCtrlIsPressed)
                    return 0.1f;
                else
                    return 1f;
            }
        }
        private Vector3 CurrentDirection
        {
            get
            {
                var currentPosition = Tool.Ray.GetRayPosition(SegmentEnd.Position.y, out _);
                return (currentPosition - SegmentEnd.Position).normalized;
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditNodeToolMode editMode ? editMode.HoverSegmentEndCircle : null;
            BeginDirection = CurrentDirection;
            BeginRotate = SegmentEnd.RotateAngle;
        }

        public override void OnMouseDrag(Event e)
        {
            var quaternion = Quaternion.FromToRotation(BeginDirection, CurrentDirection);
            var angle = (BeginRotate + quaternion.eulerAngles.y).RoundToNearest(RoundTo) % 360f;
            SegmentEnd.RotateAngle = angle > 180f ? angle - 360f : angle;
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }

        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd.RenderSides(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });

            var defaultColor = new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            SegmentEnd.Render(defaultColor, yellow, defaultColor);
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = $"{SegmentEnd.RotateAngle:0}°";
            color = SegmentEnd.IsBorderRotate ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
