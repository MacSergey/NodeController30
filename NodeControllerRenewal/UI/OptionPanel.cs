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
    public class OptionPanel<TypeItem> : EditorPropertyPanel, IReusable
        where TypeItem : UIComponent
    {
        protected NodeData Data { get; set; }

        protected TypeItem GroupItem { get; set; }
        protected List<TypeItem> Items { get; set; } = new List<TypeItem>();

        public void Init(NodeData data)
        {
            Data = data;

            PlaceItems();

            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Data = null;

            ComponentPool.Free(GroupItem);
            foreach (var item in Items)
                ComponentPool.Free(item);
            Items.Clear();
        }
        protected virtual void PlaceItems()
        {
            GroupItem = AddItem(Data);
            foreach (var segmentData in Data.SegmentEndDatas)
                Items.Add(AddItem(segmentData));
        }
        protected virtual TypeItem AddItem(INetworkData data)
        {
            var item = Content.AddUIComponent<TypeItem>();
            item.width = 50f;
            return item;
        }
    }
    public class OptionPanel<TypeItem, TypeValue> : OptionPanel<TypeItem>
        where TypeItem : UIComponent, IValueChanger<TypeValue>
    {
        public delegate TypeValue Getter(INetworkData data);
        public delegate void Setter(INetworkData data, TypeValue value);

        private SupportOption Option { get; set; }
        private Getter ValueGetter { get; set; }
        private Setter ValueSetter { get; set; }

        public void Init(NodeData data, SupportOption option, Getter getter, Setter setter)
        {
            Option = option;
            ValueGetter = getter;
            ValueSetter = setter;

            Init(data);
        }
        public override void DeInit()
        {
            base.DeInit();

            ValueGetter = null;
            ValueSetter = null;
        }
        protected override void PlaceItems()
        {
            if (Option.IsSet(SupportOption.Group))
            {
                GroupItem = AddItem(Data);
            }

            if (Option.IsSet(SupportOption.Individually))
            {
                foreach (var segmentData in Data.SegmentEndDatas)
                    Items.Add(AddItem(segmentData));
            }
        }

        protected override TypeItem AddItem(INetworkData data)
        {
            var item = base.AddItem(data);

            item.Value = ValueGetter(data);
            item.OnValueChanged += (value) => ValueSetter(data, value);

            return item;
        }
    }
    public class FloatOptionPanel : OptionPanel<FloatUITextField, float>
    {
        protected override FloatUITextField AddItem(INetworkData data)
        {
            var item = base.AddItem(data);
            item.SetDefaultStyle();
            return item;
        }
    }
    public class TextOptionPanel : OptionPanel<CustomUILabel>
    {
        protected override CustomUILabel AddItem(INetworkData data)
        {
            var item = base.AddItem(data);

            item.autoSize = false;
            item.width = 50f;
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
                item.color = Colors.GetOverlayColor(endData.Index, 255);
                item.text = $"#{endData.Id}";
            }

            return item;
        }
    }
    public class SpacePanel : EditorItem, IReusable
    {
        public override bool SupportEven => true;
    }
}
