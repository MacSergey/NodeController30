using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NodeController
{
    public class EditNodeToolMode : NodeControllerToolMode
    {
        public static NodeControllerShortcut ResetOffsetShortcut { get; } = new NodeControllerShortcut(nameof(ResetOffsetShortcut), nameof(Localize.Setting_ShortcutResetToDefault), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.SetKeepDefaults());
        public static NodeControllerShortcut ResetToDefaultShortcut { get; } = new NodeControllerShortcut(nameof(ResetToDefaultShortcut), nameof(Localize.Setting_ShortcutKeepDefault), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.ResetToDefault());
        public static NodeControllerShortcut MakeStraightEndsShortcut { get; } = new NodeControllerShortcut(nameof(MakeStraightEndsShortcut), nameof(Localize.Setting_ShortcutMakeStraightEnds), SavedInputKey.Encode(KeyCode.S, true, true, false), () => SingletonTool<NodeControllerTool>.Instance.MakeStraightEnds());

        public static NodeControllerShortcut CalculateShiftByNearbyShortcut { get; } = new NodeControllerShortcut(nameof(CalculateShiftByNearbyShortcut), nameof(Localize.Setting_ShortcutCalculateShiftByNearby), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.CalculateShiftByNearby());
        public static NodeControllerShortcut CalculateShiftByIntersectionsShortcut { get; } = new NodeControllerShortcut(nameof(CalculateShiftByIntersectionsShortcut), nameof(Localize.Setting_ShortcutCalculateShiftByIntersections), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.CalculateShiftByIntersections());
        public static NodeControllerShortcut SetShiftBetweenIntersectionsShortcut { get; } = new NodeControllerShortcut(nameof(SetShiftBetweenIntersectionsShortcut), nameof(Localize.Setting_ShortcutSetShiftBetweenIntersections), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.SetShiftBetweenIntersections());

        public static NodeControllerShortcut CalculateTwistByNearbyShortcut { get; } = new NodeControllerShortcut(nameof(CalculateTwistByNearbyShortcut), nameof(Localize.Setting_ShortcutCalculateTwistByNearby), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.CalculateTwistByNearby());
        public static NodeControllerShortcut CalculateTwistByIntersectionsShortcut { get; } = new NodeControllerShortcut(nameof(CalculateTwistByIntersectionsShortcut), nameof(Localize.Setting_ShortcutCalculateTwistByIntersections), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.CalculateTwistByIntersections());
        public static NodeControllerShortcut SetTwistBetweenIntersectionsShortcut { get; } = new NodeControllerShortcut(nameof(SetTwistBetweenIntersectionsShortcut), nameof(Localize.Setting_ShortcutSetTwistBetweenIntersections), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.SetTwistBetweenIntersections());

        //public static NodeControllerShortcut ChangeNodeStyleShortcut { get; } = new NodeControllerShortcut(nameof(ChangeNodeStyleShortcut), nameof(Localize.Setting_ShortcutChangeNodeStyle), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.ChangeNodeStyle());
        public static NodeControllerShortcut ChangeMainRoadModeShortcut { get; } = new NodeControllerShortcut(nameof(ChangeMainRoadModeShortcut), nameof(Localize.Setting_ShortcutChangeMainRoadMode), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.ChangeMainRoadMode());


        public override ToolModeType Type => ToolModeType.Edit;

        public SegmentEndData HoverSegmentCenter { get; private set; }
        private bool IsHoverSegmentCenter => HoverSegmentCenter != null;

        public SegmentEndData HoverSegmentCircle { get; private set; }
        private bool IsHoverSegmentCircle => HoverSegmentCircle != null;


        public SideType HoverCorner { get; private set; }
        public SegmentEndData HoverCornerCenter { get; private set; }
        private bool IsHoverCornerCenter => HoverCornerCenter != null;

        public SegmentEndData HoverCornerCircle { get; private set; }
        private bool IsHoverCornerCircle => HoverCornerCircle != null;

        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                yield return ResetOffsetShortcut;
                yield return ResetToDefaultShortcut;
                yield return MakeStraightEndsShortcut;

                yield return CalculateShiftByNearbyShortcut;
                yield return CalculateShiftByIntersectionsShortcut;
                yield return SetShiftBetweenIntersectionsShortcut;

                yield return CalculateTwistByNearbyShortcut;
                yield return CalculateTwistByIntersectionsShortcut;
                yield return SetTwistBetweenIntersectionsShortcut;

                //yield return ChangeNodeStyleShortcut;
                yield return ChangeMainRoadModeShortcut;
            }
        }

        public override void OnToolUpdate()
        {
            if (Tool.Data.AllowSetMainRoad && Tool.Data.Mode != Mode.FreeForm && !Tool.Panel.IsHover && Utility.OnlyAltIsPressed)
                Tool.SetMode(ToolModeType.ChangeMain);
            else if (!Tool.Panel.IsHover && Tool.Data.Mode != Mode.FreeForm && Utility.OnlyShiftIsPressed)
                Tool.SetMode(ToolModeType.Aling);
            else if (Tool.MouseRayValid && Tool.Data.IsMoveableEnds)
            {
                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                    var magnitude = (segmentData.Position - hitPos).magnitude;

                    if (segmentData.IsOffsetChangeable && magnitude < SegmentEndData.CenterRadius)
                    {
                        HoverSegmentCenter = segmentData;
                        HoverSegmentCircle = null;
                        HoverCornerCenter = null;
                        HoverCornerCircle = null;
                        return;
                    }
                    else if (!segmentData.IsNarrow && segmentData.IsRotateChangeable && magnitude > SegmentEndData.CircleRadius - 0.5f && magnitude < SegmentEndData.CircleRadius + 1f)
                    {
                        HoverSegmentCenter = null;
                        HoverSegmentCircle = segmentData;
                        HoverCornerCenter = null;
                        HoverCornerCircle = null;
                        return;
                    }
                    else if (segmentData.IsOffsetChangeable && CheckCorner(segmentData, SideType.Left) || CheckCorner(segmentData, SideType.Right))
                        return;
                }
            }

            HoverSegmentCenter = null;
            HoverSegmentCircle = null;
            HoverCornerCenter = null;
            HoverCornerCircle = null;
        }
        private bool CheckCorner(SegmentEndData segmentData, SideType side)
        {
            var hitPos = Tool.Ray.GetRayPosition(segmentData[side].MarkerPos.y, out _);
            var magnitude = (segmentData[side].MarkerPos - hitPos).magnitude;

            if (segmentData.IsOffsetChangeable && magnitude < SegmentEndData.CornerCenterRadius)
            {
                HoverSegmentCenter = null;
                HoverSegmentCircle = null;
                HoverCornerCenter = segmentData;
                HoverCornerCircle = null;
                HoverCorner = side;
                return true;
            }
            else if(segmentData.Mode == Mode.FreeForm && segmentData.IsRotateChangeable && magnitude > SegmentEndData.CornerCircleRadius - 0.5f && magnitude < SegmentEndData.CornerCircleRadius + 1f )
            {
                HoverSegmentCenter = null;
                HoverSegmentCircle = null;
                HoverCornerCenter = null;
                HoverCornerCircle = segmentData;
                HoverCorner = side;
                return true;
            }
            else
                return false;
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetData(null);
            Tool.SetMode(ToolModeType.Select);
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSegmentCenter)
                Tool.SetMode(ToolModeType.DragEnd);
            else if (IsHoverSegmentCircle)
                Tool.SetMode(ToolModeType.RotateEnd);
            else if (IsHoverCornerCenter)
                Tool.SetMode(ToolModeType.DragCorner);
            else if (IsHoverCornerCircle)
                Tool.SetMode(ToolModeType.RotateCorner);
        }
        public override string GetToolInfo()
        {
            if (IsHoverSegmentCenter)
            {
                if (HoverSegmentCenter.Mode == Mode.FreeForm)
                    return string.Format(Localize.Tool_InfoDragCenterFree, LocalizeExtension.Shift.AddInfoColor());
                else
                    return Localize.Tool_InfoDragCenter;
            }
            else if (IsHoverSegmentCircle)
                return Localize.Tool_InfoDragCircle;
            else if (IsHoverCornerCenter)
            {
                if (HoverCornerCenter.Mode == Mode.FreeForm)
                    return string.Format(Localize.Tool_InfoDragCornerFree, LocalizeExtension.Shift.AddInfoColor());
                else
                    return Localize.Tool_InfoDragCorner;
            }
            else
            {
                var info = new List<string>();

                if (Tool.Data.Mode != Mode.FreeForm)
                    info.Add(string.Format(Localize.Tool_InfoAlignMode, LocalizeExtension.Shift.AddInfoColor()));
                if (Tool.Data.Mode != Mode.FreeForm && Tool.Data.AllowSetMainRoad)
                    info.Add(string.Format(Localize.Tool_InfoChangeMainMode, LocalizeExtension.Alt.AddInfoColor()));

                return string.Join("\n", info.ToArray());
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;
            var hover = new OverlayData(cameraInfo) { RenderLimit = underground };
            var yellow = new OverlayData(cameraInfo) { Color = Yellow, RenderLimit = underground };

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var defaultColor = new OverlayData(cameraInfo) { Color = segmentData.OverlayColor, RenderLimit = underground };
                var segmentColor = new OverlayData(cameraInfo) { Color = segmentData.Color.SetOpacity(Settings.OverlayOpacity), RenderLimit = underground };
                var circle = segmentData == HoverSegmentCircle ? hover : yellow;
                var center = segmentData == HoverSegmentCenter ? hover : segmentColor;
                segmentData.Render(defaultColor, circle, center);

                if (segmentData.IsOffsetChangeable)
                {
                    var leftCenter = segmentData == HoverCornerCenter && HoverCorner == SideType.Left ? hover : yellow;
                    segmentData[SideType.Left].RenderCenter(leftCenter);

                    var rightCenter = segmentData == HoverCornerCenter && HoverCorner == SideType.Right ? hover : yellow;
                    segmentData[SideType.Right].RenderCenter(rightCenter);
                }

                if (segmentData.Mode == Mode.FreeForm && segmentData.IsRotateChangeable)
                {
                    var leftCircle = segmentData == HoverCornerCircle && HoverCorner == SideType.Left ? hover : yellow;
                    segmentData[SideType.Left].RenderCircle(leftCircle);

                    var rightCircle = segmentData == HoverCornerCircle && HoverCorner == SideType.Right ? hover : yellow;
                    segmentData[SideType.Right].RenderCircle(rightCircle);
                }
            }
        }
    }
}
