using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController
{
    public enum NodeStyleType
    {
        Middle,
        Bend,
        Stretch,
        Crossing,
        UTurn,
        Custom,
        End,
    }
    public abstract class NodeStyle
    {
        public static float DefaultShift => 0f;
        public static float DefaultRotate => 0f;
        public static float DefaultSlope => 0f;
        public static float DefaultTwist => 0f;
        public static bool DefaultNoMarking => false;
        public static bool DefaultSlopeJunction => false;


        public abstract NodeStyleType Type { get; }

        public virtual float MinOffset => 0f;
        public virtual float MaxOffset => 0f;
        public virtual float AdditionalOffset => 0f;

        public virtual SupportOption SupportOffset => SupportOption.None;
        public virtual SupportOption SupportShift => SupportOption.None;
        public virtual SupportOption SupportRotate => SupportOption.None;
        public virtual SupportOption SupportSlope => SupportOption.None;
        public virtual SupportOption SupportTwist => SupportOption.None;
        public virtual SupportOption SupportNoMarking => SupportOption.None;
        public virtual SupportOption SupportSlopeJunction => SupportOption.None;


        public virtual bool IsMoveable => false;

        public bool IsDefault
        {
            get
            {
                if (Mathf.Abs(Data.Shift - DefaultShift) > 0.001f)
                    return false;

                else if (Mathf.Abs(Data.RotateAngle - DefaultRotate) > 0.1f)
                    return false;

                else if (Mathf.Abs(Data.SlopeAngle - DefaultSlope) > 0.1f)
                    return false;

                else if (Mathf.Abs(Data.TwistAngle - DefaultTwist) > 0.1f)
                    return false;

                else if (Data.NoMarkings != DefaultNoMarking)
                    return false;

                else if (Data.IsSlopeJunctions != DefaultSlopeJunction)
                    return false;

                else
                    return true;
            }
        }

        public NodeData Data { get; }

        public NodeStyle(NodeData data)
        {
            Data = data;
        }

        public virtual float GetOffset() => Data.SegmentEndDatas.Average(s => s.Offset);
        public virtual void SetOffset(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.Offset = value;
        }

        public virtual float GetShift() => Data.SegmentEndDatas.Average(s => s.Shift);
        public virtual void SetShift(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.Shift = value;
        }

        public virtual float GetRotate() => Data.SegmentEndDatas.Average(s => s.RotateAngle);
        public virtual void SetRotate(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.RotateAngle = value;
        }

        public virtual float GetSlope() => Data.SegmentEndDatas.Average(s => s.SlopeAngle);
        public virtual void SetSlope(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.SlopeAngle = value;
        }

        public virtual float GetTwist() => Data.SegmentEndDatas.Average(s => s.TwistAngle);
        public virtual void SetTwist(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.TwistAngle = value;
        }

        public virtual bool GetNoMarkings() => Data.SegmentEndDatas.Any(s => s.NoMarkings);
        public virtual void SetNoMarkings(bool value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.NoMarkings = value;
        }

        public virtual bool GetIsSlopeJunctions() => Data.SegmentEndDatas.Any(s => s.IsSlope);
        public virtual void SetIsSlopeJunctions(bool value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.IsSlope = value;
        }

        #region UICOMPONENTS

        public virtual void GetUIComponents(UIComponent parent, Action refresh)
        {
            if (SupportSlopeJunction != SupportOption.None)
                GetJunctionButtons(parent);

            GetOffsetUIComponents(parent);
            GetShiftUIComponents(parent);
            GetRotateUIComponents(parent);
            GetSlopeUIComponents(parent);
            GetTwistUIComponents(parent);

            if (SupportNoMarking != SupportOption.None)
                GetHideMarkingProperty(parent);

            GetResetButton(parent, refresh);
        }

        protected void GetOffsetUIComponents(UIComponent parent) => GetUIComponents(parent, SupportOffset, GetOffsetProperty, GetSegmentOffsetProperty, (data) => data.Offset, (data, value) => data.Offset = value);
        protected void GetShiftUIComponents(UIComponent parent) => GetUIComponents(parent, SupportShift, GetShiftProperty, GetSegmentShiftProperty, (data) => data.Shift, (data, value) => data.Shift = value);
        protected void GetRotateUIComponents(UIComponent parent) => GetUIComponents(parent, SupportRotate, GetRotateProperty, GetSegmentRotateProperty, (data) => data.RotateAngle, (data, value) => data.RotateAngle = value);
        protected void GetSlopeUIComponents(UIComponent parent) => GetUIComponents(parent, SupportSlope, GetSlopeProperty, null, (data) => data.SlopeAngle, (data, value) => data.SlopeAngle = value);
        protected void GetTwistUIComponents(UIComponent parent) => GetUIComponents(parent, SupportTwist, GetTwistProperty, null, (data) => data.TwistAngle, (data, value) => data.TwistAngle = value);
        protected void GetUIComponents(UIComponent parent, SupportOption option, Func<UIComponent, FloatPropertyPanel> getNodeProperty, Func<UIComponent, SegmentEndData, FloatPropertyPanel> getSegmentProperty, Func<INetworkData, float> getValue, Action<INetworkData, float> setValue)
        {
            var nodeProperty = default(FloatPropertyPanel);
            var segmentProperties = new List<FloatPropertyPanel>();

            if (option.IsSet(SupportOption.Group))
            {
                nodeProperty = getNodeProperty(parent);
                nodeProperty.Value = getValue(Data);
                nodeProperty.OnValueChanged += (float newValue) =>
                {
                    setValue(Data, newValue);
                    foreach (var segmentProperty in segmentProperties)
                        segmentProperty.Value = newValue;
                };
            }

            if (option.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                {
                    var segmentProperty = getSegmentProperty(parent, segmentData);
                    segmentProperty.Value = getValue(segmentData);
                    segmentProperty.OnValueChanged += (newValue) =>
                    {
                        setValue(segmentData, newValue);
                        if (option.IsSet(SupportOption.Group))
                            nodeProperty.Value = getValue(Data);
                        Data.UpdateNode();
                    };
                    segmentProperties.Add(segmentProperty);
                }
            }
        }

        protected FloatPropertyPanel GetOffsetProperty(UIComponent parent)
        {
            var offsetProperty = GetNodeProperty(parent, "Offset");
            offsetProperty.MinValue = 0;
            offsetProperty.MaxValue = 100;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetShiftProperty(UIComponent parent)
        {
            var offsetProperty = GetNodeProperty(parent, "Shift");
            offsetProperty.MinValue = -32;
            offsetProperty.MaxValue = 32;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetRotateProperty(UIComponent parent)
        {
            var rotateProperty = GetNodeProperty(parent, "Rotate");
            rotateProperty.MinValue = SegmentEndData.MinPossibleRotate;
            rotateProperty.MaxValue = SegmentEndData.MaxPossibleRotate;

            return rotateProperty;
        }
        protected FloatPropertyPanel GetSlopeProperty(UIComponent parent)
        {
            var slopeProperty = GetNodeProperty(parent, "Slope");
            slopeProperty.MinValue = -60;
            slopeProperty.MaxValue = 60;

            return slopeProperty;
        }
        protected FloatPropertyPanel GetTwistProperty(UIComponent parent)
        {
            var twistProperty = GetNodeProperty(parent, "Twist");
            twistProperty.MinValue = -60;
            twistProperty.MaxValue = 60;

            return twistProperty;
        }

        private FloatPropertyPanel GetNodeProperty(UIComponent parent, string name)
        {
            var property = ComponentPool.Get<FloatPropertyPanel>(parent, name);
            property.Text = name;
            property.CheckMin = true;
            property.CheckMax = true;
            property.UseWheel = true;
            property.WheelStep = 1f;
            property.Init();

            return property;
        }

        protected FloatPropertyPanel GetSegmentOffsetProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.Id} offset");
            offsetProperty.MinValue = segmentData.MinOffset;
            offsetProperty.MaxValue = segmentData.MaxOffset;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetSegmentShiftProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.Id} shift");
            offsetProperty.MinValue = -32;
            offsetProperty.MaxValue = 32;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetSegmentRotateProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var rotateProperty = GetSegmentProperty(parent, $"Segment #{segmentData.Id} rotate");
            rotateProperty.MinValue = segmentData.MinRotate;
            rotateProperty.MaxValue = segmentData.MaxRotate;

            return rotateProperty;
        }
        protected FloatPropertyPanel GetSegmentSlopeProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var slopeProperty = GetSegmentProperty(parent, $"Segment #{segmentData.Id} slope");
            slopeProperty.MinValue = -60;
            slopeProperty.MaxValue = 60;

            return slopeProperty;
        }
        protected FloatPropertyPanel GetSegmentTwistProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var twistProperty = GetSegmentProperty(parent, $"Segment #{segmentData.Id} twist");
            twistProperty.MinValue = -60;
            twistProperty.MaxValue = 60;

            return twistProperty;
        }
        private FloatPropertyPanel GetSegmentProperty(UIComponent parent, string name)
        {
            var property = ComponentPool.Get<FloatPropertyPanel>(parent, name);
            property.Text = name;
            property.CheckMin = true;
            property.CheckMax = true;
            property.UseWheel = true;
            property.WheelStep = 1f;
            property.Init();

            return property;
        }
        protected BoolListPropertyPanel GetJunctionButtons(UIComponent parent)
        {
            var flatJunctionProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            flatJunctionProperty.Text = "Style";
            flatJunctionProperty.Init("Flat", "Slope", false);
            flatJunctionProperty.SelectedObject = Data.IsSlopeJunctions;
            flatJunctionProperty.OnSelectObjectChanged += (value) => Data.IsSlopeJunctions = value;

            return flatJunctionProperty;
        }
        protected BoolListPropertyPanel GetHideMarkingProperty(UIComponent parent)
        {
            var hideMarkingProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            hideMarkingProperty.Text = "Hide crosswalk marking";
            hideMarkingProperty.Init("No", "Yes");
            hideMarkingProperty.SelectedObject = Data.NoMarkings;
            hideMarkingProperty.OnSelectObjectChanged += (value) => Data.NoMarkings = value;

            return hideMarkingProperty;
        }
        protected ButtonPanel GetResetButton(UIComponent parent, Action refresh)
        {
            var resetButton = ComponentPool.Get<ButtonPanel>(parent);
            resetButton.Text = "Reset to default";
            resetButton.Init();
            resetButton.OnButtonClick += () =>
            {
                Data.ResetToDefault();
                refresh();
            };

            return resetButton;
        }

        #endregion
    }

    public class MiddleNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Middle;
        public override SupportOption SupportSlope => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportShift => SupportOption.Group;

        public MiddleNode(NodeData data) : base(data) { }

        public override float GetSlope() => (Data.FirstMainSegmentEnd.SlopeAngle - Data.SecondMainSegmentEnd.SlopeAngle) / 2;
        public override void SetSlope(float value)
        {
            Data.FirstMainSegmentEnd.SlopeAngle = value;
            Data.SecondMainSegmentEnd.SlopeAngle = -value;
        }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }

        public override float GetShift() => (Data.FirstMainSegmentEnd.Shift - Data.SecondMainSegmentEnd.Shift) / 2;
        public override void SetShift(float value)
        {
            Data.FirstMainSegmentEnd.Shift = value;
            Data.SecondMainSegmentEnd.Shift = -value;
        }
    }
    public class BendNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Bend;
        public override float MaxOffset => 100f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public BendNode(NodeData data) : base(data) { }
    }
    public class StretchNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Stretch;
        public override float MaxOffset => 100f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public StretchNode(NodeData data) : base(data) { }
    }
    public class CrossingNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Crossing;
        public override float MinOffset => 2f;
        public override float MaxOffset => 2f;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public CrossingNode(NodeData data) : base(data) { }

        public override float GetShift() => (Data.FirstMainSegmentEnd.Shift - Data.SecondMainSegmentEnd.Shift) / 2;
        public override void SetShift(float value)
        {
            Data.FirstMainSegmentEnd.Shift = value;
            Data.SecondMainSegmentEnd.Shift = -value;
        }
    }
    public class UTurnNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.UTurn;
        public override float MinOffset => 8f;
        public override float MaxOffset => 8f;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public UTurnNode(NodeData data) : base(data) { }

        public override float GetShift() => (Data.FirstMainSegmentEnd.Shift - Data.SecondMainSegmentEnd.Shift) / 2;
        public override void SetShift(float value)
        {
            Data.FirstMainSegmentEnd.Shift = value;
            Data.SecondMainSegmentEnd.Shift = -value;
        }
    }
    public class EndNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.End;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportSlope => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.Group;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public EndNode(NodeData data) : base(data) { }
    }
    public class CustomNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Custom;
        public override float MaxOffset => 100f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public CustomNode(NodeData data) : base(data) { }
    }

    public enum SupportOption
    {
        None = 0,
        Individually = 1,
        Group = 2,
        All = Individually | Group,
    }
}
