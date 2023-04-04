using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class RotateCornerToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.RotateCorner;

        public SegmentEndData SegmentEnd { get; private set; } = null;
        public SideType Corner { get; private set; }

        public Vector3 BeginDirection { get; private set; }
        public float CachedRotate { get; private set; }

        private float RoundTo
        {
            get
            {
                if (Utility.OnlyShiftIsPressed)
                    return 10f;
                else if (Utility.OnlyCtrlIsPressed)
                    return 0.1f;
                else
                    return 1f;
            }
        }
        private Vector3 CurrentDirection
        {
            get
            {
                var position = SegmentEnd[Corner].StartPos;
                var currentPosition = Tool.Ray.GetRayPosition(position.y, out _);
                return (currentPosition - position).normalized;
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            if (prevMode is EditNodeToolMode editMode)
            {
                SegmentEnd = editMode.HoverCornerCircle;
                Corner = editMode.HoverCorner;
            }
            else
                SegmentEnd = null;

            BeginDirection = CurrentDirection;
            CachedRotate = (Corner == SideType.Left ? SegmentEnd.LeftDirDelta : SegmentEnd.RightDirDelta).x;
        }

        public override void OnMouseDrag(Event e)
        {
            var quaternion = Quaternion.FromToRotation(BeginDirection, CurrentDirection);
            var angle = (CachedRotate + quaternion.eulerAngles.y).RoundToNearest(RoundTo) % 360f;

            var deltaDir = SegmentEnd[Corner].DirDelta;
            deltaDir.x = angle;
            SegmentEnd[Corner].DirDelta = deltaDir;

            SegmentEnd.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;

            SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
            SegmentEnd.RenderStart(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
            SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Yellow, RenderLimit = underground });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            var quaternion = Quaternion.FromToRotation(BeginDirection, CurrentDirection);
            var angle = (CachedRotate + quaternion.eulerAngles.y).RoundToNearest(RoundTo) % 360f;
            text = $"{(angle > 180f ? angle - 360f : angle):0}°";
            color = CommonColors.Yellow;
            return true;
        }
    }
}
