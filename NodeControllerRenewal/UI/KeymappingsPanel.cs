using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NodeController.GUI {
    // Copied from Fine Road Anarchy
    public class KeymappingsPanel : UICustomControl {
        // Token: 0x06000024 RID: 36 RVA: 0x00003718 File Offset: 0x00001918
        internal void AddKeymapping(string label, SavedInputKey savedInputKey) {
            UIPanel uipanel = base.component.AttachUIComponent(UITemplateManager.GetAsGameObject(KeymappingsPanel.kKeyBindingTemplate)) as UIPanel;
            int num = this.count;
            this.count = num + 1;
            if (num % 2 == 1) {
                uipanel.backgroundSprite = null;
            }
            UILabel uilabel = uipanel.Find<UILabel>("Name");
            UIButton uibutton = uipanel.Find<UIButton>("Binding");
            uibutton.eventKeyDown += this.OnBindingKeyDown;
            uibutton.eventMouseDown += this.OnBindingMouseDown;
            uilabel.text = label;
            uibutton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uibutton.objectUserData = savedInputKey;
        }

        // Token: 0x06000025 RID: 37 RVA: 0x000037B6 File Offset: 0x000019B6
        private void OnEnable() {
            //LocaleManager.eventLocaleChanged += this.OnLocaleChanged;
        }

        // Token: 0x06000026 RID: 38 RVA: 0x000037C9 File Offset: 0x000019C9
        private void OnDisable() {
            //LocaleManager.eventLocaleChanged -= this.OnLocaleChanged;
        }

        // Token: 0x06000027 RID: 39 RVA: 0x000037DC File Offset: 0x000019DC
        private void OnLocaleChanged() {
            this.RefreshBindableInputs();
        }

        // Token: 0x06000028 RID: 40 RVA: 0x000037E4 File Offset: 0x000019E4
        private bool IsModifierKey(KeyCode code) {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        // Token: 0x06000029 RID: 41 RVA: 0x00003818 File Offset: 0x00001A18
        private bool IsControlDown() {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        // Token: 0x0600002A RID: 42 RVA: 0x00003832 File Offset: 0x00001A32
        private bool IsShiftDown() {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        // Token: 0x0600002B RID: 43 RVA: 0x0000384C File Offset: 0x00001A4C
        private bool IsAltDown() {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        // Token: 0x0600002C RID: 44 RVA: 0x00003866 File Offset: 0x00001A66
        private bool IsUnbindableMouseButton(UIMouseButton code) {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        // Token: 0x0600002D RID: 45 RVA: 0x00003874 File Offset: 0x00001A74
        private KeyCode ButtonToKeycode(UIMouseButton button) {
            if (button == UIMouseButton.Left) {
                return KeyCode.Mouse0;
            }
            if (button == UIMouseButton.Right) {
                return KeyCode.Mouse1;
            }
            if (button == UIMouseButton.Middle) {
                return KeyCode.Mouse2;
            }
            if (button == UIMouseButton.Special0) {
                return KeyCode.Mouse3;
            }
            if (button == UIMouseButton.Special1) {
                return KeyCode.Mouse4;
            }
            if (button == UIMouseButton.Special2) {
                return KeyCode.Mouse5;
            }
            if (button == UIMouseButton.Special3) {
                return KeyCode.Mouse6;
            }
            return KeyCode.None;
        }

        // Token: 0x0600002E RID: 46 RVA: 0x000038CC File Offset: 0x00001ACC
        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p) {
            if (this.m_EditingBinding != null && !this.IsModifierKey(p.keycode)) {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey value = (p.keycode == KeyCode.Escape) ? this.m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace) {
                    value = SavedInputKey.Empty;
                }
                this.m_EditingBinding.value = value;
                (p.source as UITextComponent).text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        // Token: 0x0600002F RID: 47 RVA: 0x0000398C File Offset: 0x00001B8C
        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p) {
            if (this.m_EditingBinding == null) {
                p.Use();
                this.m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                this.m_EditingBindingCategory = p.source.stringUserData;
                UIButton uibutton = p.source as UIButton;
                uibutton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uibutton.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
                return;
            }
            if (!this.IsUnbindableMouseButton(p.buttons)) {
                p.Use();
                UIView.PopModal();
                InputKey value = SavedInputKey.Encode(this.ButtonToKeycode(p.buttons), this.IsControlDown(), this.IsShiftDown(), this.IsAltDown());
                this.m_EditingBinding.value = value;
                UIButton uibutton2 = p.source as UIButton;
                uibutton2.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                uibutton2.buttonsMask = UIMouseButton.Left;
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        // Token: 0x06000030 RID: 48 RVA: 0x00003A8C File Offset: 0x00001C8C
        private void RefreshBindableInputs() {
            foreach (UIComponent uicomponent in base.component.GetComponentsInChildren<UIComponent>()) {
                UITextComponent uitextComponent = uicomponent.Find<UITextComponent>("Binding");
                if (uitextComponent != null) {
                    SavedInputKey savedInputKey = uitextComponent.objectUserData as SavedInputKey;
                    if (savedInputKey != null) {
                        uitextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel uilabel = uicomponent.Find<UILabel>("Name");
                if (uilabel != null) {
                    //uilabel.text = Locale.Get("KEYMAPPING", uilabel.stringUserData);
                    uilabel.text = uilabel.stringUserData;
                }
            }
        }

        // Token: 0x06000031 RID: 49 RVA: 0x00003B20 File Offset: 0x00001D20
        internal InputKey GetDefaultEntry(string entryName) {
            FieldInfo field = typeof(DefaultSettings).GetField(entryName, BindingFlags.Static | BindingFlags.Public);
            if (field == null) {
                return 0;
            }
            object value = field.GetValue(null);
            if (value is InputKey) {
                return (InputKey)value;
            }
            return 0;
        }

        // Token: 0x06000032 RID: 50 RVA: 0x00003B68 File Offset: 0x00001D68
        private void RefreshKeyMapping() {
            UIComponent[] componentsInChildren = base.component.GetComponentsInChildren<UIComponent>();
            for (int i = 0; i < componentsInChildren.Length; i++) {
                UITextComponent uitextComponent = componentsInChildren[i].Find<UITextComponent>("Binding");
                SavedInputKey savedInputKey = (SavedInputKey)uitextComponent.objectUserData;
                if (this.m_EditingBinding != savedInputKey) {
                    uitextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                }
            }
        }

        // Token: 0x04000011 RID: 17
        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";

        // Token: 0x04000012 RID: 18
        private SavedInputKey m_EditingBinding;

        // Token: 0x04000013 RID: 19
        private string m_EditingBindingCategory;

        // Token: 0x04000019 RID: 25
        private int count;
    }
}
