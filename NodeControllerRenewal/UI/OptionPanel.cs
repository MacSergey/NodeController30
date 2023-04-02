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
    public interface IOptionPanel
    {
        public bool isVisible { get; set; }
        public bool isVisibleSelf { get; }
        void Refresh();
    }
    public abstract class OptionPanel<TypeNodeItem, TypeSegmentItem> : EditorPropertyPanel, IReusable, IOptionPanel
        where TypeNodeItem : UIComponent
        where TypeSegmentItem : UIComponent
    {
        public delegate bool EnableGetter(SegmentEndData data);

        bool IReusable.InCache { get; set; }
        protected NodeData Data { get; set; }
        protected SupportOption Option { get; set; }
        protected SupportOption TotalOption { get; set; }
        protected EnableGetter IsEnableGetter { get; set; }

        protected TypeNodeItem NodeItem { get; set; }
        protected Dictionary<SegmentEndData, TypeSegmentItem> SegmentItems { get; } = new Dictionary<SegmentEndData, TypeSegmentItem>();
        protected float ItemWidth => TotalOption == SupportOption.Group ? 100f : 50f;

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, EnableGetter enableGetter = null)
        {
            Data = data;
            Option = option;
            TotalOption = totalOption;
            IsEnableGetter = enableGetter;

            Content.PauseLayout(PlaceItems);

            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Data = null;
            IsEnableGetter = null;
            NodeItem = null;
            SegmentItems.Clear();

            foreach (var component in Content.components.ToArray())
                ComponentPool.Free(component);
        }
        private void PlaceItems()
        {
            if (TotalOption.IsSet(SupportOption.Group))
                NodeItem = AddNodeItem(Data);

            if (TotalOption.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                    SegmentItems[segmentData] = AddSegmentItem(segmentData);
            }

            Refresh();
        }

        protected virtual TypeNodeItem AddNodeItem(NodeData data)
        {
            var item = Content.AddUIComponent<TypeNodeItem>();
            return item;
        }
        protected virtual TypeSegmentItem AddSegmentItem(SegmentEndData data)
        {
            var item = Content.AddUIComponent<TypeSegmentItem>();
            return item;
        }

        public virtual void Refresh()
        {
            if (NodeItem is TypeNodeItem nodeItem)
                nodeItem.isEnabled = Option.IsSet(SupportOption.Group);

            foreach (var segmentData in Data.SegmentEndDatas)
            {
                if (SegmentItems.TryGetValue(segmentData, out var segmentEndItem))
                {
                    if (!Option.IsSet(SupportOption.Individually))
                        segmentEndItem.isEnabled = false;
                    else
                        segmentEndItem.isEnabled = IsEnableGetter?.Invoke(segmentData) != false;
                }
            }
        }
    }
    public abstract class OptionPanel<TypeNodeItem, TypeSegmentItem, TypeValue> : OptionPanel<TypeNodeItem, TypeSegmentItem>
        where TypeNodeItem : UIComponent, IValueChanger<TypeValue>
        where TypeSegmentItem : UIComponent, IValueChanger<TypeValue>
    {
        public delegate TypeValue Getter(INetworkData data);
        public delegate void Setter(INetworkData data, TypeValue value);

        public event Action<INetworkData, TypeValue> OnChanged;
        private Getter ValueGetter { get; set; }
        private Setter ValueSetter { get; set; }
        public string Format { get; set; }

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, Getter getter, Setter setter, EnableGetter enableGetter)
        {
            ValueGetter = getter;
            ValueSetter = setter;

            Init(data, option, totalOption, enableGetter);
        }
        public override void DeInit()
        {
            base.DeInit();

            ValueGetter = null;
            ValueSetter = null;
            Format = null;
            OnChanged = null;
        }

        protected override TypeNodeItem AddNodeItem(NodeData data)
        {
            var item = ComponentPool.Get<TypeNodeItem>(Content);

            InitNodeItem(data, item);

            item.width = ItemWidth;
            item.Format = Format;
            item.Value = ValueGetter(data);
            item.OnValueChanged += (value) => ValueChanged(data, value);

            return item;
        }

        protected override TypeSegmentItem AddSegmentItem(SegmentEndData data)
        {
            var item = ComponentPool.Get<TypeSegmentItem>(Content);

            InitSegmentItem(data, item);

            item.width = ItemWidth;
            item.Format = Format;
            item.Value = ValueGetter(data);
            item.OnValueChanged += (value) => ValueChanged(data, value);

            return item;
        }
        protected virtual void InitNodeItem(NodeData data, TypeNodeItem item) { }
        protected virtual void InitSegmentItem(SegmentEndData data, TypeSegmentItem item) { }

        private void ValueChanged(INetworkData data, TypeValue value)
        {
            ValueSetter(data, value);
            OnChanged?.Invoke(data, value);
        }
        public override void Refresh()
        {
            base.Refresh();

            if (NodeItem != null)
                NodeItem.Value = ValueGetter(Data);
            foreach (var item in SegmentItems)
                item.Value.Value = ValueGetter(item.Key);
        }
    }
    public class FloatOptionPanel : OptionPanel<FloatUITextField, FloatUITextField, float>
    {
        public delegate void MinMaxGetter(INetworkData data, out float min, out float max);

        private MinMaxGetter MinMax { get; set; }
        public string NumberFormat { get; set; }
        public float WheelStep { get; set; } = 1f;

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, Getter getter, Setter setter, MinMaxGetter minMax, EnableGetter enableGetter)
        {
            MinMax = minMax;
            Init(data, option, totalOption, getter, setter, enableGetter);
        }
        public override void DeInit()
        {
            base.DeInit();
            NumberFormat = null;
            WheelStep = 1f;
        }

        protected override void InitNodeItem(NodeData data, FloatUITextField item) => InitItem(data, item);
        protected override void InitSegmentItem(SegmentEndData data, FloatUITextField item) => InitItem(data, item);
        private void InitItem(INetworkData data, FloatUITextField item)
        {
            item.SetDefaultStyle();
            item.NumberFormat = NumberFormat;
            item.CheckMin = true;
            item.CheckMax = true;
            item.UseWheel = true;
            item.WheelStep = WheelStep;
            item.WheelTip = Settings.ShowToolTip;

            if (MinMax != null)
            {
                MinMax(data, out var min, out var max);
                item.MinValue = min;
                item.MaxValue = max;
            }
        }

        public override void Refresh()
        {
            if (MinMax != null)
            {
                foreach (var item in SegmentItems)
                {
                    MinMax(item.Key, out var min, out var max);
                    item.Value.MinValue = min;
                    item.Value.MaxValue = max;
                }
            }

            base.Refresh();
        }

        protected override void FillContent() { }

        public override void SetStyle(ControlStyle style)
        {
            NodeItem.TextFieldStyle = style.TextField;

            foreach (var item in SegmentItems.Values)
                item.TextFieldStyle = style.TextField;
        }
    }
    public class BoolOptionPanel : OptionPanel<INOSegmented, INOSegmented, bool?>
    {
        protected override void FillContent() { }

        protected override void InitNodeItem(NodeData data, INOSegmented item) => InitItem(data, item);
        protected override void InitSegmentItem(SegmentEndData data, INOSegmented item) => InitItem(data, item);
        private void InitItem(INetworkData data, INOSegmented item)
        {
            item.StopLayout();

            item.AutoButtonSize = false;
            item.ButtonWidth = ItemWidth / 2f - (data is NodeData ? 5f : 0f);
            if (TotalOption == SupportOption.Group)
            {
                item.AddItem(true, new OptionData(CommonLocalize.MessageBox_Yes));
                if (data is NodeData)
                    item.AddItem(null, new OptionData("/"), clickable: null, width: 10f);
                item.AddItem(false, new OptionData(CommonLocalize.MessageBox_No));
            }
            else
            {
                item.AddItem(true, new OptionData("I"));
                if (data is NodeData)
                    item.AddItem(null, new OptionData("/"), clickable: null, width: 10f);
                item.AddItem(false, new OptionData("O"));
            }

            item.StartLayout();
        }
        public override void SetStyle(ControlStyle style)
        {
            NodeItem.SegmentedStyle = style.Segmented;

            foreach (var item in SegmentItems.Values)
                item.SegmentedStyle = style.Segmented;
        }
    }
    public class TextOptionPanel : OptionPanel<CustomUILabel, CustomUILabel>
    {
        private static float Contrast { get; } = 4.5f;
        protected override void FillContent() { }

        protected override CustomUILabel AddNodeItem(NodeData data)
        {
            var item = base.AddNodeItem(data);
            AddItem(item);
            item.text = NodeController.Localize.Options_All;
            item.textColor = Color.black;
            return item;
        }
        protected override CustomUILabel AddSegmentItem(SegmentEndData data)
        {
            var item = base.AddSegmentItem(data);
            AddItem(item);
            item.color = data.Color;
            item.textColor = Color.white.GetContrast(data.Color) >= Contrast ? Color.white : ComponentStyle.DarkPrimaryColor15;
            item.text = $"#{data.Id}";
            return item;
        }

        private void AddItem(CustomUILabel item)
        {
            item.autoSize = false;
            item.width = ItemWidth;
            item.height = 18f;
            item.textScale = 0.65f;
            item.HorizontalAlignment = UIHorizontalAlignment.Center;
            item.VerticalAlignment = UIVerticalAlignment.Middle;
            item.Padding.top = 5;
            item.Atlas = CommonTextures.Atlas;
            item.BackgroundSprite = CommonTextures.PanelBig;
        }
        public override void SetStyle(ControlStyle style)
        {

        }
    }
    public class SpacePanel : BaseEditorPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public SpacePanel() : base()
        {
            SpritePadding.left = 10;
            SpritePadding.right = 10;
            Borders = PropertyBorder.Top;
        }

        public void Init(float height) => base.Init(height);
        public override void SetStyle(ControlStyle style) { }
    }
    public class IOSegmented : UIOnceSegmented<bool> { }
    public class INOSegmented : UIOnceSegmented<bool?> { }
}
