using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;

namespace NodeController
{
    public class SelectNodeToolMode : BaseSelectToolMode<NodeControllerTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;
        protected override Color32 NodeColor => Colors.Yellow;
        private bool IsPossibleInsertNode { get; set; }
        private Vector3 InsertPosition { get; set; }

        public override string GetToolInfo()
        {
            if (IsHoverNode)
                return string.Format(Localize.Tool_InfoClickNode, HoverNode.Id);
            else if (IsHoverSegment)
            {
                if (!IsPossibleInsertNode)
                    return Localize.Tool_InfoTooCloseNode;
                else if (HoverSegment.Id.GetSegment().Info.PedestrianLanes() >= 2)
                    return Localize.Tool_InfoInsertCrossingNode;
                else
                    return Localize.Tool_InfoInsertNode;
            }
            else
                return Localize.Tool_InfoSelectNode;
        }

        protected override bool IsValidNode(ushort nodeId)
        {
            if (Settings.SelectMiddleNodes)
                return true;

            ref var node = ref nodeId.GetNode();
            return node.m_flags.CheckFlags(0, NetNode.Flags.Middle | NetNode.Flags.Outside) || node.m_flags.CheckFlags(0, NetNode.Flags.Moveable | NetNode.Flags.Outside);
        }
        protected override bool CheckSegment(ushort segmentId) => segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable) && base.CheckSegment(segmentId);
        protected override bool CheckItemClass(ItemClass itemClass) => itemClass switch
        {
            { m_service: ItemClass.Service.Road } => true,
            { m_service: ItemClass.Service.PublicTransport } => true,
            { m_service: ItemClass.Service.Beautification, m_level: >= (ItemClass.Level)3 } => true,
            { m_service: ItemClass.Service.Beautification, m_subService: ItemClass.SubService.BeautificationParks } => true,
            _ => false,
        };

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsHoverSegment)
            {
                SegmentEndData.CalculateSegmentBeziers(HoverSegment.Id, out var bezier, out _, out _);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out _, out var position);
                IsPossibleInsertNode = PossibleInsertNode(position);
                InsertPosition = position;
            }
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
                Set(SingletonManager<Manager>.Instance[HoverNode.Id, true]);
            else if (IsHoverSegment && IsPossibleInsertNode)
            {
                var controlPoint = new NetTool.ControlPoint() { m_segment = HoverSegment.Id, m_position = InsertPosition};
                var newNode = SingletonManager<Manager>.Instance.InsertNode(controlPoint);
                Set(newNode);
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
                ref var node = ref data.Id.GetNode();
                if (!Settings.SelectMiddleNodes && node.m_flags.CheckFlags(NetNode.Flags.Moveable, NetNode.Flags.End))
                    continue;

                var gap = 8f + data.halfWidth * 2f * Mathf.Sqrt(1 - data.DeltaAngleCos * data.DeltaAngleCos);
                if ((data.Position - position).sqrMagnitude < gap * gap)
                    return false;
            }

            return true;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverSegment)
            {
                SegmentEndData.CalculateSegmentBeziers(HoverSegment.Id, out var bezier, out _, out _);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out var position);
                var direction = bezier.Tangent(t).MakeFlatNormalized();
                var halfWidth = SegmentEndData.GetSegmentWidth(HoverSegment.Id, t);

                var overlayData = new OverlayData(cameraInfo) { Width = halfWidth * 2, Color = PossibleInsertNode(position) ? Colors.Green : Colors.Red, AlphaBlend = false, Cut = true };

                var middle = new Bezier3()
                {
                    a = position + direction,
                    b = position,
                    c = position,
                    d = position - direction,
                };
                middle.RenderBezier(overlayData);

                overlayData.Width = Mathf.Min(halfWidth * 2, Selection.BorderOverlayWidth);
                overlayData.Cut = false;

                var normal = direction.MakeFlatNormalized().Turn90(true);
                RenderBorder(overlayData, position + direction, normal, halfWidth);
                RenderBorder(overlayData, position - direction, normal, halfWidth);
            }
            else
                base.RenderOverlay(cameraInfo);
        }
        private void RenderBorder(OverlayData overlayData, Vector3 position, Vector3 normal, float halfWidth)
        {
            var delta = Mathf.Max(halfWidth - Selection.BorderOverlayWidth / 2, 0f);
            var bezier = new Bezier3
            {
                a = position + normal * delta,
                b = position,
                c = position,
                d = position - normal * delta,
            };
            bezier.RenderBezier(overlayData);
        }
    }
}
