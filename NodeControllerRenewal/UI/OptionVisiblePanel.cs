using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NodeController.Utilities;

namespace NodeController.UI
{
    public class OptionVisibilitySettingsItem : ControlSettingsItem<OptionVisibleDropDown>
    {
        public event Action<OptionVisibility> OnValueChanged;

        Options option;
        public Options Option
        {
            get => option;
            set
            {
                if (value != option)
                {
                    option = value;
                    Control.SelectedObject = Value;
                }
            }
        }

        public OptionVisibility Value
        {
            get => Settings.GetOptionVisibility(Option);
            set => Settings.SetOptionVisibility(Option, value);
        }

        public OptionVisibilitySettingsItem()
        {
            Control.OnValueChanged += ModifierChanged;
        }

        private void ModifierChanged(OptionVisibleDropDown changedVisibility, OptionVisibility value)
        {
            Value = value;
            OnValueChanged?.Invoke(value);
        }
    }

    public class OptionVisibleDropDown : SimpleDropDown<OptionVisibility, OptionVisibleDropDown.OptionVisibleEntity, OptionVisibleDropDown.OptionVisiblePopup>
    {
        public event Action<OptionVisibleDropDown, OptionVisibility> OnValueChanged;

        public OptionVisibleDropDown()
        {
            ComponentStyle.DropDownSettingsStyle(this, new Vector2(278, 31));
            EntityTextScale = 1f;

            foreach (var value in EnumExtension.GetEnumValues<OptionVisibility>())
                AddItem(value, value.Description());

            SelectedObject = OptionVisibility.Visible;
        }

        protected override void SelectObject(DropDownItem<OptionVisibility> item) => OnValueChanged?.Invoke(this, item.value);
        protected override void SetPopupStyle() => Popup.PopupSettingsStyle<DropDownItem<OptionVisibility>, OptionVisibleEntity, OptionVisiblePopup>(height);

        public class OptionVisibleEntity : SimpleEntity<OptionVisibility> { }
        public class OptionVisiblePopup : SimplePopup<OptionVisibility, OptionVisibleEntity>
        {
            protected override void SetEntityStyle(OptionVisibleEntity entity) => entity.EntitySettingsStyle<DropDownItem<OptionVisibility>, OptionVisibleEntity>();
        }
    }

    //public class OptionVisiblePanel : UICustomControl
    //{
    //    private int count;
    //    private Dictionary<OptionVisibleDropDown, Options> DropDowns { get; } = new Dictionary<OptionVisibleDropDown, Options>();

    //    public OptionVisiblePanel()
    //    {
    //        foreach (var option in EnumExtension.GetEnumValues<Options>())
    //        {
    //            var panel = component.AttachUIComponent(UITemplateManager.GetAsGameObject("KeyBindingTemplate")) as UIPanel;

    //            if (count % 2 == 1)
    //                panel.backgroundSprite = null;

    //            count += 1;

    //            var button = panel.Find<UIButton>("Binding");
    //            panel.RemoveUIComponent(button);
    //            Destroy(button);

    //            var dropDown = panel.AddUIComponent<OptionVisibleDropDown>();
    //            dropDown.relativePosition = new Vector2(380, 6);
    //            dropDown.SelectedObject = Settings.GetOptionVisibility(option);
    //            dropDown.OnSelectObjectChanged += OptionChanged;

    //            var label = panel.Find<UILabel>("Name");
    //            label.text = option.Description<Options, Mod>();

    //            DropDowns.Add(dropDown, option);
    //        }
    //    }

    //    private void OptionChanged(OptionVisibleDropDown changedDropDown, OptionVisibility value)
    //    {
    //        Settings.SetOptionVisibility(DropDowns[changedDropDown], value);
    //    }
    //}

    //public class OptionVisibleDropDown : UIDropDown<OptionVisibility>
    //{
    //    public new event Action<OptionVisibleDropDown, OptionVisibility> OnSelectObjectChanged;

    //    public OptionVisibleDropDown()
    //    {
    //        this.CustomSettingsStyle(new Vector2(278, 31));

    //        foreach (var visible in EnumExtension.GetEnumValues<OptionVisibility>())
    //            AddItem(visible, new OptionData(visible.Description<OptionVisibility, Mod>()));

    //        SelectedObject = OptionVisibility.Visible;
    //    }

    //    protected override void IndexChanged(UIComponent component, int value) => OnSelectObjectChanged?.Invoke(this, SelectedObject);
    //}
}
