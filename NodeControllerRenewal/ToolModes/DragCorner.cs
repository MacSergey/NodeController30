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
                var t = SegmentEnd[Corner].CurrentT;
                var pos = SegmentEnd[Corner].RawTrajectory.Position(t);
                var dir = SegmentEnd[Corner].RawTrajectory.Tangent(t).MakeFlatNormalized();
                var normal = dir.Turn90(true);

                var rayPos = Tool.Ray.GetRayPosition(pos.y, out _);
                Line2.Intersect(XZ(pos), XZ(pos + dir), XZ(rayPos), XZ(rayPos + normal), out var x, out var z);

                if (Corner == SideType.Left)
                {
                    var delta = SegmentEnd.LeftPosDelta;
                    delta.x = x;
                    delta.z = z;
                    SegmentEnd.LeftPosDelta = delta;
                }
                else
                {
                    var delta = SegmentEnd.RightPosDelta;
                    delta.x = x;
                    delta.z = z;
                    SegmentEnd.RightPosDelta = delta;
                }
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
                SegmentEnd[Corner].Render(allow, forbidden, allow);
                SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd.RenderStart(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground });
            }
            else
            {
                SegmentEnd.RenderContour(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd.RenderStart(new OverlayData(cameraInfo) { Color = SegmentEnd.OverlayColor, RenderLimit = underground });
                SegmentEnd[Corner].RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground });
            }
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            if (SegmentEnd.Mode != Mode.FreeForm)
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
