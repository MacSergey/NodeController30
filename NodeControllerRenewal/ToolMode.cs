using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;
using ColossalFramework;
using ModsCommon;
using NodeController.Patches;

namespace NodeController
{
    public class SelectToolMode : BaseSelectToolMode<Mod, NodeControllerTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;
        public override string GetToolInfo() => IsHoverNode ? $"Node {HoverNode.Id}" : "Select node";

        protected override bool IsValidNode(ushort nodeId)
        {
            var node = nodeId.GetNode();
            return node.m_flags.CheckFlags(0, NetNode.Flags.Middle) || node.m_flags.CheckFlags(0, NetNode.Flags.Moveable);
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
                Set(Manager.Instance[HoverNode.Id, true]);
            else if (IsHoverSegment)
            {
                var controlPoint = new NetTool.ControlPoint() { m_segment = HoverSegment.Id };
                HoverSegment.GetHitPosition(Tool.Ray, out _, out controlPoint.m_position);
                if (PossibleInsertNode(controlPoint.m_position))
                    Set(Manager.Instance.InsertNode(controlPoint));
            }
        }
        private void Set(NodeData data)
        {
            if (data != null)
            {
                Tool.SetData(data);
                Tool.SetDefaultMode();
            }
        }
        public bool PossibleInsertNode(Vector3 position)
        {
            if (!IsHoverSegment)
                return false;

            foreach (var data in HoverSegment.Datas)
            {
                var node = data.Id.GetNode();
                if (node.m_flags.CheckFlags(NetNode.Flags.Moveable, NetNode.Flags.End))
                    continue;
                if ((data.Position - position).magnitude < 8f)
                    return false;
            }

            return true;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverSegment)
            {
                var segment = HoverSegment.Id.GetSegment();
                SegmentEndData.CalculateSegmentBeziers(HoverSegment.Id, out var bezier, out _, out _);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out var position);
                var direction = bezier.Tangent(t).MakeFlatNormalized();

                var overlayData = new OverlayData(cameraInfo) { Width = segment.Info.m_halfWidth * 2, Color = PossibleInsertNode(position) ? Colors.Green : Colors.Red, AlphaBlend = false, Cut = true };

                var middle = new Bezier3()
                {
                    a = position + direction,
                    b = position,
                    c = position,
                    d = position - direction,
                };
                middle.RenderBezier(overlayData);

                overlayData.Width = Selection.BorderOverlayWidth;
                overlayData.Cut = false;

