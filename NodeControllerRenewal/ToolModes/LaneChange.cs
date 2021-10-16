using ModsCommon;
using ModsCommon.Utilities;
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

        private class SelectionInfo
        {
            public ushort SegmentId;
            public uint LaneId;
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
                foreach (var segmentData in Tool.Data.MainSegmentEndDatas)
                {
                    var segment = segmentData.Id.GetSegment();
                    var isStart = segmentData.IsStartNode;
                    var lanes = segment.GetLanes();

                    foreach (var laneId in segment.GetLaneIds())
                    {
                        var lane = laneId.GetLane();
                        lane.GetClosestPosition(segmentData.Position, out var position, out _);

                        var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                        if ((position - hitPos).sqrMagnitude < Radius * Radius)
                        {
                            HoverLaneEnd = new()
                            {
                                SegmentId = segmentData.Id,
                                LaneId = laneId
                            };
                            return;
                        }
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
                var lane = HoverLaneEnd.LaneId.GetLane();
                var oldflags = lane.m_flags;
                var inversion = (ushort)NetLane.Flags.Inverted;

                if ((oldflags & inversion) == inversion)
                    lane.m_flags = (ushort)~(~oldflags & inversion);
                else
                    lane.m_flags = (ushort)(oldflags | inversion);

                NetManager.instance.UpdateSegment(HoverLaneEnd.SegmentId);
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

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var segment = segmentData.Id.GetSegment();
                foreach (var laneId in segment.GetLaneIds())
                {
                    var lane = laneId.GetLane();
                    lane.GetClosestPosition(segmentData.Position, out var position, out _);

                    position.RenderCircle(
                        new OverlayData(cameraInfo) { Color = segmentData.OverlayColor, RenderLimit = underground },
                        2.5f, segmentData.IsStartNode ? 0.0f : 2.0f);

                    if (HoverLaneEnd.LaneId == laneId)
                        position.RenderCircle(
                            new OverlayData(cameraInfo) { Color = Color.white, RenderLimit = underground },
                            3.5f, 3.0f);
                }
            }
        }
    }
}
