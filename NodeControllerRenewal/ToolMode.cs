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
        protected override bool SelectSegments => false;
        protected override bool SelectMiddleNodes => true;

        public override string GetToolInfo() => IsHoverNode ? $"Node {HoverNode.Id}" : (IsHoverSegment ? $"Segment {HoverSegment.Id}" : string.Empty);

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
            {
                var data = NodeManager.Instance.GetOrCreate(HoverNode.Id);
                Tool.SetData(data);
                Tool.SetDefaultMode();
            }
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
            foreach (var segmentId in data.Node.SegmentsId())
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
                NetSegmentPatches.FixPosAndDir(data.NodeId, segmentId, ref startPos, ref startDir, ref endPos, ref endDir);

                var line = new StraightTrajectory(startPos, startPos + 5 * startDir);
                line.Render(new OverlayData(cameraInfo) { Color = Color.green });
            }
        }
    }
}
