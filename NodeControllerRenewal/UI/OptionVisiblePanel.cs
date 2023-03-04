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
    public class OptionVisiblePanel : UICustomControl
    {
        private int count;
        private Dictionary<OptionVisibleDropDown, Options> DropDowns { get; } = new Dictionary<OptionVisibleDropDown, Options>();

        public OptionVisiblePanel()
        {
            foreach (var option in EnumExtension.GetEnumValues<Options>())
            {
                var panel = component.AttachUIComponent(UITemplateManager.GetAsGameObject("KeyBindingTemplate")) as UIPanel;

                if (count % 2 == 1)
                    panel.backgroundSprite = null;

                count += 1;

                var button = panel.Find<UIButton>("Binding");
                panel.RemoveUIComponent(button);
                Destroy(button);

                var dropDown = panel.AddUIComponent<OptionVisibleDropDown>();
                dropDown.relativePosition = new Vector2(380, 6);
                dropDown.SelectedObject = Settings.GetOptionVisibility(option);
                dropDown.OnSelectObjectChanged += OptionChanged;

                var label = panel.Find<UILabel>("Name");
                label.text = option.Description<Options, Mod>();

                DropDowns.Add(dropDown, option);
            }
        }

        private void OptionChanged(OptionVisibleDropDown changedDropDown, OptionVisibility value)
        {
            Settings.SetOptionVisibility(DropDowns[changedDropDown], value);
        }
    }

    public class OptionVisibleDropDown : UIDropDown<OptionVisibility>
    {
        public new event Action<OptionVisibleDropDown, OptionVisibility> OnSelectObjectChanged;

        public OptionVisibleDropDown()
        {
            this.CustomSettingsStyle(new Vector2(278, 31));

            foreach (var visible in EnumExtension.GetEnumValues<OptionVisibility>())
                AddItem(visible, new OptionData(visible.Description<OptionVisibility, Mod>()));

            SelectedObject = OptionVisibility.Visible;
        }

        protected override void IndexChanged(UIComponent component, int value) => OnSelectObjectChanged?.Invoke(this, SelectedObject);
    }
}
