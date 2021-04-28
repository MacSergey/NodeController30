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
        protected NodeData Data { get; set; }
        protected SupportOption Option { get; set; }
        protected float ItemWidth => Option == SupportOption.Group ? 100f : 50f;

        public void Init(NodeData data, SupportOption option)
        {
            Data = data;
            Option = option;

            PlaceItems();
            Content.Refresh();

            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Data = null;

            foreach (var component in Content.components.ToArray())
                ComponentPool.Free(component);
        }
        private void PlaceItems()
        {
            if (Option.IsSet(SupportOption.Group))
                AddItem(Data);

            if (Option.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                    AddItem(segmentData);
            }
        }

        protected virtual TypeItem AddItem(INetworkData data)
        {
            var item = Content.AddUIComponent<TypeItem>();
            return item;
        }

        public abstract void Refresh();
    }
    public abstract class OptionPanel<TypeItem, TypeValue> : OptionPanel<TypeItem>
        where TypeItem : UIComponent, IValueChanger<TypeValue>
    {
        public delegate TypeValue Getter(INetworkData data);
        public delegate void Setter(INetworkData data, TypeValue value);

        private Getter ValueGetter { get; set; }
        private Setter ValueSetter { get; set; }
        private Dictionary<INetworkData, TypeItem> Items { get; } = new Dictionary<INetworkData, TypeItem>();
        public string Format { get; set; }

        public void Init(NodeData data, SupportOption option, Getter getter, Setter setter)
        {
            ValueGetter = getter;
            ValueSetter = setter;

            Init(data, option);
        }
        public override void DeInit()
        {
            base.DeInit();

            ValueGetter = null;
            ValueSetter = null;
            Items.Clear();
            Format = null;
        }

        protected override TypeItem AddItem(INetworkData data)
        {
            var item = ComponentPool.Get<TypeItem>(Content);

            item.width = ItemWidth;
            item.Format = Format;
            item.Value = ValueGetter(data);
            item.OnValueChanged += (value) => ValueChanged(data, value);
            Items[data] = item;

            return item;
        }
        private void ValueChanged(INetworkData data, TypeValue value)
        {
            ValueSetter(data, value);
            Data.UpdateNode();
            Refresh();
        }
        public override void Refresh()
        {
            foreach (var item in Items)
                item.Value.Value = ValueGetter(item.Key);
        }
    }
    public class FloatOptionPanel : OptionPanel<FloatUITextField, float>
    {
        public delegate void MinMaxGetter(INetworkData data, out float min, out float max);

        private MinMaxGetter MinMax { get; set; }
        public string NumberFormat { get; set; }

        public void Init(NodeData data, SupportOption option, Getter getter, Setter setter, MinMaxGetter minMax)
        {
            MinMax = minMax;
            Init(data, option, getter, setter);
        }
        public override void DeInit()
        {
            base.DeInit();
            NumberFormat = null;
        }

        protected override FloatUITextField AddItem(INetworkData data)
        {
            var item = base.AddItem(data);

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

            return item;
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
                item.text = "All";
            if (data is SegmentEndData endData)
            {
                item.color = endData.Color;
                item.text = $"#{endData.Id}";
            }

            return item;
        }
        public override void Refresh() { }
    }
    public class SpacePanel : EditorItem, IReusable
    {
        public override bool SupportEven => true;
    }
}
