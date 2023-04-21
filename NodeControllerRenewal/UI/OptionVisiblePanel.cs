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
}
