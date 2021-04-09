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
        //protected override bool SelectSegments => false;

        public override string GetToolInfo() => IsHoverNode ? $"Node {HoverNode.Id}" : "Select node";

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
            {
                var data = NodeManager.Instance[HoverNode.Id, true];
                Tool.SetData(data);
                Tool.SetDefaultMode();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverSegment)
            {
                var segment = HoverSegment.Id.GetSegment();
                var hitPos = HoverSegment.GetHitPosition(Tool.Ray, out _);
                segment.GetClosestPositionAndDirection(hitPos, out var position, out var direction);

                var normal = direction.Turn90(true);
                var bezier = new Bezier3
                {
                    a = position + normal * segment.Info.m_halfWidth,
                    b = position,
                    c = position,
                    d = position - normal * segment.Info.m_halfWidth,
                };
                bezier.RenderBezier(new OverlayData(cameraInfo) { Width = 2f, Color = Colors.Green, AlphaBlend = false, Cut = true });

                RenderBorder(cameraInfo, position + direction, normal, segment.Info.m_halfWidth);
                RenderBorder(cameraInfo, position - direction, normal, segment.Info.m_halfWidth);
            }
            else
                base.RenderOverlay(cameraInfo);
        }
        private void RenderBorder(RenderManager.CameraInfo cameraInfo, Vector3 position, Vector3 normal, float halfWidth)
        {
            var bezier = new Bezier3
            {
                a = position + normal * (halfWidth - Selection.BorderOverlayWidth / 2),
                b = position,
                c = position,
                d = position - normal * (halfWidth - Selection.BorderOverlayWidth / 2),
            };
            bezier.RenderBezier(new OverlayData(cameraInfo) { Width = Selection.BorderOverlayWidth, Color = Colors.Green, AlphaBlend = false });
        }
    }
    public class EditToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Edit;
        public override void OnSecondaryMouseClicked()
        {
            Tool.SetData(null);
            Tool.SetMode(ToolModeType.Select);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var data = Tool.Data as NodeData;
            foreach (var segmentId in data.Node.SegmentIds())
            {
                var segment = segmentId.GetSegment();
                var bezier = new Bezier3()
                {
                    a = segment.m_startNode.GetNode().m_position,
                    b = segment.m_startDirection,
                    c = segment.m_endDirection,
                    d = segment.m_endNode.GetNode().m_position
                };
                NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
                bezier.RenderBezier(new OverlayData(cameraInfo));

                var selection = new SegmentSelection(segmentId);
                selection.RenderBorders(new OverlayData(cameraInfo) { Color = Color.red });

                var isStart = segment.m_startNode == data.NodeId;
                var startPos = (isStart ? segment.m_startNode : segment.m_endNode).GetNode().m_position;
                var startDir = isStart ? segment.m_startDirection : segment.m_endDirection;
                var endPos = (isStart ? segment.m_endNode : segment.m_startNode).GetNode().m_position;
                var endDir = isStart ? segment.m_endDirection : segment.m_startDirection;
                NetSegmentPatches.ShiftSegment(data.NodeId, segmentId, ref startPos, ref startDir, ref endPos, ref endDir);

                var line = new StraightTrajectory(startPos, startPos + 5 * startDir);
                line.Render(new OverlayData(cameraInfo) { Color = Color.green });
            }
        }
    }
}
