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

        private Vector3 CachedPosition { get; set; }
        private Vector3 CachedLeftPosDelta { get; set; }
        private Vector3 CachedRightPosDelta { get; set; }
        private float CachedLeftRot { get; set; }
        private float CachedRightRot { get; set; }

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
                var currentPosition = Tool.Ray.GetRayPosition(SegmentEnd.Position.y, out _);
                return (currentPosition - SegmentEnd.Position).normalized;
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditNodeToolMode editMode ? editMode.HoverSegmentEndCircle : null;
            BeginDirection = CurrentDirection;

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                BeginRotate = SegmentEnd.RotateAngle;
            }
            else
            {
                CachedPosition = SegmentEnd.Position;
                CachedLeftPosDelta = SegmentEnd.LeftPosDelta;
                CachedRightPosDelta = SegmentEnd.RightPosDelta;
                CachedLeftRot = SegmentEnd.LeftDirDelta.x;
                CachedRightRot= SegmentEnd.RightDirDelta.x;
            }
        }

        public override void OnMouseDrag(Event e)
        {
            var quaternion = Quaternion.FromToRotation(BeginDirection, CurrentDirection);
            var deltaAngle = quaternion.eulerAngles.y;
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                var angle = (BeginRotate + deltaAngle).RoundToNearest(RoundTo) % 360f;
                SegmentEnd.RotateAngle = angle > 180f ? angle - 360f : angle;
            }
            else
            {
                SegmentEnd.LeftPosDelta = CachedLeftPosDelta + GetCornerDelta(SideType.Left, deltaAngle);
                SegmentEnd.RightPosDelta = CachedRightPosDelta + GetCornerDelta(SideType.Right, deltaAngle);

                var leftDirDelta = SegmentEnd.LeftDirDelta;
                leftDirDelta.x = CachedLeftRot + deltaAngle;
                SegmentEnd.LeftDirDelta = leftDirDelta;

                var rightDirDelta = SegmentEnd.RightDirDelta;
                rightDirDelta.x = CachedRightRot + deltaAngle;
                SegmentEnd.RightDirDelta = leftDirDelta;
            }

            SegmentEnd.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        private Vector3 GetCornerDelta(SideType sideType, float deltaAngle)
        {
            var side = SegmentEnd[sideType];
            var sidePos = side.RawTrajectory.Position(side.CurrentT);
            var angle = side.RawTrajectory.Tangent(side.CurrentT).AbsoluteAngle();

            var direction = sidePos - CachedPosition;
            direction = direction.TurnDeg(-deltaAngle, false);
            var newPos = CachedPosition + direction;
            var deltaPos = newPos - sidePos;
            return Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
        }

        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;
            var allow = new OverlayData(cameraInfo) { RenderLimit = underground };
            var forbidden = new OverlayData(cameraInfo) { Color = Colors.Red, RenderLimit = underground };
            var defaultColor = new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground };

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                SegmentEnd.RenderSides(allow, forbidden, allow);
                SegmentEnd.Render(defaultColor, yellow, defaultColor);
            }
            else
            {
                SegmentEnd.Render(defaultColor, yellow, defaultColor);
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                text = $"{SegmentEnd.RotateAngle:0}°";
                color = SegmentEnd.IsBorderRotate ? Colors.Red : Colors.Yellow;
                size = 2f;
                position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
                direction = SegmentEnd.Direction;
                return true;
            }
            else
            {
                text = default;
                color = default;
                size = default;
                position = default;
                direction = default;
                return false;
            }
        }
    }
}
