using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController.UI
{
    public interface IOptionPanel
    {
        void Refresh();
    }
    public abstract class OptionPanel<TypeItem> : EditorPropertyPanel, IReusable, IOptionPanel
        where TypeItem : UIComponent
    {
        bool IReusable.InCache { get; set; }
        protected NodeData Data { get; set; }
        protected SupportOption Option { get; set; }
        protected SupportOption TotalOption { get; set; }
        protected Dictionary<INetworkData, TypeItem> Items { get; } = new Dictionary<INetworkData, TypeItem>();
        protected float ItemWidth => Option == SupportOption.Group ? 100f : 50f;

        public void Init(NodeData data, SupportOption option, SupportOption totalOption)
        {
            Data = data;
            Option = option;
            TotalOption = totalOption;

            PlaceItems();

            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Data = null;
            Items.Clear();

            foreach (var component in Content.components.ToArray())
                ComponentPool.Free(component);
        }
        private void PlaceItems()
        {
            if (TotalOption.IsSet(SupportOption.Group))
                Items[Data] = AddItem(Data);

            if (TotalOption.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                    Items[segmentData] = AddItem(segmentData);
            }

            Content.Refresh();
        }

        protected virtual TypeItem AddItem(INetworkData data)
        {
            var item = Content.AddUIComponent<TypeItem>();
            return item;
        }

        public virtual void Refresh()
        {
            if (TotalOption.IsSet(SupportOption.Group) && !Option.IsSet(SupportOption.Group))
                Items[Data].isVisible = false;

            if (TotalOption.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                {
                    if (!Option.IsSet(SupportOption.Individually))
                        Items[segmentData].isVisible = false;
                    else if (Option.IsSet(SupportOption.MainRoad))
                        Items[segmentData].isEnabled = segmentData.IsMainRoad;
                }
            }
        }
    }
    public abstract class OptionPanel<TypeItem, TypeValue> : OptionPanel<TypeItem>
        where TypeItem : UIComponent, IValueChanger<TypeValue>
    {
        public delegate TypeValue Getter(INetworkData data);
        public delegate void Setter(INetworkData data, TypeValue value);

        private Getter ValueGetter { get; set; }
        private Setter ValueSetter { get; set; }
        public string Format { get; set; }

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, Getter getter, Setter setter)
        {
            ValueGetter = getter;
            ValueSetter = setter;

            Init(data, option, totalOption);
        }
        public override void DeInit()
        {
            base.DeInit();

            ValueGetter = null;
            ValueSetter = null;
            Format = null;
        }

        protected override TypeItem AddItem(INetworkData data)
        {
            var item = ComponentPool.Get<TypeItem>(Content);

            InitItem(data, item);

            item.width = ItemWidth;
            item.Format = Format;
            item.Value = ValueGetter(data);
            item.OnValueChanged += (value) => ValueChanged(data, value);

            return item;
        }
        protected virtual void InitItem(INetworkData data, TypeItem item) { }
        private void ValueChanged(INetworkData data, TypeValue value)
        {
            ValueSetter(data, value);
            Data.UpdateNode();
            Refresh();
        }
        public override void Refresh()
        {
            base.Refresh();

            foreach (var item in Items)
                item.Value.Value = ValueGetter(item.Key);
        }
    }
    public class FloatOptionPanel : OptionPanel<FloatUITextField, float>
    {
        public delegate void MinMaxGetter(INetworkData data, out float min, out float max);

        private MinMaxGetter MinMax { get; set; }
        public string NumberFormat { get; set; }

        public void Init(NodeData data, SupportOption option, SupportOption totalOption, Getter getter, Setter setter, MinMaxGetter minMax)
        {
            MinMax = minMax;
            Init(data, option, totalOption, getter, setter);
        }
        public override void DeInit()
        {
            base.DeInit();
            NumberFormat = null;
        }

        protected override void InitItem(INetworkData data, FloatUITextField item)
        {
            item.SetDefaultStyle();
            item.NumberFormat = NumberFormat;
            item.CheckMin = true;
            item.CheckMax = true;
            item.UseWheel = true;
            item.WheelStep = 1f;
            item.WheelTip = Settings.ShowToolTip ? NodeController.Localize.FieldPanel_ScrollWheel : string.Empty;

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
                foreach (var item in Items)
                {
                    MinMax(item.Key, out var min, out var max);
                    item.Value.MinValue = min;
                    item.Value.MaxValue = max;
                }
            }

            base.Refresh();
        }
    }
    public class BoolOptionPanel : OptionPanel<IOSegmented, bool>
    {
        protected override void InitItem(INetworkData data, IOSegmented item)
        {
            item.StopLayout();

            item.AutoButtonSize = false;
            item.ButtonWidth = ItemWidth / 2f;
            item.AddItem(true, "I");
            item.AddItem(false, "O");

            item.StartLayout();
        }
    }
    public class TextOptionPanel : OptionPanel<CustomUILabel>
    {
        protected override CustomUILabel AddItem(INetworkData data)
        {
            var item = base.AddItem(data);

            item.autoSize = false;
            item.width = ItemWidth;
            item.height = 18f;
            item.textScale = 0.65f;
            item.textAlignment = UIHorizontalAlignment.Center;
            item.verticalAlignment = UIVerticalAlignment.Middle;
            item.padding.top = 3;
            item.atlas = TextureHelper.InGameAtlas;
            item.backgroundSprite = "ButtonWhite";

            if (data is NodeData)
                item.text = NodeController.Localize.Options_All;
            if (data is SegmentEndData endData)
            {
                item.color = endData.Color;
                item.text = $"#{endData.Id}";
            }

            return item;
        }
    }
    public class SpacePanel : EditorItem, IReusable
    {
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;
    }
    public class IOSegmented : UIOnceSegmented<bool> { }
}
