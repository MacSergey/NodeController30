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

        public static NodeControllerShortcut ChangeNodeStyleShortcut { get; } = new NodeControllerShortcut(nameof(ChangeNodeStyleShortcut), nameof(Localize.Setting_ShortcutChangeNodeStyle), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.ChangeNodeStyle());
        public static NodeControllerShortcut ChangeMainRoadModeShortcut { get; } = new NodeControllerShortcut(nameof(ChangeMainRoadModeShortcut), nameof(Localize.Setting_ShortcutChangeMainRoadMode), SavedInputKey.Empty, () => SingletonTool<NodeControllerTool>.Instance.ChangeMainRoadMode());


        public override ToolModeType Type => ToolModeType.Edit;

        public SegmentEndData HoverSegmentEndCenter { get; private set; }
        private bool IsHoverSegmentEndCenter => HoverSegmentEndCenter != null;

        public SegmentEndData HoverSegmentEndCircle { get; private set; }
        private bool IsHoverSegmentEndCircle => HoverSegmentEndCircle != null;

        public SegmentEndData HoverSegmentEndCorner { get; private set; }
        public SideType HoverCorner { get; private set; }
        private bool IsHoverSegmentEndCorner => HoverSegmentEndCorner != null;

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

                yield return ChangeNodeStyleShortcut;
                yield return ChangeMainRoadModeShortcut;
            }
        }

        public override void OnToolUpdate()
        {
            if (Tool.Data.AllowSetMainRoad && !Tool.Panel.IsHover && Utility.OnlyAltIsPressed)
                Tool.SetMode(ToolModeType.ChangeMain);
            else if (!Tool.Panel.IsHover && Utility.OnlyShiftIsPressed)
                Tool.SetMode(ToolModeType.Aling);
            else if (Tool.MouseRayValid && Tool.Data.IsMoveableEnds)
            {
                foreach (var segmentData in Tool.Data.SegmentEndDatas)
                {
                    var hitPos = Tool.Ray.GetRayPosition(segmentData.Position.y, out _);

                    var magnitude = (segmentData.Position - hitPos).magnitude;

                    if (segmentData.IsOffsetChangeable && magnitude < SegmentEndData.CenterDotRadius)
                    {
                        HoverSegmentEndCenter = segmentData;
                        HoverSegmentEndCircle = null;
                        HoverSegmentEndCorner = null;
                        return;
                    }
                    else if (!segmentData.IsNarrow && segmentData.IsRotateChangeable && magnitude < SegmentEndData.CircleRadius + 1f && magnitude > SegmentEndData.CircleRadius - 0.5f)
                    {
                        HoverSegmentEndCenter = null;
                        HoverSegmentEndCircle = segmentData;
                        HoverSegmentEndCorner = null;
                        return;
                    }
                    else if (segmentData.IsOffsetChangeable && CheckCorner(segmentData, SideType.Left) || CheckCorner(segmentData, SideType.Right))
                        return;
                }
            }

            HoverSegmentEndCenter = null;
            HoverSegmentEndCircle = null;
            HoverSegmentEndCorner = null;
        }
        private bool CheckCorner(SegmentEndData segmentData, SideType side)
        {
            var hitPos = Tool.Ray.GetRayPosition(segmentData[side].MarkerPosition.y, out _);

            if ((segmentData[side].MarkerPosition - hitPos).magnitude < SegmentEndData.CornerDotRadius)
            {
                HoverSegmentEndCenter = null;
                HoverSegmentEndCircle = null;
                HoverSegmentEndCorner = segmentData;
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
            if (IsHoverSegmentEndCenter)
                Tool.SetMode(ToolModeType.DragEnd);
            else if (IsHoverSegmentEndCircle)
                Tool.SetMode(ToolModeType.Rotate);
            else if (IsHoverSegmentEndCorner)
                Tool.SetMode(ToolModeType.DragCorner);
        }
        public override string GetToolInfo()
        {
            if (IsHoverSegmentEndCenter)
                return Localize.Tool_InfoDragCenter;
            else if (IsHoverSegmentEndCircle)
                return Localize.Tool_InfoDragCircle;
            if (IsHoverSegmentEndCorner)
                return Localize.Tool_InfoDragCorner;
            else
            {
                var info = new List<string>();
                info.Add(string.Format(Localize.Tool_InfoAlignMode, LocalizeExtension.Shift.AddInfoColor()));
                if (Tool.Data.AllowSetMainRoad)
                    info.Add(string.Format(Localize.Tool_InfoChangeMainMode, LocalizeExtension.Alt.AddInfoColor()));

                return string.Join("\n", info.ToArray());
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var underground = IsUnderground;
            var hover = new OverlayData(cameraInfo) { RenderLimit = underground };
            var yellow = new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = underground };

            foreach (var segmentData in Tool.Data.SegmentEndDatas)
            {
                var defaultColor = new OverlayData(cameraInfo) { Color = segmentData.OverlayColor, RenderLimit = underground };
                var outter = segmentData == HoverSegmentEndCircle ? hover : yellow;
                var inner = segmentData == HoverSegmentEndCenter ? hover : new OverlayData(cameraInfo) { Color = segmentData.Color, RenderLimit = underground };
                var left = segmentData == HoverSegmentEndCorner && HoverCorner == SideType.Left ? hover : yellow;
                var right = segmentData == HoverSegmentEndCorner && HoverCorner == SideType.Right ? hover : yellow;
                segmentData.Render(defaultColor, outter, inner, left, right);
            }
        }
    }
}
