using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class RotateSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.RotateEnd;
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
            SegmentEnd = prevMode is EditNodeToolMode editMode ? editMode.HoverSegmentCircle : null;
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
                CachedRightRot = SegmentEnd.RightDirDelta.x;
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
                var left = SegmentEnd[SideType.Left];
                var right = SegmentEnd[SideType.Right];

                left.PosDelta = GetCornerDelta(SideType.Left, CachedLeftPosDelta, deltaAngle);
                right.PosDelta = GetCornerDelta(SideType.Right, CachedRightPosDelta, deltaAngle);

                var leftDirDelta = left.DirDelta;
                var leftAngle = (CachedLeftRot + deltaAngle) % 360f;
                leftDirDelta.x = leftAngle > 180f ? leftAngle - 360f : leftAngle;
                left.DirDelta = leftDirDelta;

                var rightDirDelta = right.DirDelta;
                var rightAngle = (CachedRightRot + deltaAngle) % 360f;
                rightDirDelta.x = rightAngle > 180f ? rightAngle - 360f : rightAngle;
                right.DirDelta = rightDirDelta;
            }

            SegmentEnd.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        private Vector3 GetCornerDelta(SideType sideType, Vector3 deltaPos, float deltaAngle)
        {
            var side = SegmentEnd[sideType];
            var zeroPos = side.RawTrajectory.Position(side.CurrentT);
            var angle = side.RawTrajectory.Tangent(side.CurrentT).AbsoluteAngle();

            var oldPos = zeroPos + Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
            var direction = oldPos - CachedPosition;
            direction = direction.TurnDeg(-deltaAngle, false);
            var newPos = CachedPosition + direction;

            deltaPos = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up) * (newPos - zeroPos);
            return deltaPos;
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
                SegmentEnd.RenderGuides(allow, forbidden, allow);
                SegmentEnd.Render(defaultColor, yellow, defaultColor);
            }
            else
            {
                SegmentEnd.Render(defaultColor, yellow, defaultColor);
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                text = $"{SegmentEnd.RotateAngle:0}°";
                color = SegmentEnd.IsBorderRotate ? Colors.Red : Colors.Yellow;
                return true;
            }
            else
            {
                var quaternion = Quaternion.FromToRotation(BeginDirection, CurrentDirection);
                var angle = ((CachedLeftRot + CachedRightRot) * 0.5f + quaternion.eulerAngles.y) % 360f;
                text = $"{(angle > 180f ? angle - 360f : angle):0}°";
                color = Colors.Yellow;
                return true;
            }
        }
    }
}
