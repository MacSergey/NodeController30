using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace NodeController
{
    public enum NodeStyleType
    {
        [Description(nameof(Localize.NodeStyle_Middle))]
        Middle,

        [Description(nameof(Localize.NodeStyle_Bend))]
        Bend,

        [Description(nameof(Localize.NodeStyle_Stretch))]
        Stretch,

        [Description(nameof(Localize.NodeStyle_Crossing))]
        Crossing,

        [Description(nameof(Localize.NodeStyle_UTurn))]
        UTurn,

        [Description(nameof(Localize.NodeStyle_Custom))]
        Custom,

        [Description(nameof(Localize.NodeStyle_End))]
        End,
    }
    public static class NodeStyleTypeExtension
    {
        public static NodeStyle GetStyle(this NodeStyleType type, NodeData data) => type switch
        {
            NodeStyleType.Middle => new MiddleNode(data),
            NodeStyleType.Bend => new BendNode(data),
            NodeStyleType.Stretch => new StretchNode(data),
            NodeStyleType.Crossing => new CrossingNode(data),
            NodeStyleType.UTurn => new UTurnNode(data),
            NodeStyleType.Custom => new CustomNode(data),
            NodeStyleType.End => new EndNode(data),
            _ => throw new NotImplementedException(),
        };
    }
    public abstract class NodeStyle
    {
        public abstract NodeStyleType Type { get; }

        public static float MaxShift => 32f;
        public static float MinShift => -32f;
        public static float MaxSlope => 60f;
        public static float MinSlope => -60f;
        public static float MaxTwist => 60f;
        public static float MinTwist => -60f;
        public static float MaxStretch => 500f;
        public static float MinStretch => 1f;
        public static float MaxOffset => 1000f;
        public static float MinOffset => 0f;

        public virtual float AdditionalOffset => 0f;

        public virtual SupportOption SupportOffset => SupportOption.None;
        public virtual SupportOption SupportShift => SupportOption.None;
        public virtual SupportOption SupportRotate => SupportOption.None;
        public virtual SupportOption SupportSlope => SupportOption.None;
        public virtual SupportOption SupportTwist => SupportOption.None;
        public virtual SupportOption SupportNoMarking => SupportOption.None;
        public virtual SupportOption SupportSlopeJunction => SupportOption.None;
        public virtual SupportOption SupportStretch => SupportOption.None;
        public SupportOption TotalSupport => (SupportOffset | SupportShift | SupportRotate | SupportSlope | SupportTwist | SupportStretch) & SupportOption.All;

        public virtual float DefaultOffset => 0f;
        public virtual float DefaultShift => 0f;
        public virtual float DefaultRotate => 0f;
        public virtual float DefaultSlope => 0f;
        public virtual float DefaultTwist => 0f;
        public virtual bool DefaultNoMarking => false;
        public virtual bool DefaultSlopeJunction => false;
        public virtual float DefaultStretch => 1f;

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

                else if (Mathf.Abs(Data.Stretch - DefaultStretch) > 0.1f)
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

        public virtual float GetShift()
        {
            if (Data.IsTwoRoads)
                return (Data.FirstMainSegmentEnd.Shift - Data.SecondMainSegmentEnd.Shift) / 2f;
            else
                return Data.SegmentEndDatas.Average(s => s.Shift);
        }
        public virtual void SetShift(float value)
        {
            if (Data.IsTwoRoads)
            {
                Data.FirstMainSegmentEnd.Shift = value;
                Data.SecondMainSegmentEnd.Shift = -value;
            }
            else
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                    segmentData.Shift = value;
            }
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

        public virtual float GetStretch() => Data.SegmentEndDatas.Average(s => s.Stretch);
        public virtual void SetStretch(float value)
        {
            foreach (var segmentData in Data.SegmentEndDatas)
                segmentData.Stretch = value;
        }

        public virtual bool GetNoMarkings() => Data.SegmentEndDatas.All(s => s.NoMarkings);
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

        public List<EditorItem> GetUIComponents(UIComponent parent)
        {
            var components = new List<EditorItem>();

            if (SupportSlopeJunction > SupportOption.OnceValue)
                components.Add(GetJunctionButtons(parent));

            var totalSupport = TotalSupport;

            if (totalSupport == SupportOption.All)
            {
                var space = ComponentPool.Get<SpacePanel>(parent);
                space.Init(20f);
                components.Add(space);

                var titles = ComponentPool.Get<TextOptionPanel>(parent);
                titles.Init(Data, SupportOption.All, SupportOption.All);
                components.Add(titles);
            }

            if (SupportOffset > SupportOption.OnceValue)
            {
                var offset = ComponentPool.Get<FloatOptionPanel>(parent);
                offset.Text = Localize.Option_Offset;
                offset.Format = Localize.Option_OffsetFormat;
                offset.NumberFormat = "0.##";
                offset.Init(Data, SupportOffset, totalSupport, (data) => data.Offset, (data, value) => data.Offset = value, MinMaxOffset);
                components.Add(offset);
            }

            if (SupportShift > SupportOption.OnceValue)
            {
                var shift = ComponentPool.Get<FloatOptionPanel>(parent);
                shift.Text = Localize.Option_Shift;
                shift.Format = Localize.Option_ShiftFormat;
                shift.NumberFormat = "0.##";
                shift.Init(Data, SupportShift, totalSupport, (data) => data.Shift, (data, value) => data.Shift = value, MinMaxShift);
                components.Add(shift);
            }

            if (SupportRotate > SupportOption.OnceValue)
            {
                var rotate = ComponentPool.Get<FloatOptionPanel>(parent);
                rotate.Text = Localize.Option_Rotate;
                rotate.Format = Localize.Option_RotateFormat;
                rotate.NumberFormat = "0.#";
                rotate.Init(Data, SupportRotate, totalSupport, (data) => data.RotateAngle, (data, value) => data.RotateAngle = value, MinMaxRotate);
                components.Add(rotate);
            }

            if (SupportStretch > SupportOption.OnceValue)
            {
                var stretch = ComponentPool.Get<FloatOptionPanel>(parent);
                stretch.Text = Localize.Option_Stretch;
                stretch.Format = Localize.Option_StretchFormat;
                stretch.NumberFormat = "0.#";
                stretch.Init(Data, SupportStretch, totalSupport, (data) => data.StretchPercent, (data, value) => data.StretchPercent = value, MinMaxStretch);
                components.Add(stretch);
            }

            if (SupportSlope > SupportOption.OnceValue)
            {
                var slope = ComponentPool.Get<FloatOptionPanel>(parent);
                slope.Text = Localize.Option_Slope;
                slope.Format = Localize.Option_SlopeFormat;
                slope.NumberFormat = "0.#";
                slope.Init(Data, SupportSlope, totalSupport, (data) => data.SlopeAngle, (data, value) => data.SlopeAngle = value, MinMaxSlope);
                components.Add(slope);
            }

            if (SupportTwist > SupportOption.OnceValue)
            {
                var twist = ComponentPool.Get<FloatOptionPanel>(parent);
                twist.Text = Localize.Option_Twist;
                twist.Format = Localize.Option_TwistFormat;
                twist.NumberFormat = "0.#";
                twist.Init(Data, SupportTwist, totalSupport, (data) => data.TwistAngle, (data, value) => data.TwistAngle = value, MinMaxTwist);
                components.Add(twist);
            }

            if(SupportNoMarking > SupportOption.OnceValue)
            {
                var hideMarking = ComponentPool.Get<BoolOptionPanel>(parent);
                hideMarking.Text = Localize.Option_Marking;
                hideMarking.Init(Data, SupportNoMarking, totalSupport, (data) => !data.NoMarkings, (data, value) => data.NoMarkings = !value);
                components.Add(hideMarking);
            }

            return components;
        }

        protected BoolListPropertyPanel GetJunctionButtons(UIComponent parent)
        {
            var flatJunctionProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            flatJunctionProperty.Text = Localize.Option_Style;
            flatJunctionProperty.Init(Localize.Option_StyleFlat, Localize.Option_StyleSlope, false);
            flatJunctionProperty.SelectedObject = Data.IsSlopeJunctions;
            flatJunctionProperty.OnSelectObjectChanged += (value) => Data.IsSlopeJunctions = value;

            return flatJunctionProperty;
        }

        private void MinMaxOffset(INetworkData data, out float min, out float max)
        {
            if (data is SegmentEndData segmentEnd)
            {
                min = segmentEnd.MinOffset;
                max = segmentEnd.MaxOffset;
            }
            else
            {
                min = 0f;
                max = 1000f;
            }
        }
        private void MinMaxShift(INetworkData data, out float min, out float max)
        {
            min = -32f;
            max = 32f;
        }
        private void MinMaxRotate(INetworkData data, out float min, out float max)
        {
            if (data is SegmentEndData segmentEnd)
            {
                min = segmentEnd.MinRotate;
                max = segmentEnd.MaxRotate;
            }
            else
            {
                min = -80f;
                max = 80f;
            }
        }
        private void MinMaxStretch(INetworkData data, out float min, out float max)
        {
            min = 1f;
            max = 500f;
        }
        private void MinMaxSlope(INetworkData data, out float min, out float max)
        {
            min = -60f;
            max = 60;
        }
        private void MinMaxTwist(INetworkData data, out float min, out float max)
        {
            min = -60f;
            max = 60;
        }

        #endregion
    }

    public class MiddleNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Middle;
        public override SupportOption SupportSlope => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportSlopeJunction => SupportOption.OnceValue;

        public override bool DefaultSlopeJunction => true;

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

        public override float GetStretch() => (Data.FirstMainSegmentEnd.Stretch + Data.SecondMainSegmentEnd.Stretch) / 2;
        public override void SetStretch(float value)
        {
            Data.FirstMainSegmentEnd.Stretch = value;
            Data.SecondMainSegmentEnd.Stretch = value;
        }
    }
    public class BendNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Bend;

        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportTwist => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public BendNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class StretchNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Stretch;

        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportTwist => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public StretchNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class CrossingNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Crossing;
        public override float DefaultOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.OnceValue;
        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public CrossingNode(NodeData data) : base(data) { }
    }
    public class UTurnNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.UTurn;
        public override float DefaultOffset => 8f;

        public override SupportOption SupportOffset => SupportOption.OnceValue;
        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public UTurnNode(NodeData data) : base(data) { }
    }
    public class EndNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.End;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportSlope => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportNoMarking => SupportOption.Group;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;

        public EndNode(NodeData data) : base(data) { }
    }
    public class CustomNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Custom;

        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportNoMarking => SupportOption.All;
        public override SupportOption SupportSlopeJunction => SupportOption.Group;
        public override bool IsMoveable => true;

        public CustomNode(NodeData data) : base(data) { }
    }

    [Flags]
    public enum SupportOption
    {
        None = 0,
        OnceValue = 1,
        Individually = 2,
        Group = 4,
        All = Individually | Group,
    }
}
