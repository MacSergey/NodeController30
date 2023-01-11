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
        private float RoundTo => Utility.OnlyShiftIsPressed ? 1f : 0.1f;

#if DEBUG
        public override string GetToolInfo()
        {
            if (Settings.ExtraDebug)
            {
                return $"Rotate: {SegmentEnd.RotateAngle}, MinRot: {SegmentEnd.MinRotate}, MaxRot: {SegmentEnd.MaxRotate}, Is min: {SegmentEnd.IsMinBorderT}, Cache: {CachedRotate}" +
                    $"\nOffset: {SegmentEnd.Offset}, T: {SegmentEnd.OffsetT}";
            }
            else
                return null;
        }
#endif

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditNodeToolMode editMode ? editMode.HoverSegmentEndCenter : null;

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                CachedRotate = SegmentEnd.IsMinBorderT ? 0f : SegmentEnd.RotateAngle;
            }
            else
            {
                CachedPosition = SegmentEnd.Position;
                CachedLeftDelta = SegmentEnd.LeftPosDelta;
                CachedRightDelta = SegmentEnd.RightPosDelta;
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
                var rayPos = Tool.Ray.GetRayPosition(CachedPosition.y, out _);
                var deltaPos = rayPos - CachedPosition;

                var left = SegmentEnd[SideType.Left];
                var rigth = SegmentEnd[SideType.Right];
                var leftAngle = left.RawTrajectory.Tangent(left.CurrentT).AbsoluteAngle();
                var rigthAngle = rigth.RawTrajectory.Tangent(rigth.CurrentT).AbsoluteAngle();

                SegmentEnd.LeftPosDelta = CachedLeftDelta + Quaternion.AngleAxis(leftAngle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
                SegmentEnd.RightPosDelta = CachedRightDelta + Quaternion.AngleAxis(rigthAngle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
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
            var forbidden = new OverlayData(cameraInfo) { Color = Colors.Red, RenderLimit = underground };
            var defaultColor = new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground };

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                SegmentEnd.RenderSides(allow, forbidden, allow);
                SegmentEnd.SegmentBezier.Render(new OverlayData(cameraInfo) { Width = SegmentEndData.CenterDotRadius * 2 + 1, RenderLimit = underground });
                SegmentEnd.Render(defaultColor, defaultColor, yellow);
            }
            else
            {
                SegmentEnd.Render(defaultColor, defaultColor, yellow);
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                text = SegmentEnd.Offset.ToString("0.0");
                color = SegmentEnd.IsStartBorderOffset || SegmentEnd.IsEndBorderOffset ? Colors.Red : Colors.Yellow;
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
