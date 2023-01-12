using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController
{
    public class DragCornerToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.DragCorner;

        public SegmentEndData SegmentEnd { get; private set; } = null;
        public SideType Corner { get; private set; }

        private Vector3 CachedPosition { get; set; }
        private Vector3 CachedDelta { get; set; }
        private float CachedDeltaHeight { get; set; }
        private bool WasShiftPressed { get; set; }

        private float RoundTo => Utility.OnlyShiftIsPressed ? 1f : 0.1f;

        protected override void Reset(IToolMode prevMode)
        {
            if (prevMode is EditNodeToolMode editMode)
            {
                SegmentEnd = editMode.HoverCornerCenter;
                Corner = editMode.HoverCorner;
            }
            else if (prevMode is DragCornerToolMode dragMode)
            {
                SegmentEnd = dragMode.SegmentEnd;
                Corner = dragMode.Corner;
            }
            else
                SegmentEnd = null;

            if (SegmentEnd.Mode == Mode.FreeForm)
            {
                CachedPosition = SegmentEnd[Corner].StartPos;
                CachedDelta = SegmentEnd[Corner].PosDelta;
                CachedDeltaHeight = 0f;
                WasShiftPressed = false;
            }
        }
        public override void OnToolUpdate()
        {
            if (SegmentEnd.Mode == Mode.FreeForm)
            {
                var isShiftPressed = Utility.OnlyShiftIsPressed;
                if (isShiftPressed != WasShiftPressed)
                {
                    Reset(this);
                }
                WasShiftPressed = isShiftPressed;
            }
        }
        public override void OnMouseDrag(Event e)
        {
            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                var delta = SegmentEnd[Corner].StartPos - SegmentEnd[Corner].MarkerPos;
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

                var side = SegmentEnd[Corner];
                var angle = side.RawTrajectory.Tangent(side.CurrentT).AbsoluteAngle();

                SegmentEnd[Corner].PosDelta = CachedDelta + Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
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

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                SegmentEnd[Corner].RenderGuides(allow, forbidden, allow);
                SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd.RenderStart(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd[Corner].RenderCenter(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground });
            }
            else
            {
                SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd.RenderStart(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd[Corner].RenderCenter(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground });
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;

            if (SegmentEnd.Mode != Mode.FreeForm)
            {
                var side = SegmentEnd[Corner];

                var value = side.RawTrajectory.Cut(0f, side.CurrentT).GetLength(1f, 7) - side.AdditionalLength;
                text = $"{value:0.0}";
                color = side.IsMinBorderT || side.IsMaxBorderT ? Colors.Red : Colors.Yellow;
                return true;
            }
            else if (Utility.OnlyShiftIsPressed)
            {
                var y = SegmentEnd[Corner].PosDelta.y;
                var ySign = y < 0 ? "-" : y > 0 ? "+" : "";
                text = $"{ySign}{Mathf.Abs(y):0.0}";
                color = Colors.Yellow;
                return true;
            }
            else
            {
                var x = SegmentEnd[Corner].PosDelta.x;
                var z = SegmentEnd[Corner].PosDelta.z;
                var xSign = x < 0 ? "-" : x > 0 ? "+" : "";
                var zSign = z < 0 ? "-" : z > 0 ? "+" : "";
                text = $"X: {xSign}{Mathf.Abs(x):0.0}\nY: {zSign}{Mathf.Abs(z):0.0}";
                color = Colors.Yellow;
                return true;
            }
        }
    }
}
