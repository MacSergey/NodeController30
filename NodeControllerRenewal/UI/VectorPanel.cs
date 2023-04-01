using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController.UI
{
    public abstract class Vector3OptionPanel<TypeNodeItem, TypeSegmentItem, TypeValue> : OptionPanel<TypeNodeItem, TypeSegmentItem, TypeValue>
        where TypeNodeItem : UIComponent, IValueChanger<TypeValue>
        where TypeSegmentItem : UIComponent, IValueChanger<TypeValue>
    {
        public delegate void MinMaxGetter(INetworkData data, out TypeValue min, out TypeValue max);

        protected override float DefaultHeight => 20f * 3f + ItemsPadding * 2f + 4f;
        protected MinMaxGetter MinMax { get; set; }
        public new string[] Format { get; set; }
        public string[] NumberFormat { get; set; }
        public Vector3 WheelStep { get; set; } = Vector3.one;
        public bool[] CyclicalValue { get; set; }

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, Getter getter, Setter setter, MinMaxGetter minMax, EnableGetter enableGetter)
        {
            MinMax = minMax;
            Init(data, option, totalOption, getter, setter, enableGetter);
        }
        public override void DeInit()
        {
            base.DeInit();
            Format = null;
            NumberFormat = null;
            WheelStep = Vector3.one;
            CyclicalValue = null;
        }
    }
    public class DeltaOptionPanel : Vector3OptionPanel<TextStaticPanel, Vector3Panel, Vector3>
    {
        public string XTitle { get; set; } = string.Empty;
        public string YTitle { get; set; } = string.Empty;
        public string ZTitle { get; set; } = string.Empty;

        protected override TextStaticPanel AddNodeItem(NodeData data)
        {
            var item = base.AddNodeItem(data);
            item.width = 75f;
            return item;
        }
        protected override void InitNodeItem(NodeData data, TextStaticPanel item)
        {
            item.SetText(0, XTitle);
            item.SetText(1, YTitle);
            item.SetText(2, ZTitle);
        }
        protected override void InitSegmentItem(SegmentEndData data, Vector3Panel item)
        {
            item.FormatArray = Format;
            item.NumberFormatArray = NumberFormat;
            item.CheckMin = true;
            item.CheckMax = true;
            item.UseWheel = true;
            item.WheelStep = WheelStep;
            item.CyclicalValue = CyclicalValue;
            item.WheelTip = Settings.ShowToolTip;

            if (MinMax != null)
            {
                MinMax(data, out var min, out var max);
                item.MinValue = min;
                item.MaxValue = max;
            }
        }
        public override void DeInit()
        {
            base.DeInit();

            XTitle = string.Empty;
            YTitle = string.Empty;
            ZTitle = string.Empty;
        }

        protected override void FillContent() { }

        public override void SetStyle(ControlStyle style)
        {
            NodeItem.LabelStyle = style.Label;

            foreach (var item in SegmentItems.Values)
                item.TextFieldStyle = style.TextField;
        }
    }

    public abstract class VectorPanel<TypeVector> : CustomUIPanel, IValueChanger<TypeVector>, IReusable
    {
        public event Action<TypeVector> OnValueChanged;

        bool IReusable.InCache { get; set; }
        public abstract uint Dimension { get; }

        protected FloatUITextField[] Fields { get; }


        TypeVector _value;
        public TypeVector Value
        {
            get => _value;
            set
            {
                _value = value;
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].Value = Get(ref _value, i);
            }
        }
        public string Format { set { } }
        public string[] FormatArray
        {
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].Format = value != null && i < value.Length ? value[i] : null;
            }
        }
        public string NumberFormat { set { } }
        public string[] NumberFormatArray
        {
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].NumberFormat = value != null && i < value.Length ? value[i] : null;
            }
        }

        public TypeVector MinValue
        {
            get
            {
                var value = default(TypeVector);
                for (var i = 0; i < Dimension; i += 1)
                    Set(ref value, i, Fields[i].MinValue);
                return value;
            }
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].MinValue = Get(ref value, i);
            }
        }
        public TypeVector MaxValue
        {
            get
            {
                var value = default(TypeVector);
                for (var i = 0; i < Dimension; i += 1)
                    Set(ref value, i, Fields[i].MaxValue);
                return value;
            }
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].MaxValue = Get(ref value, i);
            }
        }
        public bool CheckMin
        {
            get => Fields.All(f => f.CheckMin);
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].CheckMin = value;
            }
        }
        public bool CheckMax
        {
            get => Fields.All(f => f.CheckMax);
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].CheckMax = value;
            }
        }
        public bool[] CyclicalValue
        {
            get => Fields.Select(f => f.CyclicalValue).ToArray();
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].CyclicalValue = value != null && i < value.Length ? value[i] : false;
            }
        }
        public bool UseWheel
        {
            get => Fields.All(f => f.UseWheel);
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].UseWheel = value;
            }
        }
        public TypeVector WheelStep
        {
            get
            {
                var value = default(TypeVector);
                for (var i = 0; i < Dimension; i += 1)
                    Set(ref value, i, Fields[i].WheelStep);
                return value;
            }
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].WheelStep = Get(ref value, i);
            }
        }
        public bool WheelTip
        {
            set
            {
                for (var i = 0; i < Dimension; i += 1)
                    Fields[i].WheelTip = value;
            }
        }

        public TextFieldStyle TextFieldStyle
        {
            set
            {
                foreach (var field in Fields)
                    field.TextFieldStyle = value;
            }
        }

        public VectorPanel()
        {
            Fields = new FloatUITextField[Dimension];
            for (var i = 0; i < Dimension; i += 1)
                Fields[i] = AddField(i);

            AutoChildrenVertically = AutoLayoutChildren.Fit;
            Padding = new RectOffset(0, 0, 0, 2);
            AutoLayout = AutoLayout.Vertical;
        }

        public void DeInit()
        {
            for (var i = 0; i < Dimension; i += 1)
            {
                Fields[i].SetDefault();
            }

            OnValueChanged = null;
        }

        protected FloatUITextField AddField(int index)
        {
            var field = AddUIComponent<FloatUITextField>();
            field.SetDefaultStyle();
            field.UseWheel = true;
            field.WheelStep = 1;
            field.NumberFormat = "0.##";
            field.OnValueChanged += (value) => FieldChanged(index, value);

            return field;
        }

        protected abstract float Get(ref TypeVector vector, int index);
        protected abstract void Set(ref TypeVector vector, int index, float value);
        private void FieldChanged(int index, float value)
        {
            Set(ref _value, index, value);
            OnValueChanged?.Invoke(_value);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (Fields != null)
            {
                foreach (var field in Fields)
                    field.width = width;
            }
        }
    }
    public class Vector3Panel : VectorPanel<Vector3>
    {
        public override uint Dimension => 3;

        protected override float Get(ref Vector3 vector, int index) => vector[index];
        protected override void Set(ref Vector3 vector, int index, float value) => vector[index] = value;
    }

    public abstract class StaticPanel<TypeItem> : CustomUIPanel, IReusable
        where TypeItem : UIComponent
    {
        bool IReusable.InCache { get; set; }
        public abstract uint Dimension { get; }

        protected TypeItem[] Fields { get; }

        public StaticPanel()
        {
            Fields = new TypeItem[Dimension];
            for (var i = 0; i < Dimension; i += 1)
                Fields[i] = AddField(i);

            AutoChildrenVertically = AutoLayoutChildren.Fit;
            Padding = new RectOffset(0, 0, 0, 2);
            AutoLayout = AutoLayout.Vertical;
        }

        public virtual void DeInit() { }

        protected virtual TypeItem AddField(int index)
        {
            var field = AddUIComponent<TypeItem>();
            return field;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (Fields != null)
            {
                foreach (var field in Fields)
                    field.width = width;
            }
        }

        public abstract LabelStyle LabelStyle { set; }
    }
    public class TextStaticPanel : StaticPanel<CustomUILabel>, IValueChanger<Vector3>
    {
        public event Action<Vector3> OnValueChanged;
        public override uint Dimension => 3;

        public Vector3 Value
        {
            get => default;
            set { }
        }
        public string Format { set { } }

        protected override CustomUILabel AddField(int index)
        {
            var field = base.AddField(index);
            field.autoSize = false;
            field.height = 20f;
            field.textScale = 0.7f;
            field.HorizontalAlignment = UIHorizontalAlignment.Right;
            field.Padding = new RectOffset(0, 0, 5, 0);
            return field;
        }
        public override void DeInit()
        {
            for (var i = 0; i < Dimension; i += 1)
            {
                Fields[i].text = string.Empty;
            }
        }

        public void SetText(int index, string text)
        {
            if (index < Fields.Length)
                Fields[index].text = text;
        }
        public override LabelStyle LabelStyle
        {
            set
            {
                foreach (var field in Fields)
                    field.LabelStyle = value;
            }
        }
    }
}
