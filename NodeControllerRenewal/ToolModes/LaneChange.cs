using ColossalFramework.Plugins;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NodeController
{
    public class LaneEditMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.LaneEdit;
        public override bool ShowPanel => false;

        private SelectionInfo HoverLaneEnd { get; set; }
        private bool IsHoverSegmentEnd => HoverLaneEnd != null;
        private float Radius => SegmentEndData.CenterDotRadius + 0.5f;

        public bool IsValid { get; private set; }

        private class SelectionInfo
        {
            public ushort SegmentId;
            public uint LaneId;
            public int LaneIndex;
        }

        protected override void Reset(IToolMode prevMode)
        {
            HoverLaneEnd = null;
        }
        public override void OnToolUpdate()
        {
            if (!Utility.OnlyCtrlIsPressed)
                Tool.SetDefaultMode();

            else if (Tool.MouseRayValid)
            {
                var nodePosition = Tool.Data.GetPosition();

                foreach (var segmentId in Tool.Data.SegmentIds)
                {
                    var segment = segmentId.GetSegment();
                    var lanes = segment.GetLanes();

                    var i = 0;
                    foreach (var laneId in segment.GetLaneIds())
                    {
                        var lane = laneId.GetLane();
                        var direction = segment.Info.m_lanes[i].m_direction;

                        if (direction == NetInfo.Direction.Forward || direction == NetInfo.Direction.Backward)
                        {
                            lane.GetClosestPosition(nodePosition, out var position, out _);

                            var hitPos = Tool.Ray.GetRayPosition(nodePosition.y, out _);

                            if ((position - hitPos).sqrMagnitude < Radius * Radius)
                            {
                                HoverLaneEnd = new()
                                {
                                    SegmentId = segmentId,
                                    LaneId = laneId,
                                    LaneIndex = i
                                };
                                return;
                            }
                        }
                        i++;
                    }
                }
            }

            HoverLaneEnd = null;
        }
        public override void OnMouseDown(Event e)
        {
            // TODO MOVE OF LANE
        }
        public override void OnMouseUp(Event e)
        {
            // TODO MOVE OF LANE
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverSegmentEnd)
            {
                ref var segment = ref HoverLaneEnd.SegmentId.GetSegment();
                ref var lane = ref HoverLaneEnd.LaneId.GetLane();
                ref var flags = ref lane.m_flags;

                flags = (ushort)(flags & ~(ushort)NetLane.Flags.Forward);
                if ((flags & (int)NetLane.Flags.Forward) != 0)
                    flags = (ushort)(flags & ~(ushort)NetLane.Flags.Forward);
                else
                    flags = (ushort)(flags | (ushort)NetLane.Flags.Forward);

                segment.UpdateLanes(HoverLaneEnd.SegmentId, true);
            }
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetDefaultMode();
        }

        public override string GetToolInfo()
        {
            return Localize.Tool_InfoClickLaneInversion;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;
            var nodePosition = Tool.Data.GetPosition();
            var i = 0; // cycling index of lane colors
            var iMax = SegmentEndData.OverlayColors.Length;

            foreach (var segmentId in Tool.Data.SegmentIds)
            {
                var segment = segmentId.GetSegment();
                var laneIdx = 0;
                foreach (var laneId in segment.GetLaneIds())
                {
                    var lane = laneId.GetLane();
                    var direction = segment.Info.m_lanes[laneIdx].m_direction;
                    var outgoing = direction == NetInfo.Direction.Forward || (direction == NetInfo.Direction.Backward && segment.IsInvert());
                    var both = direction == NetInfo.Direction.Both || direction == NetInfo.Direction.None;
                    lane.GetClosestPosition(nodePosition, out var position, out _);

                    if (!both)
                        position.RenderCircle(
                            new OverlayData(cameraInfo) { Color = SegmentEndData.OverlayColors[i++], RenderLimit = underground },
                            1.5f, outgoing ? 0.0f : 1.0f);

                    if (!both && HoverLaneEnd?.LaneId == laneId)
                        position.RenderCircle(
                            new OverlayData(cameraInfo) { Color = Color.white, RenderLimit = underground },
                            2.5f, 2.0f);

                    if (iMax == i) i = 0;
                    laneIdx++;
                }
            }
        }
    }
}
