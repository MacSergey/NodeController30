using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace NodeController
{
    public abstract class NodeStyle
    {
        #region MIN MAX VALUES
        public static float MaxShift => 64f;
        public static float MinShift => -64f;
        public static float MaxRoadSlope => 60f;
        public static float MinRoadSlope => -60f;
        public static float MaxSlope => 85f;
        public static float MinSlope => -85f;
        public static float MaxRotate => 89f;
        public static float MinRotate => -89f;
        public static float MaxRoadTwist => 60f;
        public static float MinRoadTwist => -60f;
        public static float MaxTwist => 85f;
        public static float MinTwist => -85f;
        public static float MaxRoadStretch => 500f;
        public static float MaxStretch => 1000f;
        public static float MinStretch => 1f;
        public static float MaxOffset => 1000f;
        public static float MinOffset => 0f;
        public static float MaxDeltaHeight => 10f;
        public static float MinDeltaHeight => -10f;
        public static Vector3 MinPosDelta => new Vector3(-100f, -100f, -100f);
        public static Vector3 MaxPosDelta => new Vector3(100f, 100f, 100f);
        public static Vector3 MinDirDelta => new Vector3(-180f, -180f, 0f);
        public static Vector3 MaxDirDelta => new Vector3(180f, 180f, 10f);

        private static bool HideCrosswalksEnable { get; } = DependencyUtilities.HideCrossings?.isEnabled == true;

        #endregion

        #region PROPERTIES

        public abstract NodeStyleType Type { get; }

        public virtual float AdditionalOffset => 0f;

        public virtual Mode SupportModes => Mode.Flat | Mode.Slope;
        public virtual SupportOption SupportMode => SupportOption.None;
        public virtual SupportOption SupportOffset => SupportOption.None;
        public virtual SupportOption SupportShift => SupportOption.None;
        public virtual SupportOption SupportRotate => SupportOption.None;
        public virtual SupportOption SupportSlope => SupportOption.None;
        public virtual SupportOption SupportTwist => SupportOption.None;
        public virtual SupportOption SupportMarking => SupportOption.None;
        public virtual SupportOption SupportStretch => SupportOption.None;
        public virtual SupportOption SupportCollision => SupportOption.None;
        public virtual SupportOption SupportForceNodeless => SupportOption.None;
        public virtual SupportOption SupportFollowMainSlope => SupportOption.None;
        public virtual SupportOption SupportDeltaHeight => SupportOption.None;
        public virtual SupportOption SupportCornerDelta => SupportOption.None;
        public virtual bool SupportTrafficLights => false;
        public virtual bool ForceKeepDefault => false;
        public virtual bool NeedFixDirection => true;

        public SupportOption TotalSupport => (SupportOffset | SupportShift | SupportRotate | SupportSlope | SupportTwist | SupportStretch | SupportCollision | SupportForceNodeless | SupportFollowMainSlope | SupportCornerDelta) & SupportOption.All;
        private bool OnlyOnSlope => DefaultMode != Mode.Flat;

        public virtual float DefaultOffset => 0f;
        public virtual float DefaultShift => 0f;
        public virtual float DefaultRotate => 0f;
        public virtual float DefaultSlope => 0f;
        public virtual float DefaultTwist => 0f;
        public virtual bool DefaultNoMarking => false;
        public virtual bool GetDefaultCollision(SegmentEndData segmentEnd) => !segmentEnd.IsTrack;
        public virtual bool DefaultForceNodeLess => false;
        public virtual Mode DefaultMode => Settings.NodeIsSlopedByDefault ? Mode.Slope : Mode.Flat;
        public virtual float DefaultStretch => 1f;
        public virtual Vector3 DefaultDelta => Vector3.zero;
        public virtual bool DefaultFollowSlope => true;
        public virtual float DefaultDeltaHeight => 0f;

        public virtual bool IsMoveable => false;

        public bool IsDefault
        {
            get
            {
                if (GetMode() != DefaultMode)
                    return false;

                else if (Mathf.Abs(GetShift() - DefaultShift) > 0.001f)
                    return false;

                else if (Mathf.Abs(GetRotate() - DefaultRotate) > 0.1f)
                    return false;

                else if (Mathf.Abs(GetSlope() - DefaultSlope) > 0.1f)
                    return false;

                else if (Mathf.Abs(GetTwist() - DefaultTwist) > 0.1f)
                    return false;

                else if (Mathf.Abs(GetStretch() - DefaultStretch) > 0.1f)
                    return false;

                else if (GetNoMarkings() != DefaultNoMarking)
                    return false;

                else if (!Data.SegmentEndDatas.Any(s => s.Collision == GetDefaultCollision(s)))
                    return false;

                else if (Data.ForceNodeLess != DefaultForceNodeLess)
                    return false;

                else if (Data.DeltaHeight != DefaultDeltaHeight)
                    return false;

                else if (Data.FollowSlope != DefaultFollowSlope)
                    return false;

                else
                    return true;
            }
        }

        public NodeData Data { get; }
        public IEnumerable<SegmentEndData> TouchableDatas => GetDatas(TouchablePredicate);
        public IEnumerable<SegmentEndData> IsRoadDatas => GetDatas(IsRoadPredicate);

        #endregion

        public NodeStyle(NodeData data)
        {
            Data = data;
        }

        #region GETTER SETTER

        private IEnumerable<SegmentEndData> GetDatas(Func<SegmentEndData, bool> predicate) => Data.SegmentEndDatas.Where(predicate);

        public virtual Mode GetMode() => TouchableDatas.Max(s => s.Mode);
        public virtual void SetMode(Mode value)
        {
            foreach (var segmentData in TouchableDatas)
            {
                if (segmentData.Mode != value)
                    segmentData.ResetToDefault(this, value, false, false);
            }
        }

        public virtual float GetOffset() => TouchableDatas.AverageOrDefault(s => s.Offset, DefaultOffset);
        public virtual void SetOffset(float value)
        {
            foreach (var segmentData in TouchableDatas)
            {
                if (AllowOffsetPredicate(segmentData))
                    segmentData.Offset = value;
            }
        }

        public virtual float GetShift()
        {
            if (Data.IsTwoRoads)
            {
                var first = Data.FirstMainSegmentEnd;
                var second = Data.SecondMainSegmentEnd;

                if (first.IsUntouchable)
                    return second.IsUntouchable ? 0f : second.Shift;
                else if (second.IsUntouchable)
                    return first.IsUntouchable ? 0f : -first.Shift;
                else
                    return (first.Shift - second.Shift) / 2;
            }
            else
                return TouchableDatas.AverageOrDefault(s => s.Shift, DefaultShift);
        }
        public virtual void SetShift(float value)
        {
            if (Data.IsTwoRoads)
            {
                var first = Data.FirstMainSegmentEnd;
                var second = Data.SecondMainSegmentEnd;
                var firstAllow = AllowShiftPredicate(first);
                var secondAllow = AllowShiftPredicate(second);

                if (firstAllow && secondAllow)
                {
                    first.Shift = value;
                    second.Shift = -value;
                }
                else if (firstAllow)
                    first.Shift = value;
                else if (secondAllow)
                    second.Shift = -value;
            }
            else
            {
                foreach (var segmentData in TouchableDatas)
                {
                    if (AllowShiftPredicate(segmentData))
                        segmentData.Shift = value;
                }
            }
        }

        public virtual float GetRotate() => TouchableDatas.AverageOrDefault(s => s.RotateAngle, DefaultRotate);
        public virtual void SetRotate(float value)
        {
            foreach (var segmentData in TouchableDatas)
            {
                if (AllowRotatePredicate(segmentData))
                    segmentData.RotateAngle = value;
            }
        }

        public virtual float GetSlope() => TouchableDatas.AverageOrDefault(s => s.SlopeAngle, DefaultSlope);
        public virtual void SetSlope(float value)
        {
            foreach (var segmentData in TouchableDatas)
                segmentData.SlopeAngle = value;
        }

        public virtual float GetTwist() => TouchableDatas.AverageOrDefault(s => s.TwistAngle, DefaultTwist);
        public virtual void SetTwist(float value)
        {
            foreach (var segmentData in TouchableDatas)
                segmentData.TwistAngle = value;
        }

        public virtual float GetStretch() => TouchableDatas.AverageOrDefault(s => s.Stretch, DefaultStretch);
        public virtual void SetStretch(float value)
        {
            foreach (var segmentData in TouchableDatas)
                segmentData.Stretch = value;
        }

        public virtual bool? GetNoMarkings()
        {
            if (IsRoadDatas.All(s => s.NoMarkings == true))
                return true;
            else if (IsRoadDatas.All(s => s.NoMarkings == false))
                return false;
            else
                return null;
        }
        public virtual void SetNoMarkings(bool? value)
        {
            if (value != null)
            {
                foreach (var segmentData in IsRoadDatas)
                    segmentData.NoMarkings = value;
            }
        }

        public virtual bool? GetCollision()
        {
            if (TouchableDatas.All(s => s.Collision == true))
                return true;
            else if (TouchableDatas.All(s => s.Collision == false))
                return false;
            else
                return null;
        }
        public virtual void SetCollision(bool? value)
        {
            if (value != null)
            {
                foreach (var segmentData in TouchableDatas)
                    segmentData.Collision = value;
            }
        }

        public virtual bool? GetForceNodeLess()
        {
            if (GetDatas(AllowNodeLessPredicate).All(s => s.ForceNodeLess == true))
                return true;
            else if (GetDatas(AllowNodeLessPredicate).All(s => s.ForceNodeLess == false))
                return false;
            else
                return null;
        }
        public virtual void SetForceNodeLess(bool? value)
        {
            if (value != null)
            {
                foreach (var segmentData in GetDatas(AllowNodeLessPredicate))
                    segmentData.ForceNodeLess = value;
            }
        }

        public virtual bool? GetFollowSlope()
        {
            if (GetDatas(NotMainRoadPredicate).All(s => s.FollowSlope == true))
                return true;
            else if (GetDatas(NotMainRoadPredicate).All(s => s.FollowSlope == false))
                return false;
            else
                return null;
        }

        public virtual float GetDeltaHeight() => GetDatas(FollowSlopePredicate).AverageOrDefault(s => s.DeltaHeight, DefaultDeltaHeight);
        public virtual void SetDeltaHeight(float value)
        {
            foreach (var segmentData in GetDatas(FollowSlopePredicate))
                segmentData.DeltaHeight = value;
        }

        public virtual void SetFollowSlope(bool? value)
        {
            if (value != null)
            {
                foreach (var segmentData in GetDatas(NotMainRoadPredicate))
                    segmentData.FollowSlope = value;
            }
        }

        #endregion

        #region UICOMPONENTS

        private static Dictionary<Options, EditorPropertyPanel> OptionPanels { get; } = new Dictionary<Options, EditorPropertyPanel>();
        private static int OptionsZOrder { get; set; }

        public List<EditorItem> GetUIComponents(UIComponent parent, Func<bool> getShowHidden, Action<bool> setShowHidden)
        {
            var components = new List<EditorItem>();
            OptionPanels.Clear();
            var totalSupport = TotalSupport;

            if (GetModeButtons(parent) is EditorPropertyPanel mode)
                components.Add(mode);
            if (GetMainRoadButtons(parent) is EditorPropertyPanel mainRoad)
                OptionPanels.Add(Options.MainRoad, mainRoad);

            if (totalSupport == SupportOption.All)
            {
                var space = ComponentPool.Get<SpacePanel>(parent);
                space.Init(20f);
                components.Add(space);

                var titles = ComponentPool.Get<TextOptionPanel>(parent);
                titles.Init(Data, SupportOption.All, SupportOption.All);
                components.Add(titles);
            }

            OptionsZOrder = parent.childCount;
            int hiddenCount = 0;
            foreach (var option in EnumExtension.GetEnumValues<Options>(i => true))
            {
                var visibility = Settings.GetOptionVisibility(option);
                if (visibility != OptionVisibility.Disabled)
                {
                    if (GetOptionPanel(parent, option, totalSupport) is EditorPropertyPanel optionPanel)
                    {
                        OptionPanels.Add(option, optionPanel);
                        if (visibility == OptionVisibility.Hidden)
                            hiddenCount += 1;
                    }
                }
            }

            foreach (var optionPanel in OptionPanels.Values)
            {
                components.Add(optionPanel);
            }

            var moreOptionsButton = ComponentPool.Get<ButtonPanel>(parent);
            components.Add(moreOptionsButton);
            moreOptionsButton.Text = $"▼ {Localize.Option_MoreOptions} ▼";
            moreOptionsButton.TextAlignment = UIHorizontalAlignment.Center;
            moreOptionsButton.TextPadding = new RectOffset(10, 10, 6, 3);
            moreOptionsButton.AutoSize = AutoSize.Height;
            moreOptionsButton.Init();
            moreOptionsButton.SetStyle(UIStyle.Default);
            moreOptionsButton.isVisible = hiddenCount >= 2 && !getShowHidden();
            moreOptionsButton.OnButtonClick += () =>
            {
                setShowHidden(true);
                moreOptionsButton.isVisible = false;
                RefreshUIComponents(parent, getShowHidden, setShowHidden);
            };

            RefreshUIComponents(parent, getShowHidden, setShowHidden);

            return components;
        }
        public void RefreshUIComponents(UIComponent parent, Func<bool> getShowHidden, Action<bool> setShowHidden)
        {
            foreach (var option in EnumExtension.GetEnumValues<Options>(i => true))
            {
                if (OptionPanels.TryGetValue(option, out var optionPanel))
                {
                    optionPanel.isVisible = IsVisible(option, getShowHidden());
                    if (optionPanel.isVisibleSelf && optionPanel is IOptionPanel refreshPanel)
                        refreshPanel.Refresh();
                }
            }

            var minOrder = OptionsZOrder;
            foreach (var pair in OptionPanels.OrderByDescending(pair => pair.Value.isVisible).ThenBy(pair => pair.Key.Order()))
            {
                if (pair.Key.Order() >= 0)
                {
                    pair.Value.zOrder = minOrder;
                    minOrder += 1;
                }
            }
        }

        private bool IsVisible(Options option, bool showHidden)
        {
            var visibility = Settings.GetOptionVisibility(option);
            var visible = visibility == OptionVisibility.Visible || (visibility == OptionVisibility.Hidden && showHidden);

            switch (option)
            {
                case Options.Slope:
                case Options.FollowSlope:
                case Options.DeltaHeight:
                    return visible && Data.Mode == Mode.Slope;

                case Options.Twist:
                    return visible && Data.Mode != Mode.Flat;

                case Options.Offset:
                case Options.Shift:
                case Options.Collision:
                    return visible && Data.Mode != Mode.FreeForm;

                case Options.LeftCornerPos:
                case Options.RightCornerPos:
                case Options.LeftCornerDir:
                case Options.RightCornerDir:
                    return visible && Data.Mode == Mode.FreeForm;

                case Options.MainRoad:
                    return Data.Mode == Mode.Slope;

                default:
                    return visible;
            }
        }

        private EditorPropertyPanel GetOptionPanel(UIComponent parent, Options option, SupportOption support) => option switch
        {
            Options.Offset => GetOffsetOption(parent, support),
            Options.Rotate => GetRotateOption(parent, support),
            Options.Shift => GetShiftOption(parent, support),
            Options.Slope => GetSlopeOption(parent, support),
            Options.Twist => GetTwistOption(parent, support),
            Options.Stretch => GetStretchOption(parent, support),
            Options.Marking => GetMarkingsOption(parent, support),
            Options.Collision => GetCollisionOption(parent, support),
            Options.Nodeless => GetForceNodeLessOption(parent, support),
            Options.FollowSlope => GetFollowSlopeOption(parent, support),
            Options.DeltaHeight => GetDeltaHeightOption(parent, support),
            Options.LeftCornerPos => GetCornerPosOption(parent, support, SideType.Left),
            Options.RightCornerPos => GetCornerPosOption(parent, support, SideType.Right),
            Options.LeftCornerDir => GetCornerDirOption(parent, support, SideType.Left),
            Options.RightCornerDir => GetCornerDirOption(parent, support, SideType.Right),
            _ => null,
        };

        private ModePropertyPanel GetModeButtons(UIComponent parent)
        {
            if (SupportMode != SupportOption.None)
            {
                var modeProperty = ComponentPool.Get<ModePropertyPanel>(parent, nameof(Data.Mode));
                modeProperty.Label = Localize.Option_Mode;
                modeProperty.SetStyle(UIStyle.Default);
                modeProperty.Init(m => (m & SupportModes) != 0);
                modeProperty.SelectedObject = Data.Mode;
                modeProperty.OnSelectObjectChanged += (Mode value) =>
                {
                    Data.Mode = value;
                    Data.UpdateNode();
                    SingletonTool<NodeControllerTool>.Instance.Panel.RefreshPanel();
                };

                return modeProperty;
            }
            else
                return null;
        }
        private BoolListPropertyPanel GetMainRoadButtons(UIComponent parent)
        {
            if (Data.IsJunction && !Data.IsDecoration)
            {
                var mainRoadProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Data.MainRoad));
                mainRoadProperty.Label = Localize.Option_MainSlopeDirection;
                mainRoadProperty.Init(Localize.Option_MainSlopeDirectionManually, Localize.Option_MainSlopeDirectionAuto);
                mainRoadProperty.SetStyle(UIStyle.Default);
                mainRoadProperty.SelectedObject = Data.MainRoad.Auto;
                mainRoadProperty.OnSelectObjectChanged += (value) =>
                {
                    Data.MainRoad.Auto = value;
                    Data.UpdateNode();
                    SingletonTool<NodeControllerTool>.Instance.Panel.RefreshPanel();
                };

                return mainRoadProperty;
            }
            else
                return null;
        }
        private FloatOptionPanel GetOffsetOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportOffset != SupportOption.None && Data.SegmentEndDatas.Any(s => AllowOffsetPredicate(s)))
            {
                var offset = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.Offset));
                offset.Label = Localize.Option_Offset;
                offset.Format = Localize.Option_OffsetFormat;
                offset.NumberFormat = "0.##";
                offset.Init(Data, SupportOffset, totalSupport, OffsetGetter, OffsetSetter, MinMaxOffset, HasNodePredicate);
                offset.SetStyle(UIStyle.Default);

                return offset;
            }
            else
                return null;
        }
        private FloatOptionPanel GetShiftOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportShift != SupportOption.None && Data.SegmentEndDatas.Any(s => AllowShiftPredicate(s)))
            {
                var shift = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.Shift));
                shift.Label = Localize.Option_Shift;
                shift.Format = Localize.Option_ShiftFormat;
                shift.NumberFormat = "0.##";
                shift.Init(Data, SupportShift, totalSupport, ShiftGetter, ShiftSetter, MinMaxShift, AllowShiftPredicate);
                shift.SetStyle(UIStyle.Default);

                return shift;
            }
            else
                return null;
        }
        private FloatOptionPanel GetRotateOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportRotate != SupportOption.None && Data.SegmentEndDatas.Any(s => AllowRotatePredicate(s)))
            {
                var rotate = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.RotateAngle));
                rotate.Label = Localize.Option_Rotate;
                rotate.Format = Localize.Option_RotateFormat;
                rotate.NumberFormat = "0.#";
                rotate.Init(Data, SupportRotate, totalSupport, RotateGetter, RotateSetter, MinMaxRotate, HasNodePredicate);
                rotate.SetStyle(UIStyle.Default);

                return rotate;
            }
            else
                return null;
        }
        private FloatOptionPanel GetStretchOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportStretch != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var stretch = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.Stretch));
                stretch.Label = Localize.Option_Stretch;
                stretch.Format = Localize.Option_StretchFormat;
                stretch.NumberFormat = "0.#";
                stretch.Init(Data, SupportStretch, totalSupport, StretchGetter, StretchSetter, MinMaxStretch, TouchablePredicate);
                stretch.SetStyle(UIStyle.Default);

                return stretch;
            }
            else
                return null;
        }
        private FloatOptionPanel GetSlopeOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportSlope != SupportOption.None && OnlyOnSlope)
            {
                var slope = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.SlopeAngle));
                slope.Label = Localize.Option_Slope;
                slope.Format = Localize.Option_SlopeFormat;
                slope.NumberFormat = "0.#";
                slope.Init(Data, SupportSlope, totalSupport, SlopeGetter, SlopeSetter, MinMaxSlope, FollowSlopePredicate);
                slope.SetStyle(UIStyle.Default);

                return slope;
            }
            else
                return null;
        }
        private FloatOptionPanel GetTwistOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportTwist != SupportOption.None && OnlyOnSlope)
            {
                var twist = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.TwistAngle));
                twist.Label = Localize.Option_Twist;
                twist.Format = Localize.Option_TwistFormat;
                twist.NumberFormat = "0.#";
                twist.Init(Data, SupportTwist, totalSupport, TwistGetter, TwistSetter, MinMaxTwist, FollowSlopePredicate);
                twist.SetStyle(UIStyle.Default);

                return twist;
            }
            else
                return null;
        }
        private EditorPropertyPanel GetMarkingsOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportMarking != SupportOption.None && Data.SegmentEndDatas.Any(s => IsRoadPredicate(s)))
            {
                if (HideCrosswalksEnable)
                {
                    var hideMarking = ComponentPool.Get<BoolOptionPanel>(parent, nameof(Data.NoMarkings));
                    hideMarking.Label = Localize.Option_Marking;
                    hideMarking.Init(Data, SupportMarking, totalSupport, MarkingsGetter, MarkingsSetter, IsRoadPredicate);
                    hideMarking.SetStyle(UIStyle.Default);

                    return hideMarking;
                }
                else
                {
                    var hideMarking = ComponentPool.Get<ButtonPropertyPanel>(parent, nameof(Data.NoMarkings));
                    hideMarking.Label = Localize.Option_Marking;
                    hideMarking.ButtonText = Localize.Option_HideCrosswalkModRequired;

                    hideMarking.WordWrap = true;
                    hideMarking.AutoSize = AutoSize.Height;
                    hideMarking.TextAlignment = UIHorizontalAlignment.Center;
                    hideMarking.TextPadding = new RectOffset(10, 10, 6, 3);
                    hideMarking.Init();
                    hideMarking.SetStyle(UIStyle.Default);

                    if (totalSupport == SupportOption.Group)
                        hideMarking.Width = 100f;
                    else
                    {
                        var count = 0;
                        if ((totalSupport & SupportOption.Group) != 0)
                            count += 1;
                        if ((totalSupport & SupportOption.Individually) != 0)
                            count += Data.SegmentCount;

                        hideMarking.Width = count * 50f + (count - 1) * 5f;
                    }

                    hideMarking.OnButtonClick += () => DependencyUtilities.HideCrosswalksId.GetWorkshopUrl().OpenUrl();
                    return hideMarking;
                }
            }
            else
                return null;
        }

        private BoolOptionPanel GetCollisionOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportCollision != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var collision = ComponentPool.Get<BoolOptionPanel>(parent, nameof(Data.Collision));
                collision.Label = Localize.Option_Collision;
                collision.Init(Data, SupportCollision, totalSupport, CollisionGetter, CollisionSetter, TouchablePredicate);
                collision.SetStyle(UIStyle.Default);

                return collision;
            }
            else
                return null;
        }
        private BoolOptionPanel GetForceNodeLessOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportForceNodeless != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var forceNodeLess = ComponentPool.Get<BoolOptionPanel>(parent, nameof(Data.ForceNodeLess));
                forceNodeLess.Label = Localize.Option_NodeLess;
                forceNodeLess.Init(Data, SupportForceNodeless, totalSupport, ForceNodeLessGetter, ForceNodeLessSetter, AllowNodeLessPredicate);
                forceNodeLess.SetStyle(UIStyle.Default);

                return forceNodeLess;
            }
            else
                return null;
        }
        private BoolOptionPanel GetFollowSlopeOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportFollowMainSlope != SupportOption.None && Data.SegmentEndDatas.Any(s => NotMainRoadPredicate(s)))
            {
                var followSlope = ComponentPool.Get<BoolOptionPanel>(parent, nameof(Data.FollowSlope));
                followSlope.Label = Localize.Option_FollowSlope;
                followSlope.Init(Data, SupportFollowMainSlope, totalSupport, FollowSlopeGetter, FollowSlopeSetter, NotMainRoadPredicate);
                followSlope.SetStyle(UIStyle.Default);

                return followSlope;
            }
            else
                return null;
        }
        private FloatOptionPanel GetDeltaHeightOption(UIComponent parent, SupportOption totalSupport)
        {
            if (SupportDeltaHeight != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var offset = ComponentPool.Get<FloatOptionPanel>(parent, nameof(Data.DeltaHeight));
                offset.Label = Localize.Option_DeltaHeight;
                offset.Format = Localize.Option_OffsetFormat;
                offset.NumberFormat = "0.##";
                offset.Init(Data, SupportDeltaHeight, totalSupport, DeltaHeightGetter, DeltaHeightSetter, MinMaxDeltaHeight, FollowSlopePredicate);
                offset.SetStyle(UIStyle.Default);

                return offset;
            }
            else
                return null;
        }
        private DeltaOptionPanel GetCornerPosOption(UIComponent parent, SupportOption totalSupport, SideType side)
        {
            if (SupportCornerDelta != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var cornerPos = ComponentPool.Get<DeltaOptionPanel>(parent);
                cornerPos.Label = $"{(side == SideType.Left ? Localize.Option_LeftCorner : Localize.Option_RightCorner)}\n{Localize.Option_Position}";
                cornerPos.XTitle = Localize.Option_Vertical;
                cornerPos.YTitle = Localize.Option_Horizontal;
                cornerPos.ZTitle = Localize.Option_Elevation;
                cornerPos.Format = new string[] { Localize.Option_OffsetFormat, Localize.Option_OffsetFormat, Localize.Option_OffsetFormat };
                cornerPos.NumberFormat = new string[] { "0.#", "0.#", "0.#" };
                cornerPos.Init(Data, SupportCornerDelta, totalSupport, side == SideType.Left ? LeftCornerPosGetter : RightCornerPosGetter, side == SideType.Left ? LeftCornerPosSetter : RightCornerPosSetter, MinMaxPosDelta, TouchablePredicate);
                cornerPos.SetStyle(UIStyle.Default);

                return cornerPos;
            }
            else
                return null;
        }
        private DeltaOptionPanel GetCornerDirOption(UIComponent parent, SupportOption totalSupport, SideType side)
        {
            if (SupportCornerDelta != SupportOption.None && Data.SegmentEndDatas.Any(s => TouchablePredicate(s)))
            {
                var cornerDir = ComponentPool.Get<DeltaOptionPanel>(parent);
                cornerDir.Label = $"{(side == SideType.Left ? Localize.Option_LeftCorner : Localize.Option_RightCorner)}\n{Localize.Option_Direction}";
                cornerDir.XTitle = Localize.Option_Rotate;
                cornerDir.YTitle = Localize.Option_Slope;
                cornerDir.ZTitle = Localize.Option_Distance;
                cornerDir.Format = new string[] { Localize.Option_RotateFormat, Localize.Option_RotateFormat, Localize.Option_OffsetFormat };
                cornerDir.NumberFormat = new string[] { "0.#", "0.#", "0.#" };
                cornerDir.WheelStep = new Vector3(10f, 10f, 1f);
                cornerDir.Init(Data, SupportCornerDelta, totalSupport, side == SideType.Left ? LeftCornerDirGetter : RightCornerDirGetter, side == SideType.Left ? LeftCornerDirSetter : RightCornerDirSetter, MinMaxDirDelta, TouchablePredicate);
                cornerDir.SetStyle(UIStyle.Default);

                return cornerDir;
            }
            else
                return null;
        }

        private void MinMaxOffset(INetworkData data, out float min, out float max)
        {
            if (data is SegmentEndData segmentEnd)
            {
                min = segmentEnd.MinOffset;
                max = segmentEnd.MaxOffset;
            }
            else if (data is NodeData nodeData)
            {
                min = nodeData.SegmentEndDatas.Min(s => s.MinOffset);
                max = nodeData.SegmentEndDatas.Max(s => s.MaxOffset);
            }
            else
            {
                min = MinOffset;
                max = MaxOffset;
            }
        }
        private void MinMaxShift(INetworkData data, out float min, out float max)
        {
            min = MinShift;
            max = MaxShift;
        }
        private void MinMaxRotate(INetworkData data, out float min, out float max)
        {
            if (data is SegmentEndData segmentEnd)
            {
                if (segmentEnd.Mode != Mode.FreeForm)
                {
                    min = segmentEnd.MinRotateAngle;
                    max = segmentEnd.MaxRotateAngle;
                }
                else
                {
                    min = -180f;
                    max = 180f;
                }
            }
            else if (data is NodeData nodeData)
            {
                if (nodeData.Mode != Mode.FreeForm)
                {
                    min = nodeData.SegmentEndDatas.Min(s => s.MinRotateAngle);
                    max = nodeData.SegmentEndDatas.Max(s => s.MaxRotateAngle);
                }
                else
                {
                    min = -180f;
                    max = 180f;
                }
            }
            else
            {
                min = MinRotate;
                max = MaxRotate;
            }
        }
        private bool IsNotDecoration(INetworkData data) => (data is SegmentEndData endData && !IsDecorationPredicate(endData)) || (data is NodeData nodeData && !nodeData.SegmentEndDatas.Any(IsDecorationPredicate));
        private void MinMaxStretch(INetworkData data, out float min, out float max)
        {
            min = MinStretch;

            if (!IsNotDecoration(data))
                max = MaxStretch;
            else if (data is SegmentEndData endData)
                max = Math.Max(4000f / endData.Id.GetSegment().Info.m_halfWidth, MaxRoadStretch);
            else
                max = MaxRoadStretch;
        }
        private void MinMaxSlope(INetworkData data, out float min, out float max)
        {
            if (IsNotDecoration(data))
            {
                min = MinRoadSlope;
                max = MaxRoadSlope;
            }
            else
            {
                min = MinSlope;
                max = MaxSlope;
            }
        }
        private void MinMaxTwist(INetworkData data, out float min, out float max)
        {
            if (data.Mode == Mode.FreeForm)
            {
                min = -180f;
                max = 180f;
            }
            else if (IsNotDecoration(data))
            {
                min = MinRoadTwist;
                max = MaxRoadTwist;
            }
            else
            {
                min = MinTwist;
                max = MaxTwist;
            }
        }
        private void MinMaxDeltaHeight(INetworkData data, out float min, out float max)
        {
            min = MinDeltaHeight;
            max = MaxDeltaHeight;
        }
        private void MinMaxPosDelta(INetworkData data, out Vector3 min, out Vector3 max)
        {
            min = MinPosDelta;
            max = MaxPosDelta;
        }
        private void MinMaxDirDelta(INetworkData data, out Vector3 min, out Vector3 max)
        {
            min = MinDirDelta;
            max = MaxDirDelta;
        }

        private static void OnValueSet(INetworkData data)
        {
            if(data is NodeData nodeData)
            {
                nodeData.UpdateNode();
                SingletonTool<NodeControllerTool>.Instance.Panel.RefreshPanel();
            }
            else if(data is SegmentEndData segmentData)
            {
                segmentData.UpdateNode();
                SingletonTool<NodeControllerTool>.Instance.Panel.RefreshPanel();
            }
        }
        private static void OffsetSetter(INetworkData data, float value)
        {
            data.Offset = value;
            OnValueSet(data);
        }
        private static void ShiftSetter(INetworkData data, float value)
        {
            data.Shift = value;
            OnValueSet(data);
        }
        private static void RotateSetter(INetworkData data, float value)
        {
            data.RotateAngle = value;
            OnValueSet(data);
        }
        private static void SlopeSetter(INetworkData data, float value)
        {
            data.SlopeAngle = value;
            OnValueSet(data);
        }
        private static void TwistSetter(INetworkData data, float value)
        {
            data.TwistAngle = value;
            OnValueSet(data);
        }
        private static void StretchSetter(INetworkData data, float value)
        {
            data.StretchPercent = value;
            OnValueSet(data);
        }
        private static void MarkingsSetter(INetworkData data, bool? value)
        {
            data.NoMarkings = value == null ? null : !value.Value;
            OnValueSet(data);
        }
        private static void CollisionSetter(INetworkData data, bool? value)
        {
            data.Collision = value;
            OnValueSet(data);
        }
        private static void ForceNodeLessSetter(INetworkData data, bool? value)
        {
            data.ForceNodeLess = value;
            OnValueSet(data);
        }
        private static void FollowSlopeSetter(INetworkData data, bool? value)
        {
            data.FollowSlope = value;
            OnValueSet(data);
        }
        private static void DeltaHeightSetter(INetworkData data, float value)
        {
            data.DeltaHeight = value;
            OnValueSet(data);
        }
        private static void LeftCornerPosSetter(INetworkData data, Vector3 value)
        {
            data.LeftPosDelta = InvertPosCoord(value);
            OnValueSet(data);
        }
        private static void RightCornerPosSetter(INetworkData data, Vector3 value)
        {
            data.RightPosDelta = InvertPosCoord(value);
            OnValueSet(data);
        }
        private static void LeftCornerDirSetter(INetworkData data, Vector3 value)
        {
            data.LeftDirDelta = value;
            OnValueSet(data);
        }
        private static void RightCornerDirSetter(INetworkData data, Vector3 value)
        {
            data.RightDirDelta = value;
            OnValueSet(data);
        }

        private static float OffsetGetter(INetworkData data) => data.Offset;
        private static float ShiftGetter(INetworkData data) => data.Shift;
        private static float RotateGetter(INetworkData data) => data.RotateAngle;
        private static float SlopeGetter(INetworkData data) => data.SlopeAngle;
        private static float TwistGetter(INetworkData data) => data.TwistAngle;
        private static float StretchGetter(INetworkData data) => data.StretchPercent;
        private static bool? MarkingsGetter(INetworkData data)
        {
            var value = data.NoMarkings;
            return value == null ? null : !value.Value;
        }
        private static bool? CollisionGetter(INetworkData data) => data.Collision;
        private static bool? ForceNodeLessGetter(INetworkData data) => data.ForceNodeLess;
        private static bool? FollowSlopeGetter(INetworkData data) => data.FollowSlope;
        private static float DeltaHeightGetter(INetworkData data) => data.DeltaHeight;
        private static Vector3 LeftCornerPosGetter(INetworkData data) => InvertPosCoord(data.LeftPosDelta);
        private static Vector3 RightCornerPosGetter(INetworkData data) => InvertPosCoord(data.RightPosDelta);
        private static Vector3 LeftCornerDirGetter(INetworkData data) => data.LeftDirDelta;
        private static Vector3 RightCornerDirGetter(INetworkData data) => data.RightDirDelta;
        private static Vector3 InvertPosCoord(Vector3 value) => new Vector3(value.x, value.z, value.y);

        protected static bool HasNodePredicate(SegmentEndData data) => TouchablePredicate(data) && !data.IsNodeLess;
        protected static bool TouchablePredicate(SegmentEndData data) => !data.IsUntouchable;
        protected static bool IsRoadPredicate(SegmentEndData data) => data.IsRoad;
        protected static bool IsTrackPredicate(SegmentEndData data) => data.IsTrack;
        protected static bool IsPathPredicate(SegmentEndData data) => data.IsPath;
        protected static bool IsDecorationPredicate(SegmentEndData data) => data.IsDecoration;

        protected static bool AllowOffsetPredicate(SegmentEndData data) => TouchablePredicate(data);
        protected static bool AllowShiftPredicate(SegmentEndData data) => TouchablePredicate(data) && !IsDecorationPredicate(data);
        protected static bool AllowRotatePredicate(SegmentEndData data) => TouchablePredicate(data);
        protected static bool MainRoadPredicate(SegmentEndData data) => TouchablePredicate(data) && (data.IsMainRoad || data.IsDecoration);
        protected static bool NotMainRoadPredicate(SegmentEndData data) => TouchablePredicate(data) && (!data.IsMainRoad && !data.IsDecoration);
        protected static bool AllowNodeLessPredicate(SegmentEndData data) => TouchablePredicate(data) && !data.IsNodeLessByDefault;
        protected static bool FollowSlopePredicate(SegmentEndData data) => TouchablePredicate(data) && (data.IsMainRoad || data.FollowSlope == false || data.Mode == Mode.FreeForm);

        #endregion
    }

    [Flags]
    public enum SupportOption
    {
        None = 0,
        Individually = 1,
        Group = 2,
        All = Individually | Group,
    }

    [Flags]
    public enum Options
    {
        [NotVisible]
        [Description(nameof(Localize.Option_MainSlopeDirection))]
        [Order(-1)]
        MainRoad = 1 << 0,

        [NotVisible]
        [Description(nameof(Localize.Option_Offset))]
        [Order(0)]
        Offset = 1 << 1,

        [NotVisible]
        [Description(nameof(Localize.Option_Rotate))]
        [Order(1)]
        Rotate = 1 << 2,

        [NotVisible]
        [Description(nameof(Localize.Option_Shift))]
        [Order(2)]
        Shift = 1 << 3,

        [Description(nameof(Localize.Option_Slope))]
        [Order(3)]
        Slope = 1 << 4,

        [Description(nameof(Localize.Option_Twist))]
        [Order(4)]
        Twist = 1 << 5,

        [Description(nameof(Localize.Option_Stretch))]
        [Order(5)]
        Stretch = 1 << 6,

        [Description(nameof(Localize.Option_Marking))]
        [Order(6)]
        Marking = 1 << 7,

        [Description(nameof(Localize.Option_Collision))]
        [Order(10)]
        Collision = 1 << 8,

        [Description(nameof(Localize.Option_NodeLess))]
        [Order(11)]
        Nodeless = 1 << 9,

        [Description(nameof(Localize.Option_FollowSlope))]
        [Order(8)]
        FollowSlope = 1 << 10,

        [Description(nameof(Localize.Option_DeltaHeight))]
        [Order(9)]
        DeltaHeight = 1 << 11,

        [NotVisible]
        [Order(0)]
        LeftCorner = 1 << 12,

        [Description(nameof(Localize.Option_LeftCornerPosition))]
        [Order(12)]
        LeftCornerPos = 1 << 13,

        [Description(nameof(Localize.Option_LeftCornerDirection))]
        [Order(13)]
        LeftCornerDir = 1 << 14,

        [NotVisible]
        [Order(0)]
        RightCorner = 1 << 15,

        [Description(nameof(Localize.Option_RightCornerPosition))]
        [Order(14)]
        RightCornerPos = 1 << 16,

        [Description(nameof(Localize.Option_RightCornerDirection))]
        [Order(15)]
        RightCornerDir = 1 << 17,
    }

    public enum OptionVisibility
    {
        [Description(nameof(Localize.Settings_Option_Visible))]
        Visible,

        [Description(nameof(Localize.Settings_Option_Hidden))]
        Hidden,

        [Description(nameof(Localize.Settings_Option_Disabled))]
        Disabled,
    }
}
