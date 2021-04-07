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

namespace NodeController
{
    public class SelectToolMode : BaseSelectToolMode<Mod, NodeControllerTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;
        protected override bool SelectSegments => false;

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
            }
        }
    }
}
