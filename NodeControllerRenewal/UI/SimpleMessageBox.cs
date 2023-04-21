using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController.UI
{
    public class BackwardCompatibilityMessageBox : OkMessageBox
    {
        public BackwardCompatibilityMessageBox()
        {
            Content.PauseLayout(() =>
            {
                Message.maximumSize = Vector2.zero;

                var warning = Content.AddUIComponent<CustomUILabel>();
                warning.name = "Message";
                warning.Bold = true;
                warning.WordWrap = true;
                warning.AutoSize = AutoSize.Height;
                warning.textColor = ComponentStyle.DarkPrimaryColor100;
                warning.HorizontalAlignment = UIHorizontalAlignment.Center;
                warning.VerticalAlignment = UIVerticalAlignment.Middle;
                warning.text = NodeController.Localize.Mod_BackwardCompatibilityWarning;

                CaptionText = SingletonMod<Mod>.Instance.NameRaw;
                MessageText = NodeController.Localize.Mod_BackwardCompatibilityMessage;
            });
        }
    }
}