                var normal = direction.MakeFlatNormalized().Turn90(true);
                RenderBorder(overlayData, position + direction, normal, segment.Info.m_halfWidth);
                RenderBorder(overlayData, position - direction, normal, segment.Info.m_halfWidth);
            }
            else
                base.RenderOverlay(cameraInfo);
        }
        private void RenderBorder(OverlayData overlayData, Vector3 position, Vector3 normal, float halfWidth)
        {
            var bezier = new Bezier3
            {
                a = position + normal * (halfWidth - Selection.BorderOverlayWidth / 2),
                b = position,
                c = position,
                d = position - normal * (halfWidth - Selection.BorderOverlayWidth / 2),
            };
            bezier.RenderBezier(overlayData);
        }
    }
    public class EditToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Edit;
        public SegmentEndData HoverSegmentEndCenter { get; set; }
        private bool IsHoverSegmentEndCenter => HoverSegmentEndCenter != null;
        public SegmentEndData HoverSegmentEndCircle { get; set; }
        private bool IsHoverSegmentEndCircle => HoverSegmentEndCircle != null;

        public override void OnToolUpdate()
        {
            if (Tool.Data.IsJunction && InputExtension.OnlyShiftIsPressed)
                Tool.SetMode(ToolModeType.ChangeMain);

            else if (Tool.MouseRayValid && Tool.Data.IsMoveableEnds)
            {
                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                    var magnitude = (segmentData.Position - hitPos).magnitude;
                    if (magnitude < SegmentEndData.DotRadius)
                    {
                        HoverSegmentEndCenter = segmentData;
                        HoverSegmentEndCircle = null;
                        return;
                    }
                    else if (magnitude < SegmentEndData.CircleRadius + 1f && magnitude > SegmentEndData.CircleRadius - 0.5f)
                    {
                        HoverSegmentEndCenter = null;
                        HoverSegmentEndCircle = segmentData;
                        return;
                    }
                }
            }

            HoverSegmentEndCenter = null;
            HoverSegmentEndCircle = null;
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetData(null);
            Tool.SetMode(ToolModeType.Select);
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSegmentEndCenter)
                Tool.SetMode(ToolModeType.Drag);
            else if (IsHoverSegmentEndCircle)
                Tool.SetMode(ToolModeType.Rotate);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var hoverData = new OverlayData(cameraInfo);
            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var normalData = new OverlayData(cameraInfo) { Color = Colors.Green };
                segmentData.Render(normalData, segmentData == HoverSegmentEndCircle ? hoverData : normalData, segmentData == HoverSegmentEndCenter ? hoverData : normalData);
            }
        }
    }
    public class ChangeMainRoadToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.ChangeMain;
        private SegmentEndData HoverSegmentEnd { get; set; }
        private bool IsHoverSegmentEnd => HoverSegmentEnd != null;
        private SegmentEndData SelectedSegmentEnd { get; set; }
        private bool IsSelectedSegmentEnd => SelectedSegmentEnd != null;

        protected override void Reset(IToolMode prevMode)
        {
            HoverSegmentEnd = null;
            SelectedSegmentEnd = null;
        }
        public override void OnToolUpdate()
        {
            if (!IsSelectedSegmentEnd && !InputExtension.ShiftIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                if (!IsSelectedSegmentEnd)
                {
                    foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                    {
                        var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                        var magnitude = (segmentData.Position - hitPos).magnitude;
                        if (magnitude < SegmentEndData.DotRadius)
                        {
                            HoverSegmentEnd = segmentData;
                            return;
                        }
                    }
                }
                else
                {
                    foreach (var segmentData in Tool.Data.SegmentEndDatas)
                    {
                        if (segmentData == SelectedSegmentEnd)
                            continue;

                        var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                        var magnitude = (segmentData.Position - hitPos).magnitude;
                        if (magnitude < SegmentEndData.CircleRadius - 0.5f)
                        {
                            HoverSegmentEnd = segmentData;
                            return;
                        }
                    }
                }
            }

            HoverSegmentEnd = null;
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSegmentEnd)
                SelectedSegmentEnd = HoverSegmentEnd == Tool.Data.FirstMainSegmentEnd ? Tool.Data.SecondMainSegmentEnd : Tool.Data.FirstMainSegmentEnd;
        }
        public override void OnMouseUp(Event e)
        {
            if (IsHoverSegmentEnd)
                Tool.Data.SetMain(SelectedSegmentEnd.Id, HoverSegmentEnd.Id);

            SelectedSegmentEnd = null;
        }
        public override void OnSecondaryMouseClicked()
        {
            Tool.SetDefaultMode();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var width = SegmentEndData.DotRadius * 2;

            if (!IsSelectedSegmentEnd)
            {
                Tool.Data.MainBezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });

                foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                    segmentEnd.RenderOutterCircle(new OverlayData(cameraInfo) { Color = Colors.Green });

                foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                    segmentData.RenderInnerCircle(new OverlayData(cameraInfo) { Color = segmentData == HoverSegmentEnd ? Color.white : Colors.Yellow });
            }
            else
            {
                if (IsHoverSegmentEnd)
                {
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, HoverSegmentEnd.Position, -HoverSegmentEnd.Direction);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });
                }
                else
                {
                    var endPosition = Tool.Ray.GetRayPosition(Tool.Data.Position.y, out _);
                    var bezier = new BezierTrajectory(SelectedSegmentEnd.Position, -SelectedSegmentEnd.Direction, endPosition);
                    bezier.Render(new OverlayData(cameraInfo) { Width = width, Color = Colors.Yellow });
                }

                foreach (var segmentEnd in Tool.Data.SegmentEndDatas)
                {
                    if (segmentEnd != SelectedSegmentEnd)
                        segmentEnd.RenderOutterCircle(new OverlayData(cameraInfo) { Color = segmentEnd == HoverSegmentEnd ? Color.white : Colors.Green });
                }

                SelectedSegmentEnd.RenderInnerCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow });

                if (IsHoverSegmentEnd)
                    HoverSegmentEnd.RenderInnerCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow });
                else
                {
                    var endPosition = Tool.Ray.GetRayPosition(Tool.Data.Position.y, out _);
                    endPosition.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow }, width, 0f);
                }
            }
        }
    }
    public class DragSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Drag;
        public SegmentEndData SegmentEnd { get; private set; } = null;
        private float CachedRotate { get; set; }
        private float RoundTo => InputExtension.OnlyShiftIsPressed ? 1f : 0.1f;

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditToolMode editMode ? editMode.HoverSegmentEndCenter : null;
            CachedRotate = SegmentEnd.IsBorderT ? 0f : SegmentEnd.RotateAngle;
        }
        public override void OnMouseDrag(Event e)
        {
            SegmentEnd.RawSegmentBezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out _);
            SegmentEnd.Offset = SegmentEnd.RawSegmentBezier.Cut(0f, t).Length.RoundToNearest(RoundTo);
            SegmentEnd.SetRotate(CachedRotate);
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            SegmentEnd.RenderSides(new OverlayData(cameraInfo), new OverlayData(cameraInfo) { Color = Colors.Red });
            SegmentEnd.SegmentBezier.Render(new OverlayData(cameraInfo) { Width = SegmentEndData.DotRadius * 2 + 1 });

            var normalData = new OverlayData(cameraInfo) { Color = Colors.Green };
            var dragData = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            SegmentEnd.Render(normalData, normalData, dragData);
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = SegmentEnd.Offset.ToString("0.0");
            color = SegmentEnd.IsBorderOffset ? Colors.Red : Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
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
            SegmentEnd = prevMode is EditToolMode editMode ? editMode.HoverSegmentEndCircle : null;
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

            var normalData = new OverlayData(cameraInfo) { Color = Colors.Green };
            var rotateData = new OverlayData(cameraInfo) { Color = Colors.Yellow };
            SegmentEnd.Render(normalData, rotateData, normalData);
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
