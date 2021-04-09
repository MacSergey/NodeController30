using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController
{
    public enum NodeTypeT
    {
        Middle,
        Bend,
        Stretch,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
        Custom,
        End,
    }
    public abstract class NodeType
    {
        public abstract NodeTypeT Type { get; }

        public NodeData Data { get; }

        public NodeType(NodeData data)
        {
            Data = data;
        }

        public virtual void GetUIComponents(UIComponent parent, Action refresh) { }
        protected virtual void ResetToDefault() { }

        protected void GetOffsetUIComponents(UIComponent parent) => GetUIComponents(parent, GetOffsetProperty, GetSegmentOffsetProperty, (data) => data.Offset, (data, value) => data.Offset = value);
        protected void GetShiftUIComponents(UIComponent parent) => GetUIComponents(parent, GetShiftProperty, GetSegmentShiftProperty, (data) => data.Shift, (data, value) => data.Shift = value);
        protected void GetRotateUIComponents(UIComponent parent) => GetUIComponents(parent, GetRotateProperty, GetSegmentRotateProperty, (data) => data.RotateAngle, (data, value) => data.RotateAngle = value);
        protected void GetSlopeUIComponents(UIComponent parent) => GetUIComponents(parent, GetSlopeProperty, null, (data) => data.SlopeAngle, (data, value) => data.SlopeAngle = value);
        protected void GetTwistUIComponents(UIComponent parent) => GetUIComponents(parent, GetTwistProperty, null, (data) => data.TwistAngle, (data, value) => data.TwistAngle = value);
        protected void GetUIComponents(UIComponent parent, Func<UIComponent, FloatPropertyPanel> getNodeProperty, Func<UIComponent, SegmentEndData, FloatPropertyPanel> getSegmentProperty, Func<INetworkData, float> getValue, Action<INetworkData, float> setValue)
        {
            var nodeProperty = getNodeProperty(parent);
            nodeProperty.Value = getValue(Data);

            var segmentProperties = new List<FloatPropertyPanel>();
            if (getSegmentProperty != null)
            {
                foreach (var segmentId in Data.Node.SegmentIds())
                {
                    var segmentData = SegmentEndManager.Instance[segmentId, Data.NodeId, true];
                    var segmentProperty = getSegmentProperty(parent, segmentData);
                    segmentProperty.Value = getValue(segmentData);
                    segmentProperty.OnValueChanged += (newValue) =>
                    {
                        var segmentData = SegmentEndManager.Instance[segmentId, Data.NodeId, true];
                        setValue(segmentData, newValue);
                        nodeProperty.Value = getValue(Data);
                        Data.Update();
                    };
                    segmentProperties.Add(segmentProperty);
                }
            }

            nodeProperty.OnValueChanged += (float newValue) =>
            {
                setValue(Data, newValue);
                foreach (var segmentProperty in segmentProperties)
                    segmentProperty.Value = newValue;
            };
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
            rotateProperty.MinValue = -60;
            rotateProperty.MaxValue = 60;

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
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} offset");
            offsetProperty.MinValue = 0;
            offsetProperty.MaxValue = 100;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetSegmentShiftProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var offsetProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} shift");
            offsetProperty.MinValue = -32;
            offsetProperty.MaxValue = 32;

            return offsetProperty;
        }
        protected FloatPropertyPanel GetSegmentRotateProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var rotateProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} rotate");
            rotateProperty.MinValue = -60;
            rotateProperty.MaxValue = 60;

            return rotateProperty;
        }
        protected FloatPropertyPanel GetSegmentSlopeProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var slopeProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} slope");
            slopeProperty.MinValue = -60;
            slopeProperty.MaxValue = 60;

            return slopeProperty;
        }
        protected FloatPropertyPanel GetSegmentTwistProperty(UIComponent parent, SegmentEndData segmentData)
        {
            var twistProperty = GetSegmentProperty(parent, $"Segment #{segmentData.SegmentId} twist");
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
        protected ButtonsPanel GetActionButtons(UIComponent parent)
        {
            var actionButtons = ComponentPool.Get<ButtonsPanel>(parent);
            var slopeIndex = actionButtons.AddButton("Make slope");
            var flatIndex = actionButtons.AddButton("Make flat");
            actionButtons.Init();
            actionButtons.OnButtonClick += OnButtonClick;

            return actionButtons;

            void OnButtonClick(int index)
            {
                if (index == slopeIndex)
                    Data.IsFlatJunctions = false;
                else if (index == flatIndex)
                    Data.IsFlatJunctions = true;
            }
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
    }
    public class MiddleNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.Middle;

        public MiddleNode(NodeData data) : base(data) { }



        protected override void ResetToDefault()
        {
            Data.SlopeAngle = 0f;
            Data.TwistAngle = 0f;
        }
        public override void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetSlopeUIComponents(parent);
            GetTwistUIComponents(parent);
            GetResetButton(parent, refresh);
        }
    }
    public class BendNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.Bend;

        public BendNode(NodeData data) : base(data) { }


        public override void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetOffsetUIComponents(parent);
            GetShiftUIComponents(parent);
            GetRotateUIComponents(parent);
            GetResetButton(parent, refresh);
        }
    }
    public class StretchNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.Stretch;

        public StretchNode(NodeData data) : base(data) { }


        public override void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetOffsetUIComponents(parent);
            GetShiftUIComponents(parent);
            GetRotateUIComponents(parent);
            GetResetButton(parent, refresh);
        }
    }
    public class CrossingNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.Crossing;

        public CrossingNode(NodeData data) : base(data) { }


        public override void GetUIComponents(UIComponent parent, Action refresh)
        {

        }
    }
    public class UTurnNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.UTurn;

        public UTurnNode(NodeData data) : base(data) { }


        public override void GetUIComponents(UIComponent parent, Action refresh)
        {

        }
    }
    public class EndNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.End;

        public EndNode(NodeData data) : base(data) { }


        protected override void ResetToDefault()
        {
            base.ResetToDefault();
        }

        public override void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetSlopeUIComponents(parent);
            GetTwistUIComponents(parent);
            GetResetButton(parent, refresh);
        }
    }
    public class CustomNode : NodeType
    {
        public override NodeTypeT Type => NodeTypeT.Custom;

        public CustomNode(NodeData data) : base(data) { }


        protected override void ResetToDefault()
        {
            Data.Offset = 0f;
            Data.Shift = 0f;
            Data.RotateAngle = 0f;
        }

        public override void GetUIComponents(UIComponent parent, Action refresh)
        {
            GetOffsetUIComponents(parent);
            GetShiftUIComponents(parent);
            GetRotateUIComponents(parent);
            GetHideMarkingProperty(parent);
            GetResetButton(parent, refresh);
        }
    }
}
