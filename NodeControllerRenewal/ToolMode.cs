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
                var hitPos = HoverSegment.GetHitPosition(Tool.Ray, out _);
                segment.GetClosestPositionAndDirection(hitPos, out var position, out var direction);


                var overlayData = new OverlayData(cameraInfo) { Width = segment.Info.m_halfWidth * 2, Color = PossibleInsertNode(position) ? Colors.Green : Colors.Red, AlphaBlend = false, Cut = true };

                var bezier = new Bezier3()
                {
                    a = position + direction,
                    b = position,
                    c = position,
                    d = position - direction,
                };
                bezier.RenderBezier(overlayData);

                overlayData.Width = Selection.BorderOverlayWidth;
                overlayData.Cut = false;

                var normal = direction.Turn90(true);
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
            if (Tool.MouseRayValid)
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
                    else if (magnitude < SegmentEndData.CircleRadius + 0.5f && magnitude > SegmentEndData.CircleRadius - 0.5f)
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
            var data = Tool.Data;

            foreach (var segmentData in data.SegmentEndDatas)
            {
                var overlayData = new OverlayData(cameraInfo) { Color = Colors.Red };
                segmentData.Render(overlayData);
            }

            if (IsHoverSegmentEndCenter)
                HoverSegmentEndCenter.RenderInnerCircle(new OverlayData(cameraInfo));
            else if (IsHoverSegmentEndCircle)
                HoverSegmentEndCircle.RenderOutterCircle(new OverlayData(cameraInfo));
        }
    }
    public class DragSegmentEndToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Drag;
        public SegmentEndData SegmentEnd { get; private set; } = null;
        public BezierTrajectory Bezier { get; private set; }

        protected override void Reset(IToolMode prevMode)
        {
            SegmentEnd = prevMode is EditToolMode editMode ? editMode.HoverSegmentEndCenter : null;

            var segment = SegmentEnd.Segment;

            var isStart = segment.IsStartNode(SegmentEnd.NodeId);
            var startPos = (isStart ? segment.m_startNode : segment.m_endNode).GetNode().m_position;
            var startDir = isStart ? segment.m_startDirection : segment.m_endDirection;
            var endPos = (isStart ? segment.m_endNode : segment.m_startNode).GetNode().m_position;
            var endDir = isStart ? segment.m_endDirection : segment.m_startDirection;
            NetSegmentPatches.ShiftSegment(SegmentEnd.NodeId, SegmentEnd.Id, ref startPos, ref startDir, ref endPos, ref endDir);

            Bezier = new BezierTrajectory(startPos, startDir, endPos, endDir);
        }
        public override void OnMouseDrag(Event e)
        {
            SegmentEnd.Segment.GetHitPosition(Tool.Ray, out _, out var hitPosition);
            Bezier.Trajectory.ClosestPositionAndDirection(hitPosition, out _, out _, out var t);
            SegmentEnd.Offset = Bezier.Cut(0f, t).Length.RoundToNearest(0.1f);
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }
        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            Bezier.Render(new OverlayData(cameraInfo) { Width = 3f });
            SegmentEnd.Render(new OverlayData(cameraInfo) { Color = Colors.Red });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = SegmentEnd.Offset.ToString("0.0");
            color = Colors.Yellow;
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
            var angle = (BeginRotate + quaternion.eulerAngles.y).RoundToNearest(1f) % 360f;
            SegmentEnd.RotateAngle = angle > 180f ? angle - 360f : angle;
            SegmentEnd.UpdateNode();

            Tool.Panel.UpdatePanel();
        }

        public override void OnPrimaryMouseClicked(Event e) => OnMouseUp(e);
        public override void OnMouseUp(Event e) => Tool.SetDefaultMode();

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var startDir = SegmentEnd.EndDirection;
            var endDir = SegmentEnd.EndDirection.TurnDeg(SegmentEnd.RotateAngle, false);
            SegmentEnd.Position.RenderAngle(new OverlayData(cameraInfo) { Color = Colors.Yellow }, startDir, endDir, SegmentEndData.DotRadius - 0.2f, SegmentEndData.CircleRadius - 0.2f);

            SegmentEnd.Render(new OverlayData(cameraInfo) { Color = Colors.Red });
        }
        public override bool GetExtraInfo(out string text, out Color color, out float size, out Vector3 position, out Vector3 direction)
        {
            text = $"{SegmentEnd.RotateAngle:0}°";
            color = Colors.Yellow;
            size = 2f;
            position = SegmentEnd.Position + SegmentEnd.Direction * SegmentEndData.CircleRadius;
            direction = SegmentEnd.Direction;
            return true;
        }
    }
}
