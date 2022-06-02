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
            Panel.StopLayout();

            Message.maximumSize = Vector2.zero;

            var warning = AddLabel();
            warning.textColor = Color.red;

            CaptionText = SingletonMod<Mod>.Instance.NameRaw;
            MessageText = NodeController.Localize.Mod_BackwardCompatibilityMessage;
            warning.text = NodeController.Localize.Mod_BackwardCompatibilityWarning;

            Panel.StartLayout();
        }
    }
}
