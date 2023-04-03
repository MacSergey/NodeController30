using ColossalFramework.Math;
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
        private Vector3 CachedPosition { get; set; }
        private Vector3 CachedLeftDelta { get; set; }
        private Vector3 CachedRightDelta { get; set; }
        private float CachedDeltaHeight { get; set; }
        private int WasModifierPressed { get; set; }
        private float RoundTo => Utility.OnlyShiftIsPressed ? 1f : 0.1f;

#if DEBUG
        public override string GetToolInfo()
        {
            if (Settings.ExtraDebug)
            {
                return $"Rotate: {SegmentEnd.RotateAngle}, MinRot: {SegmentEnd.MinRotateAngle}, MaxRot: {SegmentEnd.MaxRotateAngle}, Is min: {SegmentEnd.IsMinBorderT}, Cache: {CachedRotate}" +
                    $"\nOffset: {SegmentEnd.Offset}, T: {SegmentEnd.OffsetT}";
            }
            else
                return null;
        }
#endif

        protected override void Reset(IToolMode prevMode)
        {
            if (prevMode is EditNodeToolMode editMode)
                SegmentEnd = editMode.HoverSegmentCenter;
            else if (prevMode is DragSegmentEndToolMode dragMode)
                SegmentEnd = dragMode.SegmentEnd;
            else
                SegmentEnd = null;

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                CachedRotate = SegmentEnd.IsMinBorderT ? 0f : SegmentEnd.RotateAngle;
            }
            else
            {
                CachedPosition = SegmentEnd.Position;
                CachedLeftDelta = SegmentEnd.LeftPosDelta;
                CachedRightDelta = SegmentEnd.RightPosDelta;
                CachedDeltaHeight = 0f;
                WasModifierPressed = 0;
            }
        }
        public override void OnToolUpdate()
        {
            if (SegmentEnd.Mode == Mode.FreeForm)
            {
                var isPressed = 0;
                if (Utility.AltIsPressed)
                    isPressed += 1;
                if (Utility.CtrlIsPressed)
                    isPressed += 2;
                if (Utility.ShiftIsPressed)
                    isPressed += 4;

                if (isPressed != WasModifierPressed)
                {
                    Reset(this);
                }
                WasModifierPressed = isPressed;
            }
        }
        public override void OnMouseDrag(Event e)
        {
#if DEBUG
            if (Settings.ExtraDebug && Settings.SegmentId == SegmentEnd.Id)
            {
                SingletonMod<Mod>.Logger.Debug($"Drag segment end");
            }
#endif
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                SegmentEnd.RawSegmentBezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out _);
                SegmentEnd.Offset = SegmentEnd.RawSegmentBezier.Distance(0f, t).RoundToNearest(RoundTo);
                SegmentEnd.SetRotate(CachedRotate, true);
            }
            else
            {
                Vector3 deltaPos;
                if (Utility.OnlyShiftIsPressed)
                {
                    var plane = new Plane(Tool.CameraDirection, CachedPosition);
                    if (plane.Raycast(Tool.MouseRay, out var rayT))
                    {
                        var intersectPos = Tool.MouseRay.origin + Tool.MouseRay.direction * rayT;
                        CachedDeltaHeight = intersectPos.y - CachedPosition.y;
                    }
                    deltaPos = new Vector3(0f, CachedDeltaHeight, 0f);
                }
                else
                {
                    var rayPos = Tool.Ray.GetRayPosition(CachedPosition.y, out _);
                    deltaPos = rayPos - CachedPosition;
                }

                var left = SegmentEnd[SideType.Left];
                var rigth = SegmentEnd[SideType.Right];
                var leftAngle = left.RawTrajectory.Tangent(left.CurrentT).AbsoluteAngle();
                var rigthAngle = rigth.RawTrajectory.Tangent(rigth.CurrentT).AbsoluteAngle();

                var leftDelta = Quaternion.AngleAxis(leftAngle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
                var rightDelta = Quaternion.AngleAxis(rigthAngle * Mathf.Rad2Deg, Vector3.up) * deltaPos;

                if (Utility.OnlyCtrlIsPressed)
                {
                    leftDelta.z = 0f;
                    rightDelta.z = 0f;
                }
                else if (Utility.OnlyAltIsPressed)
                {
                    leftDelta.x = 0f;
                    rightDelta.x = 0f;
                }

                left.PosDelta = CachedLeftDelta + leftDelta;
                rigth.PosDelta = CachedRightDelta + rightDelta;
            }

            SegmentEnd.UpdateNode();
            Tool.Panel.RefreshPanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;
            var allow = new OverlayData(cameraInfo) { RenderLimit = underground };
            var forbidden = new OverlayData(cameraInfo) { Color = CommonColors.Red, RenderLimit = underground };
            var defaultColor = new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground };
            var yellow = new OverlayData(cameraInfo) { Color = CommonColors.Yellow, RenderLimit = underground };

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                SegmentEnd.RenderGuides(allow, forbidden, allow);
                SegmentEnd.SegmentBezier.Render(new OverlayData(cameraInfo) { Width = SegmentEndData.CenterRadius * 2 + 1, RenderLimit = underground });
                SegmentEnd.Render(defaultColor, defaultColor, yellow);
            }
            else
            {
                SegmentEnd.Render(defaultColor, defaultColor, yellow);
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            size = 2f;

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                text = SegmentEnd.Offset.ToString("0.0");
                color = SegmentEnd.IsStartBorderOffset || SegmentEnd.IsEndBorderOffset ? CommonColors.Red : CommonColors.Yellow;
                return true;
            }
            else if (Utility.OnlyShiftIsPressed)
            {
                var y = (SegmentEnd.LeftPosDelta.y + SegmentEnd.RightPosDelta.y) * 0.5f;
                var ySign = y < 0 ? "-" : y > 0 ? "+" : "";
                text = $"{ySign}{Mathf.Abs(y):0.0}";
                color = CommonColors.Yellow;
                return true;
            }
            else
            {
                var x = (SegmentEnd.LeftPosDelta.x + SegmentEnd.RightPosDelta.x) * 0.5f;
                var z = (SegmentEnd.LeftPosDelta.z + SegmentEnd.RightPosDelta.z) * 0.5f;
                var xSign = x < 0 ? "-" : x > 0 ? "+" : "";
                var zSign = z < 0 ? "-" : z > 0 ? "+" : "";
                var xText = $"X: {xSign}{Mathf.Abs(x):0.0}".AddColor(Utility.OnlyAltIsPressed ? Color.white : CommonColors.Yellow);
                var yText = $"Y: {zSign}{Mathf.Abs(z):0.0}".AddColor(Utility.OnlyCtrlIsPressed ? Color.white : CommonColors.Yellow);
                text = xText + '\n' + yText;
                color = CommonColors.Yellow;
                return true;
            }
        }
    }
}
